using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GhostBodyObject.Repository.Tests.Helpers
{
    public class TempDirectoryHelper : IDisposable
    {
        private string _directoryPath;

        public string DirectoryPath => _directoryPath;

        public TempDirectoryHelper(bool inTemp)
        {
            if (inTemp)
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

        public void Dispose()
        {
            Console.WriteLine("Delete temp. directory : " + _directoryPath);
            Directory.Delete(_directoryPath, true);
        }
    }
}
