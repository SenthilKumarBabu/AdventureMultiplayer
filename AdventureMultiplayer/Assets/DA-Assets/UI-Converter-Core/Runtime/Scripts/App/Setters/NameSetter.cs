#if UNITY_EDITOR
using DA_Assets.Constants;
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using DA_Assets.Shared;
using DA_Assets.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using TextCoreFontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace DA_Assets.UCC
{
    [Serializable]
    public class NameSetter : FcuBase
    {
        private readonly ConcurrentDictionary<string, int> _fieldNames = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _methodNames = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _classNames = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _fileNames = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _fileNameFirstHash = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _folderNames = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, int> _folderNameFirstHash = new ConcurrentDictionary<string, int>();

        private NameSanitizer sanitizer;
        private FallbackNameGenerator fallbackGen;
        private Dictionary<FcuNameType, INameProcessor> processors;
        private Dictionary<FcuNameType, IUniqueNameService> uniques;
        private Dictionary<FcuNameType, ILengthLimiter> limiters;

        public override void Init(ConverterBase monoBeh)
        {
            base.Init(monoBeh);

            sanitizer = new NameSanitizer(monoBeh);
            fallbackGen = new FallbackNameGenerator(monoBeh);

            processors = new Dictionary<FcuNameType, INameProcessor>
            {
                { FcuNameType.Object, new ObjectNameProcessor(monoBeh) },
                { FcuNameType.Class, new CsClassNameProcessor() },
                { FcuNameType.Field, new CsFieldNameProcessor() },
                { FcuNameType.Method, new CsMethodNameProcessor() },
                { FcuNameType.File, new FileNameProcessor(monoBeh) },
                { FcuNameType.Folder, new FolderNameProcessor() },
                { FcuNameType.LocKey, new LocKeyProcessor(monoBeh) },
                { FcuNameType.HumanizedTextPrefabName, new HumanizedTextPrefabProcessor(monoBeh) },
                { FcuNameType.UitkGuid, new GuidProcessor() },
                { FcuNameType.UssClass, new UssClassProcessor(monoBeh) }
            };

            uniques = new Dictionary<FcuNameType, IUniqueNameService>
            {
                { FcuNameType.Class, new ClassUniqueNameService(_classNames) },
                { FcuNameType.Method, new DictUniqueNameService(_methodNames) },
                { FcuNameType.Field, new DictUniqueNameService(_fieldNames) },
                { FcuNameType.File, new FileUniqueNameService(_fileNames, _fileNameFirstHash) },
                { FcuNameType.Folder, new FileUniqueNameService(_folderNames, _folderNameFirstHash) }
            };

            limiters = new Dictionary<FcuNameType, ILengthLimiter>
            {
                { FcuNameType.Class, new CsClassLengthLimiter(monoBeh) },
                { FcuNameType.Method, new CsMethodLengthLimiter(monoBeh) },
                { FcuNameType.Field, new CsFieldLengthLimiter(monoBeh) },
                { FcuNameType.Object, new ObjectNameLengthLimiter(monoBeh) },
                { FcuNameType.File, new FileNameLengthLimiter(monoBeh) },
                { FcuNameType.Folder, new FileNameLengthLimiter(monoBeh) },
                { FcuNameType.LocKey, new LocKeyLengthLimiter(monoBeh) }
            };
        }

        public void SetNames(Node fobject)
        {
            FNames nsd = new FNames
            {
                FigmaName = fobject.Name,
                ObjectName = GetFcuName(fobject, FcuNameType.Object),
                UitkGuid = GetFcuName(fobject, FcuNameType.UitkGuid),
                LocKey = GetFcuName(fobject, FcuNameType.LocKey),
                FieldName = GetFcuName(fobject, FcuNameType.Field),
                MethodName = GetFcuName(fobject, FcuNameType.Method),
                ClassName = GetFcuName(fobject, FcuNameType.Class),
                HumanizedTextPrefabName = GetFcuName(fobject, FcuNameType.HumanizedTextPrefabName)
            };

            fobject.Data.Names = nsd;
        }

        private string GetSpritePath(Node fobject)
        {
            Sprite sprite = monoBeh.SpriteProcessor.GetSprite(fobject);
            string assetPath = URIHelpersRef.MakeAssetUri(sprite);
            return assetPath;
        }

        private string GetFontPath(Node fobject)
        {
            TextCoreFontAsset fontAsset = monoBeh.FontLoader.GetFontFromArray(fobject, monoBeh.FontLoader.UitkFontAssets);
            FcuLogger.Debug($"UITK_Converter | FontAsset | {fobject.Data.NameHierarchy} | {fontAsset}", FcuDebugSettingsFlags.LogNameSetter);
            return URIHelpersRef.MakeAssetUri(fontAsset);
        }

        internal async Task Set_UITK_Names(List<Node> fobjects)
        {
            foreach (Node fobject in fobjects)
            {
                if (!fobject.Data.SpritePath.IsEmpty())
                {
                    fobject.Data.Names.UITK_SpritePath = GetSpritePath(fobject);
                }
                else if (fobject.ContainsTag(FcuTag.Text))
                {
                    fobject.Data.Names.UITK_FontPath = GetFontPath(fobject);
                }
            }

            await Task.CompletedTask;
        }

        internal async Task SetNames(List<Node> fobjects, FcuNameType nameType, CancellationToken token)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(fobjects, fobject =>
                {
                    fobject.Data.Names.Names[nameType] = GetFcuName(fobject, nameType);
                });
            }, token);
        }

        public string GetFcuName(Node fobj, FcuNameType type)
        {
            try
            {
                string raw = fobj.Name;

                string inputForProcessor = sanitizer.Sanitize(raw);

                bool isValidName = !string.IsNullOrWhiteSpace(inputForProcessor);

                if (!isValidName)
                {
                    inputForProcessor = fallbackGen.Get(fobj);
                }

                if (!processors.TryGetValue(type, out var processor))
                {
                    Debug.LogError($"NameSetter: no INameProcessor registered for FcuNameType.{type}");
                    return null;
                }

                string processed = processor.Process(inputForProcessor, fobj);
                if (processed == null)
                    return null;

                processed = processed.RemoveRepeatsForNonAlphanumericCharacters();
                processed = processed.Trim();

                if (uniques != null && uniques.TryGetValue(type, out var uniq))
                    processed = uniq.EnsureUnique(processed, fobj);

                if (limiters != null && limiters.TryGetValue(type, out var limiter))
                    processed = limiter.Limit(processed, fobj);

                return processed;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return "empty_name";
            }
        }

        public void ClearNames()
        {
            _fieldNames.Clear();
            _methodNames.Clear();
            _classNames.Clear();
            _fileNameFirstHash.Clear();
            _fileNames.Clear();
            _folderNameFirstHash.Clear();
            _folderNames.Clear();
        }
    }

    public class NameSanitizer
    {
        private readonly ConverterBase mono;

        public NameSanitizer(ConverterBase m)
        {
            mono = m;
        }

        public string Sanitize(string s)
        {
            string result = PathsRef.MakeValidFileName(s);




            result = result?.TrimStart('.');
            return result;
        }
    }

    public class FallbackNameGenerator
    {
        private readonly ConverterBase mono;

        public FallbackNameGenerator(ConverterBase m)
        {
            mono = m;
        }

        public string Get(Node f)
        {
            string separator = mono.IsUITK() ? "-" : " ";
            return $"{nameof(GameObject)}{separator}{f.Id.GetNumberOnly()}";
        }
    }

    public interface INameProcessor
    {
        string Process(string name, Node fobj);
    }

    public class ObjectNameProcessor : INameProcessor
    {
        private readonly ConverterBase mono;

        public ObjectNameProcessor(ConverterBase m)
        {
            mono = m;
        }

        public string Process(string name, Node f)
        {
            if (mono.IsUITK())
            {
                if (!string.IsNullOrEmpty(name) && char.IsDigit(name[0]))
                    name = "_" + name;
            }

            return name;
        }
    }

    public class CsFieldNameProcessor : INameProcessor
    {
        public string Process(string name, Node f)
        {
            string n = name.RemoveNonLettersAndNonNumbers("_").Trim();
            n = n.ToPascalCase();
            if (n.IsEmpty())
                n = "Field";

            n = "_" + n;

            if (n.Length > 1)
            {
                n = n.MakeCharLower(1);
            }

            return n;
        }
    }

    public class CsMethodNameProcessor : INameProcessor
    {
        public string Process(string name, Node f)
        {
            string n = name.RemoveNonLettersAndNonNumbers("_").Trim();
            n = n.ToPascalCase();
            if (n.IsEmpty())
                n = "Method";

            if (n.Length > 0 && (char.IsDigit(n[0]) || CsSharpKeywords.Keywords.Contains(n)))
            {
                n = "M" + n;

                if (n.Length > 2)
                {
                    n = n.MakeCharUpper(2);
                }
            }

            return n;
        }
    }

    public class CsClassNameProcessor : INameProcessor
    {
        public string Process(string name, Node f)
        {
            string n = name.RemoveNonLettersAndNonNumbers("_").Trim();
            n = n.ToPascalCase();
            if (n.IsEmpty())
                n = "Class";

            if (n.Length > 0 && (char.IsDigit(n[0]) || CsSharpKeywords.Keywords.Contains(n)))
            {
                n = "C" + n;

                if (n.Length > 2)
                {
                    n = n.MakeCharUpper(2);
                }
            }

            return n;
        }
    }

    public class FolderNameProcessor : INameProcessor
    {
        public string Process(string name, Node f)
        {
            return name;
        }
    }

    public class FileNameProcessor : INameProcessor
    {
        private readonly ConverterBase mono;

        public FileNameProcessor(ConverterBase m)
        {
            mono = m;
        }

        public string Process(string name, Node f)
        {
            string ext = f.Data.ImageFormat.ToLower();
            return name + "." + ext;
        }
    }

    public class LocKeyProcessor : INameProcessor
    {
        private readonly ConverterBase mono;

        public LocKeyProcessor(ConverterBase m)
        {
            mono = m;
        }

        public string Process(string name, Node f)
        {
            if (!IsLocKeyCandidate(name))
                return null;

            string n = name.RemoveNonLettersAndNonNumbers("_");

            var type = mono.Settings.LocalizationSettings.LocKeyCaseType;

            if (n.IsAllUpper())
            {
                switch (type)
                {
                    case LocalizationKeyCaseType.snake_case:
                        n = n.ToLower();
                        break;
                    case LocalizationKeyCaseType.UPPER_SNAKE_CASE:
                        n = n.ToUpper();
                        break;
                    case LocalizationKeyCaseType.PascalCase:
                        n = n.ToPascalCase();
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case LocalizationKeyCaseType.snake_case:
                        n = n.ToSnakeCase();
                        break;
                    case LocalizationKeyCaseType.UPPER_SNAKE_CASE:
                        n = n.ToUpperSnakeCase();
                        break;
                    case LocalizationKeyCaseType.PascalCase:
                        n = n.ToPascalCase();
                        break;
                }
            }

            return n;
        }

        private bool IsLocKeyCandidate(string name)
        {
            if (name.IsEmpty())
                return false;

            if (name.TryParseFloat(out var _))
                return false;

            if (DateTime.TryParse(name, out var _))
                return false;

            if (TimeSpan.TryParse(name, out var _))
                return false;

            if (name.IsValidEmail())
                return false;

            if (name.IsSpecialNumber())
                return false;

            if (name.IsPhoneNumber())
                return false;

            if (name.StartsWith("http://") || name.StartsWith("https://"))
                return false;

            return true;
        }
    }

    public class HumanizedTextPrefabProcessor : INameProcessor
    {
        private readonly ConverterBase mono;

        public HumanizedTextPrefabProcessor(ConverterBase m)
        {
            mono = m;
        }

        public string Process(string name, Node f)
        {
            if (f.Type != NodeType.TEXT)
                return null;

            return mono.NameHumanizer.GetHumanizedTextPrefabName(f);
        }
    }

    public class GuidProcessor : INameProcessor
    {
        public string Process(string n, Node f)
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }
    }

    public class UssClassProcessor : INameProcessor
    {
        private readonly ConverterBase mono;

        public UssClassProcessor(ConverterBase m)
        {
            mono = m;
        }

        public string Process(string name, Node f)
        {
            if (!string.IsNullOrWhiteSpace(f.Name) &&
                f.Name.StartsWith(".") &&
                f.Name.Contains($" {mono.Config.RealTagSeparator} "))
            {
                int start = 1;
                int end = f.Name.IndexOf($" {mono.Config.RealTagSeparator} ");
                if (end > start)
                    return f.Name.Substring(start, end - start);
            }

            string safeName = name.RemoveNonLettersAndNonNumbers("-");
            return "style-" + safeName + "-" + f.Data.ObjectHash;
        }
    }

    public interface IUniqueNameService
    {
        string EnsureUnique(string name, Node f);
    }

    public class DictUniqueNameService : IUniqueNameService
    {
        private readonly ConcurrentDictionary<string, int> dict;

        public DictUniqueNameService(ConcurrentDictionary<string, int> d)
        {
            dict = d;
        }

        public string EnsureUnique(string name, Node f)
        {
            if (dict.TryGetValue(name, out int n))
            {
                dict[name] = n + 1;
                return name + "_" + (n + 1);
            }

            dict.TryAdd(name, 0);
            return name;
        }
    }

    public class ClassUniqueNameService : IUniqueNameService
    {
        private readonly ConcurrentDictionary<string, int> dict;

        public ClassUniqueNameService(ConcurrentDictionary<string, int> d)
        {
            dict = d;
        }

        public string EnsureUnique(string name, Node f)
        {
            if (dict.Count < 1)
            {
                int maxNumber = AssetTools.GetMaxFileNumber("Assets", name, "cs");
                if (maxNumber >= 0)
                {
                    dict.TryAdd(name, maxNumber);
                }
            }

            int number;
            if (dict.TryGetValue(name, out number))
            {
                dict[name] = number + 1;
                number = dict[name];
            }
            else
            {
                if (!dict.TryAdd(name, 0))
                {
                    // TODO:
                    // Debug.LogError($"FindNameInDict.TryAdd | {name} | false");
                }
                number = 0;
            }

            if (number > 0)
            {
                return $"{name}_{number}";
            }

            return name;
        }
    }

    public class FileUniqueNameService : IUniqueNameService
    {
        private readonly ConcurrentDictionary<string, int> dict;
        private readonly ConcurrentDictionary<string, int> firstHash;

        public FileUniqueNameService(ConcurrentDictionary<string, int> d, ConcurrentDictionary<string, int> fh)
        {
            dict = d;
            firstHash = fh;
        }

        public string EnsureUnique(string name, Node f)
        {
            string baseKey = GetBaseKey(name).ToLower();
            int hash = f.Data.ObjectHash;

            if (firstHash.TryGetValue(baseKey, out int h))
            {
                if (h != hash)
                {
                    int idx = dict.AddOrUpdate(baseKey, 1, (_, c) => c + 1);
                    return InsertIndex(name, idx);
                }
            }
            else
            {
                firstHash.TryAdd(baseKey, hash);
                dict.TryAdd(baseKey, 0);
            }

            return name;
        }

        private string GetBaseKey(string name)
        {
            int dot = name.LastIndexOf('.');
            return dot > 0 ? name.Substring(0, dot) : name;
        }

        private string InsertIndex(string name, int idx)
        {
            int dot = name.LastIndexOf('.');
            if (dot <= 0)
                return name + "-" + idx;

            string b = name.Substring(0, dot);
            string e = name.Substring(dot);
            return b + "-" + idx + e;
        }
    }

    public interface ILengthLimiter
    {
        string Limit(string name, Node f);
    }

    public class CsClassLengthLimiter : ILengthLimiter
    {
        private readonly ConverterBase mono;

        public CsClassLengthLimiter(ConverterBase m)
        {
            mono = m;
        }

        public string Limit(string n, Node f)
        {
            return n.SubstringSafe(mono.Settings.ScriptGeneratorSettings.ClassNameMaxLenght);
        }
    }

    public class CsMethodLengthLimiter : ILengthLimiter
    {
        private readonly ConverterBase mono;

        public CsMethodLengthLimiter(ConverterBase m)
        {
            mono = m;
        }

        public string Limit(string n, Node f)
        {
            return n.SubstringSafe(mono.Settings.ScriptGeneratorSettings.MethodNameMaxLenght);
        }
    }

    public class CsFieldLengthLimiter : ILengthLimiter
    {
        private readonly ConverterBase mono;

        public CsFieldLengthLimiter(ConverterBase m)
        {
            mono = m;
        }

        public string Limit(string n, Node f)
        {
            return n.SubstringSafe(mono.Settings.ScriptGeneratorSettings.FieldNameMaxLenght);
        }
    }

    public class ObjectNameLengthLimiter : ILengthLimiter
    {
        private readonly ConverterBase mono;

        public ObjectNameLengthLimiter(ConverterBase m)
        {
            mono = m;
        }

        public string Limit(string n, Node f)
        {
            int l = f.Type == NodeType.TEXT
                ? mono.Settings.MainSettings.TextObjectNameMaxLenght
                : mono.Settings.MainSettings.GameObjectNameMaxLenght;

            return n.SubstringSafe(l);
        }
    }

    public class FileNameLengthLimiter : ILengthLimiter
    {
        private readonly ConverterBase mono;

        public FileNameLengthLimiter(ConverterBase m)
        {
            mono = m;
        }

        public string Limit(string n, Node f)
        {
            int l = f.Type == NodeType.TEXT
                ? mono.Settings.MainSettings.TextObjectNameMaxLenght
                : mono.Settings.MainSettings.GameObjectNameMaxLenght;

            string extension = Path.GetExtension(n);

            if (string.IsNullOrEmpty(extension))
            {
                return n.SubstringSafe(l);
            }

            int limitForName = l - extension.Length;

            if (limitForName <= 0)
            {
                return n.SubstringSafe(l);
            }

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(n);

            return nameWithoutExtension.SubstringSafe(limitForName) + extension;
        }
    }

    public class LocKeyLengthLimiter : ILengthLimiter
    {
        private readonly ConverterBase mono;

        public LocKeyLengthLimiter(ConverterBase m)
        {
            mono = m;
        }

        public string Limit(string n, Node f)
        {
            return n.SubstringSafe(mono.Settings.LocalizationSettings.LocKeyMaxLenght);
        }
    }
}
#endif