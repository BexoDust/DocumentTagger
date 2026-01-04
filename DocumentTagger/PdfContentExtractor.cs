using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.IO;

namespace DocumentTagger
{
    public class PdfContentExtractor : IContentExtractor
    {
        public string ExtractFileContent(string path)
        {
            string result = String.Empty;
            if (Path.GetExtension(path) != ".pdf")
                return result;

            using PdfReader pdfReader = new(path);
            using PdfDocument pdfDoc = new(pdfReader);

            for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
            {
                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                result += PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
            }

            return result;
        }
    }
}
