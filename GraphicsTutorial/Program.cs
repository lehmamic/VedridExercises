using System;
using System.IO;
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
                Debug = true
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

            var triangleVertices = new[]
            {
                new VertexPositionColor(new Vector3(-.75f, -.75f, 0f), RgbaFloat.Red),
                new VertexPositionColor(new Vector3(.75f, -.75f, 0f), RgbaFloat.Green),
                new VertexPositionColor(new Vector3(0f, .75f, 0f), RgbaFloat.Blue),
            };

            ushort[] triangleIndices = { 0, 1, 2 };

            vertexBuffer = factory.CreateBuffer(new BufferDescription(3 * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));
            commandList.UpdateBuffer(vertexBuffer, 0, triangleVertices);

            indexBuffer = factory.CreateBuffer(new BufferDescription(3 * sizeof(ushort), BufferUsage.IndexBuffer));
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
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.CounterClockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);

            pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                rasterizeState,
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { mvpResourceLayout },
                graphicsDevice.SwapchainFramebuffer.OutputDescription));

            mvpResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                mvpResourceLayout,
                mvpBuffer));
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
              new Vector3(4,3,3),
              new Vector3(0,0,0),
              new Vector3(0,1,0));

            var model = Matrix4x4.Identity;
            Matrix4x4 mvp = model * view * projection;

            // identity matrix, model is at 0,0,0 location
            commandList.UpdateBuffer(mvpBuffer, 0, mvp);

            // We want to render directly to the output window.
            commandList.SetFramebuffer(graphicsDevice.SwapchainFramebuffer);
            commandList.SetFullViewports();
            commandList.ClearColorTarget(0, RgbaFloat.Black);

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
