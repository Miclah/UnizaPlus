using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UnizaPlus.Web.Pages
{
    /// <summary>
    /// Reached via app.UseStatusCodePagesWithReExecute("/StatusCode/{0}") in Program.cs for any
    /// response that would otherwise be an empty 4xx/5xx body (a 404 from routing, for example).
    /// Unhandled exceptions go to ErrorModel/Error.cshtml instead, via UseExceptionHandler.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class StatusCodeModel : PageModel
    {
        public int Code { get; private set; }

        public void OnGet(int code)
        {
            Code = code;

            // UseStatusCodePagesWithReExecute already preserves the original status code across
            // the re-execution, but that only holds true when this page is reached that way. Set
            // it explicitly too, so a direct request to /StatusCode/404 is also honest about it.
            Response.StatusCode = code;
        }
    }
}
