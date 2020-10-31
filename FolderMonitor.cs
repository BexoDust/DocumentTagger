using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DocumentTagger
{
    public class FolderMonitor
    {
        private List<Tag> _rules;
        private readonly FileSystemWatcher _watcher;
        private PdfContentExtractor _extractor = new PdfContentExtractor();
        private ILogger<Worker> _logger;
        private Semaphore _semaphore;
        private object _lock = new object();
        private DateTime _lastFoundFile;

        private BlockingCollection<string> _inputQueue;

        private readonly string _defaultSuccessPath;

        public FolderMonitor(string watchedFolder, string processedSuccess, List<Tag> rules, ILogger<Worker> logger)
        {
            _defaultSuccessPath = processedSuccess;

            _logger = logger;
            _rules = rules;

            _watcher = new FileSystemWatcher();
            _watcher.Path = watchedFolder;
            _watcher.NotifyFilter = NotifyFilters.Attributes |
                NotifyFilters.CreationTime |
                NotifyFilters.FileName |
                NotifyFilters.LastAccess |
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
            _semaphore = new Semaphore(1, 1);
            _lastFoundFile = DateTime.MinValue;

            this.InitialFolderScan(watchedFolder);
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

        public void UpdateRules(List<Tag> newRules)
        {
            _rules.Clear();
            _rules.AddRange(newRules);
        }

        private void FileChangedCallback(object sender, FileSystemEventArgs e)
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

        private void DiscoverFiles(string fileName, string filePath)
        {
            lock (_lock)
            {
                if (!_inputQueue.Contains(filePath) && File.Exists(filePath))
                {
                    AddBreakLine();

                    _logger.LogInformation("Found file: {0}", fileName);
                    _inputQueue.Add(filePath);
                }
            }
            var consumer = Task.Run(() => ConsumeNewFile());
        }

        private void AddBreakLine()
        {
            if ((DateTime.Now - _lastFoundFile).TotalSeconds > 60)
            {
                _logger.LogWarning("============================================================================================================");
            }

            _lastFoundFile = DateTime.Now;
        }

        private void ConsumeNewFile()
        {
            foreach (var file in _inputQueue.GetConsumingEnumerable())
            {
                lock (_lock)
                {
                    if (File.Exists(file))
                    {
                        var text = _extractor.ExtractFileContent(file);

                        var ruleSet = RuleManager.GetApplicableRules(text, _rules);
                        string docDate = RuleManager.GetDocumentDate(text);
                        string newName = RuleManager.GetNewFileName(file, docDate, text, ruleSet);

                        string newPath = RuleManager.MoveToDefaultLocation(file, _defaultSuccessPath, newName);
                        var finalPaths = RuleManager.MoveToNewLocation(newPath, ruleSet);

                        string message = $"New file name: {newName}{Environment.NewLine}New location: {String.Join(',', finalPaths)}{Environment.NewLine}";

                        if (finalPaths.Any(x => !String.IsNullOrEmpty(x)))
                        {
                            _logger.LogWarning(message);
                        }
                        else
                        {
                            _logger.LogInformation(message);
                        }
                    }
                }
            }
        }
    }
}
