using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DocumentTagger
{
    public class MoveFolderMonitor : AFolderMonitor
    {
        private object _lock = new object();
        private List<Rule> _rules;

        public MoveFolderMonitor(string watchedFolder, string processedSuccess, List<Rule> rules, ILogger<Worker> logger)
            : base(watchedFolder, processedSuccess, logger)
        {
            _rules = rules;

            this.InitialFolderScan(watchedFolder);
        }

        public void UpdateRules(List<Rule> newRules)
        {
            _rules.Clear();
            _rules.AddRange(newRules);
        }

        protected override void ConsumeNewFile()
        {
            foreach (var file in _inputQueue.GetConsumingEnumerable())
            {
                lock (_lock)
                {
                    if (File.Exists(file))
                    {
                        var ruleSet = RuleManager.GetApplicableRules(file, _rules);

                        var finalPaths = RuleManager.MoveToTargetLocations(file, _successFolder, ruleSet);

                        string message = $"Moved: {Path.GetFileName(file)} to {String.Join(',', finalPaths)}{Environment.NewLine}";

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
