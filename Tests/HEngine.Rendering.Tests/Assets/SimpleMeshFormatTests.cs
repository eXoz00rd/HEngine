using System.Numerics;
using HEngine.Core.Rendering.Data;
using HEngine.Rendering.Assets;

namespace HEngine.Rendering.Tests.Assets;

public class SimpleMeshFormatTests : IDisposable
{
    private readonly string _testDirectory;

    public SimpleMeshFormatTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "HEngineTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void Save_ValidMesh_CreatesFile()
    {
        var vertices = CreateTestVertices();
        var indices = new uint[] { 0, 1, 2 };
        var path = Path.Combine(_testDirectory, "test.mesh");

        SimpleMeshFormat.Save(path, vertices, indices);

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Save_CreatesDirectoryIfNeeded()
    {
        var vertices = CreateTestVertices();
        var indices = new uint[] { 0, 1, 2 };
        var subDir = Path.Combine(_testDirectory, "subdir", "nested");
        var path = Path.Combine(subDir, "test.mesh");

        SimpleMeshFormat.Save(path, vertices, indices);

        Assert.True(Directory.Exists(subDir));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Load_SavedMesh_ReturnsCorrectData()
    {
        var originalVertices = CreateTestVertices();
        var originalIndices = new uint[] { 0, 1, 2, 2, 1, 3 };
        var path = Path.Combine(_testDirectory, "test.mesh");

        SimpleMeshFormat.Save(path, originalVertices, originalIndices);
        var (loadedVertices, loadedIndices) = SimpleMeshFormat.Load(path);

        Assert.Equal(originalVertices.Length, loadedVertices.Length);
        Assert.Equal(originalIndices.Length, loadedIndices.Length);

        for (var i = 0; i < originalVertices.Length; i++)
        {
            Assert.Equal(originalVertices[i].Position, loadedVertices[i].Position);
            Assert.Equal(originalVertices[i].Normal, loadedVertices[i].Normal);
            Assert.Equal(originalVertices[i].TexCoord, loadedVertices[i].TexCoord);
            Assert.Equal(originalVertices[i].Color, loadedVertices[i].Color);
        }

        for (var i = 0; i < originalIndices.Length; i++)
        {
            Assert.Equal(originalIndices[i], loadedIndices[i]);
        }
    }

    [Fact]
    public void Load_WithCubePrimitive_LoadsCorrectly()
    {
        var (cubeVertices, cubeIndices) = MeshPrimitives.CreateCube(2.0f);
        var path = Path.Combine(_testDirectory, "cube.mesh");

        SimpleMeshFormat.Save(path, cubeVertices, cubeIndices);
        var (loadedVertices, loadedIndices) = SimpleMeshFormat.Load(path);

        Assert.Equal(cubeVertices.Length, loadedVertices.Length);
        Assert.Equal(cubeIndices.Length, loadedIndices.Length);
    }

    [Fact]
    public void Save_WithNullPath_ThrowsArgumentException()
    {
        var vertices = CreateTestVertices();
        var indices = new uint[] { 0, 1, 2 };

        Assert.Throws<ArgumentException>(() => SimpleMeshFormat.Save(null!, vertices, indices));
        Assert.Throws<ArgumentException>(() => SimpleMeshFormat.Save("", vertices, indices));
        Assert.Throws<ArgumentException>(() => SimpleMeshFormat.Save("   ", vertices, indices));
    }

    [Fact]
    public void Save_WithNullVertices_ThrowsArgumentNullException()
    {
        var path = Path.Combine(_testDirectory, "test.mesh");
        var indices = new uint[] { 0, 1, 2 };

        Assert.Throws<ArgumentNullException>(() => SimpleMeshFormat.Save(path, null!, indices));
    }

    [Fact]
    public void Save_WithNullIndices_ThrowsArgumentNullException()
    {
        var vertices = CreateTestVertices();
        var path = Path.Combine(_testDirectory, "test.mesh");

        Assert.Throws<ArgumentNullException>(() => SimpleMeshFormat.Save(path, vertices, null!));
    }

    [Fact]
    public void Save_WithEmptyVertices_ThrowsArgumentException()
    {
        var path = Path.Combine(_testDirectory, "test.mesh");
        var vertices = Array.Empty<Vertex3D>();
        var indices = new uint[] { 0, 1, 2 };

        Assert.Throws<ArgumentException>(() => SimpleMeshFormat.Save(path, vertices, indices));
    }

    [Fact]
    public void Load_NonExistentFile_ThrowsFileNotFoundException()
    {
        var path = Path.Combine(_testDirectory, "nonexistent.mesh");

        Assert.Throws<FileNotFoundException>(() => SimpleMeshFormat.Load(path));
    }

    [Fact]
    public void Load_InvalidMagicNumber_ThrowsInvalidDataException()
    {
        var path = Path.Combine(_testDirectory, "invalid.mesh");
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(0xDEADBEEF);
            writer.Write(3u);
            writer.Write(3u);
        }

        Assert.Throws<InvalidDataException>(() => SimpleMeshFormat.Load(path));
    }

    [Fact]
    public void Load_ZeroVertices_ThrowsInvalidDataException()
    {
        var path = Path.Combine(_testDirectory, "zero.mesh");
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(MeshFileHeader.ExpectedMagicNumber);
            writer.Write(0u);
            writer.Write(0u);
        }

        Assert.Throws<InvalidDataException>(() => SimpleMeshFormat.Load(path));
    }

    [Fact]
    public void Load_TooManyVertices_ThrowsInvalidDataException()
    {
        var path = Path.Combine(_testDirectory, "toomany.mesh");
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(MeshFileHeader.ExpectedMagicNumber);
            writer.Write(20_000_000u);
            writer.Write(0u);
        }

        Assert.Throws<InvalidDataException>(() => SimpleMeshFormat.Load(path));
    }

    [Fact]
    public void Load_ExtraDataInFile_ThrowsInvalidDataException()
    {
        var vertices = CreateTestVertices();
        var indices = new uint[] { 0, 1, 2 };
        var path = Path.Combine(_testDirectory, "extra.mesh");

        SimpleMeshFormat.Save(path, vertices, indices);

        using (var stream = File.Open(path, FileMode.Append))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(0xDEADBEEF);
        }

        Assert.Throws<InvalidDataException>(() => SimpleMeshFormat.Load(path));
    }

    [Fact]
    public void RoundTrip_LargeMesh_PreservesAllData()
    {
        var vertexCount = 1000;
        var vertices = new Vertex3D[vertexCount];
        var random = new Random(42);

        for (var i = 0; i < vertexCount; i++)
        {
            vertices[i] = new Vertex3D(
                new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()),
                new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()),
                new Vector2((float)random.NextDouble(), (float)random.NextDouble()),
                new Vector4((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(),
                    (float)random.NextDouble())
            );
        }

        var indices = new uint[vertexCount];
        for (uint i = 0; i < vertexCount; i++)
        {
            indices[i] = i;
        }

        var path = Path.Combine(_testDirectory, "large.mesh");

        SimpleMeshFormat.Save(path, vertices, indices);
        var (loadedVertices, loadedIndices) = SimpleMeshFormat.Load(path);

        Assert.Equal(vertices.Length, loadedVertices.Length);
        Assert.Equal(indices.Length, loadedIndices.Length);

        for (var i = 0; i < vertices.Length; i++)
        {
            Assert.Equal(vertices[i].Position, loadedVertices[i].Position);
            Assert.Equal(vertices[i].Normal, loadedVertices[i].Normal);
            Assert.Equal(vertices[i].TexCoord, loadedVertices[i].TexCoord);
            Assert.Equal(vertices[i].Color, loadedVertices[i].Color);
        }
    }

    private static Vertex3D[] CreateTestVertices()
    {
        return new[]
        {
            new Vertex3D(
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0),
                new Vector2(0, 0),
                new Vector4(1, 0, 0, 1)
            ),
            new Vertex3D(
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector2(1, 0),
                new Vector4(0, 1, 0, 1)
            ),
            new Vertex3D(
                new Vector3(0, 0, 1),
                new Vector3(0, 1, 0),
                new Vector2(0, 1),
                new Vector4(0, 0, 1, 1)
            )
        };
    }
}