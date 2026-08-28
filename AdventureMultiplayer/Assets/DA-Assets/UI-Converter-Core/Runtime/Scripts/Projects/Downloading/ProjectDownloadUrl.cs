#if UNITY_EDITOR
using DA_Assets.Constants;
using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UCC.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC
{
    public class ProjectDownloadUrl : IProjectDownloadStrategy
    {
        private readonly ConverterBase _monoBeh;

        public ImportMode Mode => ImportMode.Url;

        public ProjectDownloadUrl(ConverterBase monoBeh)
        {
            _monoBeh = monoBeh;
        }

        public async Task DownloadProjectAsync(CancellationToken token)
        {
            if (!_monoBeh.AuthProvider.IsAuthed())
            {
                throw new Exception("Not authorized. Please authenticate first.");
            }

            if (string.IsNullOrWhiteSpace(_monoBeh.Settings.MainSettings.ProjectId))
            {
                throw new Exception(FcuLocKey.log_incorrent_project_url.Localize());
            }

            _monoBeh.Events.OnProjectDownloadStart?.Invoke(_monoBeh);
            _monoBeh.InspectorDrawer.SelectableDocument.Childs.Clear();
            _monoBeh.EditorDelegateHolder.StartProgress?.Invoke(_monoBeh, ProgressBarCategory.ProjectDownloading, 0, true);

            DARequest projectRequest = RequestCreator.CreateProjectRequest(
                _monoBeh.RequestSender.GetRequestHeader(_monoBeh.AuthProvider.Token),
                _monoBeh.Settings.MainSettings.ProjectId,
                _monoBeh.Config.FrameListDepth,
                _monoBeh.Settings.MainSettings.FigmaEnvironment);

            DAResult<DesignProject> result = await _monoBeh.RequestSender.SendRequest<DesignProject>(projectRequest, token);

            if (!result.Success)
            {
                string message;

                switch (result.Error.status)
                {
                    case 403:
                        message = FcuLocKey.log_need_auth.Localize();
                        break;
                    case 404:
                        message = FcuLocKey.log_project_not_found.Localize();
                        break;
                    default:
                        message = FcuLocKey.log_unknown_error.Localize(result.Error.err, result.Error.status, result.Error.exception);
                        break;
                }

                throw new Exception(message);
            }

            _monoBeh.CurrentProject.FigmaProject = result.Object;
            _monoBeh.CurrentProject.ProjectName = result.Object.Name;
            _monoBeh.InspectorDrawer.FillSelectableFramesArray(_monoBeh.CurrentProject.FigmaProject.Document);
        }

        public async Task<List<Node>> DownloadAllNodes(string[] selectedIds, CancellationToken token)
        {
            ConcurrentBag<List<Node>> nodeChunks = new ConcurrentBag<List<Node>>();
            ConcurrentBag<string> failedChunkMessages = new ConcurrentBag<string>();
            List<List<string>> idChunks = selectedIds.Split(_monoBeh.Config.ChunkSizeGetNodes);
            List<Task> downloadTasks = new List<Task>(idChunks.Count);
            int completedChunks = 0;
            int tempCount = -1;

            _monoBeh.EditorDelegateHolder.StartProgress?.Invoke(_monoBeh, ProgressBarCategory.DownloadingNodes, idChunks.Count, false);

            try
            {
                foreach (List<string> chunk in idChunks)
                {
                    if (token.IsCancellationRequested)
                        break;

                    string ids = string.Join(",", chunk);
                    DARequest projectRequest = RequestCreator.CreateNodeRequest(
                        _monoBeh.RequestSender.GetRequestHeader(_monoBeh.AuthProvider.Token),
                        _monoBeh.Settings.MainSettings.ProjectId,
                        ids,
                        _monoBeh.Settings.MainSettings.FigmaEnvironment);

                    Task downloadTask = Task.Run(async () =>
                    {
                        try
                        {
                            DAResult<DesignProject> result = await _monoBeh.RequestSender.SendRequest<DesignProject>(projectRequest, token);

                            if (result.Success)
                            {
                                List<Node> docs = new List<Node>();

                                if (!result.Object.IsDefault() && !result.Object.Nodes.IsEmpty())
                                {
                                    foreach (var item in result.Object.Nodes)
                                    {
                                        if (item.Value.IsDefault())
                                            continue;

                                        docs.Add(item.Value.Document);
                                    }
                                }

                                nodeChunks.Add(docs);
                            }
                            else
                            {
                                string message = FcuLocKey.log_cant_get_part_of_frames.Localize(result.Error.err, result.Error.status);
                                failedChunkMessages.Add(message);
                                Debug.LogError(message);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            failedChunkMessages.Add(ex.Message);
                            Debug.LogException(ex);
                        }
                        finally
                        {
                            int current = Interlocked.Increment(ref completedChunks);
                            _monoBeh.EditorDelegateHolder.UpdateProgress?.Invoke(_monoBeh, ProgressBarCategory.DownloadingNodes, current);
                            FcuLogger.WriteLogBeforeEqual(nodeChunks, idChunks, FcuLocKey.log_getting_frames, nodeChunks.CountAll(), selectedIds.Count(), ref tempCount);
                        }
                    }, token);

                    downloadTasks.Add(downloadTask);
                }

                await Task.WhenAll(downloadTasks);
            }
            finally
            {
                _monoBeh.EditorDelegateHolder.CompleteProgress?.Invoke(_monoBeh, ProgressBarCategory.DownloadingNodes);
            }

            if (!failedChunkMessages.IsEmpty())
            {
                throw new Exception(string.Join("\n", failedChunkMessages));
            }

            return nodeChunks.FromChunks();
        }
    }
}
#endif