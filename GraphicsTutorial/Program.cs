using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;

namespace GraphicsTutorial
{
    class Program
    {
        private static CommandList commandList;
        private static DeviceBuffer vertexBuffer;
        private static DeviceBuffer indexBuffer;
        private static DeviceBuffer projectionBuffer;
        private static DeviceBuffer viewBuffer;
        private static DeviceBuffer modelBuffer;
        private static DeviceBuffer lightPositionBuffer;
        private static Pipeline pipeline;
        private static ResourceSet projectionViewResourceSet;
        private static ResourceSet modelTextureResourceSet;
        private static ConstructedMeshInfo mesh;

        private static Pipeline fontPipeline;
        private static ResourceSet fontTextureResourceSet;

        static void Main(string[] args)
        {
            var windowCI = new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 1024,
                WindowHeight = 768,
                WindowTitle = "Veldrid Tutorial"
            };

            Sdl2Window window = VeldridStartup.CreateWindow(ref windowCI);

            GraphicsDeviceOptions options = new GraphicsDeviceOptions
            {
                Debug = true,
                SwapchainDepthFormat = PixelFormat.R16_UNorm
            };

            GraphicsDevice graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, options, GraphicsBackend.OpenGL);

            var factory = new DisposeCollectorResourceFactory(graphicsDevice.ResourceFactory);
            CreateResources(graphicsDevice, factory);

            var stopwatch = Stopwatch.StartNew();
            long previousFrameTicks = 0;

            while (window.Exists)
            {
                long currentFrameTicks = stopwatch.ElapsedTicks;
                double deltaMilliseconds = (currentFrameTicks - previousFrameTicks) * (1000.0 / Stopwatch.Frequency);
                Console.WriteLine($"{deltaMilliseconds:0.###}ms / frame");

                previousFrameTicks = currentFrameTicks;

                window.PumpEvents();
                Draw(graphicsDevice, window, factory, (double)currentFrameTicks / Stopwatch.Frequency);
            }

            factory.DisposeCollector.DisposeAll();
        }

        private static void CreateResources(GraphicsDevice graphicsDevice, ResourceFactory factory)
        {
            commandList = factory.CreateCommandList();
            commandList.Begin();

            projectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            viewBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            modelBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            lightPositionBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

            var objFile = ReadModel("suzanne.obj");
            mesh = objFile.GetFirstMesh();

            vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)mesh.Vertices.Length * VertexPositionNormalTexture.SizeInBytes, BufferUsage.VertexBuffer));
            commandList.UpdateBuffer(vertexBuffer, 0, mesh.Vertices);

            indexBuffer = factory.CreateBuffer(new BufferDescription((uint)mesh.Indices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            commandList.UpdateBuffer(indexBuffer, 0, mesh.Indices);

            var image = new ImageSharpTexture(Path.Combine(AppContext.BaseDirectory, "Textures", "uvmap.png"));
            Texture surfaceTexture = image.CreateDeviceTexture(graphicsDevice, factory);
            TextureView surfaceTextureView = factory.CreateTextureView(surfaceTexture);

            commandList.End();
            graphicsDevice.SubmitCommands(commandList);
            graphicsDevice.WaitForIdle();

            var shaderSet = new ShaderSetDescription(
                new[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                        new VertexElementDescription("Normal", VertexElementSemantic.Normal, VertexElementFormat.Float3),
                        new VertexElementDescription("TextureCoordinate", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                },
                new[]
                {
                    ShaderHelper.LoadShader(graphicsDevice, factory, "Standard", ShaderStages.Vertex),
                    ShaderHelper.LoadShader(graphicsDevice, factory, "Standard", ShaderStages.Fragment)
                });

            ResourceLayout projectionViewLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("LightPosition", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));

            ResourceLayout modelTextureLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Model", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            var rasterizeState =  new RasterizerStateDescription(
                cullMode: FaceCullMode.None,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.CounterClockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);

            pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                rasterizeState,
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { projectionViewLayout, modelTextureLayout },
                graphicsDevice.SwapchainFramebuffer.OutputDescription));

            projectionViewResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                projectionViewLayout,
                projectionBuffer,
                viewBuffer,
                lightPositionBuffer));

            modelTextureResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                modelTextureLayout,
                modelBuffer,
                surfaceTextureView,
                graphicsDevice.Aniso4xSampler));

            InitText2D(graphicsDevice, factory, "holstein.png");
        }

        private static void InitText2D(GraphicsDevice graphicsDevice, ResourceFactory factory, string texturePath)
        {
            var fontImage = new ImageSharpTexture(Path.Combine(AppContext.BaseDirectory, "Textures", texturePath));
            Texture fontTexture = fontImage.CreateDeviceTexture(graphicsDevice, factory);
            TextureView fontTextureView = factory.CreateTextureView(fontTexture);

            var shaderSet = new ShaderSetDescription(
                new[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                        new VertexElementDescription("TextureCoordinate", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                },
                new[]
                {
                    ShaderHelper.LoadShader(graphicsDevice, factory, "2dText", ShaderStages.Vertex),
                    ShaderHelper.LoadShader(graphicsDevice, factory, "2dText", ShaderStages.Fragment)
                });

            ResourceLayout fontTextureLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            var rasterizeState =  new RasterizerStateDescription(
                cullMode: FaceCullMode.None,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.CounterClockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);

            fontPipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.Disabled,
                rasterizeState,
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { fontTextureLayout },
                graphicsDevice.SwapchainFramebuffer.OutputDescription));

            fontTextureResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                fontTextureLayout,
                fontTextureView,
                graphicsDevice.Aniso4xSampler));
        }

        private static ObjFile ReadModel(string filename)
        {
            var parser = new ObjParser();
            using(var stream = File.OpenRead(Path.Combine(System.AppContext.BaseDirectory, "Models", filename)))
            {
                return parser.Parse(stream);
            }
        }

        private static void Draw(GraphicsDevice graphicsDevice, Sdl2Window window, ResourceFactory factory, double time)
        {
            // Begin() must be called before commands can be issued.
            commandList.Begin();

            var projection = Matrix4x4.CreatePerspectiveFieldOfView(
                MathUtils.Radians(45.0f),
                (float)window.Width / window.Height,
                0.1f,
                100f);
            commandList.UpdateBuffer(projectionBuffer, 0, projection);

            var view = Matrix4x4.CreateLookAt(
              new Vector3(4, 3, 3),
              new Vector3(0, 0, 0),
              new Vector3(0, 1, 0));
            commandList.UpdateBuffer(viewBuffer, 0, view);

            // identity matrix, model is at 0,0,0 location
            var model = Matrix4x4.Identity;
            commandList.UpdateBuffer(modelBuffer, 0, model);

            var lightPosition = new Vector3(4, 4, 4);
            commandList.UpdateBuffer(lightPositionBuffer, 0, lightPosition);

            // We want to render directly to the output window.
            commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
            commandList.SetFullViewports();
            commandList.ClearColorTarget(0, RgbaFloat.Black);
            commandList.ClearDepthStencil(1f);

            // Set all relevant state to draw our triangle.
            commandList.SetPipeline(pipeline);
            commandList.SetVertexBuffer(0, vertexBuffer);
            commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            commandList.SetGraphicsResourceSet(0, projectionViewResourceSet);
            commandList.SetGraphicsResourceSet(1, modelTextureResourceSet);

            // Issue a Draw command for a single instance with 12 * 3 (6 faced with 2 triangles per face) indices.
            commandList.DrawIndexed(
                indexCount: (uint)mesh.Indices.Length,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);

            PrintText2D(graphicsDevice, factory, $"{time:0.00} sec", 10, 500, 60);

            // End() must be called before commands can be submitted for execution.
            commandList.End();
            graphicsDevice.SubmitCommands(commandList);

            // Once commands have been submitted, the rendered image can be presented to the application window.
            graphicsDevice.SwapBuffers();
        }

        private static void PrintText2D(GraphicsDevice graphicsDevice, ResourceFactory factory, string text, int x, int y, int size)
        {
            var vertices = new List<FontVertex>();
            for (int i = 0; i < text.Length; i++ )
            {
                var vertex_up_left    = new Vector2(x + i * size       , y + size);
                var vertex_up_right   = new Vector2(x + i * size + size, y + size);
                var vertex_down_right = new Vector2(x + i * size + size, y       );
                var vertex_down_left  = new Vector2(x + i * size       , y       );

                char character = text[i];
                float uv_x = (character % 16) / 16.0f;
                float uv_y = (character / 16) / 16.0f;

                var uv_up_left    = new Vector2(uv_x               , uv_y);
                var uv_up_right   = new Vector2(uv_x + 1.0f / 16.0f, uv_y);
                var uv_down_right = new Vector2(uv_x + 1.0f / 16.0f, (uv_y + 1.0f / 16.0f));
                var uv_down_left  = new Vector2(uv_x               , (uv_y + 1.0f / 16.0f));

                vertices.Add(new FontVertex(vertex_up_left, uv_up_left));
                vertices.Add(new FontVertex(vertex_down_left, uv_down_left));
                vertices.Add(new FontVertex(vertex_up_right, uv_up_right));

                vertices.Add(new FontVertex(vertex_down_right, uv_down_right));
                vertices.Add(new FontVertex(vertex_up_right, uv_up_right));
                vertices.Add(new FontVertex(vertex_down_left, uv_down_left));
              }

            var indices = Enumerable.Range(0, vertices.Count).Select(i => (ushort)i).ToArray();

            var fontVertexBuffer = factory.CreateBuffer(new BufferDescription((uint)vertices.Count * FontVertex.SizeInBytes, BufferUsage.VertexBuffer));
            commandList.UpdateBuffer(fontVertexBuffer, 0, vertices.ToArray());

            var fontIndexBuffer = factory.CreateBuffer(new BufferDescription((uint)indices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            commandList.UpdateBuffer(fontIndexBuffer, 0, indices);

            // Set all relevant state to draw our triangle.
            commandList.SetPipeline(fontPipeline);
            commandList.SetVertexBuffer(0, fontVertexBuffer);
            commandList.SetIndexBuffer(fontIndexBuffer, IndexFormat.UInt16);
            commandList.SetGraphicsResourceSet(0, fontTextureResourceSet);

            // Issue a Draw command for a single instance with 12 * 3 (6 faced with 2 triangles per face) indices.
            commandList.DrawIndexed(
                indexCount: (uint)indices.Length,
                instanceCount: 1,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);
        }
    }
}
