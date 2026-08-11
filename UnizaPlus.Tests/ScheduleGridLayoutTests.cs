using UnizaPlus.Models;
using UnizaPlus.Web.Services.Scheduling;

namespace UnizaPlus.Tests;

public class ScheduleGridLayoutTests
{
    private static ScheduleItem MakeItem(int id, int startHour, int duration, string day = "Pondelok") => new()
    {
        Id = id,
        Day = day,
        StartHour = startHour,
        Duration = duration,
        Type = "P",
        Professor = "Prof",
        Classroom = "Room",
        Subject = "Subject",
    };

    [Fact]
    public void SingleItem_GetsColumnZeroOfOne()
    {
        var result = ScheduleGridLayout.ComputeDayLayout([MakeItem(1, 8, 2)]);

        var position = Assert.Single(result);
        Assert.Equal(0, position.ColumnIndex);
        Assert.Equal(1, position.ColumnCount);
    }

    [Fact]
    public void NonOverlappingItems_EachGetItsOwnFullWidthColumn()
    {
        var items = new List<ScheduleItem> { MakeItem(1, 8, 1), MakeItem(2, 10, 1) };

        var result = ScheduleGridLayout.ComputeDayLayout(items);

        Assert.All(result, p => Assert.Equal(1, p.ColumnCount));
        Assert.All(result, p => Assert.Equal(0, p.ColumnIndex));
    }

    [Fact]
    public void AdjacentItems_TouchingButNotOverlapping_DoNotShareColumns()
    {
        // 8-9 and 9-10 touch at the boundary but don't overlap.
        var items = new List<ScheduleItem> { MakeItem(1, 8, 1), MakeItem(2, 9, 1) };

        var result = ScheduleGridLayout.ComputeDayLayout(items);

        Assert.All(result, p => Assert.Equal(1, p.ColumnCount));
    }

    [Fact]
    public void TwoOverlappingItems_GetSeparateColumnsInATwoColumnGroup()
    {
        // 8-10 and 9-11 overlap on the 9-10 hour.
        var items = new List<ScheduleItem> { MakeItem(1, 8, 2), MakeItem(2, 9, 2) };

        var result = ScheduleGridLayout.ComputeDayLayout(items);

        Assert.All(result, p => Assert.Equal(2, p.ColumnCount));
        var columns = result.Select(p => p.ColumnIndex).OrderBy(c => c).ToList();
        Assert.Equal([0, 1], columns);
    }

    [Fact]
    public void ThreeMutuallyOverlappingItems_GetThreeColumns()
    {
        var items = new List<ScheduleItem>
        {
            MakeItem(1, 8, 2), // 8-10
            MakeItem(2, 9, 1), // 9-10
            MakeItem(3, 9, 1), // 9-10
        };

        var result = ScheduleGridLayout.ComputeDayLayout(items);

        Assert.All(result, p => Assert.Equal(3, p.ColumnCount));
        Assert.Equal(3, result.Select(p => p.ColumnIndex).Distinct().Count());
    }

    [Fact]
    public void NoTwoItemsInTheSameClusterShareAColumnDuringTheirOverlap()
    {
        // A(8-10), B(9-11), C(10-12): A/B overlap, B/C overlap, A/C don't.
        // All three end up in one connected cluster; A and C may safely reuse the
        // same column once B has ended, but no simultaneously-overlapping pair may.
        var items = new List<ScheduleItem>
        {
            MakeItem(1, 8, 2),
            MakeItem(2, 9, 2),
            MakeItem(3, 10, 2),
        };

        var result = ScheduleGridLayout.ComputeDayLayout(items).ToDictionary(p => p.Item.Id);

        Assert.NotEqual(result[1].ColumnIndex, result[2].ColumnIndex);
        Assert.NotEqual(result[2].ColumnIndex, result[3].ColumnIndex);
        // Every item in the cluster reports the cluster's total column count.
        Assert.Equal(result[1].ColumnCount, result[2].ColumnCount);
        Assert.Equal(result[2].ColumnCount, result[3].ColumnCount);
    }

    [Fact]
    public void DifferentDaysAreIndependent_CallerFiltersByDayBeforehand()
    {
        // ComputeDayLayout assumes its input is already filtered to one day; verify
        // that mixing days in doesn't accidentally cluster same-hour items together.
        var items = new List<ScheduleItem>
        {
            MakeItem(1, 8, 1, day: "Pondelok"),
            MakeItem(2, 8, 1, day: "Utorok"),
        };

        var result = ScheduleGridLayout.ComputeDayLayout(items);

        // Both start at the same hour but ComputeDayLayout has no day filter of its own,
        // so a caller that forgets to pre-filter by day would see them clustered.
        Assert.All(result, p => Assert.Equal(2, p.ColumnCount));
    }

    [Fact]
    public void EmptyInput_ReturnsEmptyList()
    {
        var result = ScheduleGridLayout.ComputeDayLayout([]);

        Assert.Empty(result);
    }
}
