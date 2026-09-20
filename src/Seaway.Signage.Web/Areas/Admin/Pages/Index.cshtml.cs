using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Seaway.Signage.Web.Areas.Admin.Pages;

// AppSecurity role gating goes here once wired (Program.cs) — e.g.:
//   if (!(HttpContext.GetSeawayUser()?.IsInRole("SignageViewer") ?? false)) return Forbid();
// Never [Authorize(Roles = "...")] — AppSecurity adds no role claims. See design doc §10.
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
