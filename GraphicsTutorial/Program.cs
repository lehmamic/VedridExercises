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
        private static DeviceBuffer mvpBuffer;
        private static Pipeline pipeline;
        private static ResourceSet mvpResourceSet;

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

            var cubeVertices = GetCubeVertices();

            ushort[] triangleIndices = Enumerable.Range(0, 12 * 3)
                .Select(i => (ushort)i)
                .ToArray();

            vertexBuffer = factory.CreateBuffer(new BufferDescription(12 * 3 * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));
            commandList.UpdateBuffer(vertexBuffer, 0, cubeVertices);

            indexBuffer = factory.CreateBuffer(new BufferDescription(12 * 3 * sizeof(ushort), BufferUsage.IndexBuffer));
            commandList.UpdateBuffer(indexBuffer, 0, triangleIndices);

            mvpBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

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

            ResourceLayout mvpResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("MVP", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

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
                new[] { mvpResourceLayout },
                graphicsDevice.SwapchainFramebuffer.OutputDescription));

            mvpResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                mvpResourceLayout,
                mvpBuffer));
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

            var view = Matrix4x4.CreateLookAt(
              new Vector3(4, 3, 3),
              new Vector3(0, 0, 0),
              new Vector3(0, 1, 0));

            var model = Matrix4x4.Identity;
            Matrix4x4 mvp = model * view * projection;

            // identity matrix, model is at 0,0,0 location
            commandList.UpdateBuffer(mvpBuffer, 0, mvp);

            // We want to render directly to the output window.
            commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
            commandList.SetFullViewports();
            commandList.ClearColorTarget(0, RgbaFloat.Black);
            commandList.ClearDepthStencil(1f);

            // Set all relevant state to draw our triangle.
            commandList.SetPipeline(pipeline);
            commandList.SetVertexBuffer(0, vertexBuffer);
            commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            commandList.SetGraphicsResourceSet(0, mvpResourceSet);

            // Issue a Draw command for a single instance with 3 indices.
            commandList.DrawIndexed(
                indexCount: 3,
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
