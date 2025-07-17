using HEngine.Core.Contracts;

namespace HEngine.Core.Components.Physics;

public struct Rigidbody : IComponent
{
    public float Mass;
    public float Drag;           
    public float AngularDrag;    
    public bool IsKinematic;    
    public bool UseGravity;
    
    public Rigidbody(float mass = 1f, float drag = 0f, float angularDrag = 0.05f)
    {
        Mass = mass;
        Drag = drag;
        AngularDrag = angularDrag;
        IsKinematic = false;
        UseGravity = true;
    }
    
    public float InverseMass => IsKinematic ? 0f : (Mass > 0f ? 1f / Mass : 0f);
}