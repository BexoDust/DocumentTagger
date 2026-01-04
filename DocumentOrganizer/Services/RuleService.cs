using DocumentTaggerCore;
using DocumentTaggerCore.Model;

namespace DocumentOrganizer.Services
{
    internal class RuleService : IRuleService
    {
        private readonly WorkerOptions _options;

        public event EventHandler? MoveRulesChanged;
        public event EventHandler? RenameRulesChanged;

        public RuleService(WorkerOptions options)
        {
            _options = options;
        }

        public IEnumerable<Rule> GetMoveRules() => GetRules(_options.MoveRulePath);

        public IEnumerable<Rule> GetRenameRules() => GetRules(_options.RenameRulePath);

        public void SaveMoveRules(IEnumerable<Rule> rules)
        {
            JsonIo.SaveObjectToJson(rules, _options.MoveRulePath);
            MoveRulesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SaveRenameRules(IEnumerable<Rule> rules)
        {
            JsonIo.SaveObjectToJson(rules, _options.RenameRulePath);
            RenameRulesChanged?.Invoke(this, EventArgs.Empty);
        }

        private static List<Rule> GetRules(string rulePath)
        {
            var ruleList = JsonIo.ReadObjectFromJsonFile<List<Rule>>(rulePath);

            return ruleList ?? [];
        }
    }
}
