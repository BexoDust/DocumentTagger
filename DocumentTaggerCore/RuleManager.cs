using System.Globalization;
using System.Text.RegularExpressions;
using DocumentTaggerCore.Model;

namespace DocumentTaggerCore
{
    public class RuleManager
    {
        private static readonly string _docDateFormat = "yyyy-MM";
        private static List<(string regexRule, string dateFormat)> _dateFormats = new List<(string, string)>
        {
            // the date only starts with the 2000 years to prevent the search from finding birth days (at least from adults)
            (@"(Januar|Februar|März|April|Mai|Juni|Juli|August|September|Oktober|November|Dezember) 20\d{2}", "MMMM yyyy"), // Januar 2010
            (@"\s\d{2}\/20\d{2}", " MM/yyyy"), // 01/2010
            (@"\d{2}\. \w{3} 20\d{2}", "dd. MMM yyyy"), // 20. Jan 2010
            (@"\d{2}\. \w{3}\. 20\d{2}", "dd. MMM. yyyy"), // 20. Jan. 2010
            (@"\d{2}\. \w+ 20\d{2}", "dd. MMMM yyyy"), // 20. Januar 2010
            (@"\d{2}\.\d{2}\.20\d{2}", "dd.MM.yyyy"), // 20.01.2010
            (@"\d{2}\,\d{2}\,20\d{2}", "dd,MM,yyyy"), // 20,01,2010 - detection tolerance
            (@"\d{2}\.\d{2}\.\d{2}", "dd.MM.yy"), // 20.01.10
            (@"\d{2}\-\d{2}\-20\d{2}", "dd-MM-yyyy"), // 20-01-2010
            (@"20\d{2}-\d{2}-\d{2}", "yyyy-MM-dd"), // 2010-01-20            
        };

        private static List<string> _getSubjectFormats = new List<string>
        {
             @"((.*\n))Sehr geehrte",
             @"((.*\n))Lieber Herr",
             @"((.*\n))Liebe Frau",
             @"((.*\n))Guten Tag "
        };

        public static List<Rule> GetApplicableRules(string fileContent, List<Rule> allRules)
        {
            List<Rule> fittingRules = new List<Rule>();

            if (string.IsNullOrWhiteSpace(fileContent))
            {
                return fittingRules;
            }

            foreach (var rule in allRules)
            {
                if (DoesRuleApply(fileContent, rule))
                {
                    fittingRules.Add(rule);
                }
            }

            return fittingRules;
        }

        private static bool DoesRuleApply(string fileContent, Rule rule)
        {
            bool? result = null;
            bool multiMay = rule.Keywords.Any() && rule.Keywords.All(x => x.Modifier != KeyMod.MAY_INCLUDE);

            foreach (var key in rule.Keywords)
            {
                switch (key.Modifier)
                {
                    case KeyMod.MAY_INCLUDE:
                        multiMay |= Contains(fileContent, key.Key);
                        break;
                    case KeyMod.MUST_INCLUDE:
                        result = result == null || result == true ? Contains(fileContent, key.Key)
                            : false;
                        break;
                    case KeyMod.NOT_INCLUDE:
                        result = result == null || result == true ? !Contains(fileContent, key.Key)
                            : false;
                        break;
                }
            }

            result = result == null ? multiMay : (bool)result && multiMay;

            return result ?? false;
        }

        public static string GetDocumentDate(string content)
        {
            string dateString = DateTime.Now.ToString(_docDateFormat);
            var myCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            myCulture.Calendar.TwoDigitYearMax = 2079;

            foreach (var format in _dateFormats)
            {
                try
                {
                    Regex rgx = new Regex(format.regexRule);
                    Match match = rgx.Match(content);

                    if (match.Success)
                    {
                        dateString = match.Value;
                        var date = DateTime.ParseExact(dateString, format.dateFormat, myCulture);
                        dateString = date.ToString(_docDateFormat);
                        break;
                    }
                }
                catch
                {

                }
            }

            return dateString;
        }

        public static string? GetNewFileName(string filePath, string documentDate, string content, List<Rule> rules)
        {
            string extension = Path.GetExtension(filePath);
            string fileName = documentDate;

            if (rules.Any())
            {
                foreach (var rule in rules)
                {
                    fileName = ApplyTagName(fileName, rule);
                }

            }

            fileName += GetLetterSubject(content);
            fileName += $"{extension}";

            var pathDirectory = Path.GetDirectoryName(filePath);
            return pathDirectory != null ? Path.Combine(pathDirectory, fileName) : null;
        }

        private static string GetLetterSubject(string content)
        {
            foreach (var subject in _getSubjectFormats)
            {
                Regex rgx = new Regex(subject);
                Match match = rgx.Match(content);

                if (match.Success)
                {
                    return " " + RemoveInvalidFilePathCharacters(match.Groups[1].Value);
                }
            }

            return string.Empty;
        }

        private static string RemoveInvalidFilePathCharacters(string filename)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(filename.Trim(), string.Empty);
        }

        public static string? MoveToSuccessFolder(string filePath, string successFolder, string? newName)
        {
            string newFilePath = string.Empty;

            if (!File.Exists(filePath))
            {
                return null;
            }

            if (Directory.Exists(successFolder))
            {
                var name = string.IsNullOrWhiteSpace(newName) ? Path.GetFileName(filePath) : newName;
                newFilePath = GetUniqueNameInFolder(successFolder, name);

                TryMoveFile(filePath, newFilePath);
            }

            return newFilePath;
        }

        public static void TryMoveFile(string filePath, string newFilePath)
        {
            int maxTryCount = 3;
            int tries = 0;
            bool success = false;

            while (!success || tries < maxTryCount)
            {
                try
                {
                    tries++;
                    File.Move(filePath, newFilePath);
                    success = true;
                }
                catch (IOException)
                {
                    Thread.Sleep(500);
                }
            }
        }

        public static List<string> MoveToTargetLocations(string filePath, string successFolder, List<Rule> rules)
        {
            var result = new List<string>();

            if (!File.Exists(filePath))
            {
                return result;
            }
            string fileName = Path.GetFileName(filePath);

            foreach (var rule in rules)
            {
                if (rule.Results == null)
                {
                    continue;
                }

                foreach (var location in rule.Results)
                {
                    result.Add(location);
                }
            }

            result = result.Distinct().ToList();

            if (result.Count > 0)
            {
                foreach (var subFolder in result)
                {
                    var newPath = Path.Combine(successFolder, subFolder);
                    Directory.CreateDirectory(newPath);
                    File.Copy(filePath, GetUniqueNameInFolder(newPath, fileName));
                }

            }
            else
            {
                File.Copy(filePath, GetUniqueNameInFolder(successFolder, fileName));
            }

            File.Delete(filePath);

            return result;
        }

        private static string ApplyTagName(string fileName, Rule rule)
        {
            string finalName = fileName;

            if (rule.Results.Any())
            {
                foreach (var result in rule.Results)
                {
                    if (!fileName.Contains(result))
                    {
                        finalName = $"{finalName} {result}";
                    }
                }
            }

            return finalName;
        }

        public static string GetUniqueNameInFolder(string path, string nameWithExt)
        {
            string extension = Path.GetExtension(nameWithExt);
            string name = Path.GetFileNameWithoutExtension(nameWithExt);
            string result = Path.Combine(path, name + extension);
            int count = 1;

            while (File.Exists(result))
            {
                result = Path.Combine(path, $"{name}({count++}){extension}");
            }

            return result;
        }

        private static bool Contains(string source, string token)
        {
            var rule = token.Replace(" ", @"\s");

            Regex rgx = new Regex(rule, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            Match match = rgx.Match(source);

            return match.Success;
        }
    }
}
