using DocumentTaggerCore;
using DocumentTaggerCore.Model;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;

namespace DocumentTagger
{
    public class RenameFolderMonitor : AFolderMonitor
    {
        private List<Rule> _rules;
        private PdfContentExtractor _extractor = new PdfContentExtractor();
        private object _lock = new object();

        public RenameFolderMonitor(string watchedFolder, string processedSuccess, List<Rule> rules, ILogger<Worker> logger)
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
                        var text = _extractor.ExtractFileContent(file);

                        var ruleSet = RuleManager.GetApplicableRules(text, _rules);
                        string docDate = RuleManager.GetDocumentDate(text);
                        var newName = RuleManager.GetNewFileName(file, docDate, text, ruleSet);

                        var result = RuleManager.MoveToSuccessFolder(file, _successFolder, newName);

                        if (result != null)
                        {
                            string message = $"Renamed: {Path.GetFileName(file)} to {Path.GetFileName(newName)}";
                            _logger.LogInformation(message);
                        }
                        else
                        {
                            _logger.LogWarning($"{nameof(RenameFolderMonitor)}: Could not move file, because {file} was not found.");
                        }
                    }
                }
            }
        }
    }
}
