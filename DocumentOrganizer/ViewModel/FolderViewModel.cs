using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentOrganizer.Model;
using DocumentOrganizer.Services;
using DocumentTaggerCore.Model;

namespace DocumentOrganizer.ViewModel
{
    public partial class FolderViewModel : ObservableObject
    {
        private readonly WorkerOptions _options;
        private readonly IAlertService _alertService;
        private readonly IRuleService _ruleService;

        private IEnumerable<Rule> _moveRules;

        private IEnumerable<Rule> _renameRules;

        [ObservableProperty]
        private ObservableCollection<Folder> _scanFolder;

        [ObservableProperty]
        private ObservableCollection<Folder> _targetFolder;

        [ObservableProperty]
        private Folder _selectedFolder;

        [ObservableProperty]
        private Stream _pdfFileStream;

        public FolderViewModel(WorkerOptions options, IAlertService alertService, IRuleService ruleService)
        {
            _options = options;
            _alertService = alertService;
            _ruleService = ruleService;
            ScanFolder = new ObservableCollection<Folder>();
            TargetFolder = new ObservableCollection<Folder>();

            _moveRules = _ruleService.GetMoveRules();
            _renameRules = _ruleService.GetRenameRules();

            _ruleService.MoveRulesChanged += MoveRulesChanged;
            _ruleService.RenameRulesChanged += RenameRulesChanged;

            var scan = new Folder();
            var target = new Folder();
            InitFolder(_options.FolderMoveSuccess, scan);
            InitFolder(_options.DocumentDestinationPath, target);

            ScanFolder.Add(scan);
            TargetFolder.Add(target);
        }

        private void MoveRulesChanged(object sender, EventArgs e) => _moveRules = _ruleService.GetMoveRules();

        private void RenameRulesChanged(object sender, EventArgs e) => _renameRules = _ruleService.GetRenameRules();

        [RelayCommand]
        private void CopyFolderStructureFromTargetToScan()
        {
            var dir = new DirectoryInfo(_options.FolderMoveSuccess);

            if (dir.EnumerateFiles("*", SearchOption.AllDirectories).Any())
            {
                _alertService.ShowAlert("Can't copy structure", "The structure can't be copied, because there" +
                    " are still some files left in the scan folder.");
                return;
            }

            Directory.Delete(_options.FolderMoveSuccess, true);

            CopyFolders(_options.FolderMoveSuccess, TargetFolder.First());
        }

        private void CopyFolders(string path, Folder folder)
        {
            foreach (var subDir in folder.SubFolders)
            {
                var subFolder = Path.Combine(path, subDir.Name);
                Directory.CreateDirectory(subFolder);

                CopyFolders(subFolder, subDir);
            }
        }

        internal void RenameFile(Folder sourceFolder, string newName)
        {
            ClosePdfView();

            var targetPath = Path.Combine(Path.GetDirectoryName(sourceFolder.FullPath), newName);
            File.Move(sourceFolder.FullPath, targetPath);

            UpdateRenamedFolder(ScanFolder.First(), sourceFolder.FullPath, targetPath);
            UpdateRenamedFolder(TargetFolder.First(), sourceFolder.FullPath, targetPath);
        }

        private void UpdateRenamedFolder(Folder baseFolder, string oldPath, string newPath)
        {
            foreach (var folder in baseFolder.SubFolders)
            {
                if (folder.FullPath == oldPath)
                {
                    folder.FullPath = newPath;
                    folder.Name = Path.GetFileName(newPath);
                }
                else if (folder.SubFolders.Any())
                    UpdateRenamedFolder(folder, oldPath, newPath);
            }
        }

        private void InitFolder(string path, Folder folder)
        {
            var dir = new DirectoryInfo(path);
            folder.Name = dir.Name;
            folder.FullPath = dir.FullName;

            foreach (var subDir in dir.EnumerateDirectories())
            {
                var subFolder = new Folder();
                InitFolder(subDir.FullName, subFolder);
                folder.SubFolders.Add(subFolder);
            }

            foreach (var subFile in dir.EnumerateFiles())
            {
                var file = new Folder
                {
                    Name = subFile.Name,
                    FullPath = subFile.FullName
                };
                folder.SubFolders.Add(file);
            }
        }

        private IList<Rule> GetAppliedRules(string fileName)
        {
            var rules = new List<Rule>();

            foreach (var rule in _renameRules)
            {
                if (rule.Results.All(x => fileName.Contains(x)))
                    rules.Add(rule);
            }

            return rules;
        }

        partial void OnPdfFileStreamChanging(Stream oldValue, Stream newValue)
        {
            ClosePdfView();
        }

        partial void OnSelectedFolderChanged(Folder oldValue, Folder newValue)
        {
            var ext = Path.GetExtension(newValue.Name);

            if (ext == ".pdf")
            {
                ClosePdfView();

                PdfFileStream = new FileStream(newValue.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
        }

        private void ClosePdfView()
        {
            PdfFileStream?.Close();
            PdfFileStream?.Dispose();
        }
    }
}
