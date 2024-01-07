using CommunityToolkit.Mvvm.ComponentModel;

namespace DocumentOrganizer.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private RuleEditorViewModel _ruleViewModel;

        [ObservableProperty]
        private FolderViewModel _folderViewModel;

        public MainViewModel(RuleEditorViewModel ruleViewModel, FolderViewModel folderViewModel)
        {
            _ruleViewModel = ruleViewModel;
            _folderViewModel = folderViewModel;
        }
    }
}
