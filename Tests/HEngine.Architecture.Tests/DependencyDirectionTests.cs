using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace HEngine.Architecture.Tests;

public class DependencyDirectionTests
{
    private static readonly IReadOnlyDictionary<string, int> LayerByModule = new Dictionary<string, int>
    {
        ["HEngine.Foundation"] = 1,
        ["HEngine.ECS"] = 1,
        ["HEngine.Scene"] = 2,
        ["HEngine.Assets"] = 2,
        ["HEngine.Platform"] = 2,
        ["HEngine.Serialization"] = 2,
        ["HEngine.Core"] = 3,
        ["HEngine.Platform.Windows"] = 4,
        ["HEngine.Rendering"] = 4,
    };

    private static readonly string OwnProjectFile = FindOwnProjectFile();

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
            if (!LayerByModule.TryGetValue(referencedName, out var referencedLayer))
                continue;

            Assert.True(referencedLayer < layer,
                $"{moduleName} (layer {layer}) must not reference {referencedName} (layer {referencedLayer}) — dependencies must flow strictly downward.");
        }
    }

    [Fact(DisplayName = "This project is the only one allowed to reference every module")]
    public void ThisProject_References_EveryModule()
    {
        Assert.Equal(LayerByModule.Keys.OrderBy(name => name), ModuleProjectPaths.Keys.OrderBy(name => name));
    }

    private static IEnumerable<(string Name, string Path)> GetProjectReferences(string csprojPath)
    {
        var directory = Path.GetDirectoryName(csprojPath)!;

        return XDocument.Load(csprojPath)
            .Descendants("ProjectReference")
            .Select(element => Path.GetFullPath(Path.Combine(directory, element.Attribute("Include")!.Value)))
            .Select(path => (Path.GetFileNameWithoutExtension(path), path));
    }

    private static string FindOwnProjectFile([CallerFilePath] string sourceFilePath = "")
        => Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "HEngine.Architecture.Tests.csproj");
}
