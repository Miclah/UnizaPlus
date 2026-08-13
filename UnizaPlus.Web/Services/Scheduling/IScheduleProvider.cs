using UnizaPlus.Models;

namespace UnizaPlus.Web.Services.Scheduling
{
    /// <summary>
    /// Produces the initial/baseline set of schedule items for a visitor's session.
    /// </summary>
    public interface IScheduleProvider
    {
        Task<List<ScheduleItem>> GetScheduleAsync();
    }
}
