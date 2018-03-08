using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Veldrid;
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

            var cubeVertices = GetCubeVertices();
            vertexBuffer = factory.CreateBuffer(new BufferDescription(12 * 3 * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));
            commandList.UpdateBuffer(vertexBuffer, 0, cubeVertices);

            ushort[] triangleIndices = Enumerable.Range(0, 12 * 3)
                .Select(i => (ushort)i)
                .ToArray();
            indexBuffer = factory.CreateBuffer(new BufferDescription(12 * 3 * sizeof(ushort), BufferUsage.IndexBuffer));
            commandList.UpdateBuffer(indexBuffer, 0, triangleIndices);

            commandList.End();
            graphicsDevice.SubmitCommands(commandList);
            graphicsDevice.WaitForIdle();

            var shaderSet = new ShaderSetDescription(
                new[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                        new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4))
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
                    new ResourceLayoutElementDescription("Model", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

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
                modelBuffer));
        }

        private static VertexPositionColor[] GetCubeVertices()
        {
            var vertices = new VertexPositionColor[]
            {
                new VertexPositionColor(new Vector3(-0.5f,-0.5f,-0.5f), new RgbaFloat(0.583f, 0.771f, 0.014f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f,-0.5f, 0.5f), new RgbaFloat(0.609f, 0.115f, 0.436f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0.5f), new RgbaFloat(0.327f, 0.483f, 0.844f, 1f)),

                new VertexPositionColor(new Vector3(0.5f, 0.5f,-0.5f), new RgbaFloat(0.822f, 0.569f, 0.201f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f,-0.5f,-0.5f), new RgbaFloat(0.435f, 0.602f, 0.223f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f, 0.5f,-0.5f), new RgbaFloat(0.310f, 0.747f, 0.185f, 1f)),

                new VertexPositionColor(new Vector3(0.5f,-0.5f, 0.5f), new RgbaFloat(0.597f, 0.770f, 0.761f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f,-0.5f,-0.5f), new RgbaFloat(0.559f, 0.436f, 0.730f, 1f)),
                new VertexPositionColor(new Vector3(0.5f,-0.5f,-0.5f), new RgbaFloat(0.359f, 0.583f, 0.152f, 1f)),

                new VertexPositionColor(new Vector3(0.5f, 0.5f,-0.5f), new RgbaFloat(0.483f, 0.596f, 0.789f, 1f)),
                new VertexPositionColor(new Vector3(0.5f,-0.5f,-0.5f), new RgbaFloat(0.559f, 0.861f, 0.639f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f,-0.5f,-0.5f), new RgbaFloat(0.195f, 0.548f, 0.859f, 1f)),

                new VertexPositionColor(new Vector3(-0.5f,-0.5f,-0.5f), new RgbaFloat(0.014f, 0.184f, 0.576f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0.5f), new RgbaFloat(0.771f, 0.328f, 0.970f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f, 0.5f,-0.5f), new RgbaFloat(0.406f, 0.615f, 0.116f, 1f)),

                new VertexPositionColor(new Vector3(0.5f,-0.5f, 0.5f), new RgbaFloat(0.676f, 0.977f, 0.133f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f,-0.5f, 0.5f), new RgbaFloat(0.971f, 0.572f, 0.833f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f,-0.5f,-0.5f), new RgbaFloat(0.140f, 0.616f, 0.489f, 1f)),

                new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0.5f), new RgbaFloat(0.997f, 0.513f, 0.064f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f,-0.5f, 0.5f), new RgbaFloat(0.945f, 0.719f, 0.592f, 1f)),
                new VertexPositionColor(new Vector3(0.5f,-0.5f, 0.5f), new RgbaFloat(0.543f, 0.021f, 0.978f, 1f)),

                new VertexPositionColor(new Vector3(0.5f, 0.5f, 0.5f), new RgbaFloat(0.279f, 0.317f, 0.505f, 1f)),
                new VertexPositionColor(new Vector3(0.5f,-0.5f,-0.5f), new RgbaFloat(0.167f, 0.620f, 0.077f, 1f)),
                new VertexPositionColor(new Vector3(0.5f, 0.5f,-0.5f), new RgbaFloat(0.347f, 0.857f, 0.137f, 1f)),

                new VertexPositionColor(new Vector3(0.5f,-0.5f,-0.5f), new RgbaFloat(0.055f, 0.953f, 0.042f, 1f)),
                new VertexPositionColor(new Vector3(0.5f, 0.5f, 0.5f), new RgbaFloat(0.714f, 0.505f, 0.345f, 1f)),
                new VertexPositionColor(new Vector3(0.5f,-0.5f, 0.5f), new RgbaFloat(0.783f, 0.290f, 0.734f, 1f)),

                new VertexPositionColor(new Vector3(0.5f, 0.5f, 0.5f), new RgbaFloat(0.722f, 0.645f, 0.174f, 1f)),
                new VertexPositionColor(new Vector3(0.5f, 0.5f,-0.5f), new RgbaFloat(0.302f, 0.455f, 0.848f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f, 0.5f,-0.5f), new RgbaFloat(0.225f, 0.587f, 0.040f, 1f)),

                new VertexPositionColor(new Vector3(0.5f, 0.5f, 0.5f), new RgbaFloat(0.517f, 0.713f, 0.338f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f, 0.5f,-0.5f), new RgbaFloat(0.053f, 0.959f, 0.120f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0.5f), new RgbaFloat(0.393f, 0.621f, 0.362f, 1f)),

                new VertexPositionColor(new Vector3(0.5f, 0.5f, 0.5f), new RgbaFloat(0.673f, 0.211f, 0.457f, 1f)),
                new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0.5f), new RgbaFloat(0.820f, 0.883f, 0.371f, 1f)),
                new VertexPositionColor(new Vector3(0.5f,-0.5f, 0.5f), new RgbaFloat(0.982f, 0.099f, 0.879f, 1f))
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
            commandList.SetGraphicsResourceSet(0, modelTextureResourceSet);

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
