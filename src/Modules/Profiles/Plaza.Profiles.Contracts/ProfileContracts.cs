namespace Plaza.Profiles.Contracts;

/// <summary>
/// The minimum a neighbour needs to know about a resident. No address, phone or email here:
/// personal data stays inside the Profiles module (PersonalDataProtection pattern).
/// </summary>
public sealed record ResidentProfile(string UserId, string DisplayName, bool IsVerified);

/// <summary>Read side of the Profiles module, used by other modules.</summary>
public interface IProfileDirectory
{
    ResidentProfile? Find(string userId);

    IReadOnlyList<ResidentProfile> List();
}

/// <summary>Write side used by registration, verification and the demo seeder.</summary>
public interface IProfileRegistration
{
    void Upsert(ResidentProfile profile);
}
