using CommandLine;

namespace DocCompressor
{
    internal class CommandLineOptions
    {
        [Option('s', "source", Required = true, HelpText = "The source folder with the uncompressed PDFs")]
        public string SourceFolder { get; set; }

        [Option('t', "target", Required = true, HelpText = "The target folder where the compressed PDFs shall be placed.")]
        public string TargetFolder { get; set; }
    }
}
