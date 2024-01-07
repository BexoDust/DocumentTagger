using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DocumentOrganizer.Model;

public partial class Folder : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private ObservableCollection<Folder> _subFolders;

    [ObservableProperty]
    private ImageSource _imageIcon;

    [ObservableProperty]
    private string _fullPath;

    public Folder()
    {
        SubFolders = new ObservableCollection<Folder>();
    }

    public override string ToString() => Name;

    partial void OnNameChanged(string value)
    {
        var ext = Path.GetExtension(value);

        ImageIcon = ext switch
        {
            ".zip" => "zip.png",
            ".pdf" => "pdfimage.png",
            ".docx" => "word.png",
            "" => "folder.png",
            _ => "folder.png"
        };
    }
}
