using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    public static class FieldResetExtensions
    {
        private static readonly Color IndicatorColor = new Color(0.25f, 0.56f, 0.87f, 1f);





        public static void AddResetMenu<TSettings, TValue>(
            this BaseField<TValue> field,
            TSettings current,
            TSettings defaults,
            Func<TSettings, TValue> getter,
            Action<TSettings, TValue> setter)
            where TValue : IEquatable<TValue>
        {

            SetModifiedIndicator(field, !EqualityComparer<TValue>.Default.Equals(getter(current), getter(defaults)));


            field.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                var defVal = getter(defaults);
                var curVal = getter(current);
                bool isDefault = EqualityComparer<TValue>.Default.Equals(curVal, defVal);

                evt.menu.AppendAction(
                    $"Reset to Default ({FormatValue(defVal)})",
                    _ =>
                    {
                        setter(current, defVal);
                        field.SetValueWithoutNotify(defVal);
                        SetModifiedIndicator(field, false);
                    },
                    isDefault
                        ? DropdownMenuAction.Status.Disabled
                        : DropdownMenuAction.Status.Normal);
            }));


            field.RegisterValueChangedCallback(evt =>
            {
                bool modified = !EqualityComparer<TValue>.Default.Equals(evt.newValue, getter(defaults));
                SetModifiedIndicator(field, modified);
            });
        }





        public static void AddResetMenu<TSettings, TEnum>(
            this EnumField field,
            TSettings current,
            TSettings defaults,
            Func<TSettings, TEnum> getter,
            Action<TSettings, TEnum> setter)
            where TEnum : struct, Enum
            => AddEnumResetMenuCore(field, current, defaults, getter, setter);

        public static void AddResetMenu<TSettings, TEnum>(
            this EnumFlagsField field,
            TSettings current,
            TSettings defaults,
            Func<TSettings, TEnum> getter,
            Action<TSettings, TEnum> setter)
            where TEnum : struct, Enum
            => AddEnumResetMenuCore(field, current, defaults, getter, setter);

        private static void AddEnumResetMenuCore<TSettings, TEnum>(
            BaseField<Enum> field,
            TSettings current,
            TSettings defaults,
            Func<TSettings, TEnum> getter,
            Action<TSettings, TEnum> setter)
            where TEnum : struct, Enum
        {

            SetModifiedIndicator(field, !EqualityComparer<TEnum>.Default.Equals(getter(current), getter(defaults)));


            field.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                var def = getter(defaults);
                var cur = getter(current);
                bool isDefault = EqualityComparer<TEnum>.Default.Equals(cur, def);

                evt.menu.AppendAction(
                    $"Reset to Default ({FormatValue(def)})",
                    _ =>
                    {
                        setter(current, def);
                        field.SetValueWithoutNotify(def);
                        SetModifiedIndicator(field, false);
                    },
                    isDefault
                        ? DropdownMenuAction.Status.Disabled
                        : DropdownMenuAction.Status.Normal);
            }));


            field.RegisterValueChangedCallback(evt =>
            {
                bool modified = !EqualityComparer<TEnum>.Default.Equals(
                    (TEnum)(object)evt.newValue, getter(defaults));
                SetModifiedIndicator(field, modified);
            });
        }





        public static void AddSectionResetMenu(this VisualElement header, Action resetAll)
        {
            header.AddManipulator(new ContextualMenuManipulator(evt =>
                evt.menu.AppendAction("Reset Section to Defaults", _ => resetAll())));
        }





        public static void AddDropdownResetMenu(
            this DropdownField field,
            Func<string> getter,
            string defaultValue,
            Action<string> setter)
        {
            SetModifiedIndicator(field, getter() != defaultValue);

            field.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                string cur = getter();
                bool isDefault = cur == defaultValue;

                evt.menu.AppendAction(
                    $"Reset to Default ({defaultValue})",
                    _ =>
                    {
                        setter(defaultValue);
                        field.SetValueWithoutNotify(defaultValue);
                        SetModifiedIndicator(field, false);
                    },
                    isDefault
                        ? DropdownMenuAction.Status.Disabled
                        : DropdownMenuAction.Status.Normal);
            }));

            field.RegisterValueChangedCallback(evt =>
                SetModifiedIndicator(field, evt.newValue != defaultValue));
        }





        public static void AddFolderResetMenu(
            this VisualElement container,
            Func<string> getter,
            string defaultValue,
            Action<string> setter)
        {

            SetModifiedIndicator(container, getter() != defaultValue);

            container.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                string cur = getter();
                bool isDefault = cur == defaultValue;

                evt.menu.AppendAction(
                    $"Reset to Default ({(string.IsNullOrEmpty(defaultValue) ? "<empty>" : defaultValue)})",
                    _ =>
                    {
                        setter(defaultValue);

                        var tf = container.Q<TextField>();
                        tf?.SetValueWithoutNotify(defaultValue);
                        SetModifiedIndicator(container, false);
                    },
                    isDefault
                        ? DropdownMenuAction.Status.Disabled
                        : DropdownMenuAction.Status.Normal);
            }));


            container.RegisterCallback<ChangeEvent<string>>(evt =>
                SetModifiedIndicator(container, evt.newValue != defaultValue), TrickleDown.TrickleDown);
        }






        public static void AddPopupResetMenu(
            this BaseField<string> field,
            Func<string> getter,
            string defaultValue,
            Action<string> setter)
        {

            SetModifiedIndicator(field, getter() != defaultValue);

            field.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                string cur = getter();
                bool isDefault = cur == defaultValue;

                evt.menu.AppendAction(
                    $"Reset to Default ({defaultValue})",
                    _ =>
                    {
                        setter(defaultValue);
                        field.SetValueWithoutNotify(defaultValue);
                        SetModifiedIndicator(field, false);
                    },
                    isDefault
                        ? DropdownMenuAction.Status.Disabled
                        : DropdownMenuAction.Status.Normal);
            }));

            field.RegisterValueChangedCallback(evt =>
                SetModifiedIndicator(field, evt.newValue != defaultValue));
        }





        public static void SetModifiedIndicatorPublic(VisualElement field, bool modified)
            => SetModifiedIndicator(field, modified);

        private static void SetModifiedIndicator(VisualElement field, bool modified)
        {
            if (modified)
            {
                field.style.borderLeftColor = IndicatorColor;
                field.style.borderLeftWidth = 3f;
                field.style.paddingLeft = 4f;
            }
            else
            {
                field.style.borderLeftColor = StyleKeyword.Null;
                field.style.borderLeftWidth = StyleKeyword.Null;
                field.style.paddingLeft = StyleKeyword.Null;
            }
        }

        private static string FormatValue<T>(T value) => value switch
        {
            Color c      => $"#{ColorUtility.ToHtmlStringRGBA(c)}",
            float f      => f.ToString("F2"),
            double d     => d.ToString("F2"),
            Vector2 v    => $"({v.x:F1}, {v.y:F1})",
            Vector2Int v => $"({v.x}, {v.y})",
            Vector4 v    => $"({v.x:F1}, {v.y:F1}, {v.z:F1}, {v.w:F1})",
            null         => "null",
            _            => value.ToString()
        };
    }
}