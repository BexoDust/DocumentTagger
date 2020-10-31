using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocumentTagger
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WorkerOptions _options;

        private readonly List<FolderMonitor> _monitorList;
        private readonly ConfigMonitor _configMonitor;

        public Worker(ILogger<Worker> logger, WorkerOptions options)
        {
            _logger = logger;
            _options = options;
            _monitorList = new List<FolderMonitor>();
            _configMonitor = new ConfigMonitor(_options.ConfigPath);
            _configMonitor.FileChanged += FileChangedCallback;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var ruleList = GetRules();

            foreach (var location in _options.WatchedLocations)
            {
                _monitorList.Add(new FolderMonitor(location, _options.DefaultProcessedSucces, ruleList, _logger));
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }

            foreach(var monitor in _monitorList)
            {
                monitor.Stop();
            }
        }

        private List<Tag> GetRules()
        {
            var ruleList = JsonIo.ReadObjectFromJsonFile<List<Tag>>(_options.ConfigPath);

            return ruleList;
        }

        private void FileChangedCallback(object sender, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
            {
                _logger.LogInformation("Config changed: {0}", e.Name);

                var rules = GetRules();

                if (rules != null)
                {
                    foreach (var monitor in _monitorList)
                    {
                        monitor.UpdateRules(rules);
                    }
                }
            }
        }
    }
}
