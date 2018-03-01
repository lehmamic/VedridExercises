using System.Numerics;
using Veldrid;

namespace GraphicsTutorial
{
    public struct VertexPositionColor
    {
        public const uint SizeInBytes = 24;

        public Vector2 Position; // This is the position, in normalized device coordinates.
        public RgbaFloat Color; // This is the color of the vertex.

        public VertexPositionColor(Vector2 position, RgbaFloat color)
        {
            Position = position;
            Color = color;
        }
    }
}
