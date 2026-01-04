using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DocumentTagger.Tests
{
    [TestClass()]
    public class PdfContentExtractorTests
    {
        [TestMethod()]
        //[DeploymentItem("TestFiles")]
        public void ExtractFileContentTest()
        {
            string testFile = "TestFiles\\2020-03 IKK Neue Karte Martin.pdf";

            var extractor = new PdfContentExtractor();

            var result = extractor.ExtractFileContent(testFile);

            Assert.IsTrue(result.Contains("Gesundheitskarte"));
        }
    }
}