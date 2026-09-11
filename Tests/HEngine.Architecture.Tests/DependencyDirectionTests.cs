using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace HEngine.Architecture.Tests;

public class DependencyDirectionTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string OwnProjectFile = Path.Combine(RepoRoot, "Tests", "HEngine.Architecture.Tests", "HEngine.Architecture.Tests.csproj");

    private static readonly IReadOnlyDictionary<string, int> LayerByModule =
        GetModuleLayers(Path.Combine(RepoRoot, "HEngine.slnx"));

    private static readonly IReadOnlyDictionary<string, string> ModuleProjectPaths =
        GetProjectReferences(OwnProjectFile).ToDictionary(reference => reference.Name, reference => reference.Path);

    public static IEnumerable<object[]> ModuleNames() => LayerByModule.Keys.Select(name => new object[] { name });

    [Theory(DisplayName = "A module's csproj declares ProjectReferences only to modules from a strictly lower architecture layer")]
    [MemberData(nameof(ModuleNames))]
    public void Module_OnlyReferences_StrictlyLowerLayers(string moduleName)
    {
        var layer = LayerByModule[moduleName];
        var projectPath = ModuleProjectPaths[moduleName];

        foreach (var (referencedName, _) in GetProjectReferences(projectPath))
        {
            Assert.True(LayerByModule.TryGetValue(referencedName, out var referencedLayer),
                $"{moduleName} references {referencedName}, which is not one of HEngine.slnx's registered layered modules — register it in the solution's numbered folders or fix the reference.");

            Assert.True(referencedLayer < layer,
                $"{moduleName} (layer {layer}) must not reference {referencedName} (layer {referencedLayer}) — dependencies must flow strictly downward.");
        }
    }

    [Fact(DisplayName = "HEngine.Architecture.Tests is the only project referencing every module")]
    public void ThisProject_Is_TheOnlyProject_ReferencingEveryModule()
    {
        Assert.Equal(LayerByModule.Keys.OrderBy(name => name), ModuleProjectPaths.Keys.OrderBy(name => name));

        var otherProjects = Directory.EnumerateFiles(RepoRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !string.Equals(path, OwnProjectFile, StringComparison.OrdinalIgnoreCase));

        foreach (var project in otherProjects)
        {
            var referencedNames = GetProjectReferences(project).Select(reference => reference.Name).ToHashSet();

            Assert.False(LayerByModule.Keys.All(referencedNames.Contains),
                $"{Path.GetFileNameWithoutExtension(project)} references every module, but only HEngine.Architecture.Tests should.");
        }
    }

    private static IEnumerable<(string Name, string Path)> GetProjectReferences(string csprojPath)
    {
        var directory = Path.GetDirectoryName(csprojPath)!;

        return XDocument.Load(csprojPath)
            .Descendants("ProjectReference")
            .Select(element => Path.GetFullPath(Path.Combine(directory, element.Attribute("Include")!.Value)))
            .Select(path => (Path.GetFileNameWithoutExtension(path), path));
    }

    private const string HostsFolderName = "Hosts";

    private static IReadOnlyDictionary<string, int> GetModuleLayers(string solutionFile)
    {
        var layerByModule = new Dictionary<string, int>();

        foreach (var folder in XDocument.Load(solutionFile).Descendants("Folder"))
        {
            var match = Regex.Match(folder.Attribute("Name")!.Value, @"^/(\d+)-(.+)/$");
            if (!match.Success || match.Groups[2].Value == HostsFolderName)
                continue;

            var layer = int.Parse(match.Groups[1].Value);

            foreach (var project in folder.Elements("Project"))
                layerByModule[Path.GetFileNameWithoutExtension(project.Attribute("Path")!.Value)] = layer;
        }

        return layerByModule;
    }

    private static string FindRepoRoot([CallerFilePath] string sourceFilePath = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "..", ".."));
}
