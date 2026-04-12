using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Components.Transform;
using HEngine.Core.Contracts;
using HEngine.Core.Managers;
using HEngine.Core.Queries;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering.Data;

namespace HEngine.Rendering.Systems;

public class LightingSystem : ISystem
{
    public const int MaxLights = 8;

    private bool _disposed;
    private WorldManager _world = null!;
    private QueryBuilder _queryBuilder = null!;

    private LightData[] _lastLights = Array.Empty<LightData>();
    public ReadOnlySpan<LightData> LastLights => _lastLights;

    public void Initialize(WorldManager worldManager)
    {
        _world = worldManager;
        _queryBuilder = new QueryBuilder(worldManager.ComponentManager, worldManager.EntityManager);
    }

    public void Update(float deltaTime)
    {
        if (_disposed) return;
        _lastLights = GatherLights(_world);
    }

    public LightData[] GatherLights(WorldManager world)
    {
        var result = new List<LightData>(MaxLights);
        
        var dirQuery = _queryBuilder.With<DirectionalLight>();
        foreach (var (_, d) in dirQuery)
        {
            var ld = new LightData
            {
                Type = LightType.Directional,
                Color = d.Color,
                Intensity = d.Intensity,
                Direction = SafeNormalize(d.Direction)
            };
            result.Add(ld);
            if (result.Count >= MaxLights) return result.ToArray();
        }
        
        var pointQuery = _queryBuilder.With<Transform, PointLight>();
        foreach (var (entity, t, p) in pointQuery)
        {
            if (world.HasComponent<Culled>(entity))
                continue;

            var wm = t.GetWorldMatrix(world);
            var pos = new Vector3(wm.M41, wm.M42, wm.M43);

            var ld = new LightData
            {
                Type = LightType.Point,
                Color = p.Color,
                Intensity = p.Intensity,
                Position = pos,
                Range = p.Range,
                Attenuation = p.Attenuation
            };
            result.Add(ld);
            if (result.Count >= MaxLights) break;
        }

        var spotQuery = _queryBuilder.With<Transform, SpotLight>();
        foreach (var (entity, t, s) in spotQuery)
        {
            if (world.HasComponent<Culled>(entity))
                continue;

            var wm = t.GetWorldMatrix(world);
            var pos = new Vector3(wm.M41, wm.M42, wm.M43);

            var ld = new LightData
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
            result.Add(ld);
            if (result.Count >= MaxLights) break;
        }

        return result.ToArray();
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
        _lastLights = Array.Empty<LightData>();
        _disposed = true;
    }
}
