using UnizaPlus.Models;

namespace UnizaPlus.Tests;

public class ScheduleItemTests
{
    [Theory]
    [InlineData("L", "#d4ebf2")]
    [InlineData("P", "#f7e8c3")]
    [InlineData("C", "#d8f0d8")]
    [InlineData("X", "#f2f2f2")]
    [InlineData("unknown", "#f2f2f2")]
    public void InitializeColor_SetsColorBasedOnType(string type, string expectedColor)
    {
        var item = new ScheduleItem { Type = type };

        item.InitializeColor();

        Assert.Equal(expectedColor, item.Color);
    }
}
