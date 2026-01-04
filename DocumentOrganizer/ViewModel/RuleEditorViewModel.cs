using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentOrganizer.Services;
using DocumentTaggerCore;
using DocumentTaggerCore.Model;

namespace DocumentOrganizer.ViewModel
{
    public partial class RuleEditorViewModel : ObservableObject
    {
        private readonly IRuleService _ruleService;

        public List<string> KeyTypes { get; }

        [ObservableProperty]
        private ObservableCollection<Rule> _renameRules;

        [ObservableProperty]
        private ObservableCollection<Rule> _moveRules;  

        [ObservableProperty]
        private Rule? _selectedRule;

        [ObservableProperty]
        private KeyWord? _selectedKeyword;

        [ObservableProperty]
        private string? _selectedResult;


        public RuleEditorViewModel(IRuleService ruleService)
        {
            KeyTypes = new List<string> { KeyMod.MAY_INCLUDE, KeyMod.MUST_INCLUDE, KeyMod.NOT_INCLUDE };
            _ruleService = ruleService;

            RenameRules= new ObservableCollection<Rule>(_ruleService.GetRenameRules());
            MoveRules = new ObservableCollection<Rule>(_ruleService.GetMoveRules());
        }

        [RelayCommand]
        private void SaveRules()
        {
            _ruleService.SaveMoveRules(MoveRules);
            _ruleService.SaveRenameRules(RenameRules);
        }

        [RelayCommand]
        private void AddNewMoveRule()
        {
            MoveRules.Add(new Rule());
        }

        [RelayCommand]
        private void AddNewRenameRule()
        {
            RenameRules.Add(new Rule());
        }

        [RelayCommand]
        private void AddNewKeyword()
        {
            SelectedRule.Keywords.Add(new KeyWord(string.Empty, "May"));
        }

        [RelayCommand]
        private void RemoveKeyword()
        {
            SelectedRule.Keywords.Remove(SelectedKeyword);
        }

        [RelayCommand]
        private void AddNewResult()
        {
            SelectedRule.Results.Add(string.Empty);
        }

        [RelayCommand]
        private void RemoveResult()
        {
            SelectedRule.Results.Remove(SelectedResult);
        }

        [RelayCommand]
        private void RemoveRule()
        {
            if (RenameRules.Contains(SelectedRule))
            {
                RenameRules.Remove(SelectedRule);
            }

            if (MoveRules.Contains(SelectedRule))
            {
                MoveRules.Remove(SelectedRule);
            }
        }

        private static List<Rule> GetRules(string rulePath)
        {
            var ruleList = JsonIo.ReadObjectFromJsonFile<List<Rule>>(rulePath);

            return ruleList ?? [];
        }
    }
}
