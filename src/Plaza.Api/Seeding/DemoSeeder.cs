using Plaza.Feed.Contracts;
using Plaza.Groups.Contracts;
using Plaza.Profiles.Contracts;

namespace Plaza.Api.Seeding;

/// <summary>Invented demo data for Development. Every name, street and group is fictional.</summary>
public static class DemoSeeder
{
    public const string Lindehof = "lindehof";
    public const string KiepersolPark = "kiepersol-park";

    /// <summary>Verified moderator of Lindehof.</summary>
    public const string Naledi = "u-naledi";

    /// <summary>Verified member of Lindehof.</summary>
    public const string Pieter = "u-pieter";

    /// <summary>Verified member of Lindehof and Kiepersol Park.</summary>
    public const string Zanele = "u-zanele";

    /// <summary>Unverified member of Lindehof: can read, cannot post.</summary>
    public const string Jonas = "u-jonas";

    /// <summary>Verified moderator of Kiepersol Park; not a Lindehof member.</summary>
    public const string Ayesha = "u-ayesha";

    public static void Seed(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var profiles = services.GetRequiredService<IProfileRegistration>();
        var groups = services.GetRequiredService<IGroupAdministration>();
        var feed = services.GetRequiredService<IFeedImport>();
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();

        profiles.Upsert(new ResidentProfile(Naledi, "Naledi Venter", IsVerified: true));
        profiles.Upsert(new ResidentProfile(Pieter, "Pieter Okafor", IsVerified: true));
        profiles.Upsert(new ResidentProfile(Zanele, "Zanele Brink", IsVerified: true));
        profiles.Upsert(new ResidentProfile(Jonas, "Jonas Mahlaba", IsVerified: false));
        profiles.Upsert(new ResidentProfile(Ayesha, "Ayesha Fortuin", IsVerified: true));

        groups.CreateGroup(Lindehof, "Lindehof Residents", "Lindehof");
        groups.CreateGroup(KiepersolPark, "Kiepersol Park Neighbours", "Kiepersol Park");

        groups.AddMember(Lindehof, Naledi, GroupRole.Moderator);
        groups.AddMember(Lindehof, Pieter, GroupRole.Member);
        groups.AddMember(Lindehof, Zanele, GroupRole.Member);
        groups.AddMember(Lindehof, Jonas, GroupRole.Member);
        groups.AddMember(KiepersolPark, Ayesha, GroupRole.Moderator);
        groups.AddMember(KiepersolPark, Zanele, GroupRole.Member);

        feed.Import(Lindehof, Naledi, PostKind.Notice,
            "Water will be off on Acacia Lane between 09:00 and 13:00 on Saturday for pipe repairs.", now.AddDays(-3));
        feed.Import(Lindehof, Zanele, PostKind.Notice,
            "Lost: grey tabby called Biscuit, last seen near the Lindehof park gate. Reply here if you spot him.", now.AddDays(-1));
        feed.Import(Lindehof, Pieter, PostKind.SafetyAlert,
            "Streetlight out at the corner of Protea and Vink streets. Please walk in pairs after dark; the ward office has been told.", now.AddHours(-5));
        feed.Import(Lindehof, Naledi, PostKind.Notice,
            "Clean-up this Sunday 08:30, meet at the library steps. Gloves and bags provided.", now.AddMinutes(-40));
        feed.Import(KiepersolPark, Ayesha, PostKind.Notice,
            "Kiepersol Park AGM moved to the community hall, Thursday 19:00.", now.AddDays(-2));
    }
}
