using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Plaza.Events;
using Plaza.Feed;
using Plaza.Groups;
using Plaza.Notifications;
using Plaza.Profiles;
using Xunit;

namespace Plaza.Architecture.Tests;

/// <summary>
/// Keeps the modular monolith honest: a module may use another module only through its
/// <c>.Contracts</c> project, and keeps everything except its registration class internal.
/// </summary>
public class ModuleBoundaryTests
{
    private const string SharedKernel = "Plaza.SharedKernel";
    private const string TestAssembly = "Plaza.Tests";

    private static readonly Assembly[] Modules =
    [
        typeof(ProfilesModule).Assembly,
        typeof(GroupsModule).Assembly,
        typeof(FeedModule).Assembly,
        typeof(EventsModule).Assembly,
        typeof(NotificationsModule).Assembly,
    ];

    private static readonly HashSet<string> ModuleNames = Modules.Select(a => a.GetName().Name!).ToHashSet();

    [Fact]
    public void Modules_reference_other_modules_only_through_contracts()
    {
        foreach (var module in Modules)
        {
            var illegal = module.GetReferencedAssemblies()
                .Select(r => r.Name!)
                .Where(n => n.StartsWith("Plaza.", StringComparison.Ordinal))
                .Where(n => n != SharedKernel && !n.EndsWith(".Contracts", StringComparison.Ordinal))
                .ToList();

            Assert.True(illegal.Count == 0, $"{module.GetName().Name} references module internals: {string.Join(", ", illegal)}");
        }
    }

    [Fact]
    public void Contracts_depend_on_nothing_but_the_shared_kernel()
    {
        var contracts = Modules
            .SelectMany(m => m.GetReferencedAssemblies())
            .Where(r => r.Name!.EndsWith(".Contracts", StringComparison.Ordinal))
            .DistinctBy(r => r.Name)
            .Select(Assembly.Load)
            .ToList();

        Assert.Equal(5, contracts.Count);
        foreach (var contract in contracts)
        {
            var plazaRefs = contract.GetReferencedAssemblies().Select(r => r.Name!).Where(n => n.StartsWith("Plaza.", StringComparison.Ordinal));
            Assert.All(plazaRefs, n => Assert.Equal(SharedKernel, n));
        }
    }

    [Fact]
    public void A_module_exposes_only_its_registration_class()
    {
        foreach (var module in Modules)
        {
            var exported = module.GetExportedTypes().Select(t => t.Name).ToList();
            var name = module.GetName().Name!.Replace("Plaza.", string.Empty, StringComparison.Ordinal);

            Assert.Equal([$"{name}Module"], exported);
        }
    }

    [Fact]
    public void Module_internals_are_visible_only_to_the_test_project()
    {
        foreach (var module in Modules)
        {
            var friends = module.GetCustomAttributes<InternalsVisibleToAttribute>().Select(a => a.AssemblyName);
            Assert.All(friends, f => Assert.Equal(TestAssembly, f));
        }
    }

    [Fact]
    public void Module_project_files_reference_only_contracts_and_the_shared_kernel()
    {
        var modulesDir = Path.Combine(RepoRoot(), "src", "Modules");
        var projects = Directory.GetFiles(modulesDir, "*.csproj", SearchOption.AllDirectories);
        Assert.Equal(10, projects.Length);

        foreach (var project in projects)
        {
            var references = XDocument.Load(project).Descendants("ProjectReference")
                .Select(e => Path.GetFileNameWithoutExtension(e.Attribute("Include")!.Value))
                .ToList();
            var illegal = references.Where(r => ModuleNames.Contains(r) || r.EndsWith(".Api", StringComparison.Ordinal)).ToList();

            Assert.True(illegal.Count == 0, $"{Path.GetFileName(project)} references {string.Join(", ", illegal)}");
        }
    }

    private static string RepoRoot([CallerFilePath] string thisFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", ".."));
}
