using System;
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
        private static Pipeline pipeline;
        private static ResourceSet projectionViewResourceSet;
        private static ResourceSet modelTextureResourceSet;

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

            while (window.Exists)
            {
                window.PumpEvents();
                Draw(graphicsDevice, window);
            }
        }

        private static void CreateResources(GraphicsDevice graphicsDevice, ResourceFactory factory)
        {
            commandList = factory.CreateCommandList();
            commandList.Begin();

            projectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            viewBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            modelBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            VertexPositionTexture[] cubeVertices = GetCubeVertices();
            vertexBuffer = factory.CreateBuffer(new BufferDescription(12 * 3 * VertexPositionTexture.SizeInBytes, BufferUsage.VertexBuffer));
            commandList.UpdateBuffer(vertexBuffer, 0, cubeVertices);

            ushort[] triangleIndices = Enumerable.Range(0, 12 * 3)
                .Select(i => (ushort)i)
                .ToArray();
            indexBuffer = factory.CreateBuffer(new BufferDescription(12 * 3 * sizeof(ushort), BufferUsage.IndexBuffer));
            commandList.UpdateBuffer(indexBuffer, 0, triangleIndices);

            ImageSharpTexture stoneImage = new ImageSharpTexture(Path.Combine(AppContext.BaseDirectory, "Textures", "uvtemplate.png"));
            Texture surfaceTexture = stoneImage.CreateDeviceTexture(graphicsDevice, factory);
            TextureView surfaceTextureView = factory.CreateTextureView(surfaceTexture);

            commandList.End();
            graphicsDevice.SubmitCommands(commandList);
            graphicsDevice.WaitForIdle();

            var shaderSet = new ShaderSetDescription(
                new[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                        new VertexElementDescription("TextureCoordinate", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                },
                new[]
                {
                    LoadShader(graphicsDevice, factory, ShaderStages.Vertex),
                    LoadShader(graphicsDevice, factory, ShaderStages.Fragment)
                });

            ResourceLayout projectionViewLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Projection", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("View", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

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
                viewBuffer));

            modelTextureResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                modelTextureLayout,
                modelBuffer,
                surfaceTextureView,
                graphicsDevice.Aniso4xSampler));
        }

        private static VertexPositionTexture[] GetCubeVertices()
        {
            var vertices = new VertexPositionTexture[]
            {
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f,-0.5f), new Vector2(0.000059f, 1.0f - 0.000004f)),
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f, 0.5f), new Vector2(0.000103f, 1.0f - 0.336048f)),
                new VertexPositionTexture(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(0.335973f, 1.0f - 0.335903f)),

                new VertexPositionTexture(new Vector3(0.5f, 0.5f,-0.5f), new Vector2(1.000023f, 1.0f - 0.000013f)),
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f,-0.5f), new Vector2(0.667979f, 1.0f - 0.335851f)),
                new VertexPositionTexture(new Vector3(-0.5f, 0.5f,-0.5f), new Vector2(0.999958f, 1.0f - 0.336064f)),

                new VertexPositionTexture(new Vector3(0.5f,-0.5f, 0.5f), new Vector2(0.667979f, 1.0f - 0.335851f)),
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f,-0.5f), new Vector2(0.336024f, 1.0f - 0.671877f)),
                new VertexPositionTexture(new Vector3(0.5f,-0.5f,-0.5f), new Vector2(0.667969f, 1.0f - 0.671889f)),

                new VertexPositionTexture(new Vector3(0.5f, 0.5f,-0.5f), new Vector2(1.000023f, 1.0f - 0.000013f)),
                new VertexPositionTexture(new Vector3(0.5f,-0.5f,-0.5f), new Vector2(0.668104f, 1.0f - 0.000013f)),
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f,-0.5f), new Vector2(0.667979f, 1.0f - 0.335851f)),

                new VertexPositionTexture(new Vector3(-0.5f,-0.5f,-0.5f), new Vector2(0.000059f, 1.0f - 0.000004f)),
                new VertexPositionTexture(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(0.335973f, 1.0f - 0.335903f)),
                new VertexPositionTexture(new Vector3(-0.5f, 0.5f,-0.5f), new Vector2(0.336098f, 1.0f - 0.000071f)),

                new VertexPositionTexture(new Vector3(0.5f,-0.5f, 0.5f), new Vector2(0.667979f, 1.0f - 0.335851f)),
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f, 0.5f), new Vector2(0.335973f, 1.0f - 0.335903f)),
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f,-0.5f), new Vector2(0.336024f, 1.0f - 0.671877f)),

                new VertexPositionTexture(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(1.000004f, 1.0f - 0.671847f)),
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f, 0.5f), new Vector2(0.999958f, 1.0f - 0.336064f)),
                new VertexPositionTexture(new Vector3(0.5f,-0.5f, 0.5f), new Vector2(0.667979f, 1.0f - 0.335851f)),

                new VertexPositionTexture(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(0.668104f, 1.0f - 0.000013f)),
                new VertexPositionTexture(new Vector3(0.5f,-0.5f,-0.5f), new Vector2(0.335973f, 1.0f - 0.335903f)),
                new VertexPositionTexture(new Vector3(0.5f, 0.5f,-0.5f), new Vector2(0.667979f, 1.0f - 0.335851f)),

                new VertexPositionTexture(new Vector3(0.5f,-0.5f,-0.5f), new Vector2(0.335973f, 1.0f - 0.335903f)),
                new VertexPositionTexture(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(0.668104f, 1.0f - 0.000013f)),
                new VertexPositionTexture(new Vector3(0.5f,-0.5f, 0.5f), new Vector2(0.336098f, 1.0f - 0.000071f)),

                new VertexPositionTexture(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(0.000103f, 1.0f - 0.336048f)),
                new VertexPositionTexture(new Vector3(0.5f, 0.5f,-0.5f), new Vector2(0.000004f, 1.0f - 0.671870f)),
                new VertexPositionTexture(new Vector3(-0.5f, 0.5f,-0.5f), new Vector2(00.336024f, 1.0f - 0.671877f)),

                new VertexPositionTexture(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(0.000103f, 1.0f - 0.336048f)),
                new VertexPositionTexture(new Vector3(-0.5f, 0.5f,-0.5f), new Vector2(0.336024f, 1.0f - 0.671877f)),
                new VertexPositionTexture(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(0.335973f, 1.0f - 0.335903f)),

                new VertexPositionTexture(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(0.667969f, 1.0f - 0.671889f)),
                new VertexPositionTexture(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(1.000004f, 1.0f - 0.671847f)),
                new VertexPositionTexture(new Vector3(0.5f,-0.5f, 0.5f), new Vector2(0.667979f, 1.0f - 0.335851f))
            };

            return vertices;
        }

        private static Shader LoadShader(GraphicsDevice graphicsDevice, ResourceFactory factory, ShaderStages stage)
        {
            string extension = null;
            switch (graphicsDevice.BackendType)
            {
                case GraphicsBackend.OpenGL:
                    extension = "glsl";
                    break;
                default: throw new System.InvalidOperationException();
            }

            string entryPoint = stage == ShaderStages.Vertex ? "VS" : "FS";
            string path = Path.Combine(System.AppContext.BaseDirectory, "Shaders", $"{stage.ToString()}.{extension}");
            byte[] shaderBytes = File.ReadAllBytes(path);

            return factory.CreateShader(new ShaderDescription(stage, shaderBytes, entryPoint));
        }

        private static void Draw(GraphicsDevice graphicsDevice, Sdl2Window window)
        {
            // Begin() must be called before commands can be issued.
            commandList.Begin();

            var projection = Matrix4x4.CreatePerspectiveFieldOfView(
                Radians(45.0f),
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
                indexCount: 12 * 3,
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

        public static float Radians(float degrees)
        {
            float radians = ((float)Math.PI / 180) * degrees;
            return (radians);
        }
    }
}
