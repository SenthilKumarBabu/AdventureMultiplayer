#if UNITY_EDITOR
using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.UCC.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DA_Assets.UCC
{
    [Serializable]
    public class ProjectCacher : FcuBase
    {
        internal void Cache<T>(T @object)
        {
            try
            {
                DesignProject figmaProject = (DesignProject)Convert.ChangeType(@object, typeof(DesignProject));

                string projectId = monoBeh.Settings.MainSettings.ProjectId;

                RecentProject projectCache = new RecentProject
                {
                    Url = projectId,
                    Name = figmaProject.Name,
                    DateTime = DateTime.Now
                };

                List<RecentProject> cachedProjects = GetRecentProjects();
                cachedProjects.RemoveAll(pc => pc.Url == projectId);
                cachedProjects.Insert(0, projectCache);

                if (cachedProjects.Count > monoBeh.Config.RecentProjectsLimit)
                {
                    cachedProjects = cachedProjects.Take(monoBeh.Config.RecentProjectsLimit).ToList();
                }

                SaveAll(cachedProjects);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public List<RecentProject> GetRecentProjects()
        {
#if UNITY_EDITOR
            string savedData = UnityEditor.EditorPrefs.GetString(monoBeh.Config.RECENT_PROJECTS_PREFS_KEY, "");

            if (savedData.IsEmpty())
            {
                return new List<RecentProject>();
            }

            List<RecentProject> cachedProjects = DAJson.FromJson<List<RecentProject>>(savedData);

            if (!cachedProjects.IsEmpty())
            {
                return cachedProjects.OrderByDescending(x => x.DateTime).ToList();
            }
            else
            {
                return new List<RecentProject>();
            }
#else
            return new List<RecentProject>();
#endif
        }

        private void SaveAll(List<RecentProject> cachedProjects)
        {
            if (cachedProjects == null)
            {
                cachedProjects = new List<RecentProject>();
            }

            string json = DAJson.ToJson(cachedProjects);
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetString(monoBeh.Config.RECENT_PROJECTS_PREFS_KEY, json);
#endif
        }
    }
}
#endif