using DocumentTagger.Monitor;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DocumentTagger
{
    public abstract class AFolderMonitor
    {
        private readonly FileSystemWatcher _watcher;
        private readonly object _lock = new();
        private DateTime _lastFoundFile;

        protected ILogger<Worker> _logger;
        protected BlockingCollection<string> _inputQueue;

        protected readonly string _watchedFolder;
        protected readonly string _successFolder;

        public AFolderMonitor(string watchedFolder, string processedSuccess, ILogger<Worker> logger)
        {
            _successFolder = processedSuccess;
            _watchedFolder = watchedFolder;

            _logger = logger;

            _watcher = new FileSystemWatcher();
            _watcher.Path = watchedFolder;
            _watcher.NotifyFilter = NotifyFilters.Attributes |
                NotifyFilters.CreationTime |
                NotifyFilters.FileName |
                //NotifyFilters.LastAccess |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.Security;

            _watcher.Filter = "*.pdf";
            _watcher.Changed += FileChangedCallback;
            _watcher.Created += FileChangedCallback;
            _watcher.Renamed += FileChangedCallback;
            _watcher.IncludeSubdirectories = false;
            _watcher.EnableRaisingEvents = true;

            _inputQueue = new BlockingCollection<string>();
            _lastFoundFile = DateTime.MinValue;
        }

        public void InitialFolderScan(string folder)
        {
            var files = Directory.GetFiles(folder);

            foreach (var file in files)
            {
                this.DiscoverFile(Path.GetFileName(file), file);
            }
        }

        public void Stop()
        {
            _watcher.Changed -= FileChangedCallback;
            _watcher.Created -= FileChangedCallback;
            _watcher.Renamed -= FileChangedCallback;
            _watcher.EnableRaisingEvents = false;
        }

        protected void DiscoverFile(string fileName, string filePath)
        {
            var maxTries = 10;

            if (!File.Exists(filePath))
                return;

            lock (_lock)
            {
                var tries = 0;
                while (IsFileLocked(filePath) && tries < maxTries)
                {
                    Thread.Sleep(500);
                    tries++;
                }

                if (tries == maxTries)
                {
                    _logger.LogWarning($"{this.GetType().Name}: File {filePath} still locked after {tries} tries.");
                    _logger.LogWarning($"{this.GetType().Name}: Skipped File {filePath}.");
                    return;
                }

                // Wait until file appears to be fully written (size/last write time stable)
                if (!WaitForFileReady(filePath, TimeSpan.FromSeconds(60)))
                {
                    _logger.LogWarning($"{this.GetType().Name}: File {filePath} was not stable within timeout. Skipping.");
                    return;
                }

                if (!_inputQueue.Contains(filePath) && File.Exists(filePath))
                {
                    AddBreakLine();

                    _logger.LogInformation(this.GetType().Name + ": Found file: {0}", fileName);
                    _inputQueue.Add(filePath);
                }
            }
            var consumer = Task.Run(() => ConsumeNewFile());
        }

        /// <summary>
        /// Wait until file size and last write time remain stable over a few checks.
        /// This helps avoid processing files that are still being written by other processes.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        protected bool WaitForFileReady(string filePath, TimeSpan timeout)
        {
            try
            {
                var checkInterval = TimeSpan.FromMilliseconds(500);
                var stableCountNeeded = 3;
                int stableCount = 0;

                var sw = System.Diagnostics.Stopwatch.StartNew();

                long lastSize = -1;
                DateTime lastWrite = DateTime.MinValue;

                while (sw.Elapsed < timeout)
                {
                    if (!File.Exists(filePath))
                        return false;

                    var fi = new FileInfo(filePath);
                    long size = fi.Length;
                    DateTime write = fi.LastWriteTimeUtc;

                    if (size == lastSize && write == lastWrite)
                    {
                        stableCount++;
                        if (stableCount >= stableCountNeeded)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        stableCount = 0;
                        lastSize = size;
                        lastWrite = write;
                    }

                    Thread.Sleep(checkInterval);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"{this.GetType().Name}: Error while waiting for file ready: {filePath}");
            }

            return false;
        }

        private void AddBreakLine()
        {
            if ((DateTime.Now - _lastFoundFile).TotalSeconds > 60)
            {
                Console.WriteLine("============================================================================================================");
            }

            _lastFoundFile = DateTime.Now;
        }

        protected void FileChangedCallback(object sender, FileSystemEventArgs e)
        {
            //Thread.Sleep(1000);

            //if (File.Exists(e.FullPath))
            {
                try
                {
                    this.DiscoverFile(e.Name, e.FullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while handling changed file: " + ex.Message);
                }
            }
        }

        protected virtual bool IsFileLocked(string filePath)
        {
                var list = FileUtil.WhoIsLocking(filePath);

            try
            {

                var fileInfo = new FileInfo(filePath);
                using (FileStream stream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }

        protected abstract void ConsumeNewFile();
    }
}
