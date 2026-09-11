using System.Xml.Linq;

namespace HEngine.Architecture.Tests;

public class BackendIndependenceTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [Theory(DisplayName = "A backend-agnostic module declares no graphics or windowing package")]
    [InlineData("Src/Core/HEngine.Foundation/HEngine.Foundation.csproj")]
    [InlineData("Src/Core/HEngine.ECS/HEngine.ECS.csproj")]
    [InlineData("Src/Core/HEngine.Scene/HEngine.Scene.csproj")]
    [InlineData("Src/Core/HEngine.Assets/HEngine.Assets.csproj")]
    [InlineData("Src/Core/HEngine.Platform/HEngine.Platform.csproj")]
    [InlineData("Src/Core/HEngine.Serialization/HEngine.Serialization.csproj")]
    [InlineData("Src/Rendering/HEngine.Rendering/HEngine.Rendering.csproj")]
    [InlineData("Src/Runtime/HEngine.Runtime/HEngine.Runtime.csproj")]
    public void Module_Declares_No_Backend_Package(string relativeProjectPath)
    {
        var packages = PackageReferences(Path.Combine(RepoRoot, relativeProjectPath));

        Assert.DoesNotContain(packages, package => package.StartsWith("Silk.NET", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "HEngine.Runtime composes the domain without naming a backend (Z8)")]
    public void Runtime_Does_Not_Reference_A_Backend()
    {
        var references = ProjectReferenceNames(Path.Combine(RepoRoot, "Src/Runtime/HEngine.Runtime/HEngine.Runtime.csproj"));

        Assert.DoesNotContain("HEngine.Rendering.D3D12", references);
        Assert.DoesNotContain("HEngine.Platform.Windows", references);
    }

    private static IReadOnlyList<string> PackageReferences(string csprojPath) =>
        XDocument.Load(csprojPath)
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")!.Value)
            .ToList();

    private static IReadOnlyList<string> ProjectReferenceNames(string csprojPath) =>
        XDocument.Load(csprojPath)
            .Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension(
                element.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar)))
            .ToList();

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "HEngine.slnx")))
            directory = directory.Parent;

        if (directory == null)
            throw new InvalidOperationException("Could not locate repository root (HEngine.slnx not found).");

        return directory.FullName;
    }
}
