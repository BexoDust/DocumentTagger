using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace DocumentTagger
{
    public class CompressFolderMonitor : AFolderMonitor
    {
        private object _lock = new object();

        private string _compressorToolPath;
        private string _compressorToolOptions;

        public CompressFolderMonitor(string watchedFolder, string processedSuccess,
            string compresorToolPath, string compressorToolOptions, ILogger<Worker> logger)
            : base(watchedFolder, processedSuccess, logger)
        {
            _compressorToolPath = compresorToolPath;
            _compressorToolOptions = compressorToolOptions;

            this.InitialFolderScan(watchedFolder);
        }

        protected override void ConsumeNewFile()
        {
            foreach (var file in _inputQueue.GetConsumingEnumerable())
            {
                lock (_lock)
                {
                    if (File.Exists(file))
                    {
                        var oldSize = new FileInfo(file).Length;
                        var targetFile = file.Replace(_watchedFolder, _successFolder);
                        var options = String.Format(_compressorToolOptions, targetFile, file);
                        var process = Process.Start(_compressorToolPath, options);

                        targetFile = RuleManager.GetUniqueNameInFolder(Path.GetDirectoryName(targetFile), targetFile);

                        process.WaitForExit();

                        if (File.Exists(targetFile))
                        {
                            var newSize = new FileInfo(targetFile).Length;

                            if (newSize > oldSize)
                            {
                                File.Delete(targetFile);
                                RuleManager.TryMoveFile(file, targetFile);
                                string message = $"Not Compressed: compressed size for {Path.GetFileName(file)} would've been bigger ({(newSize / (double)oldSize):P})";
                                _logger.LogWarning(message);
                            }
                            else
                            {
                                string message = $"Compressed: {Path.GetFileName(file)} ({(newSize / (double)oldSize):P})";
                                _logger.LogInformation(message);
                            }

                            File.Delete(file);
                        }
                        else
                        {
                            _logger.LogError($"Compressed file {targetFile} not found.");
                        }
                    }
                }
            }
        }
    }
}
