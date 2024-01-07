using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace DocumentTagger
{
    public class CompressFolderMonitor : AFolderMonitor
    {
        private readonly object _lock = new();

        private readonly string _compressorToolPath;
        private readonly string _compressorToolOptions;

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
                    try
                    {
                        if (!File.Exists(file))
                        {
                            _logger.LogWarning($"{nameof(CompressFolderMonitor)}: File from queue does not exist: {file}");
                            continue;
                        }

                        var oldSize = new FileInfo(file).Length;
                        var targetFile = file.Replace(_watchedFolder, _successFolder);
                        var options = String.Format(_compressorToolOptions, targetFile, file);
                        var info = new ProcessStartInfo(_compressorToolPath)
                        {
                            Arguments = options,
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            ErrorDialog = false,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };

                        _logger.LogWarning($"{nameof(CompressFolderMonitor)}: Starting compressor");
                        var process = Process.Start(info);

                        targetFile = RuleManager.GetUniqueNameInFolder(Path.GetDirectoryName(targetFile), targetFile);

                        _logger.LogWarning($"{nameof(CompressFolderMonitor)}: Waiting for compressor");
                        process.WaitForExit();
                        _logger.LogWarning($"{nameof(CompressFolderMonitor)}: Compressor finished");

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
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to compress the file {file}: {ex}");
                    }
                }
            }
        }
    }
}
