using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;

namespace DocumentTagger
{
    public class PdfContentExtractor : IContentExtractor
    {
        public string ExtractFileContent(string path)
        {
            string result = String.Empty;
            PdfReader pdfReader = new PdfReader(path);
            PdfDocument pdfDoc = new PdfDocument(pdfReader);

            for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
            {
                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                result += PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
            }

            pdfDoc.Close();
            pdfReader.Close();

            return result;
        }
    }
}
