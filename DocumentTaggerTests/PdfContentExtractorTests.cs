using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace DocumentTagger.Tests
{
    [TestClass()]
    public class PdfContentExtractorTests
    {
        [TestMethod()]
        //[DeploymentItem("TestFiles")]
        public void ExtractFileContentTest()
        {
            string testFile = Path.Combine(AppContext.BaseDirectory, "TestFiles", "2020-03 IKK Neue Karte Martin.pdf");

            var extractor = new PdfContentExtractor();

            var result = extractor.ExtractFileContent(testFile);

            Assert.IsTrue(result.Contains("Gesundheitskarte"));
        }

        [TestMethod()]
        public void ExtractFileContent_CorruptPdf_ReturnsEmptyString()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");

            try
            {
                File.WriteAllText(tempFile, "this is not a valid pdf file");

                var extractor = new PdfContentExtractor();
                var result = extractor.ExtractFileContent(tempFile);

                Assert.IsTrue(string.IsNullOrEmpty(result));
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}