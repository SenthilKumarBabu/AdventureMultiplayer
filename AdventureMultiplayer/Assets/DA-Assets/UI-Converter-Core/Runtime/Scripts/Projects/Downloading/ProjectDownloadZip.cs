#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    public class ProjectDownloadZip : IProjectDownloadStrategy
    {
        private readonly ConverterBase _monoBeh;

        public ImportMode Mode => ImportMode.Zip;

        public ProjectDownloadZip(ConverterBase monoBeh)
        {
            _monoBeh = monoBeh;
        }

        public async Task DownloadProjectAsync(CancellationToken token)
        {
            string archivePath = _monoBeh.Settings.MainSettings.ZipArchivePath;

            if (string.IsNullOrWhiteSpace(archivePath))
            {
                throw new Exception(FcuLocKey.log_zip_archive_path_not_set.Localize());
            }

            _monoBeh.Events.OnProjectDownloadStart?.Invoke(_monoBeh);
            _monoBeh.InspectorDrawer.SelectableDocument.Childs.Clear();
            _monoBeh.EditorDelegateHolder.StartProgress?.Invoke(_monoBeh, ProgressBarCategory.ProjectDownloading, 0, true);

            Debug.Log(FcuLocKey.log_zip_loading_project.Localize(archivePath));

            var zipData = ExtractArchive(archivePath);
            _monoBeh.CurrentProject.ZipData = zipData;

            var figmaProject = await LoadProjectFromJson(zipData.JsonFilePath);
            _monoBeh.CurrentProject.FigmaProject = figmaProject;
            _monoBeh.CurrentProject.ProjectName = figmaProject.Name;

            _monoBeh.InspectorDrawer.FillSelectableFramesArray(figmaProject.Document);

            ApplyManifestSettings(zipData.ManifestFilePath);

            Debug.Log(FcuLocKey.log_zip_project_loaded.Localize(figmaProject.Name));
        }

        public async Task<List<Node>> DownloadAllNodes(string[] selectedIds, CancellationToken token)
        {
            List<Node> result = new List<Node>();
            var zipData = _monoBeh.CurrentProject.ZipData;

            string archivePath = _monoBeh.Settings.MainSettings.ZipArchivePath;

            bool needReExtract = string.IsNullOrEmpty(zipData.ExtractedFolderPath)
                || !Directory.Exists(zipData.ExtractedFolderPath)
                || (File.Exists(archivePath) && ComputeArchiveHash(archivePath) != zipData.ArchiveHash);

            if (needReExtract)
            {
                if (string.IsNullOrWhiteSpace(archivePath))
                {
                    throw new Exception(FcuLocKey.log_zip_archive_path_not_set_reload.Localize());
                }

                Debug.Log(FcuLocKey.log_zip_archive_changed.Localize(archivePath));
                zipData = ExtractArchive(archivePath);
                _monoBeh.CurrentProject.ZipData = zipData;
            }

            _monoBeh.EditorDelegateHolder.StartProgress?.Invoke(_monoBeh, ProgressBarCategory.DownloadingNodes, selectedIds.Length, false);

            int loadedCount = 0;
            foreach (string nodeId in selectedIds)
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    DesignProject nodeProject = await LoadNodeJsonAsync(zipData.NodesFolder, nodeId);

                    if (!nodeProject.IsDefault() && !nodeProject.Nodes.IsEmpty())
                    {
                        foreach (var item in nodeProject.Nodes)
                        {
                            if (item.Value.IsDefault())
                                continue;

                            result.Add(item.Value.Document);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(FcuLocKey.log_zip_load_node_failed.Localize(nodeId, ex.Message));
                }
                finally
                {
                    loadedCount++;
                    _monoBeh.EditorDelegateHolder.UpdateProgress?.Invoke(_monoBeh, ProgressBarCategory.DownloadingNodes, loadedCount);
                }
            }

            _monoBeh.EditorDelegateHolder.CompleteProgress?.Invoke(_monoBeh, ProgressBarCategory.DownloadingNodes);
            Debug.Log(FcuLocKey.log_zip_nodes_loaded.Localize(result.Count));

            return result;
        }

        private ZipProjectData ExtractArchive(string zipPath)
        {
            if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
            {
                throw new FileNotFoundException($"ZIP archive not found: {zipPath}");
            }

            string extractPath = Path.Combine(Application.persistentDataPath, _monoBeh.Config.ZipExtractFolderName);

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            Directory.CreateDirectory(extractPath);

            Debug.Log(FcuLocKey.log_zip_extracting_to.Localize(extractPath));
            ZipFile.ExtractToDirectory(zipPath, extractPath);

            var data = new ZipProjectData(extractPath, _monoBeh.Config.ZipManifestFileName)
            {
                ArchiveHash = ComputeArchiveHash(zipPath)
            };

            if (!data.IsValid)
            {
                throw new InvalidDataException($"Invalid archive structure. Expected 'project.json' at: {data.JsonFilePath}");
            }

            Debug.Log(FcuLocKey.log_zip_extract_success.Localize(data.JsonFilePath));

            return data;
        }

        private static string ComputeArchiveHash(string filePath)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
            catch (Exception ex)
            {
                Debug.LogError(FcuLocKey.log_zip_hash_failed.Localize(ex.Message));
                return null;
            }
        }

        private void ApplyManifestSettings(string manifestPath)
        {
#if JSONNET_EXISTS

            if (!File.Exists(manifestPath))
            {
                Debug.LogError(FcuLocKey.log_zip_manifest_missing.Localize());
                return;
            }

            string json;

            try
            {
                json = File.ReadAllText(manifestPath);
            }
            catch (Exception ex)
            {

                Debug.LogError(FcuLocKey.log_zip_manifest_read_failed.Localize(ex.Message));
                return;
            }

            ZipManifest manifest;

            try
            {
                manifest = DAJson.FromJson<ZipManifest>(json);
            }
            catch (Exception ex)
            {

                Debug.LogError(FcuLocKey.log_zip_manifest_parse_failed.Localize(ex.Message));
                return;
            }

            if (!manifest.IsValid)
            {
                Debug.LogError(FcuLocKey.log_zip_manifest_invalid.Localize(manifest.ExportScale, manifest.ImageFormat));
                return;
            }

            _monoBeh.Settings.ImageSpritesSettings.ImageScale = manifest.ExportScale;

            if (System.Enum.TryParse<ImageFormat>(manifest.ImageFormat, ignoreCase: true, out ImageFormat parsedFormat))
            {
                _monoBeh.Settings.ImageSpritesSettings.ImageFormat = parsedFormat;
            }

            Debug.Log(FcuLocKey.log_zip_manifest_applied.Localize(manifest.ExportScale, manifest.ImageFormat));
#endif
        }


        private async Task<DesignProject> LoadProjectFromJson(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"project.json not found: {jsonPath}");
            }

            string jsonContent = File.ReadAllText(jsonPath);

#if JSONNET_EXISTS
            DAResult<DesignProject> obj = await DAJson.FromJsonAsync<DesignProject>(jsonContent);
            var project = obj.Object;
            Debug.Log(FcuLocKey.log_zip_project_json_loaded.Localize(project.Name));

            return project;
#else
            throw new InvalidOperationException("JSON.NET is required for ZIP import. Please install Newtonsoft.Json package.");
#endif
        }

        private async Task<DesignProject> LoadNodeJsonAsync(string nodesFolder, string nodeId)
        {
            string sanitizedId = ZipProjectData.SanitizeNodeId(nodeId);
            string jsonPath = Path.Combine(nodesFolder, $"{sanitizedId}{_monoBeh.Config.ZipNodeFileExtension}");

            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning(FcuLocKey.log_zip_node_json_not_found.Localize(jsonPath));

                return default;
            }

            string jsonContent = File.ReadAllText(jsonPath);

#if JSONNET_EXISTS
            DAResult<DesignProject> obj = await DAJson.FromJsonAsync<DesignProject>(jsonContent);
            Debug.Log(FcuLocKey.log_zip_node_json_loaded.Localize(nodeId));

            return obj.Object;
#else
            throw new InvalidOperationException("JSON.NET is required for ZIP import.");
#endif
        }

        public void Cleanup()
        {
            var extractedPath = _monoBeh.CurrentProject.ZipData.ExtractedFolderPath;

            if (!string.IsNullOrEmpty(extractedPath) && Directory.Exists(extractedPath))
            {
                try
                {
                    Directory.Delete(extractedPath, true);
                    Debug.Log(FcuLocKey.log_zip_cleanup.Localize(extractedPath));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(FcuLocKey.log_zip_cleanup_failed.Localize(ex.Message));
                }
            }
        }
    }
}
#endif