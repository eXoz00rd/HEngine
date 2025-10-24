using System;
using System.Linq;
using System.Numerics;
using HEngine.Core.Components.Core;
using HEngine.Core.Components.Transform;
using HEngine.Core.Managers;
using HEngine.Core.Primitives;
using HEngine.Core.Scene;
using HEngine.Core.Systems;
using Xunit;

namespace HEngine.Core.Tests.Scene
{
    public class SceneGraphClassTests : IDisposable
    {
        private readonly WorldManager _world = new();
        private readonly SceneGraph _graph;

        public SceneGraphClassTests()
        {
            _graph = new SceneGraph(_world);
        }

        public void Dispose()
        {
            _world.Dispose();
        }

        [Fact(DisplayName = "CreateEntity(name,parent) should add Transform, optional Name, and set parent")]
        public void CreateEntity_WithParent_SetsHierarchy()
        {
            var parent = _graph.CreateEntity("Parent");
            ref var pT = ref _world.GetComponent<Transform>(parent);
            pT.Position = new Vector3(10, 0, 0);
            pT.IsDirty = true;

            var child = _graph.CreateEntity("Child", parent);
            Assert.True(_world.HasComponent<Transform>(child));
            Assert.True(_world.HasComponent<Name>(child));

            ref var children = ref _world.GetComponent<Children>(parent);
            Assert.Equal(1, children.Count);
            Assert.Equal(child, children.GetChild(0));

            ref var cT = ref _world.GetComponent<Transform>(child);
            cT.Position = new Vector3(5, 0, 0);
            cT.IsDirty = true;

            _world.Update(0.016f);
            var worldMatrix = cT.GetWorldMatrix(_world);
            var pos = new Vector3(worldMatrix.M41, worldMatrix.M42, worldMatrix.M43);
            Assert.Equal(new Vector3(15, 0, 0), pos);
        }

        [Fact(DisplayName = "SetParent should reparent and update Children lists")]        
        public void SetParent_Reparents()
        {
            var a = _graph.CreateEntity("A");
            var b = _graph.CreateEntity("B");
            var c = _graph.CreateEntity("C");

            ref var aT = ref _world.GetComponent<Transform>(a);
            aT.Position = new Vector3(10, 0, 0); aT.IsDirty = true;
            ref var bT = ref _world.GetComponent<Transform>(b);
            bT.Position = new Vector3(5, 0, 0); bT.IsDirty = true;
            ref var cT = ref _world.GetComponent<Transform>(c);
            cT.Position = new Vector3(1, 0, 0); cT.IsDirty = true;

            _graph.SetParent(b, a);
            _graph.SetParent(c, a);
            _world.Update(0.016f);

            _graph.SetParent(b, c);
            _world.Update(0.016f);

            ref var aChildren = ref _world.GetComponent<Children>(a);
            Assert.Equal(1, aChildren.Count);
            Assert.Equal(c, aChildren.GetChild(0));

            ref var cChildren = ref _world.GetComponent<Children>(c);
            Assert.Equal(1, cChildren.Count);
            Assert.Equal(b, cChildren.GetChild(0));

            var bWorld = _world.GetComponent<Transform>(b).GetWorldMatrix(_world);
            var bPos = new Vector3(bWorld.M41, bWorld.M42, bWorld.M43);
            Assert.Equal(new Vector3(16, 0, 0), bPos);
        }

        [Fact(DisplayName = "RemoveEntity should recursively delete children")]
        public void RemoveEntity_RemovesChildren()
        {
            var root = _graph.CreateEntity("Root");
            var childA = _graph.CreateEntity("A", root);
            var childB = _graph.CreateEntity("B", childA);

            Assert.True(_world.HasComponent<Transform>(root));
            Assert.True(_world.HasComponent<Transform>(childA));
            Assert.True(_world.HasComponent<Transform>(childB));

            _graph.RemoveEntity(root);

            Assert.False(_world.HasComponent<Transform>(root));
            Assert.False(_world.HasComponent<Transform>(childA));
            Assert.False(_world.HasComponent<Transform>(childB));
        }

        [Fact(DisplayName = "GetChildren should return direct children only")]
        public void GetChildren_ReturnsDirectChildren()
        {
            var root = _graph.CreateEntity("Root");
            var a = _graph.CreateEntity("A", root);
            var b = _graph.CreateEntity("B", root);
            var c = _graph.CreateEntity("C", a);

            var children = _graph.GetChildren(root).ToArray();
            Assert.Equal(2, children.Length);
            Assert.Contains(a, children);
            Assert.Contains(b, children);
            Assert.DoesNotContain(c, children);
        }
    }
}