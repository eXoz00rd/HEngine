using System;
using System.Linq;
using System.Numerics;
using HEngine.Core.Components.Core;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;
using HEngine.Core.Systems;
using Xunit;

namespace HEngine.Core.Tests.Scene
{
    public class SceneGraphTests : IDisposable
    {
        private readonly WorldManager _world = new();
        private readonly TransformHierarchySystem _hierarchySystem = new();

        public SceneGraphTests()
        {
            _world.AddSystem(_hierarchySystem);
        }

        public void Dispose()
        {
            _world.Dispose();
        }

        [Fact(DisplayName = "CreateEntity with parent adds to hierarchy and updates world transform")]
        public void CreateEntity_WithParent_AddsToHierarchy()
        {
            var parent = _world.CreateEntity();
            var child = _world.CreateEntity();

            _world.AddComponent(parent, new Transform(new Vector3(10, 0, 0)));
            _world.AddComponent(child, new Transform(new Vector3(5, 0, 0)));

            _hierarchySystem.SetParent(child, parent);

           _world.Update(0.016f);

            Assert.True(_world.HasComponent<Children>(parent));
            ref var children = ref _world.GetComponent<Children>(parent);
            Assert.Equal(1, children.Count);
            Assert.Equal(child, children.GetChild(0));

            ref var childTransform = ref _world.GetComponent<Transform>(child);
            Assert.Equal(parent, childTransform.Parent);

            var worldMatrix = childTransform.GetWorldMatrix(_world);
            var worldPos = new Vector3(worldMatrix.M41, worldMatrix.M42, worldMatrix.M43);
            Assert.Equal(new Vector3(15, 0, 0), worldPos);
        }

        [Fact(DisplayName = "DestroyEntity recursively removes children and their components")]
        public void RemoveEntity_WithChildren_RemovesAll()
        {
            var root = _world.CreateEntity();
            var childA = _world.CreateEntity();
            var childB = _world.CreateEntity();

            _world.AddComponent(root, new Transform(new Vector3(0, 0, 0)));
            _world.AddComponent(childA, new Transform(new Vector3(1, 0, 0)));
            _world.AddComponent(childB, new Transform(new Vector3(2, 0, 0)));

            _hierarchySystem.SetParent(childA, root);
            _hierarchySystem.SetParent(childB, childA);

            Assert.True(_world.HasComponent<Transform>(root));
            Assert.True(_world.HasComponent<Transform>(childA));
            Assert.True(_world.HasComponent<Transform>(childB));

            DestroyEntityRecursive(root);

            Assert.False(_world.HasComponent<Transform>(root));
            Assert.False(_world.HasComponent<Transform>(childA));
            Assert.False(_world.HasComponent<Transform>(childB));
        }

        [Fact(DisplayName = "Reparent updates Children lists and world transform")]
        public void SetParent_Updates_ChildTransform()
        {
            var a = _world.CreateEntity();
            var b = _world.CreateEntity();
            var c = _world.CreateEntity();

            _world.AddComponent(a, new Transform(new Vector3(10, 0, 0)));
            _world.AddComponent(b, new Transform(new Vector3(5, 0, 0)));
            _world.AddComponent(c, new Transform(new Vector3(1, 0, 0)));

            _hierarchySystem.SetParent(b, a);
            _hierarchySystem.SetParent(c, a);

            _world.Update(0.016f);

            var bWorld1 = _world.GetComponent<Transform>(b).GetWorldMatrix(_world);
            var bPos1 = new Vector3(bWorld1.M41, bWorld1.M42, bWorld1.M43);
            Assert.Equal(new Vector3(15, 0, 0), bPos1);

            _hierarchySystem.SetParent(b, c);
            _world.Update(0.016f);

            ref var aChildren = ref _world.GetComponent<Children>(a);
            Assert.Equal(1, aChildren.Count);
            Assert.Equal(c, aChildren.GetChild(0));

            ref var cChildren = ref _world.GetComponent<Children>(c);
            Assert.Equal(1, cChildren.Count);
            Assert.Equal(b, cChildren.GetChild(0));

            var bWorld2 = _world.GetComponent<Transform>(b).GetWorldMatrix(_world);
            var bPos2 = new Vector3(bWorld2.M41, bWorld2.M42, bWorld2.M43);
            Assert.Equal(new Vector3(16, 0, 0), bPos2);
        }

        private void DestroyEntityRecursive(Entity e)
        {
            if (_world.HasComponent<Children>(e))
            {
                var children = _world.GetComponent<Children>(e);
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children.GetChild(i);
                    if (child != Entity.Null)
                        DestroyEntityRecursive(child);
                }
            }

            _world.DestroyEntity(e);
        }
    }
}
