using HEngine.Core.Rendering.Contracts;

namespace HEngine.Rendering.Contracts;

public interface IRenderContextProvider
{
    IRenderContext GetRenderContext();

    bool TryGetRenderContext(out IRenderContext context);
}
