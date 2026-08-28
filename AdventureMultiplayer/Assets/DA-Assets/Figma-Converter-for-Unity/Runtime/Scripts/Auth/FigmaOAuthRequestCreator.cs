#if UNITY_EDITOR
using DA_Assets.UCC;
using DA_Assets.UCC.Model;
using System;
using UnityEngine;

namespace DA_Assets.FCU
{
    public static class FigmaOAuthRequestCreator
    {
        public static DARequest CreateTokenRequest(
            string code,
            string redirectUri,
            string clientId,
            string clientSecret,
            FigmaEnvironment env = FigmaEnvironment.Figma)
        {
            string tokenUrl = $"{FigmaEndpoints.GetApiBaseUrl(env)}/v1/oauth/token";

            DARequest request = new DARequest
            {
                Query = tokenUrl,
                RequestType = RequestType.Post,
                WWWForm = new WWWForm()
            };

            request.WWWForm.AddField("grant_type", "authorization_code");
            request.WWWForm.AddField("code", code);
            request.WWWForm.AddField("redirect_uri", redirectUri);

            string credentials = $"{clientId}:{clientSecret}";
            string encodedCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));

            request.RequestHeader = new RequestHeader
            {
                Name = "Authorization",
                Value = $"Basic {encodedCredentials}"
            };

            return request;
        }
    }
}
#endif
