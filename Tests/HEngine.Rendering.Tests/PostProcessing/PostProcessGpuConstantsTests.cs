using System.Runtime.InteropServices;
using HEngine.Rendering.Data;

namespace HEngine.Rendering.Tests.PostProcessing;

public class PostProcessGpuConstantsTests
{
    [Fact(DisplayName = "ToneMappingCbuffer size is 16 bytes")]
    public void ToneMappingCbuffer_Size_Is_16_Bytes()
    {
        var size = Marshal.SizeOf<ToneMappingCbuffer>();
        Assert.Equal(16, size);
    }

    [Fact(DisplayName = "ToneMappingCbuffer Create sets all fields")]
    public void ToneMappingCbuffer_Create_Sets_Fields()
    {
        var cb = ToneMappingCbuffer.Create(2, 1.5f, 2.2f);

        Assert.Equal(2, cb.ToneMappingMode);
        Assert.Equal(1.5f, cb.Exposure);
        Assert.Equal(2.2f, cb.Gamma);
        Assert.Equal(0f, cb.Pad);
    }
}
