using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DA_Assets.Shared.MCP
{
    public static class McpServerProcess
    {
        private const string PidFilePathKey = "DA_Assets.MCP.LocalHttpServer.PidFilePath";
        private const string InstanceTokenKey = "DA_Assets.MCP.LocalHttpServer.InstanceToken";
        private const string StartedUtcKey = "DA_Assets.MCP.LocalHttpServer.StartedUtc";

        public const string Host = "127.0.0.1";
        public const int Port = 8765;

        private static Process _process;

        public static string HttpUrl => $"http://{Host}:{Port}";
        public static string McpUrl => $"{HttpUrl}/mcp";
        public static string HealthUrl => $"{HttpUrl}/health";
        public static string ServerCommand => $"\"{McpServerBootstrapper.UvExecutablePath}\" run --locked --no-dev da-mcp-server --transport http --host {Host} --port {Port}";

        public static void Start()
        {
            if (_process != null && !_process.HasExited)
            {
                return;
            }

            if (TryGetManagedServerPid(out _) || IsLocalHttpServerReachable())
            {
                return;
            }

            string serverRoot = McpConfig.Instance.GetServerRootPath(Application.dataPath);
            if (!Directory.Exists(serverRoot))
            {
                Debug.LogError($"DA MCP server folder not found: {serverRoot}");
                return;
            }

            if (!EnsureLaunchReady(serverRoot))
            {
                return;
            }

            string arguments = $"run --locked --no-dev da-mcp-server --transport http --host {Host} --port {Port}";
            string pidFilePath = GetPidFilePath();
            string instanceToken = Guid.NewGuid().ToString("N");
            TryDeletePidFile(pidFilePath);

            arguments += $" --pidfile \"{pidFilePath}\" --unity-instance-token {instanceToken}";
            ProcessStartInfo startInfo = CreateStartInfo(serverRoot, arguments);
            _process = Process.Start(startInfo);
            StoreTracking(pidFilePath, instanceToken);
        }

        public static async Task StartAndConnectAsync()
        {
            McpServerDesiredState.SetRunningDesired(true);
            Start();
            await TransportManager.StartWhenReachableAsync(HttpUrl);
        }

        public static async Task StopAndDisconnectAsync()
        {
            McpServerDesiredState.SetRunningDesired(false);
            await TransportManager.StopHttpAsync();
            Stop();
        }

        public static void Stop()
        {
            if (TryGetManagedServerPid(out int pid) || TryGetDetectedServerPid(out pid))
            {
                KillProcess(pid);
                ClearTracking();
                _process?.Dispose();
                _process = null;
                return;
            }

            if (_process == null || _process.HasExited)
            {
                ClearTracking();
                return;
            }

            _process.Kill();
            _process.Dispose();
            _process = null;
            ClearTracking();
        }

        public static bool IsManagedProcessRunning()
        {
            if (_process != null && !_process.HasExited)
            {
                return true;
            }

            return TryGetManagedServerPid(out _);
        }

        public static bool IsLocalHttpServerReachable()
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(Host, Port);
                return connectTask.Wait(50) && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        private static string GetPidFilePath()
        {
            string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "DA-MCP");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, $"local-http-{Port}.pid");
        }

        private static ProcessStartInfo CreateStartInfo(string serverRoot, string arguments)
        {
            if (McpConfig.Instance.ShowServerConsoleWindow)
            {
                return TerminalLauncher.CreateProcessStartInfo(BuildTerminalCommand(serverRoot, arguments));
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = McpServerBootstrapper.UvExecutablePath,
                Arguments = arguments,
                WorkingDirectory = serverRoot,
                CreateNoWindow = !McpConfig.Instance.ShowServerConsoleWindow,
                UseShellExecute = false
            };
            McpServerBootstrapper.ConfigureUvEnvironment(startInfo);
            return startInfo;
        }

        private static string BuildTerminalCommand(string serverRoot, string arguments)
        {
#if UNITY_EDITOR_WIN
            return string.Join(" && ", new[]
            {
                $"set \"UV_PROJECT_ENVIRONMENT={McpServerBootstrapper.EnvironmentPath}\"",
                $"set \"UV_CACHE_DIR={McpServerBootstrapper.UvCachePath}\"",
                $"set \"UV_PYTHON_INSTALL_DIR={McpServerBootstrapper.PythonInstallPath}\"",
                "set \"UV_MANAGED_PYTHON=true\"",
                $"set \"PATH={McpServerBootstrapper.UvInstallPath};%PATH%\"",
                $"cd /d \"{serverRoot}\"",
                $"\"{McpServerBootstrapper.UvExecutablePath}\" sync --locked --no-dev",
                $"\"{McpServerBootstrapper.UvExecutablePath}\" {arguments}"
            });
#else
            return string.Join(" && ", new[]
            {
                $"export UV_PROJECT_ENVIRONMENT={QuoteShell(McpServerBootstrapper.EnvironmentPath)}",
                $"export UV_CACHE_DIR={QuoteShell(McpServerBootstrapper.UvCachePath)}",
                $"export UV_PYTHON_INSTALL_DIR={QuoteShell(McpServerBootstrapper.PythonInstallPath)}",
                "export UV_MANAGED_PYTHON=true",
                $"export PATH={QuoteShell(McpServerBootstrapper.UvInstallPath)}:$PATH",
                $"cd {QuoteShell(serverRoot)}",
                $"{QuoteShell(McpServerBootstrapper.UvExecutablePath)} sync --locked --no-dev",
                $"{QuoteShell(McpServerBootstrapper.UvExecutablePath)} {arguments}"
            });
#endif
        }

        private static bool EnsureLaunchReady(string serverRoot)
        {
            return McpConfig.Instance.ShowServerConsoleWindow
                ? McpServerBootstrapper.EnsureUvReady()
                : McpServerBootstrapper.EnsureReady(serverRoot);
        }

        private static string QuoteShell(string value)
        {
            return "'" + value.Replace("'", "'\"'\"'") + "'";
        }

        private static void StoreTracking(string pidFilePath, string instanceToken)
        {
            EditorPrefs.SetString(PidFilePathKey, pidFilePath);
            EditorPrefs.SetString(InstanceTokenKey, instanceToken);
            EditorPrefs.SetString(StartedUtcKey, DateTime.UtcNow.ToString("O"));
        }

        private static void ClearTracking()
        {
            string pidFilePath = EditorPrefs.GetString(PidFilePathKey, string.Empty);
            TryDeletePidFile(pidFilePath);
            EditorPrefs.DeleteKey(PidFilePathKey);
            EditorPrefs.DeleteKey(InstanceTokenKey);
            EditorPrefs.DeleteKey(StartedUtcKey);
        }

        private static bool TryGetManagedServerPid(out int pid)
        {
            pid = 0;
            string pidFilePath = EditorPrefs.GetString(PidFilePathKey, string.Empty);
            string startedUtc = EditorPrefs.GetString(StartedUtcKey, string.Empty);

            if (string.IsNullOrWhiteSpace(pidFilePath) || !File.Exists(pidFilePath))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(startedUtc)
                && DateTime.TryParse(startedUtc, out DateTime startedAt)
                && DateTime.UtcNow - startedAt.ToUniversalTime() > TimeSpan.FromHours(6))
            {
                return false;
            }

            if (!int.TryParse(File.ReadAllText(pidFilePath).Trim(), out pid) || pid <= 0)
            {
                return false;
            }

            try
            {
                Process process = Process.GetProcessById(pid);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        private static void KillProcess(int pid)
        {
            if (pid <= 0 || pid == Process.GetCurrentProcess().Id)
            {
                return;
            }

            try
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/PID {pid} /T /F",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })?.WaitForExit(5000);
                    return;
                }

                Process.GetProcessById(pid).Kill();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to stop DA MCP server process {pid}: {ex.Message}");
            }
        }

        private static bool TryGetDetectedServerPid(out int pid)
        {
            pid = 0;

            foreach (int candidatePid in GetListeningProcessIdsForPort(Port))
            {
                if (LooksLikeDaMcpServer(candidatePid))
                {
                    pid = candidatePid;
                    return true;
                }
            }

            return false;
        }

        private static List<int> GetListeningProcessIdsForPort(int port)
        {
            var results = new List<int>();

            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                return results;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netstat.exe",
                    Arguments = "-ano",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using Process process = Process.Start(startInfo);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);
                string portSuffix = $":{port}";

                foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!line.Contains("LISTENING") || !line.Contains(portSuffix))
                    {
                        continue;
                    }

                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5 && parts[1].EndsWith(portSuffix) && int.TryParse(parts[parts.Length - 1], out int parsedPid))
                    {
                        results.Add(parsedPid);
                    }
                }
            }
            catch
            {
                return results;
            }

            return results;
        }

        private static bool LooksLikeDaMcpServer(int pid)
        {
            if (pid <= 0 || pid == Process.GetCurrentProcess().Id)
            {
                return false;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "wmic.exe",
                    Arguments = $"process where \"ProcessId={pid}\" get CommandLine /value",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using Process process = Process.Start(startInfo);
                string output = process.StandardOutput.ReadToEnd().ToLowerInvariant();
                process.WaitForExit(5000);

                return output.Contains("da-mcp-server")
                    && output.Contains("--transport http")
                    && output.Contains("--port " + Port);
            }
            catch
            {
                return false;
            }
        }

        private static void TryDeletePidFile(string pidFilePath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(pidFilePath) && File.Exists(pidFilePath))
                {
                    File.Delete(pidFilePath);
                }
            }
            catch
            {
            }
        }
    }
}
