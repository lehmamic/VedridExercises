using System.Numerics;

namespace GraphicsTutorial
{
    public class TangentBasis
    {
        public TangentBasis(Vector3 tangent, Vector3 bitangent)
        {
            Tangent = tangent;
            Bitangent = bitangent;
        }

        public Vector3 Tangent { get; }
        
        public Vector3 Bitangent { get; }
    }
}
