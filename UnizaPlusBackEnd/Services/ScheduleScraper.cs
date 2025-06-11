using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UnizaPlusBackEnd.Models;

namespace UnizaPlus.Services
{
    public class ScheduleScraper
    {

        private Dictionary<string, string> _professorCache = new Dictionary<string, string>();
        private Dictionary<string, string> _classroomCache = new Dictionary<string, string>();
        private Dictionary<string, (string code, string fullName)> _subjectCache = new Dictionary<string, (string, string)>();

        public List<ScheduleItem> ExtractScheduleData(IWebDriver driver)
        {
            var scheduleItems = new List<ScheduleItem>();
            string originalUrl = driver.Url;

            try
            {
                var days = GetScheduleDays(driver);

                foreach (var day in days)
                {
                    Console.WriteLine($"Processing day: {day}");
                    var dayItems = ProcessDay(driver, day, originalUrl);
                    scheduleItems.AddRange(dayItems);
                }

                return scheduleItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting schedule data: {ex.Message}");
                return scheduleItems;
            }
        }

        private List<string> GetScheduleDays(IWebDriver driver)
        {
            var days = new List<string>();
            var dayElements = driver.FindElements(By.CssSelector(".rozvrh_nazov"));

            foreach (var element in dayElements)
            {
                days.Add(element.Text);
            }

            return days;
        }

        private List<ScheduleItem> ProcessDay(IWebDriver driver, string day, string originalUrl)
        {
            var items = new List<ScheduleItem>();
            var basicItems = new List<(ScheduleItem item, string blockClass)>();

            try
            {
                driver.Navigate().GoToUrl(originalUrl);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(d => d.FindElements(By.CssSelector(".rozvrh_tyzden")).Count > 0);
                Thread.Sleep(500);

                var dayRows = driver.FindElements(By.CssSelector(".rozvrh_tyzden"));
                var dayRow = dayRows.FirstOrDefault(row =>
                {
                    try
                    {
                        return row.FindElement(By.CssSelector(".rozvrh_nazov")).Text == day;
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (dayRow == null)
                {
                    Console.WriteLine($"Could not find row for day: {day}");
                    return items;
                }

                var blocks = dayRow.FindElements(By.CssSelector("span[class^='rozvrh_bloky']"));
                for (int i = 0; i < blocks.Count; i++)
                {
                    try
                    {
                        var block = blocks[i];
                        var blockClass = block.GetAttribute("class");
                        var classType = GetClassType(blockClass);

                        if (classType != "")
                        {
                            var item = new ScheduleItem
                            {
                                Day = day,
                                StartHour = i + 7, 
                                Type = classType,
                                Duration = 1 
                            };

                            int blockIndex = i + 1;
                            while (blockIndex < blocks.Count &&
                                   blocks[blockIndex].GetAttribute("class").Contains("-c"))
                            {
                                item.Duration++;
                                i++;
                                blockIndex++;
                            }

                            if (ExtractBasicClassInfo(block, item))
                            {
                                
                                basicItems.Add((item, blockClass));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error collecting basic class info: {ex.Message}");
                        continue;
                    }
                }

                foreach (var (item, _) in basicItems)
                {
                    try
                    {
                        ExtractDetailedInfo(driver, item, originalUrl);
                        items.Add(item);
                        Console.WriteLine($"Found class: {item}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing detailed info: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing day {day}: {ex.Message}");
            }

            return items;
        }

        private bool ExtractBasicClassInfo(IWebElement block, ScheduleItem item)
        {
            try
            {
                var links = block.FindElements(By.TagName("a"));
                if (links.Count >= 4)
                {
                    item.Professor = links[0].Text;
                    item.Classroom = links[1].Text;
                    item.Subject = links[2].Text;
                    item.StudentGroups = links[3].Text;

                    item.ProfessorLink = links[0].GetAttribute("href");
                    item.ClassroomLink = links[1].GetAttribute("href");
                    item.SubjectLink = links[2].GetAttribute("href");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting basic class info: {ex.Message}");
            }
            return false;
        }

        private void ExtractDetailedInfo(IWebDriver driver, ScheduleItem item, string originalUrl)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                ExtractProfessorDetails(driver, item, wait);
                ExtractClassroomDetails(driver, item, wait);
                ExtractSubjectDetails(driver, item, wait);

                driver.Navigate().GoToUrl(originalUrl);
                wait.Until(d => d.FindElements(By.CssSelector(".rozvrh_tyzden")).Count > 0);
                Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting detailed info: {ex.Message}");
                try
                {
                    driver.Navigate().GoToUrl(originalUrl);
                }
                catch
                {
                    Console.WriteLine("Failed to return to schedule page after error");
                }
            }
        }

        private void ExtractProfessorDetails(IWebDriver driver, ScheduleItem item, WebDriverWait wait)
        {
            if (string.IsNullOrEmpty(item.ProfessorLink))
                return;

            string basicName = item.Professor;

            if (_professorCache.ContainsKey(basicName))
            {
                item.Professor = _professorCache[basicName];
                return;
            }

            try
            {
                Console.WriteLine($"Navigating to professor details: {basicName}");
                driver.Navigate().GoToUrl(item.ProfessorLink);
                wait.Until(d => d.FindElements(By.CssSelector(".formatdiv4")).Count > 0);

                var professorInfo = driver.FindElements(By.CssSelector(".formatdiv4"))
                    .FirstOrDefault(e => e.Text.Contains(basicName));

                if (professorInfo != null)
                {
                    string fullName = CleanUpText(professorInfo.Text.Split('|')[0].Trim());
                    item.Professor = fullName;
                    _professorCache[basicName] = fullName; 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting professor details: {ex.Message}");
            }
        }

        private void ExtractClassroomDetails(IWebDriver driver, ScheduleItem item, WebDriverWait wait)
        {
            if (string.IsNullOrEmpty(item.ClassroomLink))
                return;

            string basicName = item.Classroom;

            if (_classroomCache.ContainsKey(basicName))
            {
                item.Classroom = _classroomCache[basicName];
                return;
            }

            try
            {
                Console.WriteLine($"Navigating to classroom details: {basicName}");
                driver.Navigate().GoToUrl(item.ClassroomLink);
                wait.Until(d => d.FindElements(By.CssSelector(".formatdiv4")).Count > 0);

                var classroomInfo = driver.FindElements(By.CssSelector(".formatdiv4"))
                    .FirstOrDefault(e => e.Text.Contains(basicName));

                if (classroomInfo != null)
                {
                    string fullName = CleanUpText(classroomInfo.Text.Split('|')[0].Trim());
                    item.Classroom = fullName;
                    _classroomCache[basicName] = fullName;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting classroom details: {ex.Message}");
            }
        }

        private void ExtractSubjectDetails(IWebDriver driver, ScheduleItem item, WebDriverWait wait)
        {
            if (string.IsNullOrEmpty(item.SubjectLink))
                return;

            string basicName = item.Subject;

            if (_subjectCache.ContainsKey(basicName))
            {
                var (code, fullName) = _subjectCache[basicName];
                item.SubjectCode = code;
                item.Subject = fullName;
                return;
            }

            try
            {
                Console.WriteLine($"Navigating to subject details: {basicName}");
                driver.Navigate().GoToUrl(item.SubjectLink);
                wait.Until(d => d.FindElements(By.CssSelector(".SRH_bold")).Count > 0);

                var subjectInfo = driver.FindElements(By.CssSelector(".SRH_bold"))
                    .FirstOrDefault();

                if (subjectInfo != null)
                {
                    string fullSubjectText = subjectInfo.Text;
                    string[] subjectParts = fullSubjectText.Split(new[] { ' ' }, 2);

                    if (subjectParts.Length > 1)
                    {
                        item.SubjectCode = subjectParts[0];
                        item.Subject = subjectParts[1];
                        _subjectCache[basicName] = (item.SubjectCode, item.Subject); 
                    }
                    else
                    {
                        item.SubjectCode = fullSubjectText;
                        item.Subject = basicName;
                        _subjectCache[basicName] = (fullSubjectText, basicName);
                    }
                }
                else
                {
                    item.SubjectCode = ExtractSubjectCode(item.SubjectLink);
                    _subjectCache[basicName] = (item.SubjectCode, basicName); 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting subject details: {ex.Message}");
            }
        }

        private string GetClassType(string className)
        {
            if (className.Contains("-pv"))
                return "L"; 
            else if (className.Contains("-p"))
                return "P"; 
            else if (className.Contains("-cv"))
                return "C"; 
            else if (!className.EndsWith("rozvrh_bloky"))
                return "X"; 

            return ""; 
        }

        private string CleanUpText(string text)
        {
            return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        }

        private string ExtractSubjectCode(string subjectHref)
        {
            try
            {
                if (subjectHref.Contains("id="))
                {
                    var parts = subjectHref.Split(new[] { "id=" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        var codePart = parts[1].Split(',')[0];
                        return codePart;
                    }
                }
                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}