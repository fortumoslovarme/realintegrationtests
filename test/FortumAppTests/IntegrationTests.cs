using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FortumApp;
using FortumAppTests.TestUtils;
using Xunit;

namespace FortumAppTests
{
    public class IntegrationTests
    {
        [Fact]
        public async void DotNetRun_ExecuteCommandFromSourceCodeAssembly_WritesExpectedFileWithText()
        {
            // Arrange
            var expectedFilePath = Path.Combine(Path.GetTempPath(), "fortumfile.txt");
            DeleteFileIfExists(expectedFilePath);
            const string expectedFileContent = "This is a test.";

            var timeoutForTest = TimeSpan.FromSeconds(value: 10);

            var process = new Process();
            Task dotnetRunTask = null;
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var startTimeForDotnetRun = DateTime.Now;

            try
            {
                // Act
                dotnetRunTask = Task.Run(() => process.DotNetRun<Program>(), cancellationToken);

                string fileContent;
                while ((fileContent = await GetFileContent(expectedFilePath, cancellationToken)) is null)
                {
                    await Task.Delay(millisecondsDelay: 100, cancellationToken: cancellationToken);

                    if (stopwatch.Elapsed >= timeoutForTest)
                    {
                        throw new TimeoutException("The test has been running too long without a response.");
                    }
                }

                // Assert
                Assert.Equal(expectedFileContent, fileContent);
            }
            finally
            {
                // Cleanup
                try
                {
                    cancellationTokenSource.Cancel();
                    try
                    {
                        dotnetRunTask?.Wait(cancellationTokenSource.Token);
                    }
                    finally
                    {
                        cancellationTokenSource.Dispose();
                    }
                }
                catch (Exception)
                {
                    // ignore
                }

                ProcessUtils.KillDotNetProcesses(startTimeForDotnetRun);
                DeleteFileIfExists(expectedFilePath);
            }
        }

        private static void DeleteFileIfExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (File.Exists(filePath))
            {
                throw new Exception(
                    "The file still exists after trying to delete it.");
            }
        }

        private static async Task<string> GetFileContent(string filePath, CancellationToken cancellationToken)
        {
            string fileContent = null;
            try
            {
                fileContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            }
            catch (Exception)
            {
                // ignore
            }

            return fileContent;
        }
    }
}