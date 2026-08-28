using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace DA_Assets.Shared.MCP
{
    internal static class TerminalLauncher
    {
        public static ProcessStartInfo CreateProcessStartInfo(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentException("Command cannot be empty", nameof(command));
            }

            command = command.Replace("\r", "").Replace("\n", "");

#if UNITY_EDITOR_OSX
            string scriptPath = WriteScript("da-mcp-terminal.command", "#!/bin/bash\nset -e\nclear\n" + command + "\n");
            RunChmod(scriptPath);
            return new ProcessStartInfo
            {
                FileName = "/usr/bin/open",
                Arguments = $"-a Terminal \"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
#elif UNITY_EDITOR_WIN
            string scriptPath = WriteScript("da-mcp-terminal.cmd", "@echo off\r\ncls\r\n" + command + "\r\n");
            return new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"DA MCP Server\" cmd.exe /k \"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
#else
            string script = command + "; exec bash";
            string escaped = script.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string bashArgs = $"bash -c \"{escaped}\"";
            string terminal = FindLinuxTerminal();

            string args = terminal switch
            {
                "gnome-terminal" => $"-- {bashArgs}",
                "konsole" => $"-e {bashArgs}",
                "xfce4-terminal" => $"--hold -e \"{bashArgs.Replace("\"", "\\\"")}\"",
                _ => $"-hold -e {bashArgs}"
            };

            return new ProcessStartInfo
            {
                FileName = terminal,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };
#endif
        }

        private static string WriteScript(string fileName, string content)
        {
            string folder = Path.Combine(ProjectIdentity.ProjectPath, "Library", "DA-MCP", "TerminalScripts");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, fileName);
            File.WriteAllText(path, content);
            return path;
        }

#if UNITY_EDITOR_OSX
        private static void RunChmod(string scriptPath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "/bin/chmod",
                    Arguments = $"+x \"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit(3000);
            }
            catch
            {
            }
        }
#endif

#if !UNITY_EDITOR_OSX && !UNITY_EDITOR_WIN
        private static string FindLinuxTerminal()
        {
            string[] terminals = { "gnome-terminal", "xterm", "konsole", "xfce4-terminal" };

            foreach (string terminal in terminals)
            {
                try
                {
                    using Process process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "which",
                        Arguments = terminal,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    });
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                    {
                        return terminal;
                    }
                }
                catch
                {
                }
            }

            return "xterm";
        }
#endif
    }
}
