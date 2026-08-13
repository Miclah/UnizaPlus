using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace UnizaPlus.Tests;

/// <summary>
/// End-to-end smoke tests hosting the real UnizaPlus.Web pipeline in-process
/// (DI, Program.cs, middleware, Csv-mode config from appsettings.json).
/// </summary>
public class SmokeTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    /// <summary>
    /// The session cookie is Secure-only (Program.cs AddSession), so the client needs an
    /// https:// base address for its cookie container to accept and resend it - TestServer's
    /// in-memory transport ignores the scheme itself, only HttpClient's cookie handling cares.
    /// </summary>
    private HttpClient CreateClient(bool allowAutoRedirect = true) =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = allowAutoRedirect,
        });

    private static (string day, int hour) FindItemPosition(string html, int id)
    {
        var match = Regex.Match(html, $"data-id=\"{id}\"[^>]*data-day=\"([^\"]*)\"[^>]*data-hour=\"(\\d+)\"");
        Assert.True(match.Success, $"Could not find schedule item {id} in the rendered page.");
        return (match.Groups[1].Value, int.Parse(match.Groups[2].Value));
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]*)\"");
        Assert.True(match.Success, "Could not find antiforgery token in the rendered page.");
        return match.Groups[1].Value;
    }

    [Fact]
    public async Task HomePage_RendersDemoScheduleFromCsv()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("schedule-grid", html);
        Assert.Contains("data-id=\"1\"", html);
    }

    [Fact]
    public async Task MoveScheduleItem_PersistsWithinTheSameSession()
    {
        using var client = CreateClient();

        // First load establishes the session cookie and anti-forgery token the move/read below rely on.
        var before = await client.GetStringAsync("/");
        var originalPosition = FindItemPosition(before, id: 1);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", ExtractAntiforgeryToken(before));

        var moveResponse = await client.PostAsJsonAsync("/api/schedule/move", new
        {
            id = 1,
            day = "Friday",
            startHour = 19
        });
        moveResponse.EnsureSuccessStatusCode();

        var after = await client.GetStringAsync("/");
        var newPosition = FindItemPosition(after, id: 1);

        Assert.Equal(("Friday", 19), newPosition);
        Assert.NotEqual(originalPosition, newPosition);
    }

    [Fact]
    public async Task MoveScheduleItem_RejectsInvalidDay_AndLeavesItemUnchanged()
    {
        using var client = CreateClient();

        var before = await client.GetStringAsync("/");
        var originalPosition = FindItemPosition(before, id: 1);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", ExtractAntiforgeryToken(before));

        var moveResponse = await client.PostAsJsonAsync("/api/schedule/move", new
        {
            id = 1,
            day = "NotARealDay",
            startHour = 9
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, moveResponse.StatusCode);

        var after = await client.GetStringAsync("/");
        Assert.Equal(originalPosition, FindItemPosition(after, id: 1));
    }

    [Fact]
    public async Task MoveScheduleItem_AllowsMovingIntoAnOccupiedSlot_AndFlagsTheConflict()
    {
        using var client = CreateClient();

        // Item 1 in sample-data/schedule.csv occupies Monday 8-10; dragging (unlike the
        // add/edit form) is allowed to land item 3 on top of it - the grid just flags it.
        var before = await client.GetStringAsync("/"); // establishes session + anti-forgery token
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", ExtractAntiforgeryToken(before));

        var moveResponse = await client.PostAsJsonAsync("/api/schedule/move", new
        {
            id = 3,
            day = "Monday",
            startHour = 8
        });

        moveResponse.EnsureSuccessStatusCode();

        var after = await client.GetStringAsync("/");
        Assert.Equal(("Monday", 8), FindItemPosition(after, id: 3));
        Assert.Contains("has-conflict", after);
    }

    [Fact]
    public async Task MoveScheduleItem_RejectsDropOutsideTheSupportedHourRange()
    {
        using var client = CreateClient();

        var before = await client.GetStringAsync("/");
        var originalPosition = FindItemPosition(before, id: 1);
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", ExtractAntiforgeryToken(before));

        // Item 1 has a 2-hour duration; starting it at 20 would run past the grid's 21:00 edge.
        var moveResponse = await client.PostAsJsonAsync("/api/schedule/move", new
        {
            id = 1,
            day = "Monday",
            startHour = 20
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, moveResponse.StatusCode);

        var after = await client.GetStringAsync("/");
        Assert.Equal(originalPosition, FindItemPosition(after, id: 1));
    }

    [Fact]
    public async Task AddScheduleItem_AppearsOnTheHomePage()
    {
        using var client = CreateClient();

        var editPage = await client.GetStringAsync("/ScheduleEdit/-1");
        var token = ExtractAntiforgeryToken(editPage);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["ScheduleItem.Id"] = "0",
            ["ScheduleItem.Color"] = "#f2f2f2",
            ["ScheduleItem.Day"] = "Wednesday",
            ["ScheduleItem.StartHour"] = "13",
            ["ScheduleItem.Duration"] = "1",
            ["ScheduleItem.Type"] = "C",
            ["ScheduleItem.Subject"] = "Smoke Test Subject",
            ["ScheduleItem.Professor"] = "Smoke Tester",
            ["ScheduleItem.Classroom"] = "T1",
        });

        var postResponse = await client.PostAsync("/ScheduleEdit/-1", form);
        postResponse.EnsureSuccessStatusCode();

        var index = await client.GetStringAsync("/");
        Assert.Contains("Smoke Test Subject", index);
    }

    [Fact]
    public async Task ExportSchedule_ReturnsCsvOfTheSessionSchedule()
    {
        using var client = CreateClient();

        await client.GetStringAsync("/"); // establish session

        var exportPage = await client.GetStringAsync("/ScheduleExport");
        var token = ExtractAntiforgeryToken(exportPage);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        var response = await client.PostAsync("/ScheduleExport", form);
        response.EnsureSuccessStatusCode();

        var csv = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("Subject,SubjectCode,Type,Day,Start,End,Room,Teacher,Group", csv);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task EditScheduleItem_RejectsOverlapAndDoesNotSave()
    {
        using var client = CreateClient();

        // Item 1 in sample-data/schedule.csv is on its Pondelok (Monday) row, 8-10; the
        // parser normalises that to the English "Monday" - place the new item's slot
        // squarely inside that window.
        var editPage = await client.GetStringAsync("/ScheduleEdit/-1");
        var token = ExtractAntiforgeryToken(editPage);

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["ScheduleItem.Id"] = "0",
            ["ScheduleItem.Color"] = "#f2f2f2",
            ["ScheduleItem.Day"] = "Monday",
            ["ScheduleItem.StartHour"] = "9",
            ["ScheduleItem.Duration"] = "1",
            ["ScheduleItem.Type"] = "C",
            ["ScheduleItem.Subject"] = "Should Not Save",
            ["ScheduleItem.Professor"] = "Tester",
            ["ScheduleItem.Classroom"] = "T1",
        });

        var response = await client.PostAsync("/ScheduleEdit/-1", form);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode); // re-renders the form, no redirect
        Assert.Contains("overlaps with another item", body);

        var index = await client.GetStringAsync("/");
        Assert.DoesNotContain("Should Not Save", index);
    }

    [Fact]
    public async Task EditScheduleItem_UnknownId_ReturnsNotFound()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/ScheduleEdit/999999");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadSchedule_HeaderOnlyCsv_SucceedsWithEmptySchedule()
    {
        using var client = CreateClient(allowAutoRedirect: false);

        var uploadPage = await client.GetStringAsync("/UploadSchedule");
        var token = ExtractAntiforgeryToken(uploadPage);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent("Subject,SubjectCode,Type,Day,Start,End,Room,Teacher,Group\n"), "file", "empty.csv" }
        };

        var postResponse = await client.PostAsync("/UploadSchedule", form);
        Assert.Equal(System.Net.HttpStatusCode.Redirect, postResponse.StatusCode);

        var resultPage = System.Net.WebUtility.HtmlDecode(await client.GetStringAsync(postResponse.Headers.Location));
        Assert.Contains("Uploaded 0 items", resultPage);

        var index = await client.GetStringAsync("/");
        Assert.DoesNotContain("data-id=", index);
    }

    [Fact]
    public async Task UploadSchedule_GarbageFile_ShowsErrorAndKeepsPreviousSchedule()
    {
        using var client = CreateClient(allowAutoRedirect: false);

        var before = await client.GetStringAsync("/");
        Assert.Contains("data-id=\"1\"", before);

        var uploadPage = await client.GetStringAsync("/UploadSchedule");
        var token = ExtractAntiforgeryToken(uploadPage);

        using var form = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent("this,is,not,a,valid,schedule,file"), "file", "garbage.csv" }
        };

        var postResponse = await client.PostAsync("/UploadSchedule", form);
        Assert.Equal(System.Net.HttpStatusCode.Redirect, postResponse.StatusCode);

        var resultPage = System.Net.WebUtility.HtmlDecode(await client.GetStringAsync(postResponse.Headers.Location));
        Assert.Contains("The file could not be processed", resultPage);

        var after = await client.GetStringAsync("/");
        Assert.Contains("data-id=\"1\"", after); // untouched, not silently wiped
    }
}
