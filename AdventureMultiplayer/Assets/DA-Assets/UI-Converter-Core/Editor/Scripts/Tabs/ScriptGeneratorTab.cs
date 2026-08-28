using System;
using System.Collections.Generic;
using System.Linq;
using DA_Assets.DAI;
using DA_Assets.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#pragma warning disable CS0649

namespace DA_Assets.UCC
{
    internal class ScriptGeneratorTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        private ScriptGeneratorSelectionContext _selectionContext;
        private readonly Dictionary<string, FrameView> _frameViews = new Dictionary<string, FrameView>();
        private readonly Dictionary<string, VisualElement> _fieldRows = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, FieldView> _fieldViews = new Dictionary<string, FieldView>();
        private TextField _globalSearchField;
        private Label _selectionInfoLabel;
        private Button _generateButton;
        private Button _serializeButton;
        private Button _scanButton;
        private ScrollView _framesScrollView;
        private Label _framesPlaceholder;
        private string _globalSearchValue = string.Empty;
        private string _globalSearchRawValue = string.Empty;
        private VisualElement _namingErrorBox;
        private readonly List<InvalidFieldReference> _invalidFieldCache = new List<InvalidFieldReference>();
        private readonly Dictionary<Type, Texture2D> _componentIconCache = new Dictionary<Type, Texture2D>();

        private class FrameView
        {
            public ScriptGeneratorFrameSelection Frame;
            public TextField SearchField;
            public ScrollView FieldScroll;
            public Label CounterLabel;
            public VisualElement BodyContainer;
            public VisualElement Root;
            public Label GlobalFilterInfo;
            public Toggle FrameToggle;
            public TextField ClassNameField;
            public Label ClassErrorLabel;
            public string LocalFilter = string.Empty;
        }

        private class FieldView
        {
            public Toggle Toggle;
            public TextField NameField;
            public Label ErrorLabel;
            public VisualElement Row;
            public VisualElement MethodRow;
            public TextField MethodField;
            public Label MethodErrorLabel;
        }

        private readonly struct InvalidFieldReference
        {
            public InvalidFieldReference(FrameView frame, VisualElement row)
            {
                FrameView = frame;
                Row = row;
            }

            public FrameView FrameView { get; }
            public VisualElement Row { get; }
        }

        public VisualElement Draw()
        {
            var root = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    paddingTop = 10,
                    paddingBottom = 10,
                    paddingLeft = 10,
                    paddingRight = 10
                }
            };
            root.name = "root";
            root.AddToClassList(FcuSettingsWindow.StretchTabClass);

            DrawElements(root);

            return root;
        }

        private void DrawElements(VisualElement root)
        {
            var titleEl = uitk.CreateTitle(scriptableObject.Localize(FcuLocKey.label_script_generator));
            titleEl.AddSectionResetMenu(() =>
            {
                var d = FcuDefaults.ScriptGeneratorSettings;
                var s = monoBeh.Settings.ScriptGeneratorSettings;
                s.SerializationMode = d.SerializationMode;
                s.Namespace = d.Namespace;
                s.BaseClass = d.BaseClass;
                s.OutputPath = d.OutputPath;
                s.FieldNameMaxLenght = d.FieldNameMaxLenght;
                s.MethodNameMaxLenght = d.MethodNameMaxLenght;
                s.ClassNameMaxLenght = d.ClassNameMaxLenght;
                scriptableObject.RefreshTabs();
            });
            root.Add(titleEl);
            root.Add(uitk.Space10());

            var formContainer = uitk.CreateSectionPanel();
            root.Add(formContainer);

            var settings = monoBeh.Settings.ScriptGeneratorSettings;

            var serializationModeField = uitk.EnumField(scriptableObject.Localize(FcuLocKey.label_serialization_mode), settings.SerializationMode);
            serializationModeField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_serialization_mode);
            serializationModeField.RegisterValueChangedCallback(evt => settings.SerializationMode = (FieldSerializationMode)evt.newValue);
            serializationModeField.AddResetMenu(settings, FcuDefaults.ScriptGeneratorSettings, s => s.SerializationMode, (s, v) => s.SerializationMode = v);
            formContainer.Add(serializationModeField);
            formContainer.Add(uitk.ItemSeparator());

            var namespaceField = uitk.TextField(scriptableObject.Localize(FcuLocKey.label_namespace));
            namespaceField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_namespace);
            namespaceField.value = settings.Namespace;
            namespaceField.RegisterValueChangedCallback(evt => settings.Namespace = evt.newValue);
            namespaceField.AddResetMenu(settings, FcuDefaults.ScriptGeneratorSettings, s => s.Namespace, (s, v) => s.Namespace = v);
            formContainer.Add(namespaceField);
            formContainer.Add(uitk.ItemSeparator());

            var baseClassField = uitk.TextField(scriptableObject.Localize(FcuLocKey.label_base_class));
            baseClassField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_base_class);
            baseClassField.value = settings.BaseClass;
            baseClassField.RegisterValueChangedCallback(evt => settings.BaseClass = evt.newValue);
            baseClassField.AddResetMenu(settings, FcuDefaults.ScriptGeneratorSettings, s => s.BaseClass, (s, v) => s.BaseClass = v);
            formContainer.Add(baseClassField);
            formContainer.Add(uitk.ItemSeparator());

            var folderPathContainer = uitk.CreateFolderInput(
                label: scriptableObject.Localize(FcuLocKey.label_scripts_output_path),
                tooltip: scriptableObject.Localize(FcuLocKey.tooltip_scripts_output_path),
                initialValue: settings.OutputPath,
                onPathChanged: newValue => settings.OutputPath = newValue,
                onButtonClick: () => EditorUtility.OpenFolderPanel(
                    scriptableObject.Localize(FcuLocKey.label_select_folder),
                    settings.OutputPath,
                    string.Empty),
                buttonTooltip: scriptableObject.Localize(FcuLocKey.tooltip_select_folder));
            folderPathContainer.AddFolderResetMenu(
                () => settings.OutputPath,
                FcuDefaults.ScriptGeneratorSettings.OutputPath,
                v => settings.OutputPath = v);
            formContainer.Add(folderPathContainer);
            formContainer.Add(uitk.ItemSeparator());

            var fieldNameLengthField = uitk.IntegerField(scriptableObject.Localize(FcuLocKey.label_field_name_max_length));
            fieldNameLengthField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_field_name_max_length);
            fieldNameLengthField.value = settings.FieldNameMaxLenght;
            fieldNameLengthField.RegisterValueChangedCallback(evt => settings.FieldNameMaxLenght = evt.newValue);
            fieldNameLengthField.AddResetMenu(settings, FcuDefaults.ScriptGeneratorSettings, s => s.FieldNameMaxLenght, (s, v) => s.FieldNameMaxLenght = v);
            formContainer.Add(fieldNameLengthField);
            formContainer.Add(uitk.ItemSeparator());

            var methodNameLengthField = uitk.IntegerField(scriptableObject.Localize(FcuLocKey.label_method_name_max_length));
            methodNameLengthField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_method_name_max_length);
            methodNameLengthField.value = settings.MethodNameMaxLenght;
            methodNameLengthField.RegisterValueChangedCallback(evt => settings.MethodNameMaxLenght = evt.newValue);
            methodNameLengthField.AddResetMenu(settings, FcuDefaults.ScriptGeneratorSettings, s => s.MethodNameMaxLenght, (s, v) => s.MethodNameMaxLenght = v);
            formContainer.Add(methodNameLengthField);
            formContainer.Add(uitk.ItemSeparator());

            var classNameLengthField = uitk.IntegerField(scriptableObject.Localize(FcuLocKey.label_class_name_max_length));
            classNameLengthField.tooltip = scriptableObject.Localize(FcuLocKey.tooltip_class_name_max_length);
            classNameLengthField.value = settings.ClassNameMaxLenght;
            classNameLengthField.RegisterValueChangedCallback(evt => settings.ClassNameMaxLenght = evt.newValue);
            classNameLengthField.AddResetMenu(settings, FcuDefaults.ScriptGeneratorSettings, s => s.ClassNameMaxLenght, (s, v) => s.ClassNameMaxLenght = v);
            formContainer.Add(classNameLengthField);
            formContainer.Add(uitk.ItemSeparator());

            var buttonsContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    alignItems = Align.FlexStart
                }
            };

            _scanButton = uitk.Button(scriptableObject.Localize(FcuLocKey.scriptgen_button_scan), HandleScan);
            _scanButton.style.flexShrink = 0;
            buttonsContainer.Add(_scanButton);
            buttonsContainer.Add(uitk.Space10());

            _generateButton = uitk.Button(scriptableObject.Localize(FcuLocKey.scriptgen_button_generate), () =>
            {
                if (_selectionContext == null)
                {
                    Debug.LogError("_selectionContext == null");
                    return;
                }

                ApplySelectionToSyncData();
                monoBeh.EditorEventHandlers.GenerateScripts_OnClick(_selectionContext);
            });
            _generateButton.style.flexShrink = 0;
            buttonsContainer.Add(_generateButton);
            buttonsContainer.Add(uitk.Space10());

            _serializeButton = uitk.Button(scriptableObject.Localize(FcuLocKey.scriptgen_button_serialize), () =>
            {
                if (_selectionContext == null)
                {
                    Debug.LogError("_selectionContext == null");
                    return;
                }

                ApplySelectionToSyncData();
                monoBeh.EditorEventHandlers.SerializeObjects_OnClick(_selectionContext);
            });
            _serializeButton.style.flexShrink = 0;
            buttonsContainer.Add(_serializeButton);

            formContainer.Add(uitk.Space10());
            formContainer.Add(buttonsContainer);

            root.Add(uitk.Space10());
            BuildSelectionUI(root);

            UpdateActionButtonsState();
            RefreshSelectionSummary();
        }

        private void BuildSelectionUI(VisualElement parent)
        {
            var selectionContainer = uitk.CreateSectionPanel();
            selectionContainer.name = "selectionContainer";
            selectionContainer.style.flexGrow = 1;
            selectionContainer.style.flexDirection = FlexDirection.Column;
            parent.Add(selectionContainer);

            _selectionInfoLabel = new Label(scriptableObject.Localize(FcuLocKey.scriptgen_message_scan_scene))
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 8
                }
            };
            selectionContainer.Add(_selectionInfoLabel);

            _namingErrorBox = uitk.HelpBox(new HelpBoxData
            {
                Message = scriptableObject.Localize(FcuLocKey.scriptgen_banner_invalid_naming),
                MessageType = MessageType.Error,
                FontSize = 11
            });
            _namingErrorBox.style.display = DisplayStyle.None;
            _namingErrorBox.AddManipulator(new Clickable(NavigateToFirstInvalidField));
            selectionContainer.Add(_namingErrorBox);
            selectionContainer.Add(uitk.Space10());
            selectionContainer.Add(BuildGlobalSearchRow());
            selectionContainer.Add(uitk.Space10());

            _framesScrollView = new ScrollView(ScrollViewMode.Horizontal)
            {
                style =
                {
                    flexGrow = 1,
                    minHeight = 360,
                    borderTopColor = uitk.ColorScheme.OUTLINE,
                    borderTopWidth = 1,
                    paddingTop = 4
                }
            };
            _framesScrollView.contentContainer.style.flexDirection = FlexDirection.Row;
            _framesScrollView.contentContainer.style.alignItems = Align.Stretch;
            selectionContainer.Add(_framesScrollView);

            _framesPlaceholder = new Label(scriptableObject.Localize(FcuLocKey.scriptgen_message_click_scan))
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    flexGrow = 1
                }
            };
            _framesScrollView.Add(_framesPlaceholder);
        }

        private VisualElement BuildGlobalSearchRow()
        {
            var searchRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var label = new Label(scriptableObject.Localize(FcuLocKey.common_placeholder_search))
            {
                style = { marginRight = 6 }
            };

            _globalSearchField = new TextField
            {
                style = { flexGrow = 1 }
            };
            _globalSearchField.RegisterValueChangedCallback(evt =>
            {
                ApplyGlobalSearch(evt.newValue);
            });

            var clearButton = uitk.Button("✕", () =>
            {
                _globalSearchField.value = string.Empty;
            });
            clearButton.style.width = 28;
            clearButton.style.height = 20;
            clearButton.style.flexShrink = 0;
            clearButton.style.flexGrow = 0;

            searchRow.Add(label);
            searchRow.Add(_globalSearchField);
            searchRow.Add(uitk.Space5());
            searchRow.Add(clearButton);

            return searchRow;
        }

        private void HandleScan()
        {
            _selectionContext = monoBeh.ScriptGenerator.CreateSelectionContext();
            _frameViews.Clear();
            _fieldRows.Clear();
            _fieldViews.Clear();

            RebuildFrameList();
            ApplyGlobalSearch(_globalSearchField?.value ?? string.Empty, true);
            RefreshSelectionSummary();
            UpdateActionButtonsState();
        }

        private void RebuildFrameList()
        {
            if (_framesScrollView == null)
            {
                return;
            }

            _framesScrollView.contentContainer.Clear();
            _fieldViews.Clear();

            if (_selectionContext == null || _selectionContext.Frames.Count == 0)
            {
                _framesScrollView.Add(_framesPlaceholder);
                return;
            }

            foreach (var frame in _selectionContext.Frames)
            {
                var view = CreateFrameView(frame);
                _frameViews[GetFrameKey(frame)] = view;
                _framesScrollView.contentContainer.Add(view.Root);
                ApplyFrameFilter(view);
                UpdateFrameCounter(frame);
            }
        }

        private FrameView CreateFrameView(ScriptGeneratorFrameSelection frame)
        {
            string frameKey = GetFrameKey(frame);

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            frame.CustomClassName ??= frame.GetEffectiveClassName();
            frame.SanitizedClassName ??= frame.CustomClassName;

            var toggle = new Toggle
            {
                value = frame.Enabled,
                style = { flexShrink = 0 }
            };
            toggle.tooltip = frame.DisplayName;

            var classNameField = new TextField
            {
                value = frame.CustomClassName ?? frame.DisplayName,
                style = { flexGrow = 1 }
            };
            classNameField.tooltip = ScriptGeneratorNamingRules.GetRulesDescription(
                ScriptGeneratorNameType.Class,
                monoBeh.Settings.ScriptGeneratorSettings);

            var counterLabel = new Label
            {
                style =
                {
                    minWidth = 60,
                    unityTextAlign = TextAnchor.MiddleRight
                }
            };

            header.Add(toggle);
            header.Add(uitk.Space5());
            header.Add(classNameField);
            header.Add(uitk.Space5());
            header.Add(counterLabel);

            var body = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1,
                    flexShrink = 1
                }
            };

            var frameView = new FrameView
            {
                Frame = frame,
                CounterLabel = counterLabel,
                BodyContainer = body,
                FrameToggle = toggle,
                ClassNameField = classNameField,
                ClassErrorLabel = new Label
                {
                    style =
                    {
                        color = uitk.ColorScheme.RED,
                        unityFontStyleAndWeight = FontStyle.Italic,
                        fontSize = 11,
                        marginTop = 2,
                        display = DisplayStyle.None
                    }
                }
            };

            classNameField.RegisterValueChangedCallback(evt =>
            {
                HandleFrameNameChanged(frameView, evt.newValue);
            });

            body.Add(BuildFrameSearchRow(frameView, frameKey));

            frameView.GlobalFilterInfo = new Label
            {
                style =
                {
                    color = uitk.ColorScheme.TEXT_SECOND,
                    fontSize = 10,
                    unityFontStyleAndWeight = FontStyle.Italic,
                    display = DisplayStyle.None,
                    marginTop = 2,
                    marginBottom = 2
                }
            };
            body.Add(frameView.GlobalFilterInfo);
            body.Add(uitk.Space5());

            var fieldScroll = new ScrollView
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    flexBasis = 0,
                    minHeight = 260
                }
            };
            fieldScroll.contentContainer.style.flexDirection = FlexDirection.Column;
            frameView.FieldScroll = fieldScroll;

            foreach (var field in frame.Fields)
            {
                fieldScroll.Add(BuildFieldRow(frameView, field));
            }

            UpdateFrameSelectionState(frameView);

            body.Add(fieldScroll);

            var card = uitk.CreateSectionPanel();
            card.style.marginRight = DAI_UitkConstants.MarginPadding;
            card.style.minWidth = 340;
            card.style.maxWidth = 360;
            card.style.flexShrink = 0;
            card.Add(header);
            card.Add(frameView.ClassErrorLabel);
            card.Add(uitk.Space5());
            card.Add(body);

            frameView.Root = card;

            toggle.RegisterValueChangedCallback(evt =>
            {
                ApplyFrameToggleState(frameView, evt.newValue);
            });
            UpdateFrameNameValidationUI(frameView);
            UpdateGlobalFilterInfo(frameView);

            return frameView;
        }

        private VisualElement BuildFrameSearchRow(FrameView frameView, string frameKey)
        {
            var searchRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var label = new Label(scriptableObject.Localize(FcuLocKey.common_placeholder_search))
            {
                style = { marginRight = 6 }
            };

            var searchField = new TextField
            {
                style = { flexGrow = 1 }
            };

            searchField.RegisterValueChangedCallback(evt =>
            {
                frameView.LocalFilter = evt.newValue ?? string.Empty;
                ApplyFrameFilter(frameView);
            });

            var clearButton = uitk.Button("✕", () =>
            {
                searchField.value = string.Empty;
            });
            clearButton.style.width = 28;
            clearButton.style.height = 20;
            clearButton.style.flexShrink = 0;
            clearButton.style.flexGrow = 0;

            searchRow.Add(label);
            searchRow.Add(searchField);
            searchRow.Add(uitk.Space5());
            searchRow.Add(clearButton);

            frameView.SearchField = searchField;
            return searchRow;
        }

        private VisualElement BuildFieldRow(FrameView frameView, ScriptGeneratorFieldSelection field)
        {
            var frame = frameView.Frame;
            string fieldKey = GetFieldKey(field);

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginBottom = 6,
                    paddingTop = 4,
                    paddingBottom = 4,
                    paddingLeft = 4,
                    paddingRight = 4,
                    borderBottomColor = uitk.ColorScheme.SEPARATOR,
                    borderBottomWidth = 1
                }
            };

            var topRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var iconImage = new Image
            {
                image = ResolveComponentIcon(field.ComponentType),
                tooltip = field.ComponentTypeName,
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width = 18,
                    height = 18,
                    marginRight = 4
                }
            };

            var toggle = new Toggle
            {
                value = field.Enabled,
                style = { flexShrink = 0 }
            };

            var nameField = new TextField
            {
                value = field.CustomName ?? string.Empty,
                style = { flexGrow = 1 }
            };

            topRow.Add(iconImage);
            topRow.Add(uitk.Space5());
            topRow.Add(toggle);
            topRow.Add(uitk.Space5());
            topRow.Add(nameField);

            var errorLabel = new Label
            {
                style =
                {
                    color = uitk.ColorScheme.RED,
                    unityFontStyleAndWeight = FontStyle.Italic,
                    fontSize = 11,
                    display = DisplayStyle.None
                }
            };

            toggle.RegisterValueChangedCallback(evt =>
            {
                if (_fieldViews.TryGetValue(fieldKey, out var view))
                {
                    ApplyFieldEnabledState(frameView, field, view, evt.newValue, false);
                }
            });

            nameField.RegisterValueChangedCallback(evt =>
            {
                HandleFieldNameChanged(row, field, nameField, errorLabel, evt.newValue);
            });

            nameField.SetEnabled(field.Enabled);

            row.Add(topRow);
            row.Add(errorLabel);

            var viewData = new FieldView
            {
                Toggle = toggle,
                NameField = nameField,
                ErrorLabel = errorLabel,
                Row = row
            };

            if (field.HasMethod)
            {
                var methodRow = BuildMethodRow(field, viewData);
                row.Add(methodRow);
            }

            _fieldRows[fieldKey] = row;
            _fieldViews[fieldKey] = viewData;
            UpdateFieldValidationUI(row, nameField, errorLabel, field);

            return row;
        }

        private VisualElement BuildMethodRow(ScriptGeneratorFieldSelection field, FieldView view)
        {
            var method = field.MethodSelection;
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginLeft = 20,
                    marginTop = 4
                }
            };

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var label = new Label(scriptableObject.Localize(FcuLocKey.scriptgen_label_method_name))
            {
                style =
                {
                    marginRight = 6,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    minWidth = 110
                }
            };

            var methodField = new TextField
            {
                value = method?.CustomName ?? string.Empty,
                style = { flexGrow = 1 }
            };
            methodField.tooltip = ScriptGeneratorNamingRules.GetRulesDescription(
                ScriptGeneratorNameType.Method,
                monoBeh.Settings.ScriptGeneratorSettings);

            var errorLabel = new Label
            {
                style =
                {
                    color = uitk.ColorScheme.RED,
                    unityFontStyleAndWeight = FontStyle.Italic,
                    fontSize = 11,
                    marginTop = 2,
                    display = DisplayStyle.None
                }
            };

            header.Add(label);
            header.Add(methodField);

            container.Add(header);
            container.Add(errorLabel);

            view.MethodRow = container;
            view.MethodField = methodField;
            view.MethodErrorLabel = errorLabel;

            methodField.SetEnabled(field.Enabled);
            methodField.RegisterValueChangedCallback(evt =>
            {
                HandleMethodNameChanged(field, view, evt.newValue);
            });

            UpdateMethodValidationUI(view, field);

            return container;
        }

        private void HandleFieldNameChanged(
            VisualElement row,
            ScriptGeneratorFieldSelection field,
            TextField input,
            Label messageLabel,
            string newValue)
        {
            field.CustomName = newValue;

            var validation = ScriptGeneratorNamingRules.Validate(
                newValue,
                ScriptGeneratorNameType.Field,
                monoBeh.Settings.ScriptGeneratorSettings);

            field.SanitizedCustomName = validation.SanitizedValue;
            field.IsCustomNameValid = validation.IsValid;
            field.ValidationMessage = validation.Message;

            UpdateFieldValidationUI(row, input, messageLabel, field);
            RefreshSelectionSummary();
            UpdateActionButtonsState();
        }

        private void HandleFrameNameChanged(FrameView frameView, string newValue)
        {
            if (frameView?.Frame == null)
            {
                return;
            }

            var frame = frameView.Frame;
            frame.CustomClassName = newValue;

            var validation = ScriptGeneratorNamingRules.Validate(
                newValue,
                ScriptGeneratorNameType.Class,
                monoBeh.Settings.ScriptGeneratorSettings);

            frame.SanitizedClassName = validation.SanitizedValue;
            frame.IsClassNameValid = validation.IsValid;
            frame.ClassValidationMessage = validation.Message;

            UpdateFrameNameValidationUI(frameView);
            RefreshSelectionSummary();
            UpdateActionButtonsState();
        }

        private void UpdateFrameNameValidationUI(FrameView frameView)
        {
            if (frameView?.Frame == null)
            {
                return;
            }

            bool isValid = frameView.Frame.IsClassNameValid &&
                           frameView.Frame.GetEffectiveClassName().IsEmpty() == false;

            if (frameView.ClassErrorLabel != null)
            {
                if (isValid || frameView.Frame.ClassValidationMessage.IsEmpty())
                {
                    frameView.ClassErrorLabel.style.display = DisplayStyle.None;
                    frameView.ClassErrorLabel.text = string.Empty;
                }
                else
                {
                    frameView.ClassErrorLabel.style.display = DisplayStyle.Flex;
                    frameView.ClassErrorLabel.text = frameView.Frame.ClassValidationMessage;
                }
            }
        }

        private void HandleMethodNameChanged(
            ScriptGeneratorFieldSelection field,
            FieldView view,
            string newValue)
        {
            var method = field?.MethodSelection;
            if (method == null)
            {
                return;
            }

            method.CustomName = newValue;

            var validation = ScriptGeneratorNamingRules.Validate(
                newValue,
                ScriptGeneratorNameType.Method,
                monoBeh.Settings.ScriptGeneratorSettings);

            method.SanitizedCustomName = validation.SanitizedValue;
            method.IsCustomNameValid = validation.IsValid;
            method.ValidationMessage = validation.Message;

            UpdateMethodValidationUI(view, field);
            RefreshSelectionSummary();
            UpdateActionButtonsState();
        }


        private void UpdateMethodValidationUI(FieldView view, ScriptGeneratorFieldSelection field)
        {
            var method = field?.MethodSelection;
            if (view == null || method == null)
            {
                return;
            }

            bool isValid = method.IsCustomNameValid &&
                           method.GetEffectiveName().IsEmpty() == false;

            if (view.MethodErrorLabel != null)
            {
                if (isValid || method.ValidationMessage.IsEmpty())
                {
                    view.MethodErrorLabel.style.display = DisplayStyle.None;
                    view.MethodErrorLabel.text = string.Empty;
                }
                else
                {
                    view.MethodErrorLabel.style.display = DisplayStyle.Flex;
                    view.MethodErrorLabel.text = method.ValidationMessage;
                }
            }
        }

        private void ApplySelectionToSyncData()
        {
            if (_selectionContext == null)
            {
                return;
            }

            foreach (var frame in _selectionContext.Frames)
            {
                SyncFrameSelection(frame);
            }
        }

        private void SyncFrameSelection(ScriptGeneratorFrameSelection frame)
        {
            if (frame == null)
            {
                return;
            }

            string className = frame.GetEffectiveClassName();
            var rootFrame = frame.RootFrame;

            if (rootFrame != null && className.IsEmpty() == false)
            {
                rootFrame.Names ??= new FNames();
                rootFrame.Names.ClassName = className;
            }

            foreach (var field in frame.Fields)
            {
                SyncFieldSelection(field);
            }
        }

        private void SyncFieldSelection(ScriptGeneratorFieldSelection field)
        {
            if (field == null)
            {
                return;
            }

            var data = field.SyncHelper?.Data;
            if (data == null)
            {
                return;
            }

            string fieldName = field.GetEffectiveName();
            if (fieldName.IsEmpty() == false)
            {
                data.Names ??= new FNames();
                data.Names.FieldName = fieldName;
            }

            var method = field.MethodSelection;
            if (method != null)
            {
                string methodName = method.GetEffectiveName();
                if (methodName.IsEmpty() == false)
                {
                    data.Names ??= new FNames();
                    data.Names.MethodName = methodName;
                }
            }
        }

        private void ApplyFrameToggleState(FrameView frameView, bool enableAll)
        {
            if (frameView == null || frameView.Frame == null)
            {
                return;
            }

            foreach (var field in frameView.Frame.Fields)
            {
                string key = GetFieldKey(field);
                _fieldViews.TryGetValue(key, out var view);
                ApplyFieldEnabledState(frameView, field, view, enableAll, true, updateFrameState: false);
            }

            UpdateFrameSelectionState(frameView);
        }

        private void ApplyFieldEnabledState(
            FrameView frameView,
            ScriptGeneratorFieldSelection field,
            FieldView view,
            bool enabled,
            bool forceToggleUpdate,
            bool updateFrameState = true)
        {
            if (field == null || frameView == null)
            {
                return;
            }

            field.Enabled = enabled;

            if (forceToggleUpdate && view?.Toggle != null)
            {
                view.Toggle.SetValueWithoutNotify(enabled);
            }

            if (view?.NameField != null)
            {
                view.NameField.SetEnabled(enabled);
            }

            if (view?.MethodField != null)
            {
                view.MethodField.SetEnabled(enabled);
            }

            UpdateFieldValidationUI(view?.Row, view?.NameField, view?.ErrorLabel, field);
            UpdateMethodValidationUI(view, field);

            if (updateFrameState)
            {
                UpdateFrameSelectionState(frameView);
            }
        }

        private void UpdateFrameSelectionState(FrameView frameView)
        {
            if (frameView == null || frameView.Frame == null)
            {
                return;
            }

            int totalFields = frameView.Frame.Fields.Count;
            int enabledFields = frameView.Frame.Fields.Count(field => field.Enabled);

            bool anySelected = enabledFields > 0;
            bool allSelected = totalFields > 0 && enabledFields == totalFields;

            frameView.Frame.Enabled = anySelected;

            if (frameView.FrameToggle != null)
            {
                frameView.FrameToggle.showMixedValue = anySelected && anySelected != allSelected;
                frameView.FrameToggle.SetValueWithoutNotify(allSelected);
            }

            UpdateFrameCounter(frameView.Frame);
            RefreshSelectionSummary();
            UpdateActionButtonsState();
        }

        private void UpdateFieldValidationUI(
            VisualElement row,
            TextField input,
            Label messageLabel,
            ScriptGeneratorFieldSelection field)
        {
            bool isValid = field.IsCustomNameValid && field.SanitizedCustomName.IsEmpty() == false;

            if (messageLabel != null)
            {
                if (isValid || field.ValidationMessage.IsEmpty())
                {
                    messageLabel.style.display = DisplayStyle.None;
                    messageLabel.text = string.Empty;
                }
                else
                {
                    messageLabel.style.display = DisplayStyle.Flex;
                    messageLabel.text = field.ValidationMessage;
                }
            }

            if (row != null)
            {
                if (isValid)
                {
                    row.style.backgroundColor = StyleKeyword.Null;
                }
                else
                {
                    Color overlay = uitk.ColorScheme.RED;
                    overlay.a = 0.2f;
                    row.style.backgroundColor = new StyleColor(overlay);
                }
            }
        }

        private void ApplyGlobalSearch(string rawValue, bool force = false)
        {
            _globalSearchRawValue = rawValue ?? string.Empty;
            string normalized = NormalizeFilter(_globalSearchRawValue);

            if (!force && _globalSearchValue == normalized)
            {
                return;
            }

            _globalSearchValue = normalized;

            foreach (var view in _frameViews.Values)
            {
                UpdateGlobalFilterInfo(view);
                ApplyFrameFilter(view);
            }
        }

        private static string NormalizeFilter(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        private void ApplyFrameFilter(FrameView frameView)
        {
            string localFilter = NormalizeFilter(frameView.LocalFilter);

            foreach (var field in frameView.Frame.Fields)
            {
                string key = GetFieldKey(field);
                if (!_fieldRows.TryGetValue(key, out var row))
                {
                    continue;
                }

                bool matchesGlobal = _globalSearchValue.IsEmpty() || (field.SearchSignature?.Contains(_globalSearchValue) ?? false);
                bool matchesLocal = localFilter.IsEmpty() || (field.SearchSignature?.Contains(localFilter) ?? false);
                bool matches = matchesGlobal && matchesLocal;

                row.style.display = matches ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void UpdateGlobalFilterInfo(FrameView view)
        {
            if (view.GlobalFilterInfo == null)
            {
                return;
            }

            if (_globalSearchValue.IsEmpty())
            {
                view.GlobalFilterInfo.style.display = DisplayStyle.None;
                view.GlobalFilterInfo.text = string.Empty;
            }
            else
            {
                view.GlobalFilterInfo.style.display = DisplayStyle.Flex;
                view.GlobalFilterInfo.text = scriptableObject.Localize(FcuLocKey.scriptgen_label_global_filter, _globalSearchRawValue);
            }
        }

        private void UpdateFrameCounter(ScriptGeneratorFrameSelection frame)
        {
            string key = GetFrameKey(frame);
            if (!_frameViews.TryGetValue(key, out var view) || view.CounterLabel == null)
            {
                return;
            }

            int selected = frame.Fields.Count(field => field.Enabled);

            view.CounterLabel.text = $"{selected}/{frame.Fields.Count}";
        }

        private IEnumerable<ScriptGeneratorFieldSelection> EnumerateSelectedFields()
        {
            if (_selectionContext == null)
            {
                return Enumerable.Empty<ScriptGeneratorFieldSelection>();
            }

            return _selectionContext
                .GetEnabledFrames()
                .SelectMany(frame => frame.EnabledFields);
        }

        private bool HasInvalidSelections()
        {
            bool invalidFields = EnumerateSelectedFields().Any(IsFieldNameInvalid);
            bool invalidMethods = EnumerateSelectedFields().Any(IsMethodNameInvalid);
            bool invalidFrames = _selectionContext != null &&
                                 _selectionContext.GetEnabledFrames().Any(IsFrameNameInvalid);

            return invalidFields || invalidMethods || invalidFrames;
        }

        private static bool IsFieldNameInvalid(ScriptGeneratorFieldSelection field)
        {
            return field == null ||
                   field.IsCustomNameValid == false ||
                   field.GetEffectiveName().IsEmpty();
        }

        private static bool IsMethodNameInvalid(ScriptGeneratorFieldSelection field)
        {
            var method = field?.MethodSelection;
            if (method == null)
            {
                return false;
            }

            return method.IsCustomNameValid == false ||
                   method.GetEffectiveName().IsEmpty();
        }

        private static bool IsFrameNameInvalid(ScriptGeneratorFrameSelection frame)
        {
            if (frame == null)
            {
                return false;
            }

            return frame.IsClassNameValid == false ||
                   frame.GetEffectiveClassName().IsEmpty();
        }

        private void UpdateActionButtonsState()
        {
            bool hasData = _selectionContext != null && _selectionContext.Frames.Count > 0;
            bool hasFields = EnumerateSelectedFields().Any();
            bool valid = !HasInvalidSelections();
            bool canExecute = hasData && hasFields && valid;

            _generateButton?.SetEnabled(canExecute);
            _serializeButton?.SetEnabled(canExecute);
        }

        private void RefreshSelectionSummary()
        {
            if (_selectionInfoLabel == null)
            {
                return;
            }

            if (_selectionContext == null || _selectionContext.Frames.Count == 0)
            {
                _selectionInfoLabel.text = scriptableObject.Localize(FcuLocKey.scriptgen_message_scan_scene);
                if (_namingErrorBox != null)
                {
                    _namingErrorBox.style.display = DisplayStyle.None;
                }
                return;
            }

            int frameCount = _selectionContext.Frames.Count;
            int enabledFrames = _selectionContext.GetEnabledFrames().Count();
            int totalFields = _selectionContext.Frames.Sum(frame => frame.Fields.Count);
            int selectedFields = EnumerateSelectedFields().Count();

            bool hasInvalidFields = HasInvalidSelections();

            string info = scriptableObject.Localize(FcuLocKey.scriptgen_summary_counts, enabledFrames, frameCount, selectedFields, totalFields);

            if (hasInvalidFields)
            {
                info = $"{info} \u2022 {scriptableObject.Localize(FcuLocKey.scriptgen_summary_invalid_hint)}";
            }

            _selectionInfoLabel.text = info;

            if (_namingErrorBox != null)
            {
                _namingErrorBox.style.display = hasInvalidFields ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static string GetFrameKey(ScriptGeneratorFrameSelection frame)
        {
            if (frame.FrameId.IsEmpty() == false)
            {
                return frame.FrameId;
            }

            return frame.DisplayName;
        }

        private static string GetFieldKey(ScriptGeneratorFieldSelection field)
        {
            if (field.FieldId.IsEmpty() == false)
            {
                return field.FieldId;
            }

            if (field.SyncHelper != null)
            {
                return field.SyncHelper.GetInstanceID().ToString();
            }

            return field.RuntimeId;
        }

        private IEnumerable<InvalidFieldReference> EnumerateInvalidFieldRows()
        {
            if (_selectionContext == null)
            {
                yield break;
            }

            foreach (var frame in _selectionContext.Frames)
            {
                string frameKey = GetFrameKey(frame);
                if (!_frameViews.TryGetValue(frameKey, out var frameView))
                {
                    continue;
                }

                if (IsFrameNameInvalid(frame) && frameView.ClassNameField != null)
                {
                    yield return new InvalidFieldReference(frameView, frameView.ClassNameField);
                }

                foreach (var field in frame.Fields)
                {
                    string fieldKey = GetFieldKey(field);
                    _fieldRows.TryGetValue(fieldKey, out var row);
                    _fieldViews.TryGetValue(fieldKey, out var view);

                    if (IsFieldNameInvalid(field) && row != null)
                    {
                        yield return new InvalidFieldReference(frameView, row);
                    }

                    if (IsMethodNameInvalid(field) && view?.MethodRow != null)
                    {
                        yield return new InvalidFieldReference(frameView, view.MethodRow);
                    }
                }
            }
        }

        private void NavigateToFirstInvalidField()
        {
            _invalidFieldCache.Clear();
            _invalidFieldCache.AddRange(EnumerateInvalidFieldRows());

            if (_invalidFieldCache.Count == 0)
            {
                return;
            }

            var target = _invalidFieldCache[0];
            FocusOnFieldRow(target.FrameView, target.Row);
        }

        private void FocusOnFieldRow(FrameView frameView, VisualElement row)
        {
            if (frameView == null || row == null)
            {
                return;
            }

            _framesScrollView?.ScrollTo(frameView.Root);
            frameView.FieldScroll?.ScrollTo(row);

            row.schedule.Execute(() =>
            {
                row.Focus();
            });
        }

        private Texture2D ResolveComponentIcon(Type componentType)
        {
            componentType ??= typeof(GameObject);

            if (_componentIconCache.TryGetValue(componentType, out var cached) && cached != null)
            {
                return cached;
            }

            Texture2D icon = null;

#if UNITY_EDITOR
            GUIContent content = EditorGUIUtility.ObjectContent(null, componentType);
            icon = content?.image as Texture2D;
#endif

            if (icon == null)
            {
                icon = EditorGUIUtility.IconContent("GameObject Icon")?.image as Texture2D;
            }

            _componentIconCache[componentType] = icon;
            return icon;
        }
    }
}