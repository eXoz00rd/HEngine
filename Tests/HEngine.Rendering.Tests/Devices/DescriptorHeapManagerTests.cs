using HEngine.Rendering.Devices;
using Silk.NET.Direct3D12;

namespace HEngine.Rendering.Tests.Devices;

public class DescriptorHeapManagerTests
{
    [Fact]
    public void NewManager_IsNotInitialized()
    {
        using var manager = new DescriptorHeapManager();
        Assert.False(manager.IsInitialized);
    }

    [Fact]
    public void Dispose_DoesNotThrow_WhenNotInitialized()
    {
        var manager = new DescriptorHeapManager();
        var ex = Record.Exception(() => manager.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void AllocateSrv_Throws_WhenNotInitialized()
    {
        using var manager = new DescriptorHeapManager();
        Assert.Throws<InvalidOperationException>(() => manager.AllocateSrv());
    }

    [Fact]
    public void AllocateSampler_Throws_WhenNotInitialized()
    {
        using var manager = new DescriptorHeapManager();
        Assert.Throws<InvalidOperationException>(() => manager.AllocateSampler());
    }

    [Fact]
    public void AllocateStaging_Throws_WhenNotInitialized()
    {
        using var manager = new DescriptorHeapManager();
        Assert.Throws<InvalidOperationException>(() => manager.AllocateStaging());
    }

    [Fact]
    public void FreeSrv_Throws_WhenNotInitialized()
    {
        using var manager = new DescriptorHeapManager();
        Assert.Throws<InvalidOperationException>(() => manager.FreeSrv(DescriptorHandle.Invalid));
    }

    [Fact]
    public void SrvAllocatedCount_ReturnsZero_WhenNotInitialized()
    {
        using var manager = new DescriptorHeapManager();
        Assert.Equal(0, manager.SrvAllocatedCount);
    }

    [Fact]
    public void SamplerAllocatedCount_ReturnsZero_WhenNotInitialized()
    {
        using var manager = new DescriptorHeapManager();
        Assert.Equal(0, manager.SamplerAllocatedCount);
    }

    [Fact]
    public void SrvCapacity_ReturnsZero_WhenNotInitialized()
    {
        using var manager = new DescriptorHeapManager();
        Assert.Equal(0, manager.SrvCapacity);
    }

    [Fact]
    public void DefaultConstants_AreCorrect()
    {
        Assert.Equal(4096, DescriptorHeapManager.DefaultSrvHeapSize);
        Assert.Equal(64, DescriptorHeapManager.DefaultSamplerHeapSize);
        Assert.Equal(256, DescriptorHeapManager.DefaultStagingHeapSize);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var manager = new DescriptorHeapManager();
        manager.Dispose();
        var ex = Record.Exception(() => manager.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void AllocateSrv_Throws_AfterDispose()
    {
        var manager = new DescriptorHeapManager();
        manager.Dispose();
        Assert.Throws<ObjectDisposedException>(() => manager.AllocateSrv());
    }
}

public class DescriptorHandleTests
{
    [Fact]
    public void Invalid_HasNegativeIndex()
    {
        var handle = DescriptorHandle.Invalid;
        Assert.Equal(-1, handle.Index);
        Assert.False(handle.IsValid);
    }

    [Fact]
    public void ValidHandle_IsValid()
    {
        var handle = new DescriptorHandle { Index = 0 };
        Assert.True(handle.IsValid);
    }

    [Fact]
    public void Equals_SameIndex_ReturnsTrue()
    {
        var a = new DescriptorHandle { Index = 5 };
        var b = new DescriptorHandle { Index = 5 };
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_DifferentIndex_ReturnsFalse()
    {
        var a = new DescriptorHandle { Index = 5 };
        var b = new DescriptorHandle { Index = 6 };
        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void GetHashCode_SameIndex_SameHash()
    {
        var a = new DescriptorHandle { Index = 42 };
        var b = new DescriptorHandle { Index = 42 };
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equals_Object_ReturnsCorrectly()
    {
        var handle = new DescriptorHandle { Index = 1 };
        Assert.False(handle.Equals(null));
        Assert.False(handle.Equals("not a handle"));
        Assert.True(handle.Equals((object)new DescriptorHandle { Index = 1 }));
    }
}

