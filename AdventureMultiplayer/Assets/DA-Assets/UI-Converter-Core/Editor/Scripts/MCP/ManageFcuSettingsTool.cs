using DA_Assets.UCC.Model;
using DA_Assets.Shared.MCP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

#if JSONNET_EXISTS
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace DA_Assets.UCC.MCP
{
    public class ManageFcuSettingsTool : FcuMcpToolBase
    {
        private const string RootPath = "Settings";

        public ManageFcuSettingsTool(ConverterBase monoBeh, McpToolSO toolSO) : base(monoBeh, toolSO)
        {
        }

        protected override Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args)
        {
            string action = GetString(args, "action", "get").Trim().ToLowerInvariant();

            SettingsToolResponse response = action switch
            {
                "describe" => Describe(monoBeh, args),
                "get" => Get(monoBeh, args),
                "set" => Set(monoBeh, args),
                _ => SettingsToolResponse.Fail($"Unknown action '{action}'. Use describe, get, or set.")
            };

            return Task.FromResult<IReadOnlyList<ContentItem>>(new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = ToJson(response)
                }
            });
        }

        private SettingsToolResponse Describe(ConverterBase fcu, Dictionary<string, object> args)
        {
            string section = NormalizeSection(GetString(args, "section", string.Empty));
            bool includeSerializedPaths = GetBool(args, "include_serialized_paths");

            var entries = new List<SettingEntry>();
            foreach (SettingMember member in EnumerateMembers(fcu.Settings, RootPath, 0))
            {
                if (!MatchesSection(member.Path, section))
                    continue;

                entries.Add(new SettingEntry
                {
                    path = member.Path,
                    type = GetFriendlyTypeName(member.ValueType),
                    access = member.CanWrite ? "read_write" : "read_only",
                    source = member.Source,
                    enum_values = GetEnumValues(member.ValueType)
                });
            }

            if (includeSerializedPaths)
            {
                foreach (SerializedProperty prop in EnumerateSerializedLeaves(fcu))
                {
                    if (!MatchesSection(prop.propertyPath, section))
                        continue;

                    entries.Add(new SettingEntry
                    {
                        path = prop.propertyPath,
                        type = prop.propertyType.ToString(),
                        access = IsWritableSerializedProperty(prop) ? "read_write" : "read_only",
                        source = "serialized_property",
                        enum_values = prop.propertyType == SerializedPropertyType.Enum ? prop.enumDisplayNames : Array.Empty<string>()
                    });
                }
            }

            return SettingsToolResponse.Ok(entries);
        }

        private SettingsToolResponse Get(ConverterBase fcu, Dictionary<string, object> args)
        {
            string section = NormalizeSection(GetString(args, "section", string.Empty));
            List<string> paths = GetStringList(args, "paths");
            var values = new List<SettingValue>();

            if (paths.Count == 0)
            {
                foreach (SettingMember member in EnumerateMembers(fcu.Settings, RootPath, 0))
                {
                    if (!MatchesSection(member.Path, section))
                        continue;

                    values.Add(CreateValue(member.Path, member.ValueType, member.GetValue(), member.Source));
                }

                return SettingsToolResponse.Ok(values);
            }

            foreach (string requestedPath in paths)
            {
                string path = NormalizePath(requestedPath);
                if (TryResolveMember(fcu.Settings, path, out SettingMember member, out string error))
                {
                    values.Add(CreateValue(member.Path, member.ValueType, member.GetValue(), member.Source));
                    continue;
                }

                if (TryGetSerializedValue(fcu, path, out SettingValue serializedValue, out string serializedError))
                {
                    values.Add(serializedValue);
                    continue;
                }

                values.Add(new SettingValue { path = path, status = "failed", error = string.IsNullOrEmpty(error) ? serializedError : error });
            }

            return SettingsToolResponse.Ok(values);
        }

        private SettingsToolResponse Set(ConverterBase fcu, Dictionary<string, object> args)
        {
            List<SettingPatch> patches = GetPatches(args);
            if (patches.Count == 0)
                return SettingsToolResponse.Fail("No patches supplied.");

            Undo.RecordObject(fcu, "Manage FCU Settings");

            var results = new List<SettingPatchResult>();
            foreach (SettingPatch patch in patches)
            {
                string path = NormalizePath(patch.path);
                if (TryApplyMemberPatch(fcu, path, patch.value, out object appliedValue, out string error))
                {
                    results.Add(new SettingPatchResult { path = path, status = "applied", value = NormalizeValue(appliedValue) });
                    continue;
                }

                if (TryApplySerializedPatch(fcu, path, patch.value, out appliedValue, out string serializedError))
                {
                    results.Add(new SettingPatchResult { path = path, status = "applied", value = NormalizeValue(appliedValue) });
                    continue;
                }

                results.Add(new SettingPatchResult { path = path, status = "failed", error = string.IsNullOrEmpty(error) ? serializedError : error });
            }

            EditorUtility.SetDirty(fcu);
            RefreshOpenSettingsWindows(fcu);
            return SettingsToolResponse.Ok(results);
        }

        private static void RefreshOpenSettingsWindows(ConverterBase fcu)
        {
            if (fcu == null)
                return;

            foreach (FcuSettingsWindow window in Resources.FindObjectsOfTypeAll<FcuSettingsWindow>())
            {
                if (window.MonoBeh == fcu)
                    window.RefreshTabs();
            }
        }

        private bool TryApplyMemberPatch(ConverterBase fcu, string path, object rawValue, out object appliedValue, out string error)
        {
            appliedValue = null;

            if (!TryResolveMember(fcu.Settings, path, out SettingMember member, out error))
                return false;

            if (!member.CanWrite)
            {
                error = "Setting is read-only.";
                return false;
            }

            try
            {
                object convertedValue = ConvertValue(rawValue, member.ValueType);
                convertedValue = ApplyValidatedSetting(fcu, member.Path, convertedValue);
                member.SetValue(convertedValue);
                appliedValue = member.GetValue();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static object ApplyValidatedSetting(ConverterBase fcu, string path, object value)
        {
            if (path == "Settings.MainSettings.UIFramework" && value is UIFramework framework)
            {
#if (FCU_EXISTS && FCU_UITK_EXT_EXISTS) == false
                if (framework == UIFramework.UITK)
                    framework = UIFramework.UGUI;
#endif
#if NOVA_UI_EXISTS == false
                if (framework == UIFramework.NOVA)
                    framework = UIFramework.UGUI;
#endif
                return framework;
            }

            if (path == "Settings.ImageSpritesSettings.ImageComponent" && value is ImageComponent imageComponent)
            {
#if SUBC_SHAPES_EXISTS == false
                if (imageComponent == ImageComponent.SubcShape)
                    imageComponent = ImageComponent.UnityImage;
#endif
#if MPUIKIT_EXISTS == false
                if (imageComponent == ImageComponent.MPImage)
                    imageComponent = ImageComponent.UnityImage;
#endif
#if JOSH_PUI_EXISTS == false
                if (imageComponent == ImageComponent.ProceduralImage)
                    imageComponent = ImageComponent.UnityImage;
#endif
#if PROCEDURAL_UI_ASSET_STORE_RELEASE == false
                if (imageComponent == ImageComponent.RoundedImage)
                    imageComponent = ImageComponent.UnityImage;
#endif
#if VECTOR_GRAPHICS_EXISTS == false
                if (imageComponent == ImageComponent.SvgImage)
                    imageComponent = ImageComponent.UnityImage;
#endif
#if FLEXIBLE_IMAGE_EXISTS == false
                if (imageComponent == ImageComponent.FlexibleImage)
                    imageComponent = ImageComponent.UnityImage;
#endif
                if (fcu.Settings.MainSettings.UIFramework == UIFramework.UITK && imageComponent != ImageComponent.UI_Toolkit_Image)
                    imageComponent = ImageComponent.UI_Toolkit_Image;

                return imageComponent;
            }

            if (path == "Settings.ImageSpritesSettings.ImageFormat" && value is ImageFormat imageFormat)
            {
#if VECTOR_GRAPHICS_EXISTS == false
                if (imageFormat == ImageFormat.SVG)
                    imageFormat = ImageFormat.PNG;
#endif
                return imageFormat;
            }

            if (path == "Settings.TextFontsSettings.TextComponent" && value is TextComponent textComponent)
            {
#if TextMeshPro == false
                if (textComponent == TextComponent.TextMeshPro)
                    textComponent = TextComponent.UnityEngine_UI_Text;
#endif
#if RTLTMP_EXISTS == false
                if (textComponent == TextComponent.RTL_TextMeshPro)
                    textComponent = TextComponent.UnityEngine_UI_Text;
#endif
#if UNITEXT == false
                if (textComponent == TextComponent.UniText)
                    textComponent = TextComponent.UnityEngine_UI_Text;
#endif
                if (fcu.Settings.MainSettings.UIFramework == UIFramework.UITK && textComponent != TextComponent.UI_Toolkit_Text)
                    textComponent = TextComponent.UI_Toolkit_Text;

                return textComponent;
            }

            return value;
        }

        private static IEnumerable<SettingMember> EnumerateMembers(object root, string path, int depth)
        {
            if (root == null || depth > 8)
                yield break;

            Type type = root.GetType();
            foreach (MemberAccessor member in GetSettingMembers(type))
            {
                object value = member.GetValue(root);
                string childPath = $"{path}.{member.Name}";

                if (IsLeafType(member.ValueType) || value == null)
                {
                    yield return new SettingMember(childPath, member.ValueType, member.CanWrite, member.Source, () => member.GetValue(root), v => member.SetValue(root, v));
                    continue;
                }

                foreach (SettingMember child in EnumerateMembers(value, childPath, depth + 1))
                    yield return child;
            }
        }

        private static IEnumerable<MemberAccessor> GetSettingMembers(Type type)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (PropertyInfo prop in type.GetProperties(Flags).OrderBy(x => x.MetadataToken))
            {
                if (prop.GetIndexParameters().Length > 0 || prop.GetMethod == null || prop.GetMethod.IsStatic)
                    continue;

                if (!prop.GetMethod.IsPublic)
                    continue;

                yield return new MemberAccessor(prop.Name, prop.PropertyType, prop.SetMethod != null && prop.SetMethod.IsPublic, "property", obj => prop.GetValue(obj), (obj, value) => prop.SetValue(obj, value));
            }

            HashSet<string> publicPropertyNames = new HashSet<string>(
                type.GetProperties(Flags).Where(x => x.GetMethod != null && x.GetMethod.IsPublic).Select(x => x.Name),
                StringComparer.OrdinalIgnoreCase);

            foreach (FieldInfo field in type.GetFields(Flags).OrderBy(x => x.MetadataToken))
            {
                if (field.IsStatic || field.IsInitOnly || field.Name == "monoBeh")
                    continue;

                if (!field.IsPublic || Attribute.IsDefined(field, typeof(NonSerializedAttribute)))
                    continue;

                if (publicPropertyNames.Contains(field.Name))
                    continue;

                yield return new MemberAccessor(field.Name, field.FieldType, true, "field", obj => field.GetValue(obj), (obj, value) => field.SetValue(obj, value));
            }
        }

        private static bool TryResolveMember(object root, string path, out SettingMember member, out string error)
        {
            member = null;
            error = null;

            string[] parts = NormalizePath(path).Split('.');
            if (parts.Length < 2 || parts[0] != RootPath)
            {
                error = $"Path must start with '{RootPath}'.";
                return false;
            }

            object current = root;
            string currentPath = RootPath;

            for (int i = 1; i < parts.Length; i++)
            {
                if (current == null)
                {
                    error = $"Null object at '{currentPath}'.";
                    return false;
                }

                MemberAccessor accessor = GetSettingMembers(current.GetType()).FirstOrDefault(x => x.Name == parts[i]);
                if (accessor == null)
                {
                    error = $"Setting path segment '{parts[i]}' was not found at '{currentPath}'.";
                    return false;
                }

                object owner = current;
                currentPath = $"{currentPath}.{accessor.Name}";

                if (i == parts.Length - 1)
                {
                    member = new SettingMember(currentPath, accessor.ValueType, accessor.CanWrite, accessor.Source, () => accessor.GetValue(owner), v => accessor.SetValue(owner, v));
                    return true;
                }

                current = accessor.GetValue(current);
            }

            error = "Invalid path.";
            return false;
        }

        private static IEnumerable<SerializedProperty> EnumerateSerializedLeaves(ConverterBase fcu)
        {
            SerializedObject so = new SerializedObject(fcu);
            SerializedProperty iterator = so.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;

                if (!iterator.propertyPath.StartsWith(RootPath + ".", StringComparison.Ordinal))
                    continue;

                if (iterator.propertyPath.Contains(".monoBeh"))
                    continue;

                if (iterator.propertyType == SerializedPropertyType.Generic && iterator.hasVisibleChildren)
                    continue;

                yield return iterator.Copy();
                enterChildren = false;
            }
        }

        private static bool TryGetSerializedValue(ConverterBase fcu, string path, out SettingValue value, out string error)
        {
            value = null;
            SerializedObject so = new SerializedObject(fcu);
            SerializedProperty prop = so.FindProperty(path);

            if (prop == null)
            {
                error = "Serialized property was not found.";
                return false;
            }

            value = new SettingValue
            {
                path = prop.propertyPath,
                type = prop.propertyType.ToString(),
                source = "serialized_property",
                status = "ok",
                value = GetSerializedValue(prop)
            };
            error = null;
            return true;
        }

        private static bool TryApplySerializedPatch(ConverterBase fcu, string path, object rawValue, out object appliedValue, out string error)
        {
            appliedValue = null;
            SerializedObject so = new SerializedObject(fcu);
            SerializedProperty prop = so.FindProperty(path);

            if (prop == null)
            {
                error = "Serialized property was not found.";
                return false;
            }

            if (!IsWritableSerializedProperty(prop))
            {
                error = $"Serialized property type '{prop.propertyType}' is not writable by this tool.";
                return false;
            }

            try
            {
                SetSerializedValue(prop, rawValue);
                so.ApplyModifiedProperties();
                appliedValue = GetSerializedValue(prop);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool IsWritableSerializedProperty(SerializedProperty prop)
        {
            return prop.propertyType is SerializedPropertyType.Integer
                or SerializedPropertyType.Boolean
                or SerializedPropertyType.Float
                or SerializedPropertyType.String
                or SerializedPropertyType.Color
                or SerializedPropertyType.ObjectReference
                or SerializedPropertyType.Enum
                or SerializedPropertyType.Vector2
                or SerializedPropertyType.Vector3
                or SerializedPropertyType.Vector4
                or SerializedPropertyType.Rect
                or SerializedPropertyType.Vector2Int
                or SerializedPropertyType.Vector3Int
                or SerializedPropertyType.AnimationCurve;
        }

        private static void SetSerializedValue(SerializedProperty prop, object value)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.longValue = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.Float:
                    prop.doubleValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.Color:
                    prop.colorValue = ToColor(value);
                    break;
                case SerializedPropertyType.ObjectReference:
                    prop.objectReferenceValue = ToUnityObject(value, GetManagedFieldType(prop));
                    break;
                case SerializedPropertyType.Enum:
                    prop.enumValueIndex = ToEnumIndex(prop, value);
                    break;
                case SerializedPropertyType.Vector2:
                    prop.vector2Value = ToVector2(value);
                    break;
                case SerializedPropertyType.Vector3:
                    prop.vector3Value = ToVector3(value);
                    break;
                case SerializedPropertyType.Vector4:
                    prop.vector4Value = ToVector4(value);
                    break;
                case SerializedPropertyType.Rect:
                    Vector4 r = ToVector4(value);
                    prop.rectValue = new Rect(r.x, r.y, r.z, r.w);
                    break;
                case SerializedPropertyType.Vector2Int:
                    prop.vector2IntValue = ToVector2Int(value);
                    break;
                case SerializedPropertyType.Vector3Int:
                    prop.vector3IntValue = ToVector3Int(value);
                    break;
                case SerializedPropertyType.AnimationCurve:
                    prop.animationCurveValue = ToAnimationCurve(value);
                    break;
                default:
                    throw new NotSupportedException($"Serialized property type '{prop.propertyType}' is not supported.");
            }
        }

        private static object GetSerializedValue(SerializedProperty prop)
        {
            return prop.propertyType switch
            {
                SerializedPropertyType.Integer => prop.longValue,
                SerializedPropertyType.Boolean => prop.boolValue,
                SerializedPropertyType.Float => prop.doubleValue,
                SerializedPropertyType.String => prop.stringValue,
                SerializedPropertyType.Color => NormalizeValue(prop.colorValue),
                SerializedPropertyType.ObjectReference => NormalizeUnityObject(prop.objectReferenceValue),
                SerializedPropertyType.Enum => prop.enumDisplayNames != null && prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumDisplayNames.Length ? prop.enumDisplayNames[prop.enumValueIndex] : prop.enumValueIndex,
                SerializedPropertyType.Vector2 => NormalizeValue(prop.vector2Value),
                SerializedPropertyType.Vector3 => NormalizeValue(prop.vector3Value),
                SerializedPropertyType.Vector4 => NormalizeValue(prop.vector4Value),
                SerializedPropertyType.Rect => NormalizeValue(prop.rectValue),
                SerializedPropertyType.Vector2Int => NormalizeValue(prop.vector2IntValue),
                SerializedPropertyType.Vector3Int => NormalizeValue(prop.vector3IntValue),
                SerializedPropertyType.AnimationCurve => NormalizeValue(prop.animationCurveValue),
                _ => prop.ToString()
            };
        }

        private static Type GetManagedFieldType(SerializedProperty prop)
        {
            return typeof(UnityEngine.Object);
        }

        private static SettingValue CreateValue(string path, Type type, object rawValue, string source)
        {
            return new SettingValue
            {
                path = path,
                type = GetFriendlyTypeName(type),
                source = source,
                status = "ok",
                value = NormalizeValue(rawValue)
            };
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (targetType == typeof(string))
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(bool))
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(int))
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(long))
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(float))
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(double))
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (targetType.IsEnum)
                return ToEnum(value, targetType);
            if (targetType == typeof(Vector2))
                return ToVector2(value);
            if (targetType == typeof(Vector2Int))
                return ToVector2Int(value);
            if (targetType == typeof(Vector3))
                return ToVector3(value);
            if (targetType == typeof(Vector3Int))
                return ToVector3Int(value);
            if (targetType == typeof(Vector4))
                return ToVector4(value);
            if (targetType == typeof(Color))
                return ToColor(value);
            if (targetType == typeof(AnimationCurve))
                return ToAnimationCurve(value);
            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                return ToUnityObject(value, targetType);

            throw new NotSupportedException($"Type '{targetType.Name}' is not supported for direct setting. Use a leaf serialized path.");
        }

        private static object ToEnum(object value, Type enumType)
        {
            if (IsJsonArrayLike(value) && value is IEnumerable enumerable)
            {
                long combined = 0;
                foreach (object item in enumerable)
                    combined |= Convert.ToInt64(Enum.Parse(enumType, Convert.ToString(item, CultureInfo.InvariantCulture), true), CultureInfo.InvariantCulture);
                return Enum.ToObject(enumType, combined);
            }

            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long numeric))
                return Enum.ToObject(enumType, numeric);

            text = text.Replace("|", ",");
            return Enum.Parse(enumType, text, true);
        }

        private static bool IsJsonArrayLike(object value)
        {
            if (value is string || value is IDictionary)
                return false;

#if JSONNET_EXISTS
            if (value is JValue || value is JObject)
                return false;
#endif

            return value is IEnumerable;
        }

        private static int ToEnumIndex(SerializedProperty prop, object value)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            for (int i = 0; i < prop.enumDisplayNames.Length; i++)
            {
                if (string.Equals(prop.enumDisplayNames[i], text, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(prop.enumNames[i], text, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static UnityEngine.Object ToUnityObject(object value, Type targetType)
        {
            if (value == null)
                return null;

            string path = null;
            string guid = null;

            if (TryGetMap(value, out Dictionary<string, object> map))
            {
                path = map.TryGetValue("path", out object pathValue) ? Convert.ToString(pathValue, CultureInfo.InvariantCulture) : null;
                guid = map.TryGetValue("guid", out object guidValue) ? Convert.ToString(guidValue, CultureInfo.InvariantCulture) : null;
            }
            else
            {
                path = Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrEmpty(guid))
                path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                return null;

            return AssetDatabase.LoadAssetAtPath(path, targetType);
        }

        private static Vector2 ToVector2(object value)
        {
            Dictionary<string, object> map = RequireMap(value);
            return new Vector2(ToFloat(map, "x"), ToFloat(map, "y"));
        }

        private static Vector2Int ToVector2Int(object value)
        {
            Dictionary<string, object> map = RequireMap(value);
            return new Vector2Int(ToInt(map, "x"), ToInt(map, "y"));
        }

        private static Vector3 ToVector3(object value)
        {
            Dictionary<string, object> map = RequireMap(value);
            return new Vector3(ToFloat(map, "x"), ToFloat(map, "y"), ToFloat(map, "z"));
        }

        private static Vector3Int ToVector3Int(object value)
        {
            Dictionary<string, object> map = RequireMap(value);
            return new Vector3Int(ToInt(map, "x"), ToInt(map, "y"), ToInt(map, "z"));
        }

        private static Vector4 ToVector4(object value)
        {
            Dictionary<string, object> map = RequireMap(value);
            return new Vector4(ToFloat(map, "x"), ToFloat(map, "y"), ToFloat(map, "z"), ToFloat(map, "w"));
        }

        private static Color ToColor(object value)
        {
            Dictionary<string, object> map = RequireMap(value);
            return new Color(ToFloat(map, "r"), ToFloat(map, "g"), ToFloat(map, "b"), map.TryGetValue("a", out _) ? ToFloat(map, "a") : 1f);
        }

        private static AnimationCurve ToAnimationCurve(object value)
        {
            Dictionary<string, object> map = RequireMap(value);
            var keys = new List<Keyframe>();

            if (map.TryGetValue("keys", out object keysValue) && keysValue is IEnumerable enumerable && keysValue is not string)
            {
                foreach (object item in enumerable)
                    keys.Add(ToKeyframe(item));
            }

            var curve = new AnimationCurve(keys.ToArray());

            if (map.TryGetValue("preWrapMode", out object preWrapMode))
                curve.preWrapMode = (WrapMode)ToEnum(preWrapMode, typeof(WrapMode));

            if (map.TryGetValue("postWrapMode", out object postWrapMode))
                curve.postWrapMode = (WrapMode)ToEnum(postWrapMode, typeof(WrapMode));

            return curve;
        }

        private static Keyframe ToKeyframe(object value)
        {
            Dictionary<string, object> map = RequireMap(value);
            var keyframe = new Keyframe(
                ToFloat(map, "time"),
                ToFloat(map, "value"),
                map.TryGetValue("inTangent", out _) ? ToFloat(map, "inTangent") : 0f,
                map.TryGetValue("outTangent", out _) ? ToFloat(map, "outTangent") : 0f);

            if (map.TryGetValue("inWeight", out _))
                keyframe.inWeight = ToFloat(map, "inWeight");

            if (map.TryGetValue("outWeight", out _))
                keyframe.outWeight = ToFloat(map, "outWeight");

            if (map.TryGetValue("weightedMode", out object weightedMode))
                keyframe.weightedMode = (WeightedMode)ToEnum(weightedMode, typeof(WeightedMode));

            return keyframe;
        }

        private static Dictionary<string, object> RequireMap(object value)
        {
            if (TryGetMap(value, out Dictionary<string, object> map))
                return map;

            throw new ArgumentException("Value must be an object with numeric fields.");
        }

        private static bool TryGetMap(object value, out Dictionary<string, object> map)
        {
            map = null;
            if (value is Dictionary<string, object> dict)
            {
                map = dict;
                return true;
            }

            if (value is IDictionary<string, object> typedDict)
            {
                map = typedDict.ToDictionary(x => x.Key, x => x.Value);
                return true;
            }

#if JSONNET_EXISTS
            if (value is JObject jo)
            {
                map = jo.Properties().ToDictionary(x => x.Name, x => (object)x.Value);
                return true;
            }
#endif
            return false;
        }

        private static float ToFloat(Dictionary<string, object> map, string key)
        {
            return Convert.ToSingle(map[key], CultureInfo.InvariantCulture);
        }

        private static int ToInt(Dictionary<string, object> map, string key)
        {
            return Convert.ToInt32(map[key], CultureInfo.InvariantCulture);
        }

        private static object NormalizeValue(object value)
        {
            return value switch
            {
                null => null,
                Enum e => e.ToString(),
                Vector2 v => new { x = v.x, y = v.y },
                Vector2Int v => new { x = v.x, y = v.y },
                Vector3 v => new { x = v.x, y = v.y, z = v.z },
                Vector3Int v => new { x = v.x, y = v.y, z = v.z },
                Vector4 v => new { x = v.x, y = v.y, z = v.z, w = v.w },
                Rect r => new { x = r.x, y = r.y, width = r.width, height = r.height },
                Color c => new { r = c.r, g = c.g, b = c.b, a = c.a },
                AnimationCurve curve => NormalizeAnimationCurve(curve),
                UnityEngine.Object obj => NormalizeUnityObject(obj),
                _ => value
            };
        }

        private static object NormalizeUnityObject(UnityEngine.Object obj)
        {
            if (obj == null)
                return null;

            string path = AssetDatabase.GetAssetPath(obj);
            return new
            {
                name = obj.name,
                type = obj.GetType().Name,
                path,
                guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path)
            };
        }

        private static object NormalizeAnimationCurve(AnimationCurve curve)
        {
            if (curve == null)
                return null;

            return new
            {
                preWrapMode = curve.preWrapMode.ToString(),
                postWrapMode = curve.postWrapMode.ToString(),
                keys = curve.keys.Select(NormalizeKeyframe).ToArray()
            };
        }

        private static object NormalizeKeyframe(Keyframe key)
        {
            return new
            {
                time = key.time,
                value = key.value,
                inTangent = key.inTangent,
                outTangent = key.outTangent,
                inWeight = key.inWeight,
                outWeight = key.outWeight,
                weightedMode = key.weightedMode.ToString()
            };
        }

        private static string[] GetEnumValues(Type type)
        {
            return type.IsEnum ? Enum.GetNames(type) : Array.Empty<string>();
        }

        private static string GetFriendlyTypeName(Type type)
        {
            if (type.IsEnum)
                return $"enum:{type.Name}";
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return $"object:{type.Name}";
            return type.Name;
        }

        private static bool IsLeafType(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(Vector2)
                || type == typeof(Vector2Int)
                || type == typeof(Vector3)
                || type == typeof(Vector3Int)
                || type == typeof(Vector4)
                || type == typeof(Rect)
                || type == typeof(Color)
                || type == typeof(AnimationCurve)
                || typeof(UnityEngine.Object).IsAssignableFrom(type)
                || typeof(IList).IsAssignableFrom(type);
        }

        private static bool MatchesSection(string path, string section)
        {
            return string.IsNullOrEmpty(section) || path.StartsWith($"{RootPath}.{section}.", StringComparison.Ordinal) || path == $"{RootPath}.{section}";
        }

        private static string NormalizeSection(string section)
        {
            if (string.IsNullOrWhiteSpace(section))
                return string.Empty;

            section = section.Trim();
            return section.StartsWith(RootPath + ".", StringComparison.Ordinal) ? section.Substring(RootPath.Length + 1) : section;
        }

        private static string NormalizePath(string path)
        {
            path = (path ?? string.Empty).Trim();
            return path.StartsWith(RootPath + ".", StringComparison.Ordinal) ? path : $"{RootPath}.{path}";
        }

        private static string GetString(Dictionary<string, object> args, string key, string defaultValue)
        {
            return args != null && args.TryGetValue(key, out object value) && value != null
                ? Convert.ToString(value, CultureInfo.InvariantCulture)
                : defaultValue;
        }

        private static bool GetBool(Dictionary<string, object> args, string key)
        {
            return args != null && args.TryGetValue(key, out object value) && value != null && Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        private static List<string> GetStringList(Dictionary<string, object> args, string key)
        {
            var result = new List<string>();
            if (args == null || !args.TryGetValue(key, out object value) || value == null)
                return result;

            if (value is IEnumerable enumerable && value is not string)
            {
                foreach (object item in enumerable)
                    result.Add(Convert.ToString(item, CultureInfo.InvariantCulture));
            }
            else
            {
                result.Add(Convert.ToString(value, CultureInfo.InvariantCulture));
            }

            return result.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }

        private static List<SettingPatch> GetPatches(Dictionary<string, object> args)
        {
            var patches = new List<SettingPatch>();
            if (args == null || !args.TryGetValue("patches", out object value) || value == null)
                return patches;

            if (value is IEnumerable enumerable && value is not string)
            {
                foreach (object item in enumerable)
                {
                    if (TryGetMap(item, out Dictionary<string, object> map) && map.TryGetValue("path", out object path))
                    {
                        map.TryGetValue("value", out object patchValue);
                        patches.Add(new SettingPatch { path = Convert.ToString(path, CultureInfo.InvariantCulture), value = patchValue });
                    }
                }
            }

            return patches;
        }

        private static string ToJson(object value)
        {
#if JSONNET_EXISTS
            return JsonConvert.SerializeObject(value, Formatting.Indented);
#else
            return JsonUtility.ToJson(value, true);
#endif
        }

        private sealed class MemberAccessor
        {
            public readonly string Name;
            public readonly Type ValueType;
            public readonly bool CanWrite;
            public readonly string Source;
            private readonly Func<object, object> _getter;
            private readonly Action<object, object> _setter;

            public MemberAccessor(string name, Type valueType, bool canWrite, string source, Func<object, object> getter, Action<object, object> setter)
            {
                Name = name;
                ValueType = valueType;
                CanWrite = canWrite;
                Source = source;
                _getter = getter;
                _setter = setter;
            }

            public object GetValue(object owner) => _getter(owner);
            public void SetValue(object owner, object value) => _setter(owner, value);
        }

        private sealed class SettingMember
        {
            public readonly string Path;
            public readonly Type ValueType;
            public readonly bool CanWrite;
            public readonly string Source;
            private readonly Func<object> _getter;
            private readonly Action<object> _setter;

            public SettingMember(string path, Type valueType, bool canWrite, string source, Func<object> getter, Action<object> setter)
            {
                Path = path;
                ValueType = valueType;
                CanWrite = canWrite;
                Source = source;
                _getter = getter;
                _setter = setter;
            }

            public object GetValue() => _getter();
            public void SetValue(object value) => _setter(value);
        }

        [Serializable]
        private sealed class SettingPatch
        {
            public string path;
            public object value;
        }

        [Serializable]
        private sealed class SettingsToolResponse
        {
            public bool success;
            public string error;
            public object data;

            public static SettingsToolResponse Ok(object data) => new SettingsToolResponse { success = true, data = data };
            public static SettingsToolResponse Fail(string error) => new SettingsToolResponse { success = false, error = error };
        }

        [Serializable]
        private sealed class SettingEntry
        {
            public string path;
            public string type;
            public string access;
            public string source;
            public string[] enum_values;
        }

        [Serializable]
        private sealed class SettingValue
        {
            public string path;
            public string type;
            public string source;
            public string status;
            public object value;
            public string error;
        }

        [Serializable]
        private sealed class SettingPatchResult
        {
            public string path;
            public string status;
            public object value;
            public string error;
        }
    }
}