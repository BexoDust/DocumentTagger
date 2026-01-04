using DocumentOrganizer.Model;
using DocumentOrganizer.Services;
using DocumentOrganizer.ViewModel;
using Microsoft.Maui.Controls;
using Syncfusion.Maui.TreeView;

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

    private async void SfTreeView_ItemDoubleTapped(object sender, ItemDoubleTappedEventArgs e)
    {
        if (e?.Node == null)
            return;

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

    private void DragGestureRecognizer_DragStarting(object sender, DragStartingEventArgs e)
    {
        var gesture = sender as DragGestureRecognizer;

        if (gesture?.BindingContext is Folder folderViewModel)
        {
            if (folderViewModel.IsFolder)
            {
                e.Cancel = true;
                return;
            }

            e.Data.Properties.Add("folder", folderViewModel);
            e.Data.Text = "Test Text";
        }
    }

    private void DropGestureRecognizer_Drop(object sender, DropEventArgs e)
    {
        var gesture = sender as DropGestureRecognizer;

        if (!(gesture?.BindingContext is Folder dropFolder))
            return;

        var folder = dropFolder.IsFolder ? dropFolder : dropFolder.Parent;

        if (e.Data.Properties["folder"] is Folder dragFolder && folder != null)
        {
            _folderViewModel.MoveFile(dragFolder, folder);
        }
    }
}
