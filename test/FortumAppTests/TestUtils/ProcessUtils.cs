using System;
using System.Diagnostics;
using System.IO;
using Microsoft.DotNet.PlatformAbstractions;

namespace FortumAppTests.TestUtils
{
    public static class ProcessUtils
    {
        private static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;
        private const string DotNetProcessName = "dotnet";

        /// <summary>
        /// Execute the command "dotnet run" from a shell in a default or specified directory.
        /// </summary>
        /// <typeparam name="T">The type to retrieve the assembly name from,
        /// where the command will be executed if the executionDirectoryPath is not provided.</typeparam>
        /// <param name="process"></param>
        /// <param name="executionDirectoryPath">Optional: the full absolute path to the directory
        /// where the command should be executed from.</param>
        public static void DotNetRun<T>(this Process process, string executionDirectoryPath = null)
        {
            process.StartInfo = CreateDotNetRunStartInfo(GetExecutionDirectoryPath<T>(executionDirectoryPath));
            process.Start();
        }

        /// <summary>
        /// Create a dotnet run process from a default or specified directory.
        /// </summary>
        /// <typeparam name="T">The type to retrieve the assembly name from,
        /// where the command will be executed if the executionDirectoryPath is not provided.</typeparam>
        /// <param name="executionDirectoryPath">Optional: the full absolute path to the directory
        /// where the command should be executed from.</param>
        /// <returns></returns>
        public static Process CreateDotNetRunProcess<T>(string executionDirectoryPath = null)
        {
            return CreateProcess(CreateDotNetRunStartInfo(GetExecutionDirectoryPath<T>(executionDirectoryPath)));
        }

        /// <summary>
        /// Kill all processes with a given name that were started at a minimum start time.
        /// </summary>
        /// <param name="processName">The name of the processes that should be killed.</param>
        /// <param name="minimumStartTime">The minimum start time that the processes were started at.</param>
        public static void KillProcesses(string processName, DateTime minimumStartTime)
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                if (!process.HasExited && process.StartTime >= minimumStartTime)
                {
                    process.Kill();
                    process.WaitForExit(milliseconds: 10000);
                }
            }
        }

        /// <summary>
        /// Kill all dotnet processes with a given name that were started at a minimum start time.
        /// </summary>
        /// <param name="minimumStartTime">The minimum start time that the processes were started at.</param>
        public static void KillDotNetProcesses(DateTime minimumStartTime)
        {
            KillProcesses(DotNetProcessName, minimumStartTime);
        }

        private static ProcessStartInfo CreateStartInfo(string fileName, string arguments, string currentDirectoryPath)
        {
            return new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = currentDirectoryPath
            };
        }

        private static string GetExecutionDirectoryPath<T>(string executionDirectoryPath = null)
        {
            executionDirectoryPath = executionDirectoryPath ?? GetDefaultExecutionDirectoryPath<T>();
            if (!Directory.Exists(executionDirectoryPath))
            {
                throw new DirectoryNotFoundException($"The directory \"{executionDirectoryPath}\" does not exist.");
            }

            return executionDirectoryPath;
        }

        private static string GetDefaultExecutionDirectoryPath<T>()
        {
            var currentExecutionDirectoryPath = GetConfigPath();

            if (currentExecutionDirectoryPath.Contains(CreateBinDebugSubPath(DirectorySeparatorChar)))
            {
                currentExecutionDirectoryPath = GetParentDirectory(currentExecutionDirectoryPath);
                currentExecutionDirectoryPath = GetParentDirectory(currentExecutionDirectoryPath);
            }

            currentExecutionDirectoryPath = GetParentDirectory(currentExecutionDirectoryPath);
            currentExecutionDirectoryPath = GetParentDirectory(currentExecutionDirectoryPath);
            currentExecutionDirectoryPath = GetParentDirectory(currentExecutionDirectoryPath);

            return
                $"{currentExecutionDirectoryPath}{DirectorySeparatorChar}src{DirectorySeparatorChar}{typeof(T).Assembly.GetName().Name}";
        }

        private static string CreateBinDebugSubPath(char directorySeparatorChar)
        {
            return $"bin{directorySeparatorChar}Debug";
        }

        private static Process CreateProcess(ProcessStartInfo processStartInfo)
        {
            return new Process
            {
                StartInfo = processStartInfo
            };
        }

        private static ProcessStartInfo CreateDotNetRunStartInfo(string currentDirectoryPath)
        {
            return CreateStartInfo(DotNetProcessName, "run", currentDirectoryPath);
        }

        private static string GetParentDirectory(string currentDirectoryPath)
        {
            return Directory.GetParent(currentDirectoryPath).FullName;
        }

        public static string GetConfigPath()
        {
            var path = ApplicationEnvironment.ApplicationBasePath;
            var ind = path.IndexOf(CreateBinDebugSubPath(DirectorySeparatorChar), StringComparison.Ordinal);
            return path.Substring(startIndex: 0, length: ind);
        }
    }
}