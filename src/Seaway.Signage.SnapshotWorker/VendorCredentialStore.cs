namespace Seaway.Signage.SnapshotWorker;

/// <summary>
/// Server-held vendor credentials for content that needs a login. Deliberately lives only in this
/// process, never reachable from the device-facing Web app. Which vendor pages need credentials
/// here is one of the design doc's §12 open questions (#3) — do not hard-code a specific vendor
/// integration until that's answered.
/// </summary>
public class VendorCredentialStore
{
    public Task<string?> GetCredentialAsync(string vendorKey, CancellationToken ct = default)
    {
        throw new NotImplementedException("VendorCredentialStore — Phase 3, see IMPLEMENTATION_PLAN.md milestone 33.");
    }
}
