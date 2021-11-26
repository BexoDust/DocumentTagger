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

        private readonly List<AFolderMonitor> _monitorList;
        private readonly ConfigMonitor _renameRulesMonitor;
        private readonly ConfigMonitor _moveRulesMonitor;

        public Worker(ILogger<Worker> logger, WorkerOptions options)
        {
            _logger = logger;
            _options = options;
            _monitorList = new List<AFolderMonitor>();

            _renameRulesMonitor = new ConfigMonitor(_options.RenameRulePath);
            _renameRulesMonitor.FileChanged += RenameRulesChangedCallback;
            _moveRulesMonitor = new ConfigMonitor(_options.MoveRulePath);
            _moveRulesMonitor.FileChanged += MoveRulesChangedCallback;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var renameRuleList = GetRules(_options.RenameRulePath);
            var moveRuleList = GetRules(_options.MoveRulePath);

            _monitorList.Add(new RenameFolderMonitor(_options.WatchRename, _options.FolderRenameSuccess, renameRuleList, _logger));
            _monitorList.Add(new CompressFolderMonitor(_options.WatchCompress, _options.FolderCompressSuccess,
                _options.CompressorToolPath, _options.CompressorToolOptions, _logger));
            _monitorList.Add(new MoveFolderMonitor(_options.WatchMove, _options.FolderMoveSuccess, moveRuleList, _logger));

            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }

            _logger.LogInformation("Stopping monitors");
            foreach (var monitor in _monitorList)
            {
                monitor.Stop();
            }
        }

        private List<Rule> GetRules(string rulePath)
        {
            var ruleList = JsonIo.ReadObjectFromJsonFile<List<Rule>>(rulePath);

            return ruleList;
        }

        private void RenameRulesChangedCallback(object sender, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
            {
                _logger.LogInformation("Rename rules changed: {0}", e.Name);

                var rules = GetRules(_options.RenameRulePath);

                if (rules != null)
                {
                    var renameMonitors = _monitorList.Select(y => y as RenameFolderMonitor);

                    foreach (var monitor in renameMonitors)
                    {
                        monitor?.UpdateRules(rules);
                    }
                }
            }
        }

        private void MoveRulesChangedCallback(object sender, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
            {
                _logger.LogInformation("Move rules changed: {0}", e.Name);

                var rules = GetRules(_options.MoveRulePath);

                if (rules != null)
                {
                    var moveMonitors = _monitorList.Select(y => y as MoveFolderMonitor);

                    foreach (var monitor in moveMonitors)
                    {
                        monitor?.UpdateRules(rules);
                    }
                }
            }
        }
    }
}
