using System;
using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Rendering.Data;
using HEngine.ECS.Queries;
using HEngine.Rendering.Components;

namespace HEngine.Rendering.Systems;

public class LightingSystem : ISystem
{
    public const int MaxLights = 8;

    private bool _disposed;
    private bool _isInitialized;
    private WorldManager _world = null!;
    private QueryBuilder _queryBuilder = null!;

    private readonly LightData[] _lightBuffer = new LightData[MaxLights];
    private int _lightCount;

    public ReadOnlySpan<LightData> LastLights => new(_lightBuffer, 0, _lightCount);

    public void Initialize(WorldManager worldManager)
    {
        _world = worldManager;
        _queryBuilder = new QueryBuilder(worldManager.ComponentManager, worldManager.EntityManager);
        _isInitialized = true;
    }

    public void Update(float deltaTime)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LightingSystem));
        }

        if (!_isInitialized)
        {
            throw new InvalidOperationException("LightingSystem must be initialized before calling Update.");
        }

        _lightCount = GatherLightsInto(_world, _lightBuffer);
    }

    public LightData[] GatherLights(WorldManager world)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LightingSystem));
        }

        if (!_isInitialized)
        {
            throw new InvalidOperationException("LightingSystem must be initialized before calling GatherLights.");
        }

        if (!ReferenceEquals(world, _world))
        {
            throw new ArgumentException(
                "The provided WorldManager does not match the instance LightingSystem was initialized with.",
                nameof(world));
        }

        Span<LightData> buffer = stackalloc LightData[MaxLights];
        var count = GatherLightsInto(world, buffer);
        return buffer[..count].ToArray();
    }

    private int GatherLightsInto(WorldManager world, Span<LightData> buffer)
    {
        var count = 0;

        var dirQuery = _queryBuilder.With<DirectionalLight>();
        foreach (var (_, d) in dirQuery)
        {
            buffer[count++] = new LightData
            {
                Type = LightType.Directional,
                Color = d.Color,
                Intensity = d.Intensity,
                Direction = SafeNormalize(d.Direction)
            };
            if (count >= MaxLights) return count;
        }

        var pointQuery = _queryBuilder.With<Transform, PointLight>();
        foreach (var (entity, t, p) in pointQuery)
        {
            if (world.HasComponent<Culled>(entity))
                continue;

            var wm = t.GetWorldMatrix(world);
            var pos = new Vector3(wm.M41, wm.M42, wm.M43);

            buffer[count++] = new LightData
            {
                Type = LightType.Point,
                Color = p.Color,
                Intensity = p.Intensity,
                Position = pos,
                Range = p.Range,
                Attenuation = p.Attenuation
            };
            if (count >= MaxLights) return count;
        }

        var spotQuery = _queryBuilder.With<Transform, SpotLight>();
        foreach (var (entity, t, s) in spotQuery)
        {
            if (world.HasComponent<Culled>(entity))
                continue;

            var wm = t.GetWorldMatrix(world);
            var pos = new Vector3(wm.M41, wm.M42, wm.M43);

            buffer[count++] = new LightData
            {
                Type = LightType.Spot,
                Color = s.Color,
                Intensity = s.Intensity,
                Position = pos,
                Direction = SafeNormalize(s.Direction),
                Range = s.Range,
                InnerConeAngle = s.InnerConeAngle,
                OuterConeAngle = s.OuterConeAngle
            };
            if (count >= MaxLights) return count;
        }

        return count;
    }

    private static Vector3 SafeNormalize(in Vector3 v)
    {
        var len = v.Length();
        if (len < 1e-6f) return v;
        return v / len;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _queryBuilder = null!;
        _world = null!;
        _lightCount = 0;
        _disposed = true;
    }
}
