using System.Numerics;
using System.Runtime.InteropServices;
using HEngine.Rendering.Data;
using HEngine.Rendering.Enums;

namespace HEngine.Rendering.Managers;

public sealed class MaterialPropertyBuffer
{
    private const int DefaultBufferSize = 4096;
    private const int ConstantBufferAlignment = 256;

    public static byte[] SerializeToBytes(MaterialPropertyBlock propertyBlock)
    {
        using var stream = new MemoryStream(DefaultBufferSize);
        using var writer = new BinaryWriter(stream);

        var properties = propertyBlock.GetProperties().ToList();
        writer.Write(properties.Count);

        foreach (var property in properties)
        {
            writer.Write(property.Name);
            writer.Write((int)property.Type);

            switch (property.Type)
            {
                case MaterialPropertyType.Float:
                    writer.Write(property.AsFloat());
                    break;

                case MaterialPropertyType.Int:
                    writer.Write(property.AsInt());
                    break;

                case MaterialPropertyType.Vector2:
                    var v2 = property.AsVector2();
                    writer.Write(v2.X);
                    writer.Write(v2.Y);
                    break;

                case MaterialPropertyType.Vector3:
                    var v3 = property.AsVector3();
                    writer.Write(v3.X);
                    writer.Write(v3.Y);
                    writer.Write(v3.Z);
                    break;

                case MaterialPropertyType.Vector4:
                case MaterialPropertyType.Color:
                    var v4 = property.AsVector4();
                    writer.Write(v4.X);
                    writer.Write(v4.Y);
                    writer.Write(v4.Z);
                    writer.Write(v4.W);
                    break;

                case MaterialPropertyType.Matrix4x4:
                    var matrix = property.AsMatrix4x4();
                    WriteMatrix4x4(writer, matrix);
                    break;

                case MaterialPropertyType.Texture2D:
                case MaterialPropertyType.TextureCube:
                    writer.Write(property.AsTexturePath());
                    break;
            }
        }

        return stream.ToArray();
    }

    public static byte[] SerializeToConstantBuffer(MaterialPropertyBlock propertyBlock)
    {
        var buffer = new List<byte>(ConstantBufferAlignment);

        foreach (var property in propertyBlock.GetProperties())
        {
            if (property.Type == MaterialPropertyType.Texture2D ||
                property.Type == MaterialPropertyType.TextureCube)
                continue;

            switch (property.Type)
            {
                case MaterialPropertyType.Float:
                    buffer.AddRange(BitConverter.GetBytes(property.AsFloat()));
                    break;

                case MaterialPropertyType.Int:
                    buffer.AddRange(BitConverter.GetBytes(property.AsInt()));
                    break;

                case MaterialPropertyType.Vector2:
                    var v2 = property.AsVector2();
                    buffer.AddRange(BitConverter.GetBytes(v2.X));
                    buffer.AddRange(BitConverter.GetBytes(v2.Y));
                    break;

                case MaterialPropertyType.Vector3:
                    var v3 = property.AsVector3();
                    buffer.AddRange(BitConverter.GetBytes(v3.X));
                    buffer.AddRange(BitConverter.GetBytes(v3.Y));
                    buffer.AddRange(BitConverter.GetBytes(v3.Z));
                    buffer.Add(0);
                    break;

                case MaterialPropertyType.Vector4:
                case MaterialPropertyType.Color:
                    var v4 = property.AsVector4();
                    buffer.AddRange(BitConverter.GetBytes(v4.X));
                    buffer.AddRange(BitConverter.GetBytes(v4.Y));
                    buffer.AddRange(BitConverter.GetBytes(v4.Z));
                    buffer.AddRange(BitConverter.GetBytes(v4.W));
                    break;

                case MaterialPropertyType.Matrix4x4:
                    var matrix = property.AsMatrix4x4();
                    buffer.AddRange(GetMatrix4x4Bytes(matrix));
                    break;
            }
        }

        var alignedSize = (buffer.Count + ConstantBufferAlignment - 1) & ~(ConstantBufferAlignment - 1);
        while (buffer.Count < alignedSize)
        {
            buffer.Add(0);
        }

        return buffer.ToArray();
    }

    public static MaterialPropertyBlock DeserializeFromBytes(byte[] data)
    {
        var propertyBlock = new MaterialPropertyBlock();

        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        var count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            var name = reader.ReadString();
            var type = (MaterialPropertyType)reader.ReadInt32();

            switch (type)
            {
                case MaterialPropertyType.Float:
                    propertyBlock.SetFloat(name, reader.ReadSingle());
                    break;

                case MaterialPropertyType.Int:
                    propertyBlock.SetInt(name, reader.ReadInt32());
                    break;

                case MaterialPropertyType.Vector2:
                    propertyBlock.SetVector2(name, new Vector2(
                        reader.ReadSingle(),
                        reader.ReadSingle()));
                    break;

                case MaterialPropertyType.Vector3:
                    propertyBlock.SetVector3(name, new Vector3(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle()));
                    break;

                case MaterialPropertyType.Vector4:
                case MaterialPropertyType.Color:
                    propertyBlock.SetVector4(name, new Vector4(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle()));
                    break;

                case MaterialPropertyType.Matrix4x4:
                    propertyBlock.SetMatrix(name, ReadMatrix4x4(reader));
                    break;

                case MaterialPropertyType.Texture2D:
                    propertyBlock.SetTexture(name, reader.ReadString());
                    break;

                case MaterialPropertyType.TextureCube:
                    propertyBlock.SetCubeTexture(name, reader.ReadString());
                    break;
            }
        }

        propertyBlock.MarkClean();
        return propertyBlock;
    }

    private static void WriteMatrix4x4(BinaryWriter writer, Matrix4x4 matrix)
    {
        writer.Write(matrix.M11); writer.Write(matrix.M12); writer.Write(matrix.M13); writer.Write(matrix.M14);
        writer.Write(matrix.M21); writer.Write(matrix.M22); writer.Write(matrix.M23); writer.Write(matrix.M24);
        writer.Write(matrix.M31); writer.Write(matrix.M32); writer.Write(matrix.M33); writer.Write(matrix.M34);
        writer.Write(matrix.M41); writer.Write(matrix.M42); writer.Write(matrix.M43); writer.Write(matrix.M44);
    }

    private static Matrix4x4 ReadMatrix4x4(BinaryReader reader)
    {
        return new Matrix4x4(
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
        );
    }

    private static byte[] GetMatrix4x4Bytes(Matrix4x4 matrix)
    {
        var bytes = new List<byte>(64);
        bytes.AddRange(BitConverter.GetBytes(matrix.M11));
        bytes.AddRange(BitConverter.GetBytes(matrix.M12));
        bytes.AddRange(BitConverter.GetBytes(matrix.M13));
        bytes.AddRange(BitConverter.GetBytes(matrix.M14));
        bytes.AddRange(BitConverter.GetBytes(matrix.M21));
        bytes.AddRange(BitConverter.GetBytes(matrix.M22));
        bytes.AddRange(BitConverter.GetBytes(matrix.M23));
        bytes.AddRange(BitConverter.GetBytes(matrix.M24));
        bytes.AddRange(BitConverter.GetBytes(matrix.M31));
        bytes.AddRange(BitConverter.GetBytes(matrix.M32));
        bytes.AddRange(BitConverter.GetBytes(matrix.M33));
        bytes.AddRange(BitConverter.GetBytes(matrix.M34));
        bytes.AddRange(BitConverter.GetBytes(matrix.M41));
        bytes.AddRange(BitConverter.GetBytes(matrix.M42));
        bytes.AddRange(BitConverter.GetBytes(matrix.M43));
        bytes.AddRange(BitConverter.GetBytes(matrix.M44));
        return bytes.ToArray();
    }
}
