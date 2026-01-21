using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GhostBodyObject.HandWritten.Tests
{
    public class TempDirectoryHelper : IDisposable
    {
        private string _directoryPath;

        public string DirectoryPath => _directoryPath;

        public TempDirectoryHelper(bool temporaryFolder)
        {
            if (temporaryFolder)
            {
                _directoryPath = GetTemporaryDirectory();
                Console.WriteLine("Temp. directory : " + _directoryPath);
                return;
            }
            else
            {
                string assemblyLocation = Assembly.GetExecutingAssembly().Location;
                string baseDirectory = Path.GetDirectoryName(assemblyLocation);

                if (string.IsNullOrEmpty(baseDirectory))
                {
                    throw new DirectoryNotFoundException("Could not determine the base directory of the executing assembly.");
                }

                string tempFolderName = Path.GetRandomFileName();
                string tempDirectoryPath = Path.Combine(baseDirectory, tempFolderName);

                Directory.CreateDirectory(tempDirectoryPath);

                _directoryPath = tempDirectoryPath;

                Console.WriteLine("Temp. directory : " + _directoryPath);
            }
        }

        public string FilePathInDir(string fileName)
            => Path.Combine(_directoryPath, fileName);

        private static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public string[] GetFiles()
        {
            return Directory.GetFiles(_directoryPath).Select(f => Path.GetFileName(f)).ToArray();
        }

        public void GCCollect()
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
        }

        public void Dispose()
        {
            try
            {
                Console.WriteLine("Delete temp. directory : " + _directoryPath);
                Directory.Delete(_directoryPath, true);
            } catch
            {
                var retry = 1;
            _redo:
                if (GetFiles().Length > 0)
                {
                    Console.WriteLine("Remainning files in temp. Directory before deletion:");
                    foreach (var file in GetFiles())
                    {
                        Console.WriteLine(" - " + file);
                    }
                    GCCollect();
                    Thread.Sleep(500);
                    try
                    {
                        Directory.Delete(_directoryPath, true);
                        Console.WriteLine("Directory deleted !");
                    }
                    catch
                    {
                        Console.WriteLine("Retry " + retry + " failled.");
                        if (retry++ > 10)
                            throw new InvalidOperationException($"Could not delete temp. Directory files remains after multiple GC attempts ({GetFiles().Length} files : {string.Join(", ", GetFiles().Take(3).Select(f => Path.GetFileName(f)))}).");
                        goto _redo;
                    }
                }
            }
            
        }
    }
}
