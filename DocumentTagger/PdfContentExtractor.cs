using iText.IO.Exceptions;
using iText.Kernel.Exceptions;
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

            try
            {
                using PdfReader pdfReader = new(path);
                using PdfDocument pdfDoc = new(pdfReader);

                for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                {
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    result += PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
                }
            }
            catch (BadPasswordException)
            {
                return string.Empty;
            }
            catch (PdfException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
            catch (NotSupportedException)
            {
                return string.Empty;
            }

            return result;
        }
    }
}
