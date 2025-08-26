namespace HEngine.Core.Rendering.Contracts;

public interface IRenderCommandListFactory
{
    IRenderCommandList CreateCommandList(ICommandQueue commandQueue);
}