using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

using SharpDX.Direct3D11;
using SharpDX.DXGI;

using ColorChecker.GraphicsCapture.Interop;

using Device = SharpDX.Direct3D11.Device;

namespace Rendering
{

    public class DirectXCapturingWindow : IDisposable
    {
        private Direct3D11CaptureFramePool  captureFramePool;
        private GraphicsCaptureItem         captureItem;
        private GraphicsCaptureSession      captureSession;

        public bool IsCapturing { get; private set; }

        public DirectXCapturingWindow()
        {
            IsCapturing = false;
        }

        public void Dispose()
        {
            StopCapture();
        }

        public void StartCapture(IntPtr hWnd, Device device, Factory factory)
        {
            var captureHandle = hWnd;
            if (captureHandle == IntPtr.Zero)
                return;

            captureItem = CreateItemForWindow(captureHandle);
            if (captureItem == null)
            {
                return;
            }

            captureItem.Closed += CaptureItemOnClosed;

            var hr = ColorChecker.NativeMethods.CreateDirect3D11DeviceFromDXGIDevice(device.NativePointer, out var pUnknown);
            if (hr != 0)
            {
                StopCapture();
                return;
            }

            var winrtDevice = (IDirect3DDevice)Marshal.GetObjectForIUnknown(pUnknown);
            Marshal.Release(pUnknown);

            captureFramePool = Direct3D11CaptureFramePool.Create(winrtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, captureItem.Size);
            captureSession = captureFramePool.CreateCaptureSession(captureItem);
            captureSession.StartCapture();
            IsCapturing = true;
        }

        public Texture2D TryGetNextFrameAsTexture2D(Device device)
        {
            using var frame = captureFramePool?.TryGetNextFrame();
            if (frame == null)
            {
                return null;
            }

            var surfaceDxgiInterfaceAccess = (IDirect3DDxgiInterfaceAccess)frame.Surface;
            var pResource = surfaceDxgiInterfaceAccess.GetInterface(new Guid("dc8e63f3-d12b-4952-b47b-5e45026a862d"));
            var surfaceTexture = new Texture2D(pResource); // shared resource

            var texture2dDescription = new Texture2DDescription
            {
                ArraySize           = 1,
                BindFlags           = BindFlags.ShaderResource,
                CpuAccessFlags      = CpuAccessFlags.None,
                Format              = Format.B8G8R8A8_UNorm,
                Height              = surfaceTexture.Description.Height,
                MipLevels           = 1,
                SampleDescription   = new SampleDescription(1, 0),
                Usage               = ResourceUsage.Default,
                Width               = surfaceTexture.Description.Width
            };
            var texture2d = new Texture2D(device, texture2dDescription);

            device.ImmediateContext.CopyResource(surfaceTexture, texture2d);
            return texture2d;
        }

        public void StopCapture()
        {
            captureSession  ?.Dispose();
            captureFramePool?.Dispose();
            captureSession   = null;
            captureFramePool = null;
            captureItem      = null;

            IsCapturing = false;
        }

        private static GraphicsCaptureItem CreateItemForWindow(IntPtr hWnd)
        {
            var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
            var interop = (IGraphicsCaptureItemInterop)factory;
            var pointer = interop.CreateForWindow(hWnd, typeof(GraphicsCaptureItem).GetInterface("IGraphicsCaptureItem").GUID);
            var capture = Marshal.GetObjectForIUnknown(pointer) as GraphicsCaptureItem;
            Marshal.Release(pointer);

            return capture;
        }

        private void CaptureItemOnClosed(GraphicsCaptureItem sender, object args)
        {
            StopCapture();
        }
    }

} // namespace Rendering 
