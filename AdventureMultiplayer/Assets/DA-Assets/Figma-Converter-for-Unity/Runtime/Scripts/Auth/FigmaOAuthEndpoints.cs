#if UNITY_EDITOR
using DA_Assets.UCC;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;

namespace DA_Assets.FCU
{
    public static class FigmaOAuthEndpoints
    {
        public static string GetOAuthUrl(FigmaScope scopes, FigmaEnvironment env) =>
            $"{FigmaEndpoints.GetWebBaseUrl(env)}/oauth?client_id={{0}}&redirect_uri={{1}}&scope={GetScopesAsString(scopes)}&state={{2}}&response_type=code";

        public static string GetScopesAsString(FigmaScope scopes)
        {
            var selectedScopes = new List<string>();

            foreach (FigmaScope scope in Enum.GetValues(typeof(FigmaScope)))
            {
                if (scopes.HasFlag(scope))
                {
                    switch (scope)
                    {
                        case FigmaScope.CurrentUserRead:
                            selectedScopes.Add("current_user:read");
                            break;
                        case FigmaScope.FileContentRead:
                            selectedScopes.Add("file_content:read");
                            break;
                        case FigmaScope.LibraryContentRead:
                            selectedScopes.Add("library_content:read");
                            break;
                        case FigmaScope.LibraryAnalyticsRead:
                            selectedScopes.Add("library_analytics:read");
                            break;
                        case FigmaScope.LibraryAssetsRead:
                            selectedScopes.Add("library_assets:read");
                            break;
                        case FigmaScope.OrgActivityLogRead:
                            selectedScopes.Add("org:activity_log_read");
                            break;
                        case FigmaScope.OrgDiscoveryRead:
                            selectedScopes.Add("org:discovery_read");
                            break;
                        case FigmaScope.ProjectsRead:
                            selectedScopes.Add("projects:read");
                            break;
                        case FigmaScope.SelectionsRead:
                            selectedScopes.Add("selections:read");
                            break;
                        case FigmaScope.TeamLibraryContentRead:
                            selectedScopes.Add("team_library_content:read");
                            break;
                        case FigmaScope.WebhooksRead:
                            selectedScopes.Add("webhooks:read");
                            break;
                        case FigmaScope.WebhooksWrite:
                            selectedScopes.Add("webhooks:write");
                            break;
                        case FigmaScope.FileCommentsRead:
                            selectedScopes.Add("file_comments:read");
                            break;
                        case FigmaScope.FileCommentsWrite:
                            selectedScopes.Add("file_comments:write");
                            break;
                        case FigmaScope.FileDevResourcesRead:
                            selectedScopes.Add("file_dev_resources:read");
                            break;
                        case FigmaScope.FileDevResourcesWrite:
                            selectedScopes.Add("file_dev_resources:write");
                            break;
                        case FigmaScope.FileMetadataRead:
                            selectedScopes.Add("file_metadata:read");
                            break;
                        case FigmaScope.FileVariablesRead:
                            selectedScopes.Add("file_variables:read");
                            break;
                        case FigmaScope.FileVariablesWrite:
                            selectedScopes.Add("file_variables:write");
                            break;
                        case FigmaScope.FileVersionsRead:
                            selectedScopes.Add("file_versions:read");
                            break;
                    }
                }
            }

            return string.Join("%20", selectedScopes);
        }
    }
}
#endif
