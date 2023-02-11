using CommandLine;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DocCompressor
{
    public class Program
    {
        private static string profile = "user/Compress";
        private static string compressorToolPath = @"C:\Program Files\PDF24\pdf24-DocTool.exe";
        private static string compressorOptions = "-applyProfile -profile {0} -noProgress -outputFile \"{1}\" \"{2}\"";


        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                   .WithParsed(o =>
                   {
                       var files = Directory.GetFiles(o.SourceFolder, "*.*", SearchOption.AllDirectories);
                       var maxFileCount = files.Length;
                       double count = 0;

                       //Parallel.ForEach(files, file =>
                       foreach (var file in files)
                       {
                           var targetFile = file.Replace(o.SourceFolder, o.TargetFolder);

                           count++;
                           Console.WriteLine($"File {count} out of {maxFileCount} ({(count / maxFileCount):p})");

                           if (File.Exists(targetFile))
                               continue;

                           var directory = Path.GetDirectoryName(targetFile);

                           if (!Directory.Exists(directory))
                           {
                               Directory.CreateDirectory(directory);
                           }

                           var ext = Path.GetExtension(file);
                           if (Path.GetExtension(file) != ".pdf")
                           {
                               File.Copy(file, targetFile);
                           }
                           else
                           {
                               var options = String.Format(compressorOptions, profile, targetFile, file);
                               Console.WriteLine(compressorToolPath + " " + options);
                               var process = Process.Start(compressorToolPath, options);

                               process.WaitForExit();

                               if (process.ExitCode != 0)
                               {
                                   Console.WriteLine("Error code: " + process.ExitCode + process.StandardError);
                               }
                           }
                           //});
                       }
                   });
        }
    }
}