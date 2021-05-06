using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;

namespace Rendering
{
    public class DirectXShader : IDisposable
    {
        CompilationResult vertexShaderBlob;
        CompilationResult pixelShaderBlob;

        private string sourceCode;
        public  string source
        {
            get { return sourceCode; }
            set
            {
                if (sourceCode == value) return;
                sourceCode = value;

                Dispose();
                vertexShaderBlob = ShaderBytecode.Compile(sourceCode, "VS", "vs_5_0", ShaderFlags.None, EffectFlags.None);
                pixelShaderBlob  = ShaderBytecode.Compile(sourceCode, "PS", "ps_5_0", ShaderFlags.None, EffectFlags.None);
            }
        }

        InputLayout  inputLayout;
        VertexShader vertexShader;
        PixelShader  pixelShader;
        SamplerState samplerState;

        public void Dispose()
        {
            samplerState?.Dispose();
            inputLayout ?.Dispose();
            vertexShader?.Dispose();
            pixelShader ?.Dispose();
            samplerState = null;
            inputLayout  = null;
            vertexShader = null;
            pixelShader  = null;
        }

        void CreateResources(Device device, DeviceContext context)
        {
            if (vertexShader == null)
            {
                if (vertexShaderBlob != null)
                {
                    vertexShader = new VertexShader(device, vertexShaderBlob);
                }
            }

            if (inputLayout == null)
            {
                if (vertexShaderBlob != null)
                {
                    inputLayout = new InputLayout
                    (
                        device,
                        ShaderSignature.GetInputSignature(vertexShaderBlob),
                        new []
                        {
                            new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32A32_Float,  0, 0),
                            new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float,       12, 0)
                        }
                    );
                }
            }

            if (pixelShader == null)
            {
                if (pixelShaderBlob != null)
                {
                    pixelShader = new PixelShader(device, pixelShaderBlob);
                }
            }

            if (samplerState == null)
            {
                var samplerStateDescription = new SamplerStateDescription
                {
                    AddressU = TextureAddressMode.Clamp,
                    AddressV = TextureAddressMode.Clamp,
                    AddressW = TextureAddressMode.Clamp,
                    Filter = Filter.MinMagMipLinear
                };
                samplerState = new SamplerState(device, samplerStateDescription);
            }

            context.VertexShader.Set(vertexShader);
            context.InputAssembler.InputLayout = inputLayout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            context.PixelShader.Set(pixelShader);
            context.PixelShader.SetSampler(0, samplerState);
        }

        public void SetContext(Device device, DeviceContext context)
        {
            CreateResources(device, context);
        }
    }

} // namespace Rendering 
