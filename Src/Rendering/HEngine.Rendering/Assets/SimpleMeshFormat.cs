using System.Numerics;
using HEngine.Core.Rendering.Data;

namespace HEngine.Rendering.Assets;

public struct MeshFileHeader
{
    public uint MagicNumber;
    public uint VertexCount;
    public uint IndexCount;

    public const uint ExpectedMagicNumber = 0x4853454D;
}

public static class SimpleMeshFormat
{
    public static void Save(string path, Vertex3D[] vertices, uint[] indices)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(indices);
        if (vertices.Length == 0)
            throw new ArgumentException("Vertices array cannot be empty", nameof(vertices));

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        writer.Write(MeshFileHeader.ExpectedMagicNumber);
        writer.Write((uint)vertices.Length);
        writer.Write((uint)indices.Length);

        foreach (var v in vertices)
        {
            writer.Write(v.Position.X);
            writer.Write(v.Position.Y);
            writer.Write(v.Position.Z);

            writer.Write(v.Normal.X);
            writer.Write(v.Normal.Y);
            writer.Write(v.Normal.Z);

            writer.Write(v.TexCoord.X);
            writer.Write(v.TexCoord.Y);

            writer.Write(v.Color.X);
            writer.Write(v.Color.Y);
            writer.Write(v.Color.Z);
            writer.Write(v.Color.W);
        }

        foreach (var i in indices)
            writer.Write(i);
    }

    public static (Vertex3D[] vertices, uint[] indices) Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException($"Mesh file not found: {path}");

        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        var magicNumber = reader.ReadUInt32();
        if (magicNumber != MeshFileHeader.ExpectedMagicNumber)
            throw new InvalidDataException(
                $"Invalid mesh file format. Expected magic number 0x{MeshFileHeader.ExpectedMagicNumber:X8}, got 0x{magicNumber:X8}");

        var vertexCount = reader.ReadUInt32();
        var indexCount = reader.ReadUInt32();

        switch (vertexCount)
        {
            case 0:
                throw new InvalidDataException("Mesh file contains zero vertices");
            case > 10_000_000:
                throw new InvalidDataException($"Mesh file contains too many vertices: {vertexCount}");
        }

        var vertices = new Vertex3D[vertexCount];
        for (var i = 0; i < vertexCount; i++)
        {
            var posX = reader.ReadSingle();
            var posY = reader.ReadSingle();
            var posZ = reader.ReadSingle();

            var normX = reader.ReadSingle();
            var normY = reader.ReadSingle();
            var normZ = reader.ReadSingle();

            var texU = reader.ReadSingle();
            var texV = reader.ReadSingle();

            var colorR = reader.ReadSingle();
            var colorG = reader.ReadSingle();
            var colorB = reader.ReadSingle();
            var colorA = reader.ReadSingle();

            vertices[i] = new Vertex3D(
                new Vector3(posX, posY, posZ),
                new Vector3(normX, normY, normZ),
                new Vector2(texU, texV),
                new Vector4(colorR, colorG, colorB, colorA)
            );
        }

        var indices = new uint[indexCount];
        for (var i = 0; i < indexCount; i++) indices[i] = reader.ReadUInt32();

        return stream.Position != stream.Length
            ? throw new InvalidDataException("Mesh file contains unexpected extra data")
            : (vertices, indices);
    }
}