using System.Numerics;
using HEngine.Core.Managers;
using Xunit;
using Transform3D = HEngine.Core.Components.Transform.Transform;

namespace HEngine.Core.Tests.Components.Transform
{
    public class TransformHierarchyTests
    {
        [Fact(DisplayName = "Child world matrix equals parent * child local (translation only)")]
        public void ChildWorldMatrix_WithParentTranslation_ComputesCorrectly()
        {
            using var world = new WorldManager();

            var parent = world.CreateEntity();
            var child = world.CreateEntity();

            var parentT = new Transform3D(new Vector3(10, 0, 0));
            var childT = new Transform3D(new Vector3(2, 0, 0));

            world.AddComponent(parent, parentT);
            world.AddComponent(child, childT);
            
            ref var childRef = ref world.GetComponent<Transform3D>(child);
            childRef.Parent = parent;
            childRef.IsDirty = true;

            var expected = parentT.ToMatrix() * childT.ToMatrix();
            var actual = childRef.GetWorldMatrix(world);

            Assert.Equal(expected, actual);
        }

        [Fact(DisplayName = "Child inherits rotation from parent")]
        public void ChildWorldMatrix_WithParentRotation_ComputesCorrectly()
        {
            using var world = new WorldManager();

            var parent = world.CreateEntity();
            var child = world.CreateEntity();

            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
            var parentT = new Transform3D(Vector3.Zero, rotation, Vector3.One);
            var childLocalPos = new Vector3(0, 0, 1);
            var childT = new Transform3D(childLocalPos, Quaternion.Identity, Vector3.One);

            world.AddComponent(parent, parentT);
            world.AddComponent(child, childT);

            ref var childRef = ref world.GetComponent<Transform3D>(child);
            childRef.Parent = parent;
            childRef.IsDirty = true;

            var expected = parentT.ToMatrix() * childT.ToMatrix();
            var actual = childRef.GetWorldMatrix(world);

            Assert.Equal(expected, actual);

            var worldPoint = Vector3.Transform(Vector3.Zero, actual);
            var expectedPoint = Vector3.Transform(Vector3.Zero, expected);
            Assert.Equal(expectedPoint, worldPoint);
        }

        [Fact(DisplayName = "Dirty flag invalidates cache and recomputes")]
        public void DirtyFlag_ControlsCache()
        {
            using var world = new WorldManager();

            var parent = world.CreateEntity();
            var child = world.CreateEntity();

            var parentT = new Transform3D(new Vector3(1, 0, 0));
            var childT = new Transform3D(new Vector3(1, 0, 0));

            world.AddComponent(parent, parentT);
            world.AddComponent(child, childT);

            ref var childRef = ref world.GetComponent<Transform3D>(child);
            childRef.Parent = parent;
            childRef.IsDirty = true;

            var first = childRef.GetWorldMatrix(world);

            childRef.Position = new Vector3(3, 0, 0);
            childRef.IsDirty = true;

            var second = childRef.GetWorldMatrix(world);

            Assert.NotEqual(first, second);

            var third = childRef.GetWorldMatrix(world);
            Assert.Equal(second, third);
        }
    }
}
