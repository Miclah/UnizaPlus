using Microsoft.Extensions.Caching.Memory;
using UnizaPlus.Models;

namespace UnizaPlus.Web.Services.Scheduling
{
    /// <summary>
    /// Holds each visitor's own working copy of their schedule, keyed by the
    /// ASP.NET Core session cookie, so two visitors never see or overwrite
    /// each other's edits. Backed by IMemoryCache with a sliding expiration
    /// so abandoned sessions are evicted automatically instead of leaking.
    /// </summary>
    public class SessionScheduleStore(IMemoryCache cache)
    {
        private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(30);
        private readonly IMemoryCache _cache = cache;

        public async Task<List<ScheduleItem>> GetOrCreateAsync(HttpContext context, Func<Task<List<ScheduleItem>>> factory)
        {
            var sessionId = await EnsureSessionIdAsync(context);
            var key = CacheKey(sessionId);

            if (_cache.TryGetValue<List<ScheduleItem>>(key, out var existing) && existing != null)
            {
                return existing;
            }

            var created = await factory();
            _cache.Set(key, created, SlidingExpiration);
            return created;
        }

        public async Task SetAsync(HttpContext context, List<ScheduleItem> items)
        {
            var sessionId = await EnsureSessionIdAsync(context);
            _cache.Set(CacheKey(sessionId), items, SlidingExpiration);
        }

        public async Task ResetAsync(HttpContext context)
        {
            var sessionId = await EnsureSessionIdAsync(context);
            _cache.Remove(CacheKey(sessionId));
        }

        private static async Task<string> EnsureSessionIdAsync(HttpContext context)
        {
            await context.Session.LoadAsync();

            // The session cookie is only sent back to the client once
            // something has actually been stored in the session; without
            // this, Session.Id would come back different on every request.
            if (!context.Session.Keys.Any())
            {
                context.Session.SetString("_init", "1");
            }

            return context.Session.Id;
        }

        private static string CacheKey(string sessionId) => $"schedule:{sessionId}";
    }
}
