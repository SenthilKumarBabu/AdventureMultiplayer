using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UCC;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.FCU
{
    internal class AuthTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, ConverterBase>
    {
        private VisualElement root;
        private Authorizer Authorizer => ((FigmaConverterUnity)monoBeh).Authorizer;
        private AuthSettings CurrentAuthSettings => ((FigmaConverterUnity)monoBeh).AuthSettings;
        private static readonly AuthSettings DefaultAuthSettings = new AuthSettings();

        public override void OnLink()
        {
            base.OnLink();
            Authorizer.OnAuthUpdated += Rebuild;
            _ = Authorizer.Init();
        }

        public VisualElement Draw()
        {
            root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);
            BuildUI(root);
            return root;
        }

        private void Rebuild()
        {
            if (root == null)
            {
                return;
            }

            root.Clear();
            BuildUI(root);
        }

        private void BuildUI(VisualElement parent)
        {
            parent.Add(uitk.CreateTitle(scriptableObject.Localize(FcuLocKey.label_figma_auth)));
            parent.Add(uitk.Space10());

            DrawAuthenticationPanel(parent);
            parent.Add(uitk.Space10());
            DrawOAuthConfigPanel(parent);
        }

        private void DrawAuthenticationPanel(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            if (Authorizer.Options.IsEmpty() == false)
            {
                List<string> options = Authorizer.Options.Select(o => o.text).ToList();
                var sessionDropdown = uitk.DropdownField(scriptableObject.Localize(FcuLocKey.label_recent_sessions), null);
                sessionDropdown.choices = options;
                sessionDropdown.index = Authorizer.SelectedTableIndex;

                sessionDropdown.RegisterValueChangedCallback(evt =>
                {
                    int selectedIndex = options.IndexOf(evt.newValue);
                    if (selectedIndex != -1)
                    {
                        Authorizer.SelectedTableIndex = selectedIndex;
                        Authorizer.CurrentSession = Authorizer.RecentSessions[selectedIndex];
                    }
                });

                panel.Add(sessionDropdown);
                panel.Add(uitk.ItemSeparator());
            }

            {
                VisualElement tokenInputContainer = new VisualElement();
                tokenInputContainer.style.flexDirection = FlexDirection.Row;
                tokenInputContainer.style.alignItems = Align.Center;
                panel.Add(tokenInputContainer);

                try
                {
                    TextField tokenField = uitk.TextField(scriptableObject.Localize(FcuLocKey.label_access_token));
                    tokenField.value = Authorizer.Token;

                    try
                    {
                        tokenField.isPasswordField = true;
                    }
                    catch (Exception ex0)
                    {
                        Debug.LogException(ex0);
                    }

                    tokenField.style.flexGrow = 1;
                    tokenField.RegisterValueChangedCallback(evt =>
                    {
                        FigmaSessionItem session = Authorizer.CurrentSession;
                        AuthResult authResult = session.AuthResult;
                        authResult.AccessToken = evt.newValue;
                        session.AuthResult = authResult;
                        Authorizer.CurrentSession = session;
                    });
                    tokenInputContainer.Add(tokenField);
                }
                catch (System.Exception ex1)
                {
                    Debug.LogException(ex1);
                    Debug.LogError($"{uitk} | {monoBeh} | {Authorizer} | {Authorizer?.Token}");
                    Debug.LogError($"Please send these errors to the developer.");
                }

                VisualElement signInTokenButton = uitk.Button(scriptableObject.Localize(FcuLocKey.auth_button_sign_in_token), () =>
                {
                    Authorizer.ManualAuthCts?.Cancel();
                    Authorizer.ManualAuthCts?.Dispose();
                    Authorizer.ManualAuthCts = new CancellationTokenSource();
                    _ = Authorizer.AddNew(new AuthResult
                    {
                        AccessToken = Authorizer.Token
                    }, AuthType.Manual, Authorizer.ManualAuthCts.Token);
                });

                signInTokenButton.style.flexGrow = 0;
                signInTokenButton.style.marginLeft = DAI_UitkConstants.SpacingXXS;
                signInTokenButton.style.maxHeight = 18;
                signInTokenButton.style.marginTop = 1;
                UIHelpers.SetRadius(signInTokenButton, DAI_UitkConstants.CornerRadiusSmall);

                tokenInputContainer.Add(signInTokenButton);
            }

            panel.Add(uitk.ItemSeparator());
            panel.Add(uitk.Space5());

            VisualElement buttonsContainer = new VisualElement();
            buttonsContainer.style.flexDirection = FlexDirection.Row;
            buttonsContainer.style.justifyContent = Justify.SpaceBetween;
     
            panel.Add(buttonsContainer);

            VisualElement signInWebButton = uitk.Button(scriptableObject.Localize(FcuLocKey.auth_button_sign_in_web), Authorizer.Auth);
            buttonsContainer.Add(signInWebButton);

            buttonsContainer.Add(uitk.Space10());

            VisualElement deleteSessionsButton = uitk.Button(scriptableObject.Localize(FcuLocKey.auth_button_delete_sessions), async () =>
            {
                EditorPrefs.DeleteKey(CurrentAuthSettings.SessionsPrefsKey);
                await Authorizer.Init();
                Rebuild();
            });
            buttonsContainer.Add(deleteSessionsButton);
        }

        private void DrawOAuthConfigPanel(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label oauthLabel = new Label(scriptableObject.Localize(FcuLocKey.label_oauth_app_credentials));
            oauthLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(oauthLabel);
            panel.Add(uitk.ItemSeparator());

            AuthSettings authSettings = CurrentAuthSettings;

            EnumFlagsField scopesField = uitk.EnumFlagsField(scriptableObject.Localize(FcuLocKey.label_oauth_scopes), authSettings.Scopes);
            scopesField.RegisterValueChangedCallback(evt =>
            {
                authSettings.Scopes = (FigmaScope)evt.newValue;
                EditorUtility.SetDirty(monoBeh);
            });
            scopesField.AddResetMenu(
                authSettings, DefaultAuthSettings,
                s => s.Scopes,
                (s, v) => { s.Scopes = v; EditorUtility.SetDirty(monoBeh); });
            panel.Add(scopesField);
            panel.Add(uitk.ItemSeparator());

            TextField redirectUriField = uitk.TextField(scriptableObject.Localize(FcuLocKey.label_redirect_uri));
            redirectUriField.value = authSettings.RedirectUri;
            redirectUriField.RegisterValueChangedCallback(evt =>
            {
                authSettings.RedirectUri = evt.newValue;
                EditorUtility.SetDirty(monoBeh);
            });
            redirectUriField.AddPopupResetMenu(
                () => authSettings.RedirectUri,
                AuthSettings.DefaultRedirectUri,
                v => { authSettings.RedirectUri = v; EditorUtility.SetDirty(monoBeh); });
            panel.Add(redirectUriField);
            panel.Add(uitk.ItemSeparator());

            TextField clientIdField = uitk.TextField(scriptableObject.Localize(FcuLocKey.label_client_id));
            clientIdField.value = authSettings.ClientId;
            clientIdField.RegisterValueChangedCallback(evt =>
            {
                authSettings.ClientId = evt.newValue;
                EditorUtility.SetDirty(monoBeh);
            });
            clientIdField.AddPopupResetMenu(
                () => authSettings.ClientId,
                AuthSettings.DefaultClientId,
                v => { authSettings.ClientId = v; EditorUtility.SetDirty(monoBeh); });
            panel.Add(clientIdField);
            panel.Add(uitk.ItemSeparator());

            TextField clientSecretField = uitk.TextField(scriptableObject.Localize(FcuLocKey.label_client_secret));
            clientSecretField.value = authSettings.ClientSecret;
            clientSecretField.isPasswordField = true;
            clientSecretField.RegisterValueChangedCallback(evt =>
            {
                authSettings.ClientSecret = evt.newValue;
                EditorUtility.SetDirty(monoBeh);
            });
            clientSecretField.AddPopupResetMenu(
                () => authSettings.ClientSecret,
                AuthSettings.DefaultClientSecret,
                v => { authSettings.ClientSecret = v; EditorUtility.SetDirty(monoBeh); });
            panel.Add(clientSecretField);
        }
    }
}
