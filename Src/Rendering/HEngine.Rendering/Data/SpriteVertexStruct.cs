using System.Numerics;
using System.Runtime.InteropServices;

namespace HEngine.Rendering.Data;

[StructLayout(LayoutKind.Sequential)]
public struct SpriteVertex
{
    public Vector3 Position;
    public Vector4 Color;
    
    public SpriteVertex(Vector3 position, Vector4 color)
    {
        Position = position;
        Color = color;
    }
    
    public static uint GetStride() => (uint)Marshal.SizeOf<SpriteVertex>();
}
