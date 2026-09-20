namespace Seaway.Signage.SnapshotWorker;

/// <summary>
/// Headless-Chromium rendering, via Playwright for .NET (see the commented-out PackageReference
/// in the csproj). Logs in with server-held vendor credentials as needed — no vendor credential
/// is ever delivered to a display. See design doc §8, §6.3.
/// </summary>
public class ChromiumRenderer(VendorCredentialStore credentials)
{
    public Task<byte[]> RenderAsync(Uri url, CancellationToken ct = default)
    {
        throw new NotImplementedException("ChromiumRenderer — Phase 3, see IMPLEMENTATION_PLAN.md milestone 33.");
    }
}
