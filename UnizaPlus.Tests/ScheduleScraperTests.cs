using UnizaPlus.Services;

namespace UnizaPlus.Tests;

public class ScheduleScraperTests
{
    private readonly ScheduleScraper _scraper = new();

    [Theory]
    [InlineData("rozvrh_bloky-pv", "L")]
    [InlineData("rozvrh_bloky-p", "P")]
    [InlineData("rozvrh_bloky-cv", "C")]
    [InlineData("rozvrh_bloky", "")]
    [InlineData("rozvrh_bloky-something-else", "X")]
    public void GetClassType_ReturnsExpectedCode(string className, string expected)
    {
        var result = _scraper.GetClassType(className);

        Assert.Equal(expected, result);
    }
}
