using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;

namespace Rendering
{
    class DirectXSwapChain : IDisposable
    {
        SwapChain1          swapChain1;
        RenderTargetView    renderTargetView;

        public void Present()
        {
            swapChain1.Present(1, PresentFlags.None, new PresentParameters());
        }

        public void Resize(int w, int h)
        {
            ReleaseRTV();
            var desc = swapChain1.Description;
            swapChain1.ResizeBuffers(desc.BufferCount, w, h, desc.ModeDescription.Format, desc.Flags);
        }

        public void ReleaseRTV()
        {
            renderTargetView?.Dispose();
            renderTargetView = null;
        }

        public RenderTargetView GetRenderTargetView(SharpDX.Direct3D11.Device device)
        {
            if (renderTargetView == null)
            {
                using (var m_backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain1, 0))
                {
                    renderTargetView = new RenderTargetView(device, m_backBuffer);
                }
            }
            return renderTargetView;
        }

        public void Dispose()
        {
            ReleaseRTV();
            swapChain1?.Dispose();
            swapChain1 = null;
        }

        public DirectXSwapChain(SwapChain1 swapchain)
        {
            swapChain1 = swapchain;
        }
    }

} // namespace Rendering 
