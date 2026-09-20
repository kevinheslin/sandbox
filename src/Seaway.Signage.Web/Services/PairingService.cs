using Seaway.Signage.Domain;

namespace Seaway.Signage.Web.Services;

/// <summary>
/// Enroll -> pairing code -> claim -> re-claim-by-IP. See design doc §4 for the flow and §4.1 for
/// the failure mode this exists to handle (anything that clears local storage on the device drops
/// it back to Unclaimed; DHCP-reservation-based IP matching lets an admin one-click re-claim
/// instead of a fresh pairing). See IMPLEMENTATION_PLAN.md milestones 12, 13, 30.
/// </summary>
public class PairingService
{
    public Task<Display> EnrollAsync(string? ipAddress, string? macAddress, CancellationToken ct = default)
    {
        throw new NotImplementedException("PairingService — Phase 1, see IMPLEMENTATION_PLAN.md milestone 12.");
    }

    public Task<Display> ClaimAsync(string pairingCode, string name, Guid? groupId, CancellationToken ct = default)
    {
        throw new NotImplementedException("PairingService — Phase 1, see IMPLEMENTATION_PLAN.md milestone 13.");
    }

    /// <summary>Matches a re-enrolling device's IP against its previous owner and offers a
    /// one-click re-claim. Phase 2 — see IMPLEMENTATION_PLAN.md milestone 30.</summary>
    public Task<Display?> TryMatchPreviousOwnerAsync(string ipAddress, CancellationToken ct = default)
    {
        throw new NotImplementedException("PairingService — Phase 2, see IMPLEMENTATION_PLAN.md milestone 30.");
    }
}
