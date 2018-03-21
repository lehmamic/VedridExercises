using System.Numerics;

namespace GraphicsTutorial
{
    public struct FontVertex
    {
        public const byte SizeInBytes = 16;

        public Vector2 Position;

        public Vector2 TextureCoordinate;

        public FontVertex(Vector2 position, Vector2 textureCoordinate)
        {
            Position = position;
            TextureCoordinate = textureCoordinate;
        }
    }
}
