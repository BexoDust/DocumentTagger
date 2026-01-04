using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls;

namespace DocumentOrganizer.Model;

public partial class Folder : ObservableObject
{
    [ObservableProperty]
    private Folder? _parent;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private ObservableCollection<Folder> _subFolders;

    [ObservableProperty]
    private ImageSource _imageIcon;

    [ObservableProperty]
    private string _fullPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilesInSubfoldersString))]
    private int _filesInSubfolders;

    public string FilesInSubfoldersString => $"({FilesInSubfolders:n0})";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileSizeString))]
    private double _fileSize;

    public string FileSizeString => $"({FileSize:n0} kB)";

    [ObservableProperty]
    private bool _isFile;

    [ObservableProperty]
    private bool _isFolder;

    [ObservableProperty]
    private bool _isChecked;

    [ObservableProperty]
    private bool _allFilesInSubfoldersChecked;

    public Folder()
    {
        SubFolders = [];
        SubFolders.CollectionChanged += this.SubFolders_CollectionChanged;

        Name = string.Empty;
        ImageIcon = "folder.png";
        FullPath = string.Empty;
    }

    private void SubFolders_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>
        RecountFiles();

    public void RecountFiles()
    {
        if (IsFolder)
        {
            var filesInFolder = SubFolders.Where(x => x.IsFile).Count();
            var filesInSubfolders = SubFolders.Where(x => x.IsFolder).Sum(y => y.FilesInSubfolders);
            FilesInSubfolders = filesInSubfolders + filesInFolder;

            //AllFilesInSubfoldersChecked = SubFolders.All(x => x.IsChecked);
        }
        else
            // Lazy coding. With this I don't need to do multi-binding in the UI :)
            FilesInSubfolders = 1;

        Parent?.RecountFiles();
    }

    public void SetChecked(bool isChecked)
    {
        var info = new FileInfo(FullPath);
        if (isChecked)
            info.Attributes |= FileAttributes.Device;
        else
            info.Attributes &= ~FileAttributes.Device;

        IsChecked = isChecked;
    }


    public override string ToString() => Name;

    partial void OnNameChanged(string value)
    {
        var ext = Path.GetExtension(value).ToLower();

        ImageIcon = ext switch
        {
            ".zip" => "zip.png",
            ".7z" => "zip.png",
            ".jpg" => "image.png",
            ".png" => "image.png",
            ".pdf" => "pdfimage.png",
            ".docx" => "word.png",
            ".doc" => "word.png",
            "" => "folder.png",
            _ => "folder.png"
        };
    }

    partial void OnFullPathChanged(string value)
    {
        IsFile = File.Exists(value);
        IsFolder = Directory.Exists(value);

        if (File.Exists(value))
        {
            var info = new FileInfo(value);

            FileSize = info.Length / 1024;
        }

        RecountFiles();
    }

    partial void OnIsCheckedChanged(bool value)
    {
        Parent?.RecountFiles();
    }
}
