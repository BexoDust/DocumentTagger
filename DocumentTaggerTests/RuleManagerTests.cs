using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using DocumentTaggerCore.Model;
using DocumentTaggerCore;

namespace DocumentTagger.Tests
{
    [TestClass()]
    public class RuleManagerTests
    {
        private List<Rule> GetTestRules()
        {
            var result = new List<Rule>();

            var rule = new Rule();
            rule.Results = new List<string> { "Empty" };
            result.Add(rule);

            rule = new Rule();
            rule.Keywords = new List<KeyWord> { new KeyWord("First", KeyMod.MUST_INCLUDE) };
            rule.Results = new List<string> { "First!" };
            result.Add(rule);

            rule = new Rule();
            rule.Keywords = new List<KeyWord> { new KeyWord("Second", KeyMod.MAY_INCLUDE), new KeyWord("Third", KeyMod.MAY_INCLUDE) };
            rule.Results = new List<string> { "D:\\" };
            result.Add(rule);

            rule = new Rule();
            rule.Keywords = new List<KeyWord> { new KeyWord("Alpha", KeyMod.MUST_INCLUDE), new KeyWord("Beta", KeyMod.MAY_INCLUDE), new KeyWord("Gamma", KeyMod.MAY_INCLUDE) };
            rule.Results = new List<string> { "D:\\" };
            result.Add(rule);

            return result;

        }

        [TestMethod()]
        public void GetApplicableRulesTest()
        {
            var rules = GetTestRules();

            Assert.AreEqual(0, RuleManager.GetApplicableRules(null, rules).Count);

            Assert.AreEqual(2, RuleManager.GetApplicableRules("First Second asd", rules).Count);

            Assert.AreEqual(1, RuleManager.GetApplicableRules("first", rules).Count);

            Assert.AreEqual(1, RuleManager.GetApplicableRules("SecondThird", rules).Count);

            Assert.AreEqual(1, RuleManager.GetApplicableRules("Second", rules).Count);

            Assert.AreEqual(1, RuleManager.GetApplicableRules("Third", rules).Count);

            Assert.AreEqual(0, RuleManager.GetApplicableRules("Alpha", rules).Count);

            Assert.AreEqual(1, RuleManager.GetApplicableRules("Alpha Beta", rules).Count);

            Assert.AreEqual(1, RuleManager.GetApplicableRules("GammaAlpha", rules).Count);

            Assert.AreEqual(0, RuleManager.GetApplicableRules("BetaGamma", rules).Count);
        }

        [TestMethod()]
        public void GetNewFileNameTest()
        {
            var rules = GetTestRules();
            string nameBase = DateTime.Now.ToString("yyyy-MM");
            string previousName = "TestFile.pdf";
            string finalName = nameBase + " Empty " + "First!" + ".pdf";

            Assert.AreEqual(finalName, RuleManager.GetNewFileName(previousName, nameBase, "", rules));
        }

        [TestMethod()]
        public void GetDocumentDateTest()
        {
            string defaultDate = DateTime.Now.ToString("yyyy-MM");
            string dateStringTest = "No date here";

            Assert.AreEqual(defaultDate, RuleManager.GetDocumentDate(dateStringTest));

            dateStringTest = "aaaaaaaaaaaaaaa20. Mai 2020bbbbbbbbbbb";
            Assert.AreEqual("2020-05", RuleManager.GetDocumentDate(dateStringTest));

            dateStringTest = "aaaaaaaaaaaaaaa20. Apr 2020bbbbbbbbbbb";
            Assert.AreEqual("2020-04", RuleManager.GetDocumentDate(dateStringTest));

            dateStringTest = "aaaaaaaaaaaaaaa20. April 2020bbbbbbbbbbb";
            Assert.AreEqual("2020-04", RuleManager.GetDocumentDate(dateStringTest));

            dateStringTest = "aaaaaaaaaaaaaaa20.04.2020bbbbbbbbbbb";
            Assert.AreEqual("2020-04", RuleManager.GetDocumentDate(dateStringTest));

            dateStringTest = "13.02.2013 14. März 2013 15. Apr 2013";
            Assert.AreEqual("2013-04", RuleManager.GetDocumentDate(dateStringTest));

            dateStringTest = "14. März 2013 15. Apr 2013 13.02.2013 ";
            Assert.AreEqual("2013-04", RuleManager.GetDocumentDate(dateStringTest));

            dateStringTest = "15. Apr 2013 14. März 2013  13.02.2013 ";
            Assert.AreEqual("2013-04", RuleManager.GetDocumentDate(dateStringTest));

            dateStringTest = "14. März 2013  13.02.2013 ";
            Assert.AreEqual("2013-03", RuleManager.GetDocumentDate(dateStringTest));

            dateStringTest = "  13.02.2013 14. März 2013";
            Assert.AreEqual("2013-03", RuleManager.GetDocumentDate(dateStringTest));

            dateStringTest = "Ist es Januar 2010?";
            Assert.AreEqual("2010-01", RuleManager.GetDocumentDate(dateStringTest));
        }
    }
}