﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading;

namespace DocumentTagger
{
    public class RuleManager
    {
        private static readonly string _docDateFormat = "yyyy-MM";
        private static List<(string regexRule, string dateFormat)> _dateFormats = new List<(string, string)>
        {
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

        public static List<Tag> GetApplicableRules(string fileContent, List<Tag> allRules)
        {
            List<Tag> fittingRules = new List<Tag>();

            if (String.IsNullOrWhiteSpace(fileContent))
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

        private static bool DoesRuleApply(string fileContent, Tag rule)
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

        public static string GetNewFileName(string filePath, string documentDate, string content, List<Tag> rules)
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

            return fileName;
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

            return String.Empty;
        }

        private static string RemoveInvalidFilePathCharacters(string filename)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(filename.Trim(), String.Empty);
        }

        public static string MoveToDefaultLocation(string filePath, string defaultLocation, string newName)
        {
            string newFilePath = String.Empty;

            if (!File.Exists(filePath))
            {
                return null;
            }

            if (Directory.Exists(defaultLocation))
            {
                newFilePath = GetUniqueNameInFolder(defaultLocation, newName);

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

            return newFilePath;
        }

        public static List<string> MoveToNewLocation(string filePath, List<Tag> rules)
        {
            var result = new List<string>();

            if (!File.Exists(filePath))
            {
                return result;
            }
            string fileName = Path.GetFileName(filePath);

            foreach (var rule in rules)
            {
                if (rule.MoveLocations == null)
                {
                    continue;
                }

                foreach (var location in rule.MoveLocations)
                {
                    if (Directory.Exists(location))
                    {
                        result.Add(location);
                    }
                }
            }

            result = result.Distinct().ToList();

            if (result.Count > 0)
            {
                foreach (var loc in result)
                {
                    File.Copy(filePath, GetUniqueNameInFolder(loc, fileName));
                }

                File.Delete(filePath);
                SystemSounds.Asterisk.Play();
            }

            return result;
        }

        private static string ApplyTagName(string fileName, Tag rule)
        {
            string result = fileName;

            if (!String.IsNullOrWhiteSpace(rule.AddedFileWord) && !fileName.Contains(rule.AddedFileWord))
            {
                result = $"{fileName} {rule.AddedFileWord}";
            }

            return result;
        }

        private static string GetUniqueNameInFolder(string path, string nameWithExt)
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