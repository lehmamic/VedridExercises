﻿using System;
using System.IO;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace GraphicsTutorial
{
    class Program
    {
        private static CommandList commandList;
        private static DeviceBuffer vertexBuffer;
        private static DeviceBuffer indexBuffer;
        private static Shader vertexShader;
        private static Shader fragmentShader;
        private static Pipeline pipeline;

        static void Main(string[] args)
        {
            var windowCI = new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = "Veldrid Tutorial"
            };

            Sdl2Window window = VeldridStartup.CreateWindow(ref windowCI);
            GraphicsDevice graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, GraphicsBackend.OpenGL);

            CreateResources(graphicsDevice);

            while (window.Exists)
            {
                window.PumpEvents();
                Draw(graphicsDevice);
            }

            DisposeResources(graphicsDevice);
        }

        private static void CreateResources(GraphicsDevice graphicsDevice)
        {
            ResourceFactory factory = graphicsDevice.ResourceFactory;

            var quadVertices = new[]
            {
                new VertexPositionColor(new Vector2(-.75f, .75f), RgbaFloat.Red),
                new VertexPositionColor(new Vector2(.75f, .75f), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(-.75f, -.75f), RgbaFloat.Blue),
                new VertexPositionColor(new Vector2(.75f, -.75f), RgbaFloat.Yellow)
            };

            ushort[] quadIndices = { 0, 1, 2, 3 };

            vertexBuffer = factory.CreateBuffer(new BufferDescription(4 * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));
            indexBuffer = factory.CreateBuffer(new BufferDescription(4 * sizeof(ushort), BufferUsage.IndexBuffer));

            graphicsDevice.UpdateBuffer(vertexBuffer, 0, quadVertices);
            graphicsDevice.UpdateBuffer(indexBuffer, 0, quadIndices);

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));

            vertexShader = LoadShader(graphicsDevice, ShaderStages.Vertex);
            fragmentShader = LoadShader(graphicsDevice, ShaderStages.Fragment);

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;

            pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual);

            pipelineDescription.RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);

            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();

            pipelineDescription.ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
                shaders: new Shader[] { vertexShader, fragmentShader });

            pipelineDescription.Outputs = graphicsDevice.SwapchainFramebuffer.OutputDescription;
            pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

            commandList = factory.CreateCommandList();
        }

        private static Shader LoadShader(GraphicsDevice graphicsDevice, ShaderStages stage)
        {
            string extension = null;
            switch (graphicsDevice.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                    extension = "hlsl.bytes";
                    break;
                case GraphicsBackend.Vulkan:
                    extension = "spv";
                    break;
                case GraphicsBackend.OpenGL:
                    extension = "glsl";
                    break;
                default: throw new System.InvalidOperationException();
            }

            string entryPoint = stage == ShaderStages.Vertex ? "VS" : "FS";
            string path = Path.Combine(System.AppContext.BaseDirectory, "Shaders", $"{stage.ToString()}.{extension}");
            byte[] shaderBytes = File.ReadAllBytes(path);

            return graphicsDevice.ResourceFactory.CreateShader(new ShaderDescription(stage, shaderBytes, entryPoint));
        }
        private static void Draw(GraphicsDevice graphicsDevice)
        {
            // Begin() must be called before commands can be issued.
            commandList.Begin();

            // We want to render directly to the output window.
            commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
            commandList.SetFullViewports();
            commandList.ClearColorTarget(0, RgbaFloat.Black);

            // Set all relevant state to draw our quad.
            commandList.SetVertexBuffer(0, vertexBuffer);
            commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            commandList.SetPipeline(pipeline);
            // Issue a Draw command for a single instance with 4 indices.
            commandList.DrawIndexed(
                indexCount: 4,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);

            // End() must be called before commands can be submitted for execution.
            commandList.End();
            graphicsDevice.SubmitCommands(commandList);

            // Once commands have been submitted, the rendered image can be presented to the application window.
            graphicsDevice.SwapBuffers();
        }

        private static void DisposeResources(GraphicsDevice graphicsDevice)
        {
            pipeline.Dispose();
            vertexShader.Dispose();
            fragmentShader.Dispose();
            commandList.Dispose();
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            graphicsDevice.Dispose();
        }
    }
}
