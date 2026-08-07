using System.Text;
using System.Text.Json;
using UnizaPlusBackEnd.Models;

namespace UnizaPlus.Web.Services
{
    public class ScheduleService
    {
        private readonly ILogger<ScheduleService> _logger;
        private readonly ScraperService _scraperService;
        private List<ScheduleItem> _scheduleItems = new();
        private readonly string _scheduleFilePath;

        public ScheduleService(ILogger<ScheduleService> logger, ScraperService scraperService = null)
        {
            _logger = logger;
            _scraperService = scraperService;

            string solutionDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\"));
            _scheduleFilePath = Path.Combine(solutionDir, "schedule.csv");

            LoadScheduleFromFile();
        }

        public void ReloadScheduleData()
        {
            LoadScheduleFromFile();
        }

        public List<ScheduleItem> GetScheduleItems() => _scheduleItems;

        public Task<List<ScheduleItem>> GetScheduleAsync()
        {
            if (_scheduleItems.Count == 0)
            {
                if (File.Exists(_scheduleFilePath))
                {
                    LoadScheduleFromFile();
                }
            }
            return Task.FromResult(_scheduleItems);
        }

        public async Task<ScheduleItem?> GetScheduleItemAsync(int id)
        {
            var item = _scheduleItems.FirstOrDefault(i => i.Id == id);
            return await Task.FromResult(item);
        }

        public Task UpdateScheduleItemAsync(ScheduleItem item)
        {
            var existingItem = _scheduleItems.FirstOrDefault(i => i.Id == item.Id);
            if (existingItem != null)
            {
                int index = _scheduleItems.IndexOf(existingItem);
                _scheduleItems[index] = item;
                SaveScheduleToFile();
            }
            return Task.CompletedTask;
        }

        private void LoadScheduleFromFile()
        {
            try
            {
                if (!File.Exists(_scheduleFilePath))
                {
                    _logger.LogWarning($"Schedule file not found: {_scheduleFilePath}");
                    return;
                }

                var lines = File.ReadAllLines(_scheduleFilePath);
                int nextId = 1;
                int successCount = 0;
                _scheduleItems.Clear();

                int startLine = _scheduleFilePath.EndsWith(".csv") ? 1 : 0;

                for (int i = startLine; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var item = _scheduleFilePath.EndsWith(".csv")
                        ? ParseCsvScheduleItem(line)
                        : ParseScheduleItem(line, nextId);

                    if (item != null)
                    {
                        
                        item.Id = nextId++;
                        _scheduleItems.Add(item);
                        successCount++;
                    }
                }

                _logger.LogInformation("Loaded {SuccessCount} schedule items from file", successCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading schedule items");
            }
        }

        public ScheduleItem? GetScheduleItem(int id)
        {
            return _scheduleItems.FirstOrDefault(i => i.Id == id);
        }

        public void UpdateScheduleItem(ScheduleItem item)
        {
            var existingItem = _scheduleItems.FirstOrDefault(i => i.Id == item.Id);
            if (existingItem != null)
            {
                int index = _scheduleItems.IndexOf(existingItem);
                _scheduleItems[index] = item;
            }
        }

        private ScheduleItem? ParseScheduleItem(string line, int id)
        {
            try
            {
                
                var parts = line.Split(',');
                if (parts.Length < 7)
                    return null;

                string day = parts[0].Trim();

                string timeStr = parts[1].Trim();
                int startHour = int.Parse(timeStr.Split(':')[0]);

                string durationStr = parts[2].Trim();
                durationStr = durationStr.Replace("h)", "").Replace("(", "");
                int duration = int.Parse(durationStr);

                string type = parts[3].Trim();
                string professor = parts[4].Trim();
                string classroom = parts[5].Trim();

                string subjectFull = parts[6].Trim();
                string subject = subjectFull;
                string subjectCode = "";

                if (subjectFull.Contains('(') && subjectFull.Contains(')'))
                {
                    int codeStart = subjectFull.LastIndexOf('(');
                    int codeEnd = subjectFull.LastIndexOf(')');

                    if (codeStart >= 0 && codeEnd > codeStart)
                    {
                        subjectCode = subjectFull[(codeStart + 1)..codeEnd];
                        subject = subjectFull.Substring(0, codeStart).Trim();
                    }
                }

                string groups = "";
                if (parts.Length > 7)
                {
                    groups = string.Join(",", parts.Skip(7)).Trim();
                    if (groups.StartsWith("Groups:"))
                        groups = groups.Substring(7).Trim();
                }

                var scheduleItem = new ScheduleItem
                {
                    Id = id,
                    Day = day,
                    StartHour = startHour,
                    Duration = duration,
                    Type = type,
                    Professor = professor,
                    Classroom = classroom,
                    Subject = subject,
                    SubjectCode = subjectCode,
                    StudentGroups = groups
                };

                scheduleItem.InitializeColor();

                return scheduleItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing schedule item: {line}");
                return null;
            }
        }
        internal ScheduleItem ParseCsvScheduleItem(string line)
        {
            try
            {
                List<string> fields = new List<string>();
                bool inQuotes = false;
                StringBuilder field = new StringBuilder();

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];

                    if (c == '"')
                    {
                        if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                        {
                            field.Append('"');
                            i++; 
                        }
                        else
                        {
                            inQuotes = !inQuotes;
                        }
                    }
                    else if (c == ',' && !inQuotes)
                    {
                        fields.Add(field.ToString());
                        field.Clear();
                    }
                    else
                    {
                        field.Append(c);
                    }
                }

                fields.Add(field.ToString());

                if (fields.Count < 10)
                {
                    _logger.LogError($"Invalid CSV format, not enough fields: {line}");
                    return null;
                }

                var item = new ScheduleItem
                {
                    Id = int.Parse(fields[0]),
                    Day = fields[1],
                    StartHour = int.Parse(fields[2]),
                    Duration = int.Parse(fields[3]),
                    Type = fields[4],    
                    Professor = fields[5],
                    Classroom = fields[6],
                    Subject = fields[7],
                    SubjectCode = fields[8],
                    StudentGroups = fields[9]
                };
                
                if (fields.Count > 10 && !string.IsNullOrEmpty(fields[10]))
                    item.Color = fields[10];
                else
                    item.InitializeColor();

                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing CSV schedule item: {line}");
                return null;
            }
        }
        public Task UpdateAllScheduleItemsAsync(List<ScheduleItem> items)
        {
            _scheduleItems = items;
            _logger.LogInformation($"Updated all schedule items: {items.Count} items loaded");
            return Task.CompletedTask;
        }

        public bool HasScheduleData()
        {
            return _scheduleItems.Count > 0;
        }

        public async Task<bool> IsTimeSlotAvailableAsync(string day, int startHour, int duration, int excludeItemId)
        {
            var items = await GetScheduleAsync();

            bool hasOverlap = items.Any(item =>
                item.Id != excludeItemId &&
                item.Day == day &&
                ((startHour < item.StartHour + item.Duration) &&
                 (item.StartHour < startHour + duration))
            );

            bool withinBoundaries = startHour >= 7 && (startHour + duration) <= 21;

            return !hasOverlap && withinBoundaries;
        }

        public void SaveScheduleToFile()
        {
            try
            {
                string solutionDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\"));
                string filePath = Path.Combine(solutionDir, "schedule.csv");
                
                var lines = new List<string>
                {
                    "Id,Day,StartHour,Duration,Type,Professor,Classroom,Subject,SubjectCode,StudentGroups,Color"
                };
                
                foreach (var item in _scheduleItems)
                {
                    lines.Add($"{item.Id},{EscapeCsv(item.Day)},{item.StartHour},{item.Duration},{EscapeCsv(item.Type)}," +
                              $"{EscapeCsv(item.Professor)},{EscapeCsv(item.Classroom)},{EscapeCsv(item.Subject)}," +
                              $"{EscapeCsv(item.SubjectCode)},{EscapeCsv(item.StudentGroups)},{EscapeCsv(item.Color)}");
                }
                
                File.WriteAllLines(filePath, lines);
                _logger.LogInformation($"Saved {_scheduleItems.Count} schedule items to file");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving schedule items to file", ex);
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            
           
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            
            return value;
        }

        public List<ScheduleItem> ScheduleItems { get; set; } = [];
        public List<string> Days { get; } = ["Pondelok", "Utorok", "Streda", "Štvrtok", "Piatok"];
    }
}