using UnizaPlus.Web.Services.Scheduling;

namespace UnizaPlus.Tests;

public class ScheduleCsvParserTests
{
    private const string Header = "Subject,SubjectCode,Type,Day,Start,End,Room,Teacher,Group";

    private static Task<ScheduleCsvParseResult> ParseAsync(string csv)
    {
        using var reader = new StringReader(csv);
        return ScheduleCsvParser.ParseAsync(reader);
    }

    [Fact]
    public async Task ParsesValidRow()
    {
        var csv = Header + "\n" +
                  "Databázové systémy,6BI0005,P,Pondelok,8,10,RA1A3,\"doc. Novák, PhD.\",FRI22";

        var result = await ParseAsync(csv);

        Assert.Empty(result.Warnings);
        var item = Assert.Single(result.Items);
        Assert.Equal("Databázové systémy", item.Subject);
        Assert.Equal("6BI0005", item.SubjectCode);
        Assert.Equal("P", item.Type);
        Assert.Equal("Monday", item.Day);
        Assert.Equal(8, item.StartHour);
        Assert.Equal(2, item.Duration);
        Assert.Equal("RA1A3", item.Classroom);
        Assert.Equal("doc. Novák, PhD.", item.Professor);
        Assert.Equal("FRI22", item.StudentGroups);
    }

    [Theory]
    [InlineData("Monday")] // current interface value
    [InlineData("Pondelok")] // interface used to show Slovak day names; old files still use them
    [InlineData("PONDELOK")] // case-insensitive
    public async Task AcceptsDayInEnglishOrSlovak_AndNormalisesToEnglish(string dayValue)
    {
        var csv = Header + "\n" +
                  $"Predmet,K1,P,{dayValue},8,10,,,";

        var result = await ParseAsync(csv);

        Assert.Empty(result.Warnings);
        var item = Assert.Single(result.Items);
        Assert.Equal("Monday", item.Day);
    }

    [Fact]
    public async Task SkipsBlankLines()
    {
        var csv = Header + "\n" +
                  "\n" +
                  "Predmet,K1,P,Utorok,8,10,,,\n" +
                  "   \n" +
                  "Predmet2,K2,C,Streda,9,11,,,";

        var result = await ParseAsync(csv);

        Assert.Empty(result.Warnings);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task SkipsRowWithInvalidDay_AndLogsWarning()
    {
        var csv = Header + "\n" +
                  "Predmet,K1,P,Nedeľa,8,10,,,";

        var result = await ParseAsync(csv);

        Assert.Empty(result.Items);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public async Task SkipsRowWithInvalidType_AndLogsWarning()
    {
        var csv = Header + "\n" +
                  "Predmet,K1,X,Pondelok,8,10,,,";

        var result = await ParseAsync(csv);

        Assert.Empty(result.Items);
        Assert.Single(result.Warnings);
    }

    [Theory]
    [InlineData("abc", "10")]
    [InlineData("8", "abc")]
    [InlineData("10", "8")]
    [InlineData("8", "8")]
    public async Task SkipsRowWithBadTimeRange_AndLogsWarning(string start, string end)
    {
        var csv = Header + "\n" +
                  $"Predmet,K1,P,Pondelok,{start},{end},,,";

        var result = await ParseAsync(csv);

        Assert.Empty(result.Items);
        Assert.Single(result.Warnings);
    }

    [Theory]
    [InlineData("3", "5")]   // starts before the grid's earliest hour (7)
    [InlineData("20", "23")] // ends after the grid's latest hour (21)
    [InlineData("7", "12")]  // 5h duration, longer than the UI/CSS support (max 4h)
    public async Task SkipsRowWithOutOfRangeTimes_AndLogsWarning(string start, string end)
    {
        var csv = Header + "\n" +
                  $"Predmet,K1,P,Pondelok,{start},{end},,,";

        var result = await ParseAsync(csv);

        Assert.Empty(result.Items);
        Assert.Single(result.Warnings);
    }

    [Theory]
    [InlineData(7, 8, 1)]
    [InlineData(20, 21, 1)]  // last valid start hour
    [InlineData(7, 11, 4)]   // longest supported duration
    public async Task AcceptsRowsAtTheEdgeOfTheSupportedTimeRange(int start, int end, int expectedDuration)
    {
        var csv = Header + "\n" +
                  $"Predmet,K1,P,Pondelok,{start},{end},,,";

        var result = await ParseAsync(csv);

        Assert.Empty(result.Warnings);
        var item = Assert.Single(result.Items);
        Assert.Equal(start, item.StartHour);
        Assert.Equal(expectedDuration, item.Duration);
    }

    [Fact]
    public async Task SkipsRowWithMissingSubject_AndLogsWarning()
    {
        var csv = Header + "\n" +
                  ",K1,P,Pondelok,8,10,,,";

        var result = await ParseAsync(csv);

        Assert.Empty(result.Items);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public async Task ReportsMissingRequiredColumn_WithoutThrowing()
    {
        var csv = "Subject,Type,Day,Start\n" + // missing End
                  "Predmet,P,Pondelok,8";

        var result = await ParseAsync(csv);

        Assert.Empty(result.Items);
        Assert.Single(result.Warnings);
        Assert.Contains("End", result.Warnings[0]);
    }

    [Fact]
    public async Task EmptyFile_ReturnsNoItemsWithWarning_WithoutThrowing()
    {
        var result = await ParseAsync(string.Empty);

        Assert.Empty(result.Items);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public async Task ContinuesParsingAfterASkippedRow()
    {
        var csv = Header + "\n" +
                  "Zlý,K1,P,Nedeľa,8,10,,,\n" +
                  "Dobrý,K2,C,Utorok,9,11,,,";

        var result = await ParseAsync(csv);

        Assert.Single(result.Warnings);
        var item = Assert.Single(result.Items);
        Assert.Equal("Dobrý", item.Subject);
    }
}
