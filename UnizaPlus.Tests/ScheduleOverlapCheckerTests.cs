using UnizaPlus.Models;
using UnizaPlus.Web.Services.Scheduling;

namespace UnizaPlus.Tests;

public class ScheduleOverlapCheckerTests
{
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
    public void IsAvailable_ReturnsFalse_WhenSlotOverlapsExistingItem()
    {
        var items = new List<ScheduleItem> { MakeItem(1, "Pondelok", 8, 2) };

        var available = ScheduleOverlapChecker.IsAvailable(items, "Pondelok", 9, 1, excludeItemId: -1);

        Assert.False(available);
    }

    [Fact]
    public void IsAvailable_ReturnsTrue_WhenSlotDoesNotOverlap()
    {
        var items = new List<ScheduleItem> { MakeItem(1, "Pondelok", 8, 2) };

        var available = ScheduleOverlapChecker.IsAvailable(items, "Pondelok", 10, 1, excludeItemId: -1);

        Assert.True(available);
    }

    [Fact]
    public void IsAvailable_IgnoresTheItemBeingMoved()
    {
        var items = new List<ScheduleItem> { MakeItem(1, "Pondelok", 8, 2) };

        var available = ScheduleOverlapChecker.IsAvailable(items, "Pondelok", 8, 2, excludeItemId: 1);

        Assert.True(available);
    }

    [Theory]
    [InlineData(6, 1)]
    [InlineData(20, 2)]
    public void IsAvailable_RejectsSlotsOutsideDayBoundaries(int startHour, int duration)
    {
        var available = ScheduleOverlapChecker.IsAvailable([], "Pondelok", startHour, duration, excludeItemId: -1);

        Assert.False(available);
    }

    [Fact]
    public void FindConflictingItemIds_FindsItemsWithDifferentStartHoursThatStillOverlap()
    {
        // 8-10 and 9-11 overlap on the hour 9-10 even though neither shares the other's StartHour.
        var items = new List<ScheduleItem>
        {
            MakeItem(1, "Pondelok", 8, 2),
            MakeItem(2, "Pondelok", 9, 2),
        };

        var conflicts = ScheduleOverlapChecker.FindConflictingItemIds(items);

        Assert.Equal(new HashSet<int> { 1, 2 }, conflicts);
    }

    [Fact]
    public void FindConflictingItemIds_IgnoresNonOverlappingItems()
    {
        var items = new List<ScheduleItem>
        {
            MakeItem(1, "Pondelok", 8, 2),
            MakeItem(2, "Pondelok", 10, 2),
            MakeItem(3, "Utorok", 8, 2),
        };

        var conflicts = ScheduleOverlapChecker.FindConflictingItemIds(items);

        Assert.Empty(conflicts);
    }

    [Fact]
    public void FindConflictingItemIds_HandlesMoreThanTwoOverlappingItems()
    {
        var items = new List<ScheduleItem>
        {
            MakeItem(1, "Pondelok", 8, 2),  // 8-10
            MakeItem(2, "Pondelok", 9, 1),  // 9-10, overlaps 1 only
            MakeItem(3, "Pondelok", 13, 1), // unrelated
        };

        var conflicts = ScheduleOverlapChecker.FindConflictingItemIds(items);

        Assert.Equal(new HashSet<int> { 1, 2 }, conflicts);
    }
}
