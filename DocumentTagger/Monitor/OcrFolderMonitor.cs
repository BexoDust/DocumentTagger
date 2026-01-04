using DocumentTaggerCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace DocumentTagger
{
    public class OcrFolderMonitor : AFolderMonitor
    {
        private readonly object _lock = new();

        private PdfContentExtractor _extractor = new PdfContentExtractor();
        private readonly string _ocrToolPath;
        private readonly string _ocrToolOptions;

        public OcrFolderMonitor(string watchedFolder, string processedSuccess,
            string ocrToolPath, string ocrToolOptions, ILogger<Worker> logger)
            : base(watchedFolder, processedSuccess, logger)
        {
            _ocrToolPath = ocrToolPath;
            _ocrToolOptions = ocrToolOptions;

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
                            _logger.LogWarning($"{nameof(OcrFolderMonitor)}: File from queue does not exist: {file}");
                            continue;
                        }


                        var targetFile = file.Replace(_watchedFolder, _successFolder);
                        targetFile = RuleManager.GetUniqueNameInFolder(Path.GetDirectoryName(targetFile), targetFile);
                        var text = _extractor.ExtractFileContent(file);

                        if (!string.IsNullOrEmpty(text)) 
                        {
                            _logger.LogDebug($"{nameof(OcrFolderMonitor)}: File already has text, no need for OCR.");

                            RuleManager.TryMoveFile(file, targetFile);

                            continue;
                        }

                        var options = String.Format(_ocrToolOptions, file, file);
                        var info = new ProcessStartInfo(_ocrToolPath)
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

                        _logger.LogWarning($"{nameof(OcrFolderMonitor)}: Starting character recognition");
                        var process = Process.Start(info);


                        _logger.LogWarning($"{nameof(OcrFolderMonitor)}: Waiting for OCR");
                        process.WaitForExit(20000);
                        _logger.LogWarning($"{nameof(OcrFolderMonitor)}: OCR finished");

                        text = _extractor.ExtractFileContent(file);

                        if (!string.IsNullOrEmpty(text))
                        {
                            RuleManager.TryMoveFile(file, targetFile);
                        }
                        else
                        {
                            _logger.LogError($"No text recognized for: {targetFile}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to recognize text in the file {file}: {ex}");
                    }
                }
            }
        }
    }
}
