using DocumentOrganizer.Model;
using DocumentOrganizer.Services;
using DocumentOrganizer.ViewModel;

namespace DocumentOrganizer.Views;

public partial class FolderView : ContentPage
{
    private readonly IAlertService _alertService;
    private readonly FolderViewModel _folderViewModel;

    public FolderView()
    {
        _folderViewModel = ServiceHelper.GetService<FolderViewModel>();
        _alertService = ServiceHelper.GetService<IAlertService>();

        BindingContext = _folderViewModel;
        InitializeComponent();
    }

    private async void SfTreeView_ItemDoubleTapped(object sender, Syncfusion.Maui.TreeView.ItemDoubleTappedEventArgs e)
    {
        if (e.Node.HasChildNodes)
        {
            e.Node.IsExpanded = !e.Node.IsExpanded;
            return;
        }

        if (e.Node.Content is not Folder folder)
            return;

        var newName = await _alertService.ShowPromptAsync("Rename", "Please enter a new name for the file.", folder.Name);

        if (newName != null && newName != folder.Name)
        {
            _folderViewModel.RenameFile(folder, newName);
        }
    }
}