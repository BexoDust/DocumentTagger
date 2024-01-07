using DocumentTaggerCore;
using DocumentTaggerCore.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace DocumentTagger.Tests
{
    [TestClass()]
    public class JsonIoTests
    {
        private Rule CreateTestRenameRule(int i = 0)
        {
            var result = new Rule();

            result.Results = new ObservableCollection<string> { "Bau " + i, "Aua" };
            result.Keywords = new ObservableCollection<KeyWord> { new KeyWord("Ei", KeyMod.MAY_INCLUDE), new KeyWord("Papa", KeyMod.MAY_INCLUDE) };

            return result;
        }

        private WorkerOptions CreateTestConfig()
        {
            var result = new WorkerOptions();

            result.RenameRulePath = @"D:\Daten\NextCloud\Dokumente\Sonstiges";
            result.FolderRenameSuccess = @"D:\Daten\NextCloud\Dokumente\_Eingang\Bearbeitet";
            result.WatchCompress = @"D:\Daten\NextCloud\Dokumente\_Eingang\Analysieren";

            return result;
        }

        [TestMethod()]
        public void SerializeSingleRenameRuleTest()
        {
            var tag = CreateTestRenameRule();
            string path = @"D:\Daten\ExampleRule.json";

            JsonIo.SaveObjectToJson(tag, path);
            Assert.IsTrue(File.Exists(path));
        }

        [TestMethod()]
        public void SerializeMultipleRenameRuleTest()
        {
            int i = 1;
            var tagList = new List<Rule>();
            tagList.Add(CreateTestRenameRule(i++));
            tagList.Add(CreateTestRenameRule(i++));

            string path = @"D:\Daten\ExampleRules.json";

            JsonIo.SaveObjectToJson(tagList, path);
            Assert.IsTrue(File.Exists(path));
        }

        [TestMethod()]
        [DeploymentItem("TestFiles")]
        public void DeserializeMultipleTagsTest()
        {
            string testFile = "TestFiles\\ExampleRules.json";

           var result = JsonIo.ReadObjectFromJsonFile<List<Rule>>(testFile);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(x => x.Results.Contains("Bau 1")));
            Assert.IsTrue(result.Any(x => x.Results.Contains("Bau 2")));
        }

        [TestMethod()]
        public void SerializeConfigTest()
        {
            var config = CreateTestConfig();
            string path = @"D:\Daten\ExampleConfig.json";

            JsonIo.SaveObjectToJson(config, path);
            Assert.IsTrue(File.Exists(path));
        }
    }
}