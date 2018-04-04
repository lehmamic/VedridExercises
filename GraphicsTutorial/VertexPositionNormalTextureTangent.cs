using System.Numerics;
using Veldrid;

namespace GraphicsTutorial
{
    public struct VertexPositionNormalTextureTangent
    {
        public const byte SizeInBytes = 56;

        public readonly Vector3 Position;
        public readonly Vector3 Normal;
        public readonly Vector2 TextureCoordinates;
        public readonly Vector3 Tangent;
        public readonly Vector3 Bitangent;

        public VertexPositionNormalTextureTangent(Vector3 position, Vector3 normal, Vector2 texCoords, Vector3 tangent, Vector3 bitangent)
        {
            Position = position;
            Normal = normal;
            TextureCoordinates = texCoords;
            Tangent = tangent;
            Bitangent = bitangent;
        }
    }
}
