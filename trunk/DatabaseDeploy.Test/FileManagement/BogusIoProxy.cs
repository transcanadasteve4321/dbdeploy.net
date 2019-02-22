// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="BogusIoProxy.cs" company="Database Deploy 2">
//    Copyright (c) 2019 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DatabaseDeploy.Core.FileManagement;

namespace DatabaseDeploy.Test.FileManagement
{
    /// <summary>
    ///     Stores database change scripts in memory;
    ///     allows access to DbDeploy's own database scripts from the real file system.
    /// </summary>
    public class BogusIoProxy : IIoProxy
    {
        private readonly Dictionary<string, IFileEntry> fileSystem = new Dictionary<string, IFileEntry>();

        public void AddInMemoryFile(string filePath, string fileContents)
        {
            fileSystem[filePath] = new InMemoryFileEntry(fileContents);
        }

        public void AddRealFilesFromDirectory(string directory)
        {
            foreach (var fileName in Directory.GetFileSystemEntries(directory))
            {
                var filePath = Path.Combine(directory, fileName);
                fileSystem[filePath] = new RealFileEntry(filePath);
            }
        }

        public bool Exists(string fileName)
        {
            System.Console.WriteLine($"Exists({fileName})");
            return fileSystem.Keys.Any(f => f.EndsWith(fileName));
        }

        public string[] GetFiles(string rootDirectory, string searchPattern, SearchOption searchOption)
        {
            System.Console.WriteLine($"GetFiles({rootDirectory}, {searchPattern})");
            return fileSystem.Keys.Where(f => f.StartsWith(rootDirectory)).ToArray();
        }

        public StreamReader GetStreamReader(string fileName)
        {
            throw new NotImplementedException();
        }

        public StreamWriter GetStreamWriter(string fileName)
        {
            return new StreamWriter(new CallbackEnabledMemoryStream(bytes =>
            {
                using (var memoryStream = new MemoryStream(bytes))
                using (var reader = new StreamReader(memoryStream))
                {
                    fileSystem[fileName] = new InMemoryFileEntry(reader.ReadToEnd());
                    System.Console.WriteLine($"GetStreamWriter({fileName}) has written to dictionary");
                }
            }));
        }

        public string[] ReadAllLines(string fileName)
        {
            throw new NotImplementedException();
        }

        public string ReadAllText(string fileName)
        {
            System.Console.WriteLine($"ReadAllText({fileName})");
            return fileSystem[fileName].Contents;
        }

        private interface IFileEntry
        {
            string Contents { get; }
        }

        private class InMemoryFileEntry : IFileEntry
        {
            public string Contents { get; }

            public InMemoryFileEntry(string contents)
            {
                Contents = contents;
            }
        }

        private class RealFileEntry : IFileEntry
        {
            private readonly string realFilePath;

            public RealFileEntry(string realFilePath)
            {
                this.realFilePath = realFilePath;
            }

            public string Contents => File.ReadAllText(realFilePath);
        }

        private class CallbackEnabledMemoryStream : MemoryStream
        {
            private readonly Action<byte[]> actionWhenDisposed;

            public CallbackEnabledMemoryStream(Action<byte[]> actionWhenDisposed)
            {
                this.actionWhenDisposed = actionWhenDisposed;
            }

            protected override void Dispose(bool disposing)
            {
                actionWhenDisposed(ToArray());
                base.Dispose(disposing);
            }
        }
    }
}