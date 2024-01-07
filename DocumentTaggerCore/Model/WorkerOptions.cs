namespace DocumentTaggerCore.Model
{
    public class WorkerOptions
    {
        public string WatchRename { get; set; } = "";
        public string WatchCompress { get; set; } = "";
        public string WatchMove { get; set; } = "";

        public string FolderRenameSuccess { get; set; } = "";
        public string FolderCompressSuccess { get; set; } = "";
        public string FolderMoveSuccess { get; set; } = "";

        public string RenameRulePath { get; set; } = "";
        public string MoveRulePath { get; set; } = "";
        public string LogPath { get; set; } = "";

        public string DocumentDestinationPath { get; set; } = ""; 

        public string CompressorToolPath { get; set; } = "";
        public string CompressorToolOptions { get; set; } = "";
    }
}
