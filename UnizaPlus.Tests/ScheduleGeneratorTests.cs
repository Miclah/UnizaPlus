using UnizaPlus.Models;
using UnizaPlus.Web.Services.Scheduling;

namespace UnizaPlus.Tests;

public class ScheduleGeneratorTests
{
    private static int _nextId = 1;

    private static ScheduleItem Item(string day, int startHour, int duration, string type, string subject, string group = "")
    {
        return new ScheduleItem
        {
            Id = _nextId++,
            Day = day,
            StartHour = startHour,
            Duration = duration,
            Type = type,
            Subject = subject,
            StudentGroups = group,
            Professor = "Prof",
            Classroom = "Room",
        };
    }

    private static readonly ScheduleGenerationPreferences NoPreferences = new();

    // ---- Block extraction ----

    [Fact]
    public void ExtractBlocks_GroupsBySameSubjectAndType_IgnoringEmptyGroups()
    {
        var fixedItem = Item("Monday", 8, 2, "P", "Fixed Subject");
        var alt1 = Item("Monday", 9, 2, "P", "Math", "G1");
        var alt2 = Item("Tuesday", 9, 2, "P", "Math", "G2");
        var items = new List<ScheduleItem> { fixedItem, alt1, alt2 };

        var blocks = ScheduleGenerator.ExtractBlocks(items);

        var block = Assert.Single(blocks);
        Assert.Equal("Math", block.Subject);
        Assert.Equal("P", block.Type);
        Assert.Equal(2, block.Alternatives.Count);
        Assert.Contains(block.Alternatives, i => i.Id == alt1.Id);
        Assert.Contains(block.Alternatives, i => i.Id == alt2.Id);
    }

    [Fact]
    public void ExtractBlocks_SingleItemWithAGroupButNoSibling_IsNotABlock()
    {
        var lonely = Item("Monday", 8, 2, "P", "Math", "G1");
        var items = new List<ScheduleItem> { lonely };

        Assert.Empty(ScheduleGenerator.ExtractBlocks(items));
        Assert.Contains(ScheduleGenerator.ExtractFixedItems(items), i => i.Id == lonely.Id);
    }

    [Fact]
    public void ExtractFixedItems_ExcludesOnlyRealBlockAlternatives()
    {
        var fixedNoGroup = Item("Monday", 8, 2, "P", "Fixed Subject");
        var alt1 = Item("Monday", 9, 2, "C", "Math", "G1");
        var alt2 = Item("Tuesday", 9, 2, "C", "Math", "G2");
        var items = new List<ScheduleItem> { fixedNoGroup, alt1, alt2 };

        var fixedItems = ScheduleGenerator.ExtractFixedItems(items);

        var only = Assert.Single(fixedItems);
        Assert.Equal(fixedNoGroup.Id, only.Id);
    }

    // ---- End-to-end generation ----

    [Fact]
    public void NoBlocks_ReturnsTheFixedItemsAsTheOnlyVariant()
    {
        var a = Item("Monday", 8, 2, "P", "A");
        var b = Item("Tuesday", 8, 2, "P", "B");
        var items = new List<ScheduleItem> { a, b };

        var result = new ScheduleGenerator().Generate(items, NoPreferences);

        Assert.Equal(0, result.BlockCount);
        Assert.True(result.IsConflictFree);
        Assert.Equal(0, result.BestConflictCount);
        var variant = Assert.Single(result.Variants);
        Assert.Equal([a.Id, b.Id], variant.Items.Select(i => i.Id).OrderBy(x => x));
    }

    [Fact]
    public void EmptyInput_ReturnsAnEmptyConflictFreeVariant_WithoutThrowing()
    {
        var result = new ScheduleGenerator().Generate([], NoPreferences);

        Assert.Equal(0, result.BlockCount);
        Assert.True(result.IsConflictFree);
        var variant = Assert.Single(result.Variants);
        Assert.Empty(variant.Items);
    }

    [Fact]
    public void SingleSolution_OneBlockWhereOnlyOneAlternativeAvoidsTheFixedItem()
    {
        var fixedItem = Item("Monday", 8, 2, "P", "Fixed Subject");
        var conflicting = Item("Monday", 9, 2, "P", "Math", "G1"); // overlaps fixedItem (8-10 vs 9-11)
        var free = Item("Tuesday", 8, 2, "P", "Math", "G2"); // different day, no overlap
        var items = new List<ScheduleItem> { fixedItem, conflicting, free };

        var result = new ScheduleGenerator().Generate(items, NoPreferences);

        Assert.Equal(1, result.BlockCount);
        Assert.True(result.IsConflictFree);
        var variant = Assert.Single(result.Variants); // the unique conflict-free combination
        Assert.Contains(variant.Items, i => i.Id == fixedItem.Id);
        Assert.Contains(variant.Items, i => i.Id == free.Id);
        Assert.DoesNotContain(variant.Items, i => i.Id == conflicting.Id);
    }

    [Fact]
    public void NoConflictFreeSolutionExists_ReturnsTheFewestConflictsAndSaysSo()
    {
        var fixedItem = Item("Monday", 8, 2, "P", "Fixed Subject");
        var altA = Item("Monday", 8, 2, "C", "Math", "G1"); // exact overlap with fixedItem
        var altB = Item("Monday", 9, 2, "C", "Math", "G2"); // also overlaps fixedItem
        var items = new List<ScheduleItem> { fixedItem, altA, altB };

        var result = new ScheduleGenerator().Generate(items, NoPreferences);

        Assert.False(result.IsConflictFree);
        Assert.Equal(1, result.BestConflictCount);
        Assert.Equal(2, result.Variants.Count); // both alternatives are tied at 1 conflict each
        Assert.All(result.Variants, v => Assert.Equal(1, v.ConflictCount));
    }

    [Fact]
    public void BaselineConflictBetweenFixedItems_IsReportedEvenWithNoBlocks()
    {
        var a = Item("Monday", 8, 2, "P", "A");
        var b = Item("Monday", 9, 2, "C", "B"); // overlaps a, neither has a group
        var items = new List<ScheduleItem> { a, b };

        var result = new ScheduleGenerator().Generate(items, NoPreferences);

        Assert.Equal(0, result.BlockCount);
        Assert.False(result.IsConflictFree);
        Assert.Equal(1, result.BestConflictCount);
        var variant = Assert.Single(result.Variants);
        Assert.Equal(1, variant.ConflictCount);
    }

    [Fact]
    public void MultipleIndependentBlocks_ReturnsAllCombinationsTiedForFewestConflicts()
    {
        // A1 and B1 collide with each other; every other pairing is conflict-free.
        var a1 = Item("Monday", 8, 2, "P", "X", "G1");
        var a2 = Item("Tuesday", 8, 2, "P", "X", "G2");
        var b1 = Item("Monday", 8, 2, "C", "Y", "G1");
        var b2 = Item("Wednesday", 8, 2, "C", "Y", "G2");
        var items = new List<ScheduleItem> { a1, a2, b1, b2 };

        var result = new ScheduleGenerator().Generate(items, NoPreferences, maxVariants: 10);

        Assert.Equal(2, result.BlockCount);
        Assert.True(result.IsConflictFree);
        Assert.Equal(3, result.Variants.Count); // (a1,b2), (a2,b1), (a2,b2) - (a1,b1) is the only conflicting pair
        Assert.All(result.Variants, v => Assert.Equal(0, v.ConflictCount));
        Assert.DoesNotContain(result.Variants, v =>
            v.Items.Any(i => i.Id == a1.Id) && v.Items.Any(i => i.Id == b1.Id));
    }

    [Fact]
    public void MaxVariants_LimitsHowManyTiedSolutionsAreReturned()
    {
        var a1 = Item("Monday", 8, 2, "P", "X", "G1");
        var a2 = Item("Tuesday", 8, 2, "P", "X", "G2");
        var b1 = Item("Monday", 8, 2, "C", "Y", "G1");
        var b2 = Item("Wednesday", 8, 2, "C", "Y", "G2");
        var items = new List<ScheduleItem> { a1, a2, b1, b2 };

        var result = new ScheduleGenerator().Generate(items, NoPreferences, maxVariants: 1);

        Assert.Single(result.Variants);
    }

    [Fact]
    public void LargerInput_StillFindsTheUniqueConflictFreeCombinationAmongManyBlocks()
    {
        // Five independent blocks (32 combinations); only picking the "good" alternative in
        // every block avoids the shared anchor item, so exactly one combination is conflict-free.
        var anchor = Item("Monday", 8, 1, "P", "Anchor");
        var items = new List<ScheduleItem> { anchor };
        var goodIds = new List<int>();
        string[] freeDays = ["Tuesday", "Wednesday", "Thursday", "Friday", "Tuesday"];

        for (int i = 0; i < 5; i++)
        {
            var subject = "Block" + i;
            var good = Item(freeDays[i], 10 + i, 1, "P", subject, "Good");
            var bad = Item("Monday", 8, 1, "P", subject, "Bad"); // collides with the anchor
            items.Add(good);
            items.Add(bad);
            goodIds.Add(good.Id);
        }

        var result = new ScheduleGenerator().Generate(items, NoPreferences, maxVariants: 10);

        Assert.True(result.IsConflictFree);
        var variant = Assert.Single(result.Variants);
        foreach (var id in goodIds)
        {
            Assert.Contains(variant.Items, i => i.Id == id);
        }
    }

    // ---- Preferences ----

    [Fact]
    public void NoEarlyMornings_RanksTheLaterAlternativeFirst()
    {
        var early = Item("Monday", 8, 2, "P", "Math", "G1");
        var late = Item("Tuesday", 10, 2, "P", "Math", "G2");
        var items = new List<ScheduleItem> { early, late };

        var result = new ScheduleGenerator().Generate(items, new ScheduleGenerationPreferences { NoEarlyMornings = true }, maxVariants: 2);

        Assert.Equal(2, result.Variants.Count);
        Assert.Contains(result.Variants[0].Items, i => i.Id == late.Id);
        Assert.Equal(0, result.Variants[0].PreferenceScore);
        Assert.Contains(result.Variants[1].Items, i => i.Id == early.Id);
        Assert.Equal(1, result.Variants[1].PreferenceScore);
    }

    [Fact]
    public void FreeFriday_RanksTheNonFridayAlternativeFirst()
    {
        var friday = Item("Friday", 10, 2, "P", "Math", "G1");
        var monday = Item("Monday", 10, 2, "P", "Math", "G2");
        var items = new List<ScheduleItem> { friday, monday };

        var result = new ScheduleGenerator().Generate(items, new ScheduleGenerationPreferences { FreeFriday = true }, maxVariants: 2);

        Assert.Equal(2, result.Variants.Count);
        Assert.Contains(result.Variants[0].Items, i => i.Id == monday.Id);
        Assert.Contains(result.Variants[1].Items, i => i.Id == friday.Id);
    }

    [Fact]
    public void MinimizeGaps_RanksTheTighterlyPackedDayFirst()
    {
        var fixedItem = Item("Monday", 8, 2, "P", "Fixed Subject"); // 8-10
        var backToBack = Item("Monday", 10, 1, "C", "Math", "G1"); // 10-11, no gap after fixedItem
        var withGap = Item("Monday", 13, 1, "C", "Math", "G2"); // 13-14, 3h idle gap after fixedItem
        var items = new List<ScheduleItem> { fixedItem, backToBack, withGap };

        var result = new ScheduleGenerator().Generate(items, new ScheduleGenerationPreferences { MinimizeGaps = true }, maxVariants: 2);

        Assert.Equal(2, result.Variants.Count);
        Assert.Contains(result.Variants[0].Items, i => i.Id == backToBack.Id);
        Assert.Equal(0, result.Variants[0].PreferenceScore);
        Assert.Contains(result.Variants[1].Items, i => i.Id == withGap.Id);
        Assert.Equal(3, result.Variants[1].PreferenceScore);
    }

    [Fact]
    public void AllPreferencesDisabled_EveryVariantScoresZero()
    {
        var early = Item("Friday", 8, 2, "P", "Math", "G1");
        var late = Item("Monday", 10, 2, "P", "Math", "G2");
        var items = new List<ScheduleItem> { early, late };

        var result = new ScheduleGenerator().Generate(items, NoPreferences, maxVariants: 2);

        Assert.All(result.Variants, v => Assert.Equal(0, v.PreferenceScore));
    }

    [Fact]
    public void Generate_IsDeterministic_ForTheSameInput()
    {
        var a1 = Item("Monday", 8, 2, "P", "X", "G1");
        var a2 = Item("Tuesday", 8, 2, "P", "X", "G2");
        var items = new List<ScheduleItem> { a1, a2 };
        var preferences = new ScheduleGenerationPreferences { NoEarlyMornings = true };

        var first = new ScheduleGenerator().Generate(items, preferences, maxVariants: 5);
        var second = new ScheduleGenerator().Generate(items, preferences, maxVariants: 5);

        Assert.Equal(
            first.Variants.Select(v => string.Join(",", v.Items.Select(i => i.Id))),
            second.Variants.Select(v => string.Join(",", v.Items.Select(i => i.Id))));
    }
}
