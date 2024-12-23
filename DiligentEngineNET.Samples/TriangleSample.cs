﻿using System.Numerics;
using System.Runtime.CompilerServices;
using Diligent;
using ValueType = Diligent.ValueType;

namespace DiligentEngineNET.Samples;

public class TriangleSample(GraphicsBackend backend) : Application(backend)
{
    private IPipelineState? _pipelineState;
    private IShader CreateShader(ShaderType shaderType)
    {
        using var shaderSourceFactory = EngineFactory.CreateDefaultShaderSourceStreamFactory(Path.Combine(Environment.CurrentDirectory, "Assets"));
        var shaderCi = new ShaderCreateInfo()
        {
            SourceLanguage = ShaderSourceLanguage.Hlsl,
            Desc = new ShaderDesc()
            {
                Name = $"Cube {shaderType}",
                UseCombinedTextureSamplers = true,
                ShaderType = shaderType,
            },
            EntryPoint = "main",
            CompileFlags = ShaderCompileFlags.PackMatrixRowMajor,
            FilePath = shaderType == ShaderType.Vertex ? "Shaders/TriangleVS.hlsl" : "Shaders/TrianglePS.hlsl",
            ShaderSourceStreamFactory = shaderSourceFactory,
        };

        return Device.CreateShader(shaderCi);
    }

    private IPipelineState CreatePipelineState()
    {
        using var vertexShader = CreateShader(ShaderType.Vertex);
        using var pixelShader = CreateShader(ShaderType.Pixel);

        var pipelineCreateInfo = new GraphicsPipelineStateCreateInfo()
        {
            PSODesc = new PipelineStateDesc()
            {
                Name = "Triangle PSO",
                PipelineType = PipelineType.Graphics,
            },
            GraphicsPipeline = new GraphicsPipelineDesc()
            {
                NumRenderTargets = 1,
                RTVFormats = [SwapChain.Desc.ColorBufferFormat],
                DSVFormat = SwapChain.Desc.DepthBufferFormat,
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                RasterizerDesc = new RasterizerStateDesc()
                {
                    CullMode = CullMode.None,
                },
                DepthStencilDesc = new DepthStencilStateDesc()
                {
                    DepthEnable = true
                },
            },
            VS = vertexShader,
            PS = pixelShader,
        };

        return Device.CreateGraphicsPipelineState(pipelineCreateInfo);
    }

    private void Render()
    {
        var rtv = SwapChain.CurrentBackBufferRTV;
        var dsv = SwapChain.DepthBufferDSV;

        var clearColor = new[] { .350f, .350f, .350f, 1.0f };

        ImmediateContext.SetRenderTargets([rtv], dsv, ResourceStateTransitionMode.Transition);
        ImmediateContext.ClearRenderTarget(rtv, clearColor, ResourceStateTransitionMode.Transition);
        ImmediateContext.ClearDepthStencil(dsv, 
            ClearDepthStencilFlags.ClearDepthFlag, 
            1.0f, 0,
            ResourceStateTransitionMode.Transition);
        
        ImmediateContext.SetPipelineState(_pipelineState ?? throw new NullReferenceException());

        var drawAttribs = new DrawAttribs()
        {
            NumVertices = 3
        };
        ImmediateContext.Draw(drawAttribs);
    }

    protected override void OnSetup()
    {
        _pipelineState = CreatePipelineState();
    }

    protected override void OnUpdate(double dt)
    {
        Render();

        // Present Render Image on Window
        SwapChain.Present();
    }

    protected override void OnExit()
    {
        _pipelineState?.Dispose();
    }
}