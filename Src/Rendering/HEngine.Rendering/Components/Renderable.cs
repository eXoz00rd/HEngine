using HEngine.Core.Contracts;

namespace HEngine.Core.Components.Rendering;

public struct Renderable : IComponent {
    public bool IsVisible;
    public int Layer;
    public uint MaterialId;
    public uint MeshId;

    public bool CastShadows;
    public bool ReceiveShadows;
    public float LodBias;
    public RenderingMode Mode;

    public Renderable(bool isVisible = true, int layer = 0)
    {
        IsVisible = isVisible;
        Layer = layer;
        MaterialId = 0;
        MeshId = 0;
        CastShadows = true;
        ReceiveShadows = true;
        LodBias = 1f;
        Mode = RenderingMode.Opaque;
    }
}

public enum RenderingMode {
    Opaque,
    Transparent,
    Cutout,
    Additive
}