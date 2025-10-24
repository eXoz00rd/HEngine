using System.Numerics;
using HEngine.Core.Rendering;
using Xunit;

namespace HEngine.Core.Tests.Rendering
{
    public class FreeCameraTests
    {
        [Fact(DisplayName = "FreeCamera default matrices should be valid")]
        public void FreeCamera_Defaults_Should_Produce_Valid_Matrices()
        {
            var cam = new FreeCamera();

            var expectedView = Matrix4x4.CreateLookAt(cam.Position, cam.Target, cam.Up);
            Assert.Equal(expectedView, cam.ViewMatrix);

            var expectedProj = Matrix4x4.CreatePerspectiveFieldOfView(
                MathF.PI / 4f,
                16f / 9f,
                0.1f,
                1000f);
            Assert.Equal(expectedProj, cam.ProjectionMatrix);
        }

        [Fact(DisplayName = "FreeCamera matrices should reflect updated properties")]
        public void FreeCamera_Updates_Should_Reflect_In_Matrices()
        {
            var cam = new FreeCamera
            {
                Position = new Vector3(1, 2, 3),
                Target = new Vector3(0, 1, 0),
                Up = Vector3.UnitY,
                FieldOfView = MathF.PI / 3f,
                NearPlane = 0.5f,
                FarPlane = 500f,
                AspectRatio = 4f / 3f
            };

            var expectedView = Matrix4x4.CreateLookAt(new Vector3(1, 2, 3), new Vector3(0, 1, 0), Vector3.UnitY);
            Assert.Equal(expectedView, cam.ViewMatrix);

            var expectedProj = Matrix4x4.CreatePerspectiveFieldOfView(
                MathF.PI / 3f,
                4f / 3f,
                0.5f,
                500f);
            Assert.Equal(expectedProj, cam.ProjectionMatrix);
        }

        [Fact(DisplayName = "FreeCamera Projection clamps edge parameters")]
        public void FreeCamera_Projection_Should_Clamp_Parameters()
        {
            var cam = new FreeCamera
            {
                FieldOfView = 0f,
                NearPlane = -1f,
                FarPlane = 0f,
                AspectRatio = 0f 
            };

            var proj = cam.ProjectionMatrix;
            Assert.True(proj.M11 != 0 || proj.M22 != 0);
        }
    }
}
