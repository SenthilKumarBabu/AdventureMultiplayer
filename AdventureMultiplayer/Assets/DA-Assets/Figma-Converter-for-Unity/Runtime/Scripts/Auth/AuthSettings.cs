#if UNITY_EDITOR
using System;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public class AuthSettings
    {
        public const string DefaultClientId = "LaB1ONuPoY7QCdfshDbQbT";
        public const string DefaultClientSecret = "E9PblceydtAyE7Onhg5FHLmnvingDp";
        public const string DefaultRedirectUri = "http://localhost:1923/";
        public const string DefaultSessionsPrefsKey = "FigmaSessions";
        public const int DefaultSessionsLimit = 10;

        public const FigmaScope DefaultScopes =
            FigmaScope.CurrentUserRead |
            FigmaScope.FileContentRead |
            FigmaScope.LibraryContentRead;

        [SerializeField] string clientId = DefaultClientId;
        [SerializeField] string clientSecret = DefaultClientSecret;
        [SerializeField] string redirectUri = DefaultRedirectUri;
        [SerializeField] FigmaScope scopes = DefaultScopes;
        [SerializeField] string sessionsPrefsKey = DefaultSessionsPrefsKey;
        [SerializeField] int sessionsLimit = DefaultSessionsLimit;

        public string     ClientId     { get => clientId;     set => clientId = value; }
        public string     ClientSecret { get => clientSecret; set => clientSecret = value; }
        public string     RedirectUri  { get => redirectUri;  set => redirectUri = value; }
        public FigmaScope Scopes       { get => scopes;       set => scopes = value; }
        public string SessionsPrefsKey { get => sessionsPrefsKey; set => sessionsPrefsKey = value; }
        public int SessionsLimit { get => sessionsLimit; set => sessionsLimit = value; }
    }
}
#endif
