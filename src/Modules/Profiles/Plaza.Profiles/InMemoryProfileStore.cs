using System.Collections.Concurrent;
using Plaza.Profiles.Contracts;

namespace Plaza.Profiles;

/// <summary>Thread-safe in-memory store. Slice 2 swaps it for Postgres behind the same interfaces.</summary>
internal sealed class InMemoryProfileStore : IProfileDirectory, IProfileRegistration
{
    private readonly ConcurrentDictionary<string, ResidentProfile> _profiles = new(StringComparer.Ordinal);

    public ResidentProfile? Find(string userId) =>
        _profiles.TryGetValue(userId, out var profile) ? profile : null;

    public IReadOnlyList<ResidentProfile> List() =>
        _profiles.Values.OrderBy(p => p.DisplayName, StringComparer.Ordinal).ToList();

    public void Upsert(ResidentProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(profile.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(profile.DisplayName);
        _profiles[profile.UserId] = profile;
    }
}
