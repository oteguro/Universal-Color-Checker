using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.Windows;

using Buffer        = SharpDX.Direct3D11.Buffer;
using Device        = SharpDX.Direct3D11.Device;

namespace ColorChecker
{
    internal struct Vertex
    {
        public RawVector3 Position;
        public RawVector2 TexCoord;
    }

    public class AppWindow : IDisposable
    {
        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        private readonly Rendering.DirectXCapturingWindow capturingApplicationWindow;
        private readonly string titleString;

        public AppWindow(string title)
        {
            titleString = title;
            capturingApplicationWindow = new Rendering.DirectXCapturingWindow();
        }

        public void Dispose()
        {
            capturingApplicationWindow?.Dispose();
        }

        private Texture3D Create3dLutFromPng(Device device, Bitmap image)
        {   // 2D image -> 3D LUT 
            var boundsRect  = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            var bitmap      = image.Clone(boundsRect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var mapSrc      = bitmap.LockBits(boundsRect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var databox     = new[] { new DataBox(mapSrc.Scan0, image.Height * 4, image.Height * image.Height * 4) };
            var desc        = new Texture3DDescription()
            {
                Height              = image.Height,
                Width               = image.Height,
                Depth               = image.Height,
                MipLevels           = 1,
                Format              = Format.B8G8R8A8_UNorm,
                Usage               = ResourceUsage.Default,
                BindFlags           = BindFlags.ShaderResource,
                CpuAccessFlags      = CpuAccessFlags.None,
                OptionFlags         = ResourceOptionFlags.None,
            };
            return new Texture3D(device, desc, databox);
        }

        private SwapChainDescription DefaultSwapChainDescription(Size clientSize, IntPtr formHandle)
        {
            return new SwapChainDescription
            {
                BufferCount         = 2,
                Flags               = SwapChainFlags.None,
                IsWindowed          = true,
                ModeDescription     = new ModeDescription(clientSize.Width, clientSize.Height, new Rational(60, 1), Format.B8G8R8A8_UNorm),
                OutputHandle        = formHandle,
                SampleDescription   = new SampleDescription(1, 0),
                SwapEffect          = SwapEffect.Discard,
                Usage               = Usage.RenderTargetOutput
            };
        }

        private void SetVertices(Device device, DeviceContext context)
        {
            using var vertices = Buffer.Create(device, BindFlags.VertexBuffer, new[]
            {
                new Vertex { Position = new RawVector3(-1.0f,  1.0f, 0.0f), TexCoord = new RawVector2(0.0f, 0.0f) },
                new Vertex { Position = new RawVector3( 1.0f,  1.0f, 0.0f), TexCoord = new RawVector2(1.0f, 0.0f) },
                new Vertex { Position = new RawVector3(-1.0f, -1.0f, 0.0f), TexCoord = new RawVector2(0.0f, 1.0f) },
                new Vertex { Position = new RawVector3( 1.0f, -1.0f, 0.0f), TexCoord = new RawVector2(1.0f, 1.0f) }
            });
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, Utilities.SizeOf<Vertex>(), 0));
        }

        public void Show()
        {
            using var form = new RenderForm(titleString);

            // Device, Context 
            var swapChainDescription = DefaultSwapChainDescription(form.ClientSize, form.Handle);
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, swapChainDescription, out var device, out var swapChain);
            DeviceContext context = device.ImmediateContext;

            // SwapChain. 
            using var swapChain1 = swapChain.QueryInterface<SwapChain1>();
            using var factory = swapChain1.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);
            Rendering.DirectXSwapChain appSwapChain = new Rendering.DirectXSwapChain(swapChain1);

            // Shader. 
            Rendering.DirectXShader shader = new Rendering.DirectXShader();
            shader.source = Properties.Resources.Shader_fx;

            // Quad vertices. 
            SetVertices(device, context);

            using var contantBuffer = new SharpDX.Direct3D11.Buffer(device, Utilities.SizeOf<RawVector4>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            // Create 3DLUT texture. 
            // @see : https://github.com/andrewwillmott/colour-blind-luts
            using var lutTexture0 = Create3dLutFromPng(device, Properties.Resources.lut0);
            using var lutTexture1 = Create3dLutFromPng(device, Properties.Resources.lut1);
            using var lutTexture2 = Create3dLutFromPng(device, Properties.Resources.lut2);
            using var lutTexture3 = Create3dLutFromPng(device, Properties.Resources.lut3);
            using var lutTexture4 = Create3dLutFromPng(device, Properties.Resources.lut4);
            using var lutTexture5 = Create3dLutFromPng(device, Properties.Resources.lut5);
            using var lutTexture6 = Create3dLutFromPng(device, Properties.Resources.lut6);

            // Event 
            var isResized = false;
            form.UserResized += (_, __) => isResized = true;

            bool correction = false;
            var keyIndex = 0;
            form.KeyDown += (sender, e) =>
            {
                if (e.KeyCode      == Keys.F1)
                {
                    keyIndex = 0;
                }
                else if (e.KeyCode == Keys.F2)
                {
	                keyIndex = 1;
                }
                else if (e.KeyCode == Keys.F3)
                {
                    keyIndex = 2;
                }
                else if (e.KeyCode == Keys.F4)
                {
                    keyIndex = 3;
                }
                else if (e.KeyCode == Keys.F5)
                {
                    correction = !correction;
                }
            };

            // Mainloop. 
            Size originalClientSize = form.ClientSize;
            IntPtr windowHandle = IntPtr.Zero;
            RenderLoop.Run(form, () =>
            {
                if (!capturingApplicationWindow.IsCapturing)
                {
                    var windowSelection = new WindowChooser();
                    windowSelection.ShowDialog();
                    windowHandle = windowSelection.PickCaptureTarget();
                    keyIndex = windowSelection.SelectedLut;
                    correction = windowSelection.ApplyLut;
                    capturingApplicationWindow.StartCapture(windowHandle, device, factory);

                    NativeMethods.RECT rect;
                    if (NativeMethods.GetWindowRect(windowHandle, out rect))
                    {
                        originalClientSize = new Size(
                            (rect.right  - rect.left),
                            (rect.bottom - rect.top)
                        );
                    }
                }
                if (windowHandle != IntPtr.Zero)
                {
                    if (NativeMethods.DwmGetWindowAttribute(windowHandle, DWMWA_EXTENDED_FRAME_BOUNDS, out var bounds, Marshal.SizeOf(typeof(NativeMethods.RECT))) == 0)
                    {
                        var windowSize = new Size(
                        (bounds.right  - bounds.left),
                        (bounds.bottom - bounds.top)

                        );
                        if (form.ClientSize != windowSize)
                        {
                            form.ClientSize = windowSize;
                            form.FormBorderStyle = FormBorderStyle.FixedToolWindow;
                            isResized = true;
                        }
                    }
                }
                if (isResized)
                {
                    appSwapChain.Resize(form.ClientSize.Width, form.ClientSize.Height);
                    isResized = false;
                }
                string text = "ColorChecker ";
                switch(keyIndex)
                {
                    case 0:
                        text += "[P]";
                        break;
                    case 1:
                        text += "[D]";
                        break;
                    case 2:
                        text += "[T]";
                        break;
                    default:
                        break;
                }
                if (correction && keyIndex < 3)
                {
                    text += " : with color correction LUT";
                }
                form.Text = text;

                context.Rasterizer  .SetViewport(0, 0, form.ClientSize.Width, form.ClientSize.Height);
                context.OutputMerger.SetTargets(appSwapChain.GetRenderTargetView(device));
                context.ClearRenderTargetView(appSwapChain.GetRenderTargetView(device), SharpDX.Color.Black);
                shader.SetContext(device, context);

                float rectScaleWidth  = (float)(form.ClientSize.Width)  / (float)(originalClientSize.Width);
                float rectScaleHeight = (float)(form.ClientSize.Height) / (float)(originalClientSize.Height);
                rectScaleWidth  = Math.Max(Math.Min(rectScaleWidth,  1.0f), 0.01f);
                rectScaleHeight = Math.Max(Math.Min(rectScaleHeight, 1.0f), 0.01f);

                RawVector4 windowScale = new RawVector4(rectScaleWidth, rectScaleHeight, 1, 1);
                context.UpdateSubresource(ref windowScale, contantBuffer);
                context.VertexShader.SetConstantBuffer(0, contantBuffer);

                using var texture2d = capturingApplicationWindow.TryGetNextFrameAsTexture2D(device);
                if (texture2d != null)
                {
                    using var shaderResourceView0 = new ShaderResourceView(device, texture2d);
                    context.PixelShader.SetShaderResource(0, shaderResourceView0);
                }

                var lutTexture = (keyIndex == 0) ? lutTexture1 :
                                 (keyIndex == 1) ? lutTexture2 :
                                 (keyIndex == 2) ? lutTexture3 : lutTexture0;
                using var shaderResourceView1 = new ShaderResourceView(device, lutTexture);
                var correctTexture = (correction) ?
                                     (keyIndex == 0) ? lutTexture4 :
                                     (keyIndex == 1) ? lutTexture5 :
                                     (keyIndex == 2) ? lutTexture6 : lutTexture0
                                     : lutTexture0;
                using var shaderResourceView2 = new ShaderResourceView(device, correctTexture);

                context.PixelShader.SetShaderResource(1, shaderResourceView1);
                context.PixelShader.SetShaderResource(2, shaderResourceView2);
                context.Draw(4, 0);

                appSwapChain.Present();
            });

            shader      .Dispose();
            appSwapChain.Dispose();
            swapChain   .Dispose();
            device      .Dispose();
        }
    }

} // namespace ColorChecker 
