namespace UnizaPlusBackEnd.Models
{
    public class ScheduleItem
    {
        public int Id { get; set; }
        public string Day { get; set; } = string.Empty;
        public int StartHour { get; set; }
        public int Duration { get; set; } 
        public string Type { get; set; } = string.Empty; 
        public string Professor { get; set; } = string.Empty;
        public string Classroom { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string StudentGroups { get; set; } = string.Empty;

        public string ProfessorLink { get; set; } = string.Empty;
        public string ClassroomLink { get; set; } = string.Empty;
        public string SubjectLink { get; set; } = string.Empty;

       
        public string Color { get; set; } = "#f2f2f2";

        private string GetColorForType(string type)
        {
            return type switch
            {
                "L" => "#d4ebf2",
                "P" => "#f7e8c3", 
                "C" => "#d8f0d8", 
                _ => "#f2f2f2"   
            };
        }

        public void InitializeColor()
        {
            Color = GetColorForType(Type);
        }

        public override string ToString()
        {
            return $"{Day}, {StartHour}:00 ({Duration}h), {Type}, {Professor}, {Classroom}, {Subject} ({SubjectCode}), Groups: {StudentGroups}";
        }
    }
}