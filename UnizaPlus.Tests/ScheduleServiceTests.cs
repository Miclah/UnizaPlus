using Microsoft.Extensions.Logging.Abstractions;
using UnizaPlus.Web.Services;
using UnizaPlusBackEnd.Models;

namespace UnizaPlus.Tests;

public class ScheduleServiceTests
{
    private static ScheduleService CreateService() => new(NullLogger<ScheduleService>.Instance);

    private static ScheduleItem MakeItem(int id, string day, int startHour, int duration) => new()
    {
        Id = id,
        Day = day,
        StartHour = startHour,
        Duration = duration,
        Type = "P",
        Professor = "Prof",
        Classroom = "Room",
        Subject = "Subject",
        SubjectCode = "CODE",
        StudentGroups = "Group"
    };

    [Fact]
    public async Task IsTimeSlotAvailableAsync_ReturnsFalse_WhenSlotOverlapsExistingItem()
    {
        var service = CreateService();
        await service.UpdateAllScheduleItemsAsync(new List<ScheduleItem>
        {
            MakeItem(1, "Pondelok", 8, 2)
        });

        var available = await service.IsTimeSlotAvailableAsync("Pondelok", 9, 1, excludeItemId: -1);

        Assert.False(available);
    }

    [Fact]
    public async Task IsTimeSlotAvailableAsync_ReturnsTrue_WhenSlotDoesNotOverlap()
    {
        var service = CreateService();
        await service.UpdateAllScheduleItemsAsync(new List<ScheduleItem>
        {
            MakeItem(1, "Pondelok", 8, 2)
        });

        var available = await service.IsTimeSlotAvailableAsync("Pondelok", 10, 1, excludeItemId: -1);

        Assert.True(available);
    }

    [Fact]
    public async Task IsTimeSlotAvailableAsync_IgnoresTheItemBeingMoved()
    {
        var service = CreateService();
        await service.UpdateAllScheduleItemsAsync(new List<ScheduleItem>
        {
            MakeItem(1, "Pondelok", 8, 2)
        });

        var available = await service.IsTimeSlotAvailableAsync("Pondelok", 8, 2, excludeItemId: 1);

        Assert.True(available);
    }

    [Theory]
    [InlineData(6, 1)]
    [InlineData(20, 2)]
    public async Task IsTimeSlotAvailableAsync_RejectsSlotsOutsideDayBoundaries(int startHour, int duration)
    {
        var service = CreateService();

        var available = await service.IsTimeSlotAvailableAsync("Pondelok", startHour, duration, excludeItemId: -1);

        Assert.False(available);
    }

    [Fact]
    public void ParseCsvScheduleItem_ParsesQuotedFieldsCorrectly()
    {
        var service = CreateService();
        var line = "1,\"Pondelok\",8,2,\"P\",\"Prof, PhD.\",\"Room \"\"A\"\"\",\"Subject\",\"CODE\",\"Group1\"";

        var item = service.ParseCsvScheduleItem(line);

        Assert.Equal(1, item.Id);
        Assert.Equal("Pondelok", item.Day);
        Assert.Equal(8, item.StartHour);
        Assert.Equal(2, item.Duration);
        Assert.Equal("P", item.Type);
        Assert.Equal("Prof, PhD.", item.Professor);
        Assert.Equal("Room \"A\"", item.Classroom);
        Assert.Equal("Group1", item.StudentGroups);
    }

    [Fact]
    public void ParseCsvScheduleItem_ReturnsNull_WhenNotEnoughFields()
    {
        var service = CreateService();

        var item = service.ParseCsvScheduleItem("1,Pondelok,8");

        Assert.Null(item);
    }
}
