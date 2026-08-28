using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UCC.Snapshot;
using DA_Assets.Tools;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.UCC
{
    internal class DebugTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        private PopupField<string> _baselinePopup;

        public VisualElement Draw()
        {
            var root = new VisualElement
            {
                style =
                {
                    paddingTop = DAI_UitkConstants.MarginPadding,
                    paddingBottom = DAI_UitkConstants.MarginPadding,
                    paddingLeft = DAI_UitkConstants.MarginPadding,
                    paddingRight = DAI_UitkConstants.MarginPadding
                }
            };

            DrawElements(root);
            root.Add(uitk.Space10());
            DrawElements2(root);

            return root;
        }

        private void DrawElements(VisualElement parent)
        {
            Label title = new Label(scriptableObject.Localize(FcuLocKey.label_debug_tools))
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeTitle,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            parent.Add(title);
            parent.Add(uitk.Space10());

            var formContainer = uitk.CreateSectionPanel();

            parent.Add(formContainer);

            var debugFlagsField = uitk.EnumFlagsField(scriptableObject.Localize(FcuLocKey.debug_label_settings), FcuDebugSettings.Settings);
            debugFlagsField.RegisterValueChangedCallback(evt =>
            {
                FcuDebugSettings.Settings = (FcuDebugSettingsFlags)evt.newValue;
            });
            formContainer.Add(debugFlagsField);
            formContainer.Add(uitk.ItemSeparator());
            formContainer.Add(uitk.Space5());

            var buttonsContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    alignItems = Align.FlexStart
                }
            };

            var openLogsButton = uitk.Button(
                scriptableObject.Localize(FcuLocKey.debug_button_open_logs),
                () => monoBeh.Config.LogPath.OpenFolderInOS());

            openLogsButton.style.flexShrink = 0;
            buttonsContainer.Add(openLogsButton);

            buttonsContainer.Add(uitk.Space10());

            var openCacheButton = uitk.Button(
                scriptableObject.Localize(FcuLocKey.debug_button_open_cache),
                () => monoBeh.Config.CachePath.OpenFolderInOS());

            openCacheButton.style.flexShrink = 0;
            buttonsContainer.Add(openCacheButton);

            buttonsContainer.Add(uitk.Space10());

            var openBackupButton = uitk.Button(
                scriptableObject.Localize(FcuLocKey.debug_button_open_backup),
                () => SceneBackuper.GetBackupsPath().OpenFolderInOS());

            openBackupButton.style.flexShrink = 0;
            buttonsContainer.Add(openBackupButton);

            buttonsContainer.Add(uitk.Space10());

            var testButton = uitk.Button(
                scriptableObject.Localize(FcuLocKey.debug_button_test),
                TestButton_OnClick);

            testButton.style.flexShrink = 0;
            buttonsContainer.Add(testButton);

            formContainer.Add(buttonsContainer);
        }

        private void DrawElements2(VisualElement parent)
        {
            var title = new Label(scriptableObject.Localize(FcuLocKey.debug_label_snapshot_testing))
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeTitle,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            parent.Add(title);
            parent.Add(uitk.Space10());

            var formContainer = uitk.CreateSectionPanel();
            parent.Add(formContainer);

            var rootField = new ObjectField(scriptableObject.Localize(FcuLocKey.debug_label_root_frame))
            {
                objectType = typeof(Transform),
                allowSceneObjects = true,
                value = monoBeh.SnapshotSettings.RootFrame
            };
            rootField.RegisterValueChangedCallback(evt =>
            {
                monoBeh.SnapshotSettings.RootFrame = evt.newValue as Transform;
            });
            formContainer.Add(rootField);

            formContainer.Add(uitk.ItemSeparator());
            formContainer.Add(uitk.Space5());

            var figmaLogRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            string currentLogPath = monoBeh.SnapshotSettings.FigmaResponseLogPath;
            string logDisplayText = string.IsNullOrEmpty(currentLogPath) ? scriptableObject.Localize(FcuLocKey.debug_label_none) : System.IO.Path.GetFileName(currentLogPath);

            var logLabel = new Label($"{scriptableObject.Localize(FcuLocKey.debug_label_figma_log)}: {logDisplayText}")
            {
                style =
                {
                    flexGrow = 1,
                    overflow = Overflow.Hidden,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            logLabel.tooltip = currentLogPath;
            figmaLogRow.Add(logLabel);

            var browseBtn = uitk.Button(scriptableObject.Localize(FcuLocKey.debug_button_browse), () =>
            {
                string path = EditorUtility.OpenFilePanel(scriptableObject.Localize(FcuLocKey.debug_button_browse), "", "json");

                if (!string.IsNullOrEmpty(path))
                {
                    monoBeh.SnapshotSettings.FigmaResponseLogPath = path;
                    logLabel.text = $"{scriptableObject.Localize(FcuLocKey.debug_label_figma_log)}: {System.IO.Path.GetFileName(path)}";
                    logLabel.tooltip = path;
                }
            });
            browseBtn.style.flexShrink = 0;
            figmaLogRow.Add(browseBtn);

            formContainer.Add(figmaLogRow);

            formContainer.Add(uitk.ItemSeparator());
            formContainer.Add(uitk.Space5());

            var baselines = SnapshotSaver.GetAvailableBaselines().ToList();

            if (baselines.Count == 0)
                baselines.Add(scriptableObject.Localize(FcuLocKey.debug_label_no_baselines));

            if (string.IsNullOrEmpty(monoBeh.SnapshotSettings.SelectedBaseline) || !baselines.Contains(monoBeh.SnapshotSettings.SelectedBaseline))
                monoBeh.SnapshotSettings.SelectedBaseline = baselines[0];

            var baselineRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            _baselinePopup = new PopupField<string>(scriptableObject.Localize(FcuLocKey.debug_label_baseline), baselines, monoBeh.SnapshotSettings.SelectedBaseline);
            _baselinePopup.RegisterValueChangedCallback(evt =>
            {
                monoBeh.SnapshotSettings.SelectedBaseline = evt.newValue;
            });
            _baselinePopup.style.flexGrow = 1;
            baselineRow.Add(_baselinePopup);

            var refreshBtn = uitk.Button("↻", RefreshBaselineList);
            refreshBtn.style.width = DAI_UitkConstants.SmallButtonSize;
            refreshBtn.style.marginLeft = DAI_UitkConstants.SpacingXS;
            refreshBtn.style.flexShrink = 0;
            baselineRow.Add(refreshBtn);

            formContainer.Add(baselineRow);

            formContainer.Add(uitk.ItemSeparator());
            formContainer.Add(uitk.Space5());

            var buttonsContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    alignItems = Align.FlexStart
                }
            };

            var exportBtn = uitk.Button(scriptableObject.Localize(FcuLocKey.debug_button_export_baseline), () =>
            {
                if (monoBeh.SnapshotSettings.RootFrame == null)
                {
                    EditorUtility.DisplayDialog(scriptableObject.Localize(FcuLocKey.debug_dialog_snapshot_title), scriptableObject.Localize(FcuLocKey.debug_dialog_assign_root), "OK");
                    return;
                }

                SnapshotSaver.ExportBaselineZip(monoBeh.SnapshotSettings.RootFrame, monoBeh.SnapshotSettings.FigmaResponseLogPath);
                RefreshBaselineList();
                EditorUtility.DisplayDialog(scriptableObject.Localize(FcuLocKey.debug_dialog_snapshot_title), scriptableObject.Localize(FcuLocKey.debug_dialog_export_success), "OK");
            });
            exportBtn.style.flexShrink = 0;
            buttonsContainer.Add(exportBtn);

            buttonsContainer.Add(uitk.Space10());

            var runTestBtn = uitk.Button(scriptableObject.Localize(FcuLocKey.debug_button_run_test), () =>
            {
                if (monoBeh.SnapshotSettings.RootFrame == null)
                {
                    EditorUtility.DisplayDialog(scriptableObject.Localize(FcuLocKey.debug_dialog_snapshot_title), scriptableObject.Localize(FcuLocKey.debug_dialog_assign_root), "OK");
                    return;
                }

                if (string.IsNullOrEmpty(monoBeh.SnapshotSettings.SelectedBaseline) || monoBeh.SnapshotSettings.SelectedBaseline == scriptableObject.Localize(FcuLocKey.debug_label_no_baselines))
                {
                    EditorUtility.DisplayDialog(scriptableObject.Localize(FcuLocKey.debug_dialog_snapshot_title), scriptableObject.Localize(FcuLocKey.debug_dialog_no_baseline), "OK");
                    return;
                }

                string zipPath = SnapshotSaver.GetBaselinePath(monoBeh.SnapshotSettings.SelectedBaseline);
                var report = SnapshotComparer.Compare(monoBeh.SnapshotSettings.RootFrame, zipPath);

                if (report.RootEntries == null || report.RootEntries.Count == 0)
                {
                    EditorUtility.DisplayDialog(scriptableObject.Localize(FcuLocKey.debug_dialog_snapshot_title), scriptableObject.Localize(FcuLocKey.debug_dialog_compare_failed), "OK");
                    return;
                }

                if (report.TotalDeviations == 0)
                {
                    EditorUtility.DisplayDialog(scriptableObject.Localize(FcuLocKey.debug_dialog_snapshot_title),
                        scriptableObject.Localize(FcuLocKey.debug_dialog_all_match, report.TotalComponents), "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog(scriptableObject.Localize(FcuLocKey.debug_dialog_snapshot_title),
                        scriptableObject.Localize(FcuLocKey.debug_dialog_deviations, report.TotalDeviations, report.TotalComponents), "View Diff");
                }

                SnapshotDiffWindow.ShowWithReport(report);
            });
            runTestBtn.style.flexShrink = 0;
            buttonsContainer.Add(runTestBtn);

            formContainer.Add(buttonsContainer);
        }

        private void RefreshBaselineList()
        {
            if (_baselinePopup == null)
                return;

            var baselines = SnapshotSaver.GetAvailableBaselines().ToList();
            if (baselines.Count == 0)
                baselines.Add(scriptableObject.Localize(FcuLocKey.debug_label_no_baselines));

            _baselinePopup.choices = baselines;

            if (!baselines.Contains(monoBeh.SnapshotSettings.SelectedBaseline))
                monoBeh.SnapshotSettings.SelectedBaseline = baselines[0];

            _baselinePopup.SetValueWithoutNotify(monoBeh.SnapshotSettings.SelectedBaseline);
        }

        private void TestButton_OnClick()
        {

        }
    }
}