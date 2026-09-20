using Microsoft.EntityFrameworkCore;
using Seaway.Signage.Data;
using Seaway.Signage.Web.Areas.Device;
using Seaway.Signage.Web.Streaming;

var builder = WebApplication.CreateBuilder(args);

// ---- Data ---------------------------------------------------------------
// Provider is Sqlite for local dev/CI; production provider depends on the still-open database
// decision — see IMPLEMENTATION_PLAN.md §4.4. Swapping providers here is the only change needed.
builder.Services.AddDbContext<SignageDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Signage")
        ?? "Data Source=signage.dev.db"));

// ---- Seaway shared services ----------------------------------------------
// Every internal Seaway app wires these in; see design doc §10 for the reasoning per row.
// Left as clearly-marked TODOs rather than invented calls, since the real packages (and their
// actual registration APIs) aren't available from this environment. Fill these in once
// Seaway.AppSecurity etc. are referenced in the .csproj.
//
// builder.Services.AddSeawayAppSecurity(builder.Configuration);
//     -- gates /Areas/Admin via cookie auth. Roles are NOT usable via [Authorize(Roles=...)] —
//        AppSecurity adds no role claims. Use HttpContext.GetSeawayUser()?.IsInRole(...) instead,
//        everywhere a role check is needed. This is the single most-flagged AppSecurity gotcha in
//        the design doc (§10) — do not "fix" it by trying [Authorize(Roles=...)] again.
//
// builder.Services.AddSeawayNotifications(builder.Configuration);
//     -- required at startup even before it's used for anything — the app will not start without
//        the DB grant configured. Real use: the Phase 2 dark-display alert.
//
// builder.Services.AddSeawayStorage(builder.Configuration);
//     -- narrow use only: static fallback/notice assets. Not used for snapshot images, which are
//        transient and served from the snapshot worker's own cache.
//
// builder.Services.AddSeawayPortal(builder.Configuration);
//     -- registers the launcher tile. Category: Operations / Internal Tools.
//
// builder.Services.AddSeawayAdminUi(builder.Configuration);
//     -- the default-on /Admin page. Reads CHANGELOG.md (this project's copy, not the repo
//        root's) and the csproj Version/AssemblyVersion properties above. Impersonation targets
//        are SignageEditor and SignageViewer only — never SignageAdmin.

// ---- App services (Phase 1+ — stubs registered here so DI wiring exists from day one) --------
builder.Services.AddSingleton<DisplayStreamRegistry>();

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // serves Player/wwwroot/player/* at /player/*

app.UseRouting();

// app.UseAuthentication(); // enabled once Seaway.AppSecurity is wired in above
app.UseAuthorization();

app.MapRazorPages();

// Player shell — every display loads this single URL. See design doc §3.2.
app.MapGet("/play", () => Results.Redirect("/player/index.html"));

// Device API — enrollment, SSE stream, heartbeat, events. Bearer-token gated once the auth
// middleware for /api/** lands in Phase 1 (see IMPLEMENTATION_PLAN.md milestone 17).
// Real endpoint implementations are Phase 1 work (milestones 12–18) — this just reserves the
// route group so the shape is visible from the start.
app.MapGroup("/api")
    .MapDeviceEndpoints();

app.Run();

// Exposes the top-level-statement Program class to WebApplicationFactory<Program> in
// Seaway.Signage.Web.IntegrationTests — top-level statements generate an internal Program class
// by default, which WebApplicationFactory can't see without this.
public partial class Program;
