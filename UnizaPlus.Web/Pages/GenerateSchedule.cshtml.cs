using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using UnizaPlus.Models;
using UnizaPlus.Web.Services;
using UnizaPlus.Web.Services.Scheduling;

namespace UnizaPlus.Web.Pages
{
    /// <summary>
    /// Wizard for picking one alternative per <see cref="ScheduleAlternativeBlock"/> via
    /// <see cref="ScheduleGenerator"/>. The generated variants and which one is currently being
    /// previewed live only in this session's ISession (not the main SessionScheduleStore) -
    /// they're a draft the user is browsing, not the active schedule, until Confirm is pressed.
    /// </summary>
    public class GenerateScheduleModel(ScheduleService scheduleService, IStringLocalizer<SharedResource> localizer) : PageModel
    {
        private const string SessionKey = "GenerateSchedule.State";

        private readonly ScheduleService _scheduleService = scheduleService;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;

        [BindProperty]
        public bool NoEarlyMornings { get; set; }

        [BindProperty]
        public bool MinimizeGaps { get; set; }

        [BindProperty]
        public bool FreeFriday { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public int BlockCount { get; private set; }
        public bool HasBlocks => BlockCount > 0;
        public bool HasGenerated { get; private set; }
        public bool IsConflictFree { get; private set; }
        public int ConflictCount { get; private set; }
        public int VariantIndex { get; private set; }
        public int VariantCount { get; private set; }
        public List<ScheduleItem> PreviewItems { get; private set; } = [];
        public IReadOnlyList<string> Days { get; } = ScheduleDays.All;

        public async Task OnGetAsync()
        {
            var items = await _scheduleService.GetScheduleAsync();
            BlockCount = ScheduleGenerator.ExtractBlocks(items).Count;

            var state = LoadState();
            if (state == null)
            {
                return;
            }

            NoEarlyMornings = state.NoEarlyMornings;
            MinimizeGaps = state.MinimizeGaps;
            FreeFriday = state.FreeFriday;
            ApplyState(state);
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            var items = await _scheduleService.GetScheduleAsync();
            var preferences = new ScheduleGenerationPreferences
            {
                NoEarlyMornings = NoEarlyMornings,
                MinimizeGaps = MinimizeGaps,
                FreeFriday = FreeFriday,
            };

            var result = new ScheduleGenerator().Generate(items, preferences);

            SaveState(new StoredState
            {
                NoEarlyMornings = NoEarlyMornings,
                MinimizeGaps = MinimizeGaps,
                FreeFriday = FreeFriday,
                VariantIndex = 0,
                Variants = result.Variants
                    .Select(v => new StoredVariant { Items = [.. v.Items], ConflictCount = v.ConflictCount })
                    .ToList(),
            });

            return RedirectToPage();
        }

        public IActionResult OnPostNext()
        {
            var state = LoadState();
            if (state != null && state.Variants.Count > 0)
            {
                state.VariantIndex = (state.VariantIndex + 1) % state.Variants.Count;
                SaveState(state);
            }
            return RedirectToPage();
        }

        public IActionResult OnPostPrevious()
        {
            var state = LoadState();
            if (state != null && state.Variants.Count > 0)
            {
                state.VariantIndex = (state.VariantIndex - 1 + state.Variants.Count) % state.Variants.Count;
                SaveState(state);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostConfirmAsync()
        {
            var state = LoadState();
            if (state == null || state.Variants.Count == 0)
            {
                return RedirectToPage();
            }

            var chosen = state.Variants[Math.Clamp(state.VariantIndex, 0, state.Variants.Count - 1)];
            await _scheduleService.UpdateAllScheduleItemsAsync(chosen.Items);
            ClearState();

            SuccessMessage = chosen.ConflictCount == 0
                ? _localizer["Generated schedule applied - no time conflicts."]
                : _localizer["Generated schedule applied with {0} remaining conflict(s) - no fully conflict-free combination was found.", chosen.ConflictCount];

            return RedirectToPage("Index");
        }

        public IActionResult OnPostDiscard()
        {
            ClearState();
            return RedirectToPage();
        }

        private void ApplyState(StoredState state)
        {
            if (state.Variants.Count == 0)
            {
                return;
            }

            HasGenerated = true;
            VariantCount = state.Variants.Count;
            VariantIndex = Math.Clamp(state.VariantIndex, 0, state.Variants.Count - 1);

            var current = state.Variants[VariantIndex];
            PreviewItems = current.Items;
            ConflictCount = current.ConflictCount;
            IsConflictFree = current.ConflictCount == 0;
        }

        private StoredState? LoadState()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<StoredState>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private void SaveState(StoredState state) =>
            HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(state));

        private void ClearState() => HttpContext.Session.Remove(SessionKey);

        private class StoredState
        {
            public bool NoEarlyMornings { get; set; }
            public bool MinimizeGaps { get; set; }
            public bool FreeFriday { get; set; }
            public int VariantIndex { get; set; }
            public List<StoredVariant> Variants { get; set; } = [];
        }

        private class StoredVariant
        {
            public List<ScheduleItem> Items { get; set; } = [];
            public int ConflictCount { get; set; }
        }
    }
}
