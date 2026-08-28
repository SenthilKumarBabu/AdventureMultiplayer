#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using DA_Assets.UCC.Model;

namespace DA_Assets.UCC
{
    public class RequestCreator
    {
        public static DARequest CreateImageLinksRequest(string projectUrl, string format, float scale, IEnumerable<string> chunk, RequestHeader requestHeader, FigmaEnvironment env = FigmaEnvironment.Figma)
        {
            string query = CreateImagesQuery(
                    chunk,
                    projectUrl,
                    format,
                    scale,
                    env);

            DARequest request = new DARequest
            {
                Query = query,
                RequestType = RequestType.Get,
                RequestHeader = requestHeader
            };

            return request;
        }

        public static string CreateImagesQuery(
            IEnumerable<string> chunk,
            string projectId,
            string extension,
            float scale,
            FigmaEnvironment env = FigmaEnvironment.Figma)
        {
            string joinedIds = string.Join(",", chunk);

            if (string.IsNullOrWhiteSpace(joinedIds))
                return null;

            if (joinedIds[0] == ',')
                joinedIds = joinedIds.Remove(0, 1);

            string baseUrl = FigmaEndpoints.GetApiBaseUrl(env);
            string query = $"{baseUrl}/v1/images/{projectId}?ids={joinedIds}&format={extension}&scale={scale.ToString(CultureInfo.InvariantCulture)}";
            return query;
        }

        public static DARequest CreateProjectRequest(RequestHeader requestHeader, string projectId, int frameListDepth, FigmaEnvironment env = FigmaEnvironment.Figma)
        {
            string baseUrl = FigmaEndpoints.GetApiBaseUrl(env);
            string query = string.Format("{0}/v1/files/{1}?depth={2}&plugin_data=shared", baseUrl, projectId, frameListDepth);

            DARequest request = new DARequest
            {
                Name = RequestName.Project,
                Query = query,
                RequestType = RequestType.Get,
                RequestHeader = requestHeader
            };

            return request;
        }

        public static DARequest CreateNodeRequest(RequestHeader requestHeader, string projectId, string nodeIds, FigmaEnvironment env = FigmaEnvironment.Figma)
        {
            string baseUrl = FigmaEndpoints.GetApiBaseUrl(env);
            string query = string.Format("{0}/v1/files/{1}/nodes?ids={2}&geometry=paths&plugin_data=shared", baseUrl, projectId, nodeIds);

            DARequest request = new DARequest
            {
                Query = query,
                RequestType = RequestType.Get,
                RequestHeader = requestHeader
            };

            return request;
        }

        public static DARequest CreateFileStructRequest(RequestHeader requestHeader, string projectId, int depth, FigmaEnvironment env = FigmaEnvironment.Figma)
        {
            string baseUrl = FigmaEndpoints.GetApiBaseUrl(env);
            string query = string.Format("{0}/v1/files/{1}?depth={2}", baseUrl, projectId, depth);

            DARequest request = new DARequest
            {
                Query = query,
                RequestType = RequestType.Get,
                RequestHeader = requestHeader
            };

            return request;
        }
    }
}
#endif