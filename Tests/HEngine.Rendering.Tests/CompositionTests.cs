using System.Linq;
using System.Numerics;
using HEngine.Core.Components.Rendering;
using HEngine.Core.Configuration;
using HEngine.Core.Extensions;
using HEngine.Core.Managers;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering;
using HEngine.Rendering.Extensions;
using HEngine.Rendering.PostProcessing;
using HEngine.Rendering.Systems;
using HEngine.Rendering.Systems.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HEngine.Rendering.Tests
{
    public class CompositionTests
    {
        private static ServiceCollection BuildProductionServiceCollection(EngineConfiguration? config = null)
        {
            var services = new ServiceCollection();
            var configuration = config ?? new EngineConfiguration();

            services.AddHEngineCore(configuration);
            services.AddHEngineRendering(configuration);
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));

            return services;
        }

        [Fact(DisplayName = "Production composition validates every constructor-injected registration in the graph without instantiating anything; factory-backed registrations are covered separately by the targeted resolve tests below")]
        public void Composition_Validates_Full_Service_Graph()
        {
            var services = BuildProductionServiceCollection();

            using var provider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        }

        [Fact(DisplayName = "Shadow settings resolved from DI reflect engine configuration, not a disabled fallback")]
        public void Composition_Resolves_ShadowSettings_From_EngineConfiguration()
        {
            var configuration = new EngineConfiguration
            {
                Shadow = new ShadowSettings { Enabled = true, Resolution = 4096, CascadeCount = 3 }
            };

            using var provider = BuildProductionServiceCollection(configuration).BuildServiceProvider();

            var shadowSettings = provider.GetRequiredService<ShadowSettings>();

            Assert.True(shadowSettings.Enabled);
            Assert.Equal(4096, shadowSettings.Resolution);
            Assert.Equal(3, shadowSettings.CascadeCount);
            Assert.Same(configuration.Shadow, shadowSettings);
        }

        [Fact(DisplayName = "PBR and post-processing configuration sections are registered in DI")]
        public void Composition_Resolves_Pbr_And_PostProcessing_Settings()
        {
            var configuration = new EngineConfiguration
            {
                PBR = new PbrSettings { Exposure = 2.5f },
                PostProcessing = new PostProcessingSettings { BloomIntensity = 3.0f }
            };

            using var provider = BuildProductionServiceCollection(configuration).BuildServiceProvider();

            var pbrSettings = provider.GetRequiredService<PbrSettings>();
            var postProcessingSettings = provider.GetRequiredService<PostProcessingSettings>();

            Assert.Same(configuration.PBR, pbrSettings);
            Assert.Same(configuration.PostProcessing, postProcessingSettings);
            Assert.Equal(2.5f, pbrSettings.Exposure);
            Assert.Equal(3.0f, postProcessingSettings.BloomIntensity);
        }

        [Fact(DisplayName = "LightingSystem is registered as an initialized singleton that can gather lights from the resolved WorldManager")]
        public void Composition_Resolves_Initialized_LightingSystem_Singleton()
        {
            using var provider = BuildProductionServiceCollection().BuildServiceProvider();

            var world = provider.GetRequiredService<WorldManager>();
            var entity = world.CreateEntity();
            world.AddComponent(entity, new DirectionalLight
            {
                Direction = new Vector3(0, -1, 0),
                Color = Vector3.One,
                Intensity = 1f
            });

            var lightingSystem = provider.GetRequiredService<LightingSystem>();
            lightingSystem.Update(0f);

            Assert.Equal(1, lightingSystem.LastLights.Length);
            Assert.Same(lightingSystem, provider.GetRequiredService<LightingSystem>());
        }

        [Fact(DisplayName = "ShadowRenderingSystem is registered as a shared singleton")]
        public void Composition_Resolves_ShadowRenderingSystem_As_Singleton()
        {
            using var provider = BuildProductionServiceCollection().BuildServiceProvider();

            var shadowRenderingSystemA = provider.GetRequiredService<ShadowRenderingSystem>();
            var shadowRenderingSystemB = provider.GetRequiredService<ShadowRenderingSystem>();

            Assert.Same(shadowRenderingSystemA, shadowRenderingSystemB);
        }

        [Fact(DisplayName = "PostProcessStack is registered in DI as a shared singleton")]
        public void Composition_Resolves_PostProcessStack_As_Singleton()
        {
            using var provider = BuildProductionServiceCollection().BuildServiceProvider();

            var stackA = provider.GetRequiredService<PostProcessStack>();
            var stackB = provider.GetRequiredService<PostProcessStack>();

            Assert.Same(stackA, stackB);
        }

        [Fact(DisplayName = "Production composition resolves the real RenderPipeline/RenderingSystem implementations, not a test double")]
        public void Composition_Resolves_RenderPipeline_And_RenderingSystem_As_Concrete_Production_Types()
        {
            using var provider = BuildProductionServiceCollection().BuildServiceProvider();

            Assert.IsType<RenderPipeline>(provider.GetRequiredService<IRenderPipeline>());
            Assert.IsType<RenderingSystem>(provider.GetRequiredService<IRenderingSystem>());
        }

        [Fact(DisplayName = "Production composition fails to build when a required dependency's registration is removed")]
        public void Composition_Fails_When_A_Required_Registration_Is_Missing()
        {
            var services = BuildProductionServiceCollection();
            var shadowSettingsDescriptor = services.Single(d => d.ServiceType == typeof(ShadowSettings));
            services.Remove(shadowSettingsDescriptor);

            Assert.ThrowsAny<Exception>(() =>
                services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }));
        }

        [Fact(DisplayName = "Shadows are enabled by default configuration but no IShadowRenderer is wired in production yet — tracks #19",
            Skip = "Intentionally red until #19 wires a production IShadowRenderer; unskip as part of #19's Definition of Done")]
        public void Composition_ShadowsEnabledByDefault_RequireAWiredShadowRenderer()
        {
            using var provider = BuildProductionServiceCollection().BuildServiceProvider();

            var shadowSettings = provider.GetRequiredService<ShadowSettings>();
            var shadowRenderingSystem = provider.GetRequiredService<ShadowRenderingSystem>();

            Assert.True(shadowSettings.Enabled);
            Assert.True(shadowRenderingSystem.HasShadowRenderer);
        }

        [Fact(DisplayName = "Bloom is enabled by default configuration but no post-process effect is registered in production yet — tracks #20",
            Skip = "Intentionally red until #20 registers a production bloom post-process effect; unskip as part of #20's Definition of Done")]
        public void Composition_BloomEnabledByDefault_RequiresARegisteredPostProcessEffect()
        {
            using var provider = BuildProductionServiceCollection().BuildServiceProvider();

            var postProcessingSettings = provider.GetRequiredService<PostProcessingSettings>();
            var postProcessStack = provider.GetRequiredService<PostProcessStack>();

            Assert.True(postProcessingSettings.EnableBloom);
            Assert.True(postProcessStack.EnabledEffectCount > 0);
        }
    }
}
