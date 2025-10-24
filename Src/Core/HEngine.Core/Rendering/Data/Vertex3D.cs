using System.Numerics;

namespace HEngine.Core.Rendering.Data
{
    public struct Vertex3D
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;
        public Vector4 Color;

        public Vertex3D(Vector3 position, Vector3 normal, Vector2 texCoord, Vector4 color)
        {
            Position = position;
            Normal = normal;
            TexCoord = texCoord;
            Color = color;
        }

        public static uint GetStride() => (uint)(sizeof(float) * (3 + 3 + 2 + 4));
    }
}
