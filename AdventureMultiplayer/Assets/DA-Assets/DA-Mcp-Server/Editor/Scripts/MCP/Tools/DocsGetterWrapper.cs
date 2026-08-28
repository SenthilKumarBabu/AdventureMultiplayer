using DA_Assets.Shared;
using DA_Assets.Shared.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DA_Assets.FCU.MCP
{
    public class DocsGetterWrapper
    {
        bool _debug;

        private const string CACHE_FOLDER_NAME = "docs_cache";
        private const int PROCESS_TIMEOUT_MS = 120000;

        private readonly string _pythonPath;
        private readonly string _scriptPath;
        private readonly string _cacheDir;
        private readonly string _baseUrl;

        private readonly DocsGetterSettings _settings;

        public DocsGetterWrapper(string baseUrl, DocsGetterSettings settings)
        {
            _baseUrl = baseUrl;
            _settings = settings;

            _pythonPath = FindPythonExecutable();
            _scriptPath = GetScriptPath();
            _cacheDir = Path.Combine(Application.persistentDataPath, CACHE_FOLDER_NAME);
            Directory.CreateDirectory(_cacheDir);
        }

        private string GetScriptPath()
        {
#if UNITY_EDITOR
            if (_settings.PythonDocsGetter != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(_settings.PythonDocsGetter);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    return Path.GetFullPath(assetPath);
                }
            }
#endif
            return null;
        }

        public async Task<string> GetDocumentationAsync(string url = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                url = null;

            if (string.IsNullOrEmpty(_pythonPath))
                return "{\"error\": \"Python not found. Please install Python 3.x and add it to PATH.\"}";

            if (string.IsNullOrEmpty(_scriptPath))
                return "{\"error\": \"Python script file not assigned in SharedConfig.PythonDocsGetter.\"}";

            if (string.IsNullOrEmpty(_baseUrl))
                return "{\"error\": \"BaseUrl is not configured.\"}";

            try
            {
                return await RunPythonScriptAsync(url);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return $"{{\"error\": \"{EscapeJsonString(ex.Message)}\"}}";
            }
        }

        private async Task<string> RunPythonScriptAsync(string url)
        {
            var arguments = new StringBuilder();
            arguments.Append($"\"{_scriptPath}\"");
            
            arguments.Append($" --base-url \"{_baseUrl}\"");
            
            arguments.Append($" --cache-dir \"{_cacheDir}\"");
            arguments.Append($" --cache-expiry {_settings.CacheExpiryHours}");
            arguments.Append($" --user-agent \"{_settings.UserAgent}\"");
            arguments.Append($" --timeout {_settings.TimeoutSeconds}");
            arguments.Append($" --cache-enabled {(_settings.CacheEnabled ? "true" : "false")}");
            
            if (!string.IsNullOrEmpty(url))
            {
                arguments.Append($" \"{url}\"");
            }

            Debug.Log(SharedLocKey.log_mcp_docs_getter_running.Localize(nameof(DocsGetterWrapper), _pythonPath, arguments));

            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = arguments.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        errorBuilder.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var completed = await Task.Run(() => process.WaitForExit(PROCESS_TIMEOUT_MS));

                if (!completed)
                {
                    try { process.Kill(); } catch { }
                    return "{\"error\": \"Python script timed out.\"}";
                }

                string stderr = errorBuilder.ToString().Trim();
                if (!string.IsNullOrEmpty(stderr))
                {
                    Debug.LogError(SharedLocKey.log_mcp_docs_getter_stderr.Localize(nameof(DocsGetterWrapper), stderr));
                }

                string output = outputBuilder.ToString().Trim();
                
                if (process.ExitCode != 0)
                {
                    Debug.LogError(SharedLocKey.log_mcp_docs_getter_exit_code.Localize(nameof(DocsGetterWrapper), process.ExitCode));
                    return $"{{\"error\": \"Python script failed: {EscapeJsonString(stderr)}\"}}";
                }

                if (string.IsNullOrEmpty(output))
                {
                    return "{\"error\": \"Python script returned empty output.\"}";
                }

                Debug.Log(SharedLocKey.log_mcp_docs_getter_received.Localize(nameof(DocsGetterWrapper), output.Length));
                return output;
            }
        }

        private string FindPythonExecutable()
        {
            string[] pythonNames = { "python", "python3", "py" };
            
            foreach (var name in pythonNames)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = name,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(startInfo))
                    {
                        process.WaitForExit(5000);
                        if (process.ExitCode == 0)
                        {
                            if (_debug)
                            {
                                Debug.Log(SharedLocKey.log_mcp_docs_getter_found_python.Localize(nameof(DocsGetterWrapper), name));
                            }

                            return name;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex); 
                }
            }

            Debug.LogError(SharedLocKey.log_mcp_docs_getter_python_not_found_in_path.Localize(nameof(DocsGetterWrapper)));
            return null;
        }


        private static string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
