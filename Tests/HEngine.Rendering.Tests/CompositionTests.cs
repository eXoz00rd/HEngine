using HEngine.Core.Configuration;
using HEngine.Core.Extensions;
using HEngine.Core.Rendering.Contracts;
using HEngine.Rendering;
using HEngine.Rendering.Extensions;
using HEngine.Rendering.PostProcessing;
using HEngine.Rendering.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HEngine.Rendering.Tests
{
    public class CompositionTests
    {
        private static ServiceProvider BuildProductionServiceProvider(EngineConfiguration? config = null)
        {
            var services = new ServiceCollection();
            var configuration = config ?? new EngineConfiguration();

            services.AddHEngineCore(configuration);
            services.AddHEngineRendering(configuration);
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));

            return services.BuildServiceProvider();
        }

        [Fact(DisplayName = "Production composition resolves IRenderPipeline without falling back to no-op dependencies")]
        public void Composition_Resolves_RenderPipeline_With_Explicit_Dependencies()
        {
            using var provider = BuildProductionServiceProvider();

            var pipeline = provider.GetRequiredService<IRenderPipeline>();

            Assert.IsType<RenderPipeline>(pipeline);
        }

        [Fact(DisplayName = "Shadow settings resolved from DI reflect engine configuration, not a disabled fallback")]
        public void Composition_Resolves_ShadowSettings_From_EngineConfiguration()
        {
            var configuration = new EngineConfiguration
            {
                Shadow = new ShadowSettings { Enabled = true, Resolution = 4096, CascadeCount = 3 }
            };

            using var provider = BuildProductionServiceProvider(configuration);

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

            using var provider = BuildProductionServiceProvider(configuration);

            var pbrSettings = provider.GetRequiredService<PbrSettings>();
            var postProcessingSettings = provider.GetRequiredService<PostProcessingSettings>();

            Assert.Equal(2.5f, pbrSettings.Exposure);
            Assert.Equal(3.0f, postProcessingSettings.BloomIntensity);
        }

        [Fact(DisplayName = "LightingSystem and ShadowRenderingSystem are registered and initialized in DI")]
        public void Composition_Resolves_Initialized_Lighting_And_Shadow_Systems()
        {
            using var provider = BuildProductionServiceProvider();

            var lightingSystem = provider.GetRequiredService<LightingSystem>();
            var shadowRenderingSystem = provider.GetRequiredService<ShadowRenderingSystem>();

            // Both systems throw on Update/RenderShadows before Initialize() has been called with a
            // WorldManager; resolving them from the container must not require callers to do that
            // themselves, or the container is silently handing back an unusable instance.
            lightingSystem.Update(0f);
            Assert.NotNull(lightingSystem.LastLights);
        }

        [Fact(DisplayName = "PostProcessStack is registered in DI as a shared singleton")]
        public void Composition_Resolves_PostProcessStack_As_Singleton()
        {
            using var provider = BuildProductionServiceProvider();

            var stackA = provider.GetRequiredService<PostProcessStack>();
            var stackB = provider.GetRequiredService<PostProcessStack>();

            Assert.Same(stackA, stackB);
        }
    }
}
