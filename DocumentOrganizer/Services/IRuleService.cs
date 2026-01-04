using DocumentTaggerCore.Model;

namespace DocumentOrganizer.Services
{
    public interface IRuleService
    {
        event EventHandler MoveRulesChanged;

        event EventHandler RenameRulesChanged;

        IEnumerable<Rule> GetMoveRules();

        IEnumerable<Rule> GetRenameRules();

        void SaveMoveRules(IEnumerable<Rule> rules);

        void SaveRenameRules(IEnumerable<Rule> rules);
    }

}
