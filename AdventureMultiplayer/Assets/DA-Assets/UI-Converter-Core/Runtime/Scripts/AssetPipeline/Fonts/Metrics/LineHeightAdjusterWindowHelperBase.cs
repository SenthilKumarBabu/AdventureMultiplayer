#if UNITY_EDITOR

using DA_Assets.UCC.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DA_Assets.UCC
{
    public static class LineHeightAdjusterWindowHelperBase
    {
        public static bool CanShowWindow(
            ConverterBase monoBeh,
            LineHeightAdjusterWindowData data,
            Action<LineHeightAdjusterWindowData, Action<LineHeightAdjusterWindowResult>> showDelegate)
        {
            if (monoBeh.IsPlaying())
            {
                return false;
            }

            if (showDelegate == null)
            {
                return false;
            }

            if (data.Fonts == null || data.Fonts.Count == 0)
            {
                return false;
            }

            return true;
        }

        public static async Task<LineHeightAdjusterWindowResult> ShowWindow(
            ConverterBase monoBeh,
            LineHeightAdjusterWindowData data,
            Action<LineHeightAdjusterWindowData, Action<LineHeightAdjusterWindowResult>> showDelegate,
            CancellationToken token)
        {
            var result = new LineHeightAdjusterWindowResult
            {
                Action = LineHeightAdjusterAction.None
            };

            if (!CanShowWindow(monoBeh, data, showDelegate))
            {
                return new LineHeightAdjusterWindowResult
                {
                    Action = LineHeightAdjusterAction.ContinueImport
                };
            }

            await monoBeh.AssetTools.ReselectFcu(token);

            showDelegate(data, output => result = output);

            while (result.Action == LineHeightAdjusterAction.None)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(1000, token);
            }

            return result;
        }
    }
}
#endif