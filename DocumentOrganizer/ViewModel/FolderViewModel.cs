using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentOrganizer.Model;
using DocumentOrganizer.Services;
using DocumentTagger;
using DocumentTaggerCore;
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
        //[NotifyPropertyChangedFor(nameof(ScanFolder))]
        private ObservableCollection<Folder> _targetFolder;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteSelectedFolderCommand))]
        private Folder? _selectedFolder;

        [ObservableProperty]
        private Stream? _pdfFileStream;

        public FolderViewModel(WorkerOptions options, IAlertService alertService, IRuleService ruleService)
        {
            _options = options;
            _alertService = alertService;
            _ruleService = ruleService;
            ScanFolder = [];
            TargetFolder = [];

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

        private void MoveRulesChanged(object? sender, EventArgs e) => _moveRules = _ruleService.GetMoveRules();

        private void RenameRulesChanged(object? sender, EventArgs e) => _renameRules = _ruleService.GetRenameRules();

        private bool CanDeleteSelectedFolder() => SelectedFolder?.Parent != null;

        [RelayCommand(CanExecute = nameof(CanDeleteSelectedFolder))]
        private void DeleteSelectedFolder()
        {
            SelectedFolder!.Parent!.SubFolders.Remove(SelectedFolder);

            var path = SelectedFolder.FullPath;
            SelectedFolder = SelectedFolder.Parent;

            PdfFileStream = null;
            try
            {
                File.Delete(path);
            }
            catch (Exception)
            {
            }
        }

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
            Directory.CreateDirectory(_options.FolderMoveSuccess);

            CopyFolders(_options.FolderMoveSuccess, TargetFolder.First());

            var scan = new Folder();
            InitFolder(_options.FolderMoveSuccess, scan);
            ScanFolder.Clear();
            ScanFolder.Add(scan);
        }


        [RelayCommand]
        private void MoveFilesFromScanToTarget()
        {
            var scanBasePath = ScanFolder.First().FullPath;
            var targetBasePath = TargetFolder.First().FullPath;

            var scanFiles = Directory.GetFiles(ScanFolder.First().FullPath, "*.*", SearchOption.AllDirectories);

            foreach (var scanFile in scanFiles)
            {
                var newPath = scanFile.Replace(scanBasePath, targetBasePath);
                var checkDuplicatePath = RuleManager.GetUniqueNameInFolder(Path.GetDirectoryName(newPath), Path.GetFileName(newPath));

                File.Move(scanFile, checkDuplicatePath);
            }

            var scan = new Folder();
            InitFolder(_options.FolderMoveSuccess, scan);
            ScanFolder.Clear();
            ScanFolder.Add(scan);
        }

        private void CopyFolders(string path, Folder folder)
        {
            foreach (var subDir in folder.SubFolders)
            {
                if (subDir.IsFile)
                    continue;

                var asd = Path.GetFileName(_options.WatchBaseFolder);
                if (subDir.Name == Path.GetFileName(_options.WatchBaseFolder))
                    continue;

                var subFolder = Path.Combine(path, subDir.Name);

                Directory.CreateDirectory(subFolder);
                CopyFolders(subFolder, subDir);
            }
        }

        internal void RenameFile(Folder sourceFolder, string newName)
        {
            ClosePdfView();

            var targetPath = RuleManager.GetUniqueNameInFolder(sourceFolder.Parent!.FullPath, newName);
            File.Move(sourceFolder.FullPath, targetPath);

            UpdateRenamedFolder(ScanFolder.First(), sourceFolder.FullPath, targetPath);
            UpdateRenamedFolder(TargetFolder.First(), sourceFolder.FullPath, targetPath);
        }

        internal void MoveFile(Folder sourceFolder, Folder targetFolder)
        {
            ClosePdfView();

            sourceFolder!.Parent!.SubFolders.Remove(sourceFolder);
            targetFolder.SubFolders.Add(sourceFolder);

            var targetPath = RuleManager.GetUniqueNameInFolder(targetFolder.FullPath, sourceFolder.Name);
            File.Move(sourceFolder.FullPath, targetPath);

            sourceFolder.FullPath = targetPath;
            sourceFolder.Parent = targetFolder;
            sourceFolder.Name = Path.GetFileName(targetPath);
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

        private void InitFolder(string path, Folder folder, Folder? parent = null)
        {
            var dir = new DirectoryInfo(path);
            folder.Name = dir.Name;
            folder.FullPath = dir.FullName;
            folder.Parent = parent;

            foreach (var subDir in dir.EnumerateDirectories())
            {
                var subFolder = new Folder();
                InitFolder(subDir.FullName, subFolder, folder);
                folder.SubFolders.Add(subFolder);
            }

            foreach (var subFile in dir.EnumerateFiles())
            {
                var file = new Folder
                {
                    Name = subFile.Name,
                    FullPath = subFile.FullName,
                    Parent = folder
                };
                folder.SubFolders.Add(file);
            }
        }

        private List<Rule> GetAppliedRules(string fileName)
        {
            var rules = new List<Rule>();

            foreach (var rule in _renameRules)
            {
                if (rule.Results.All(x => fileName.Contains(x)))
                    rules.Add(rule);
            }

            return rules;
        }

        partial void OnPdfFileStreamChanging(Stream? oldValue, Stream? newValue)
        {
            ClosePdfView();
        }

        partial void OnSelectedFolderChanged(Folder? oldValue, Folder? newValue)
        {
            if (!File.Exists(newValue?.FullPath))
                return;

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
