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
        private object _lock = new object();
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
                this.DiscoverFiles(Path.GetFileName(file), file);
            }
        }

        public void Stop()
        {
            _watcher.Changed -= FileChangedCallback;
            _watcher.Created -= FileChangedCallback;
            _watcher.Renamed -= FileChangedCallback;
            _watcher.EnableRaisingEvents = false;
        }

        protected void DiscoverFiles(string fileName, string filePath)
        {
            lock (_lock)
            {
                var tries = 0;
                while (IsFileLocked(filePath) || tries < 10)
                {
                    Thread.Sleep(500);
                    tries++;
                }

                if (tries == 10)
                    _logger.LogWarning($"{this.GetType().Name}: File {filePath} still locked after {tries} tries.");

                if (!_inputQueue.Contains(filePath) && File.Exists(filePath))
                {
                    AddBreakLine();

                    _logger.LogInformation(this.GetType().Name + ": Found file: {0}", fileName);
                    _inputQueue.Add(filePath);
                }
            }
            var consumer = Task.Run(() => ConsumeNewFile());
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
                    this.DiscoverFiles(e.Name, e.FullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while handling changed file: " + ex.Message);
                }
            }
        }

        protected virtual bool IsFileLocked(string filePath)
        {
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
