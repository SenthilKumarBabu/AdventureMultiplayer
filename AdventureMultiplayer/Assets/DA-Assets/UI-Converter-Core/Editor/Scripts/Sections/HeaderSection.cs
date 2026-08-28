using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.Tools;
using DA_Assets.UpdateChecker;
using DA_Assets.UpdateChecker.Models;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#if !UNITY_2020_1_OR_NEWER
using UnityEditor.UIElements;
#endif

namespace DA_Assets.UCC
{
    [Serializable]
    internal class HeaderSection : MonoBehaviourLinkerEditor<ConverterEditorBase, ConverterBase>
    {
        private Label _lblKb;
        private Label _lblUser;
        private Label _lblProj;

        public VisualElement BuildHeaderUI()
        {
            var wrap = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = uitk.ColorScheme.CLEAR,
                    minWidth = 0
                }
            };

            var progressBarsContainer = new VisualElement { name = "ProgressBarsContainer" };
            progressBarsContainer.style.marginTop = -15;
            progressBarsContainer.style.marginLeft = -15;
            progressBarsContainer.style.marginRight = -15;
            wrap.Add(progressBarsContainer);

            EditorProgressBarManager.RegisterContainer(monoBeh, progressBarsContainer, uitk);

            var topRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.Center,
                    alignItems = Align.Center,
                    backgroundColor = uitk.ColorScheme.CLEAR,
                    minWidth = 0
                }
            };

            topRow.Add(uitk.CenteredLogo(
                scriptableObject.LogoDarkTheme,
                scriptableObject.LogoLightTheme,
                96,
                384,
                45,
                45));
            wrap.Add(uitk.Space(28));
            wrap.Add(topRow);
            wrap.Add(uitk.Space5());
            var midRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = uitk.ColorScheme.CLEAR,
                    minWidth = 0
                }
            };

            var spacer = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    minWidth = 0
                }
            };
            midRow.Add(spacer);

            var rightStack = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.FlexEnd,
                    backgroundColor = uitk.ColorScheme.CLEAR,
                    minWidth = 0
                }
            };

            var infoRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    alignItems = Align.Center,
                    backgroundColor = uitk.ColorScheme.CLEAR,
                    minWidth = 0
                }
            };

            _lblKb = new Label(string.Empty)
            {
                tooltip = scriptableObject.Localize(FcuLocKey.tooltip_kilobytes)
            };
            infoRow.Add(_lblKb);
            infoRow.Add(scriptableObject.uitk.Space10());
            infoRow.Add(new Label("—"));
            infoRow.Add(scriptableObject.uitk.Space10());

            _lblUser = new Label(string.Empty)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    flexShrink = 1,
                    minWidth = 0,
                    unityTextAlign = TextAnchor.MiddleRight
                }
            };
            infoRow.Add(_lblUser);

            rightStack.Add(infoRow);
            midRow.Add(rightStack);
            wrap.Add(midRow);

            var projRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    backgroundColor = uitk.ColorScheme.CLEAR,
                    minWidth = 0
                }
            };

            _lblProj = new Label(string.Empty)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    flexShrink = 1,
                    minWidth = 0,
                    unityTextAlign = TextAnchor.MiddleRight
                }
            };
            projRow.Add(_lblProj);
            wrap.Add(projRow);

            wrap.schedule.Execute(UpdateUI).Every(10);

            return wrap;
        }

        private void UpdateUI()
        {
            if (_lblKb != null)
            {
                _lblKb.text = $"{(monoBeh.RequestSender.PbarBytes / 1024f).ToString("F0", System.Globalization.CultureInfo.CurrentCulture)}\u00A0KiB";
            }

            if (_lblUser != null)
            {
                string userId = monoBeh.AuthProvider?.UserIdShort ?? string.Empty;
                string userName = monoBeh.AuthProvider?.UserDisplayName ?? string.Empty;

                bool isUserIdEmpty = string.IsNullOrWhiteSpace(userId);
                bool isUserNameEmpty = string.IsNullOrWhiteSpace(userName);

                if (isUserIdEmpty && isUserNameEmpty)
                {
                    _lblUser.text = scriptableObject.Localize(FcuLocKey.header_status_not_logged);
                    _lblUser.tooltip = null;
                }
                else if (!isUserNameEmpty)
                {
                    _lblUser.text = userName;
                    _lblUser.tooltip = scriptableObject.Localize(FcuLocKey.label_user_name);
                }
                else
                {
                    _lblUser.text = userId;
                    _lblUser.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_user_id);
                }
            }

            if (_lblProj != null)
            {
                _lblProj.text = string.IsNullOrEmpty(monoBeh.CurrentProject.ProjectName)
                    ? string.Empty
                    : monoBeh.CurrentProject.ProjectName;
            }
        }
    }
}