using HEngine.Core.Contracts;
using System.Numerics;

namespace HEngine.Rendering.Components;

public struct Sprite : IComponent {
    public Vector2 Size;
    public Vector4 Color;
    public string TexturePath;
    public Vector2 Origin;

    public Sprite()
    {
        Size = Vector2.One;
        Color = Vector4.One;
        TexturePath = string.Empty;
        Origin = Vector2.Zero;
    }

    public Sprite(Vector2 size, Vector4 color, string texturePath = "", Vector2 origin = default)
    {
        Size = size;
        Color = color;
        TexturePath = texturePath;
        Origin = origin;
    }
}