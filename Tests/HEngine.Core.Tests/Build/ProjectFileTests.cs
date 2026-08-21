using System.Xml.Linq;

namespace HEngine.Core.Tests.Build;

public class ProjectFileTests
{
    [Fact(DisplayName = "Folder Include entries only exist for directories with no tracked files")]
    public void FolderInclude_Entries_Point_Only_To_Directories_Without_Tracked_Files()
    {
        var repoRoot = FindRepoRoot();
        var violations = new List<string>();

        foreach (var csprojPath in Directory.EnumerateFiles(repoRoot, "*.csproj", SearchOption.AllDirectories))
        {
            if (csprojPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                csprojPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                continue;

            var projectDirectory = Path.GetDirectoryName(csprojPath)!;
            var document = XDocument.Load(csprojPath);

            foreach (var folderInclude in document.Descendants("Folder"))
            {
                var relativePath = folderInclude.Attribute("Include")?.Value;
                if (relativePath == null)
                    continue;

                var fullPath = Path.Combine(projectDirectory, relativePath);

                if (!Directory.Exists(fullPath))
                {
                    violations.Add($"{csprojPath}: '{relativePath}' does not exist (phantom Folder Include)");
                    continue;
                }

                if (Directory.EnumerateFileSystemEntries(fullPath).Any())
                    violations.Add($"{csprojPath}: '{relativePath}' already contains tracked files (redundant Folder Include)");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

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
