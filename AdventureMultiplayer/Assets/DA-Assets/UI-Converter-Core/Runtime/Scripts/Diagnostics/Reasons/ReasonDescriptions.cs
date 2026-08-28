#if UNITY_EDITOR
using DA_Assets.UCC.Model;
using DA_Assets.Tools;
using System.Collections.Generic;
using System.Linq;

namespace DA_Assets.UCC.Extensions
{
    public static class ReasonDescriptions
    {
        public static string GetDescription(this ReasonKey key)
        {
            return FcuConfig.ReasonDescriptions.TryGetValue(key, out string desc)
                ? desc : key.ToString();
        }

        public static string GetDescription(this ReasonKey key, SyncData data)
        {
            string template = key.GetDescription();

            if (data?.ReasonArgs != null &&
                data.ReasonArgs.TryGetValue(key, out var args) &&
                string.IsNullOrEmpty(args) == false)
            {
                return string.Format(template, args);
            }

            return template;
        }

        public static void SetReason(this Node fobject, ReasonKey reason)
        {
            SetReason(fobject, reason, FcuTag.None);
        }

        public static void SetReason(this Node fobject, ReasonKey reason, FcuTag relatedTag)
        {
            if (fobject.Data.Reasons == null)
                fobject.Data.Reasons = new List<ReasonEntry>();

            var entry = new ReasonEntry(reason, relatedTag);
            if (!fobject.Data.Reasons.Contains(entry))
                fobject.Data.Reasons.Add(entry);
        }

        public static void SetReason(this Node fobject, ReasonKey reason, List<string> args)
        {
            SetReason(fobject, reason);

            if (args != null && args.Count > 0)
            {
                if (fobject.Data.ReasonArgs == null)
                    fobject.Data.ReasonArgs = new SerializedDictionary<ReasonKey, string>();

                fobject.Data.ReasonArgs[reason] = string.Join(", ", args);
            }
        }

        public static string GetReasonGroupFromPrefix(this ReasonKey key)
        {
            string name = key.ToString();
            int idx = name.IndexOf('_');
            return idx > 0 ? name.Substring(0, idx) : "Other";
        }
    }
}
#endif