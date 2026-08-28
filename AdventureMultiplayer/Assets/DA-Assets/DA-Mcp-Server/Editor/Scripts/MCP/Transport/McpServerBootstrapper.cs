using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DA_Assets.Shared.MCP
{
    internal static class McpServerBootstrapper
    {
        private const string UvVersion = "0.11.6";
        private const int BootstrapTimeoutMs = 10 * 60 * 1000;

        public static string DataRoot => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "DA-MCP");
        public static string EnvironmentPath => Path.Combine(DataRoot, "env");
        public static string UvCachePath => Path.Combine(DataRoot, "cache");
        public static string UvInstallPath => Path.Combine(DataRoot, "uv");
        public static string PythonInstallPath => Path.Combine(DataRoot, "python");
        public static string UvExecutablePath => Path.Combine(UvInstallPath, IsWindows ? "uv.exe" : "uv");

        private static bool IsWindows => Application.platform == RuntimePlatform.WindowsEditor;

        public static bool EnsureReady(string serverRoot)
        {
            if (!EnsureUvReady())
            {
                return false;
            }

            return RunUv("sync --locked --no-dev", serverRoot, "DA MCP server dependency sync failed.");
        }

        public static bool EnsureUvReady()
        {
            Directory.CreateDirectory(DataRoot);
            Directory.CreateDirectory(UvInstallPath);
            Directory.CreateDirectory(UvCachePath);
            Directory.CreateDirectory(PythonInstallPath);

            if (!EnsureUvInstalled())
            {
                return false;
            }

            return true;
        }

        public static void ConfigureUvEnvironment(ProcessStartInfo startInfo)
        {
            startInfo.EnvironmentVariables["UV_PROJECT_ENVIRONMENT"] = EnvironmentPath;
            startInfo.EnvironmentVariables["UV_CACHE_DIR"] = UvCachePath;
            startInfo.EnvironmentVariables["UV_PYTHON_INSTALL_DIR"] = PythonInstallPath;
            startInfo.EnvironmentVariables["UV_MANAGED_PYTHON"] = "true";
            startInfo.EnvironmentVariables["PATH"] = UvInstallPath + Path.PathSeparator + startInfo.EnvironmentVariables["PATH"];
        }

        private static bool EnsureUvInstalled()
        {
            if (File.Exists(UvExecutablePath))
            {
                return true;
            }

            return IsWindows ? InstallUvWindows() : InstallUvUnix();
        }

        private static bool InstallUvWindows()
        {
            string scriptUrl = $"https://astral.sh/uv/{UvVersion}/install.ps1";
            string arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"irm {scriptUrl} | iex\"";
            return RunBootstrapProcess("powershell.exe", arguments, DataRoot, "DA MCP uv install failed.");
        }

        private static bool InstallUvUnix()
        {
            string scriptUrl = $"https://astral.sh/uv/{UvVersion}/install.sh";
            string arguments = $"-c \"curl -LsSf {scriptUrl} | sh\"";
            return RunBootstrapProcess("/bin/sh", arguments, DataRoot, "DA MCP uv install failed.");
        }

        private static bool RunUv(string arguments, string workingDirectory, string errorMessage)
        {
            return RunBootstrapProcess(UvExecutablePath, arguments, workingDirectory, errorMessage);
        }

        private static bool RunBootstrapProcess(string fileName, string arguments, string workingDirectory, string errorMessage)
        {
            var output = new StringBuilder();
            var error = new StringBuilder();

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            ConfigureUvEnvironment(startInfo);
            startInfo.EnvironmentVariables["UV_INSTALL_DIR"] = UvInstallPath;
            startInfo.EnvironmentVariables["INSTALLER_NO_MODIFY_PATH"] = "1";

            try
            {
                using Process process = Process.Start(startInfo);
                process.OutputDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        output.AppendLine(args.Data);
                    }
                };
                process.ErrorDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        error.AppendLine(args.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (!process.WaitForExit(BootstrapTimeoutMs))
                {
                    process.Kill();
                    Debug.LogError($"{errorMessage}\nTimed out after {BootstrapTimeoutMs / 1000} seconds.");
                    return false;
                }

                process.WaitForExit();
                if (process.ExitCode == 0 && File.Exists(UvExecutablePath))
                {
                    return true;
                }

                Debug.LogError($"{errorMessage}\nExit code: {process.ExitCode}\n{output}\n{error}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{errorMessage}\n{ex.Message}");
                return false;
            }
        }
    }
}
