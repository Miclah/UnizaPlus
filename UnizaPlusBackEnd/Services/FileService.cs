using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnizaPlusBackEnd.Models;

namespace UnizaPlusBackEnd.Services
{
    public class FileService
    {
        public void SaveScheduleToFile(List<ScheduleItem> scheduleItems, string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("Id,Day,StartHour,Duration,Type,Professor,Classroom,Subject,SubjectCode,StudentGroups,Color");

                    foreach (var item in scheduleItems)
                    {
                        writer.WriteLine($"{item.Id++}," +
                                        $"\"{EscapeCsv(item.Day)}\"," +
                                        $"{item.StartHour}," +
                                        $"{item.Duration}," +
                                        $"\"{EscapeCsv(item.Type)}\"," +
                                        $"\"{EscapeCsv(item.Professor)}\"," +
                                        $"\"{EscapeCsv(item.Classroom)}\"," +
                                        $"\"{EscapeCsv(item.Subject)}\"," +
                                        $"\"{EscapeCsv(item.SubjectCode)}\"," +
                                        $"\"{EscapeCsv(item.StudentGroups)}\"," +
                                        $"\"{EscapeCsv(item.Color)}\"");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving schedule to file: {ex.Message}");
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace("\"", "\"\"");
        }
    }
}