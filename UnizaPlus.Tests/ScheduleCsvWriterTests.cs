using UnizaPlus.Models;
using UnizaPlus.Web.Services.Scheduling;

namespace UnizaPlus.Tests;

public class ScheduleCsvWriterTests
{
    [Fact]
    public void Write_EmptySchedule_ProducesHeaderOnly()
    {
        var csv = ScheduleCsvWriter.Write([]);

        Assert.Equal("Subject,SubjectCode,Type,Day,Start,End,Room,Teacher,Group" + Environment.NewLine, csv);
    }

    [Fact]
    public async Task RoundTrips_ThroughScheduleCsvParser()
    {
        var original = new ScheduleItem
        {
            Subject = "Databázové systémy",
            SubjectCode = "6BI0005",
            Type = "P",
            Day = "Monday",
            StartHour = 8,
            Duration = 2,
            Classroom = "RA1A3",
            Professor = "doc. Novák, PhD.",
            StudentGroups = "FRI22"
        };

        var csv = ScheduleCsvWriter.Write([original]);

        using var reader = new StringReader(csv);
        var result = await ScheduleCsvParser.ParseAsync(reader);

        Assert.Empty(result.Warnings);
        var roundTripped = Assert.Single(result.Items);
        Assert.Equal(original.Subject, roundTripped.Subject);
        Assert.Equal(original.SubjectCode, roundTripped.SubjectCode);
        Assert.Equal(original.Type, roundTripped.Type);
        Assert.Equal(original.Day, roundTripped.Day);
        Assert.Equal(original.StartHour, roundTripped.StartHour);
        Assert.Equal(original.Duration, roundTripped.Duration);
        Assert.Equal(original.Classroom, roundTripped.Classroom);
        Assert.Equal(original.Professor, roundTripped.Professor);
        Assert.Equal(original.StudentGroups, roundTripped.StudentGroups);
    }

    [Fact]
    public void Write_EscapesFieldsContainingCommasAndQuotes()
    {
        var item = new ScheduleItem
        {
            Subject = "Subject",
            Professor = "Novák, PhD.",
            Classroom = "Room \"A\"",
            Day = "Monday",
            Type = "P",
            StartHour = 8,
            Duration = 1
        };

        var csv = ScheduleCsvWriter.Write([item]);

        Assert.Contains("\"Novák, PhD.\"", csv);
        Assert.Contains("\"Room \"\"A\"\"\"", csv);
    }
}
