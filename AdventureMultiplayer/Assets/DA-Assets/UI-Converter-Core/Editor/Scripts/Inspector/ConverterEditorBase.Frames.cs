using UnityEngine.UIElements;
using static DA_Assets.DAI.DAInspectorUITK;

namespace DA_Assets.UCC
{
    public partial class ConverterEditorBase
    {
        private ScrollView CreateScroll()
        {
            var sv = new ScrollView
            {
                style =
                {
                    backgroundColor = uitk.ColorScheme.CLEAR
                }
            };
#if UNITY_2020_1_OR_NEWER
            sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
#else
            sv.showHorizontal = false;
#endif
            return sv;
        }

        private VisualElement BuildFramesCard(bool hasContent)
        {
            var framesCard = Card();

            _framesScroll = CreateScroll();
            _framesHost = new VisualElement();

            if (hasContent)
                _framesHost.Add(FrameList.BuildFramesSectionUI());

            _framesScroll.Add(_framesHost);
            framesCard.Add(_framesScroll);

            return framesCard;
        }

        private void RefreshFrames()
        {
            if (_framesScroll == null || _framesHost == null)
                return;

            _framesHost.Clear();
            _framesHost.Add(this.FrameList.BuildFramesSectionUI());

            _framesScroll.schedule.Execute(() =>
            {
                if (_framesScroll != null && _framesHost != null)
                    _framesScroll.ScrollTo(_framesHost);
            });

            Repaint();
        }
    }
}