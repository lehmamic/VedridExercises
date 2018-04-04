using System;
using System.Collections.Generic;
using System.Numerics;
using static Veldrid.Utilities.ObjFile;

namespace GraphicsTutorial
{
    public static class ObjParserExtensions
    {
        public static ConstructedMesh GetFirstMeshWithTangentInfo(this Veldrid.Utilities.ObjFile file)
        {
            if(file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return file.GetMeshWithTangentInfo(file.MeshGroups[0]);
        }

        public static ConstructedMesh GetMeshWithTangentInfo(this Veldrid.Utilities.ObjFile file, MeshGroup group)
        {
            if(file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            Dictionary<FaceVertex, ushort> vertexMap = new Dictionary<FaceVertex, ushort>();
            ushort[] indices = new ushort[group.Faces.Length * 3];
            List<VertexPositionNormalTextureTangent> vertices = new List<VertexPositionNormalTextureTangent>();

            for (int i = 0; i < group.Faces.Length; i++)
            {
                Face face = group.Faces[i];

                var tangentBasis = file.ComputeTangentBasis(face);

                ushort index0 = file.GetOrCreate(vertexMap, vertices, face.Vertex0, face.Vertex1, face.Vertex2, tangentBasis);
                ushort index1 = file.GetOrCreate(vertexMap, vertices, face.Vertex1, face.Vertex2, face.Vertex0, tangentBasis);
                ushort index2 = file.GetOrCreate(vertexMap, vertices, face.Vertex2, face.Vertex0, face.Vertex1, tangentBasis);

                // Reverse winding order here.
                indices[(i * 3)] = index0;
                indices[(i * 3) + 2] = index1;
                indices[(i * 3) + 1] = index2;
            }

            return new ConstructedMesh(vertices.ToArray(), indices, group.Material);
        }

        private static ushort GetOrCreate(
            this Veldrid.Utilities.ObjFile file,
            Dictionary<FaceVertex, ushort> vertexMap,
            List<VertexPositionNormalTextureTangent> vertices,
            FaceVertex key,
            FaceVertex adjacent1,
            FaceVertex adjacent2,
            TangentBasis tangentBasis)
        {
            ushort index;
            if (!vertexMap.TryGetValue(key, out index))
            {
                VertexPositionNormalTextureTangent vertex = file.ConstructVertex(key, adjacent1, adjacent2, tangentBasis);
                vertices.Add(vertex);
                index = checked((ushort)(vertices.Count - 1));
                vertexMap.Add(key, index);
            }
            else
            {
                vertices[index] = AverageTangentBasis(vertices[index], tangentBasis);
            }

            return index;
        }

        private static VertexPositionNormalTextureTangent ConstructVertex(
            this Veldrid.Utilities.ObjFile file,
            FaceVertex key,
            FaceVertex adjacent1,
            FaceVertex adjacent2,
            TangentBasis tangentBasis)
        {
            Vector3 position = file.Positions[key.PositionIndex - 1];
            Vector3 normal;
            if (key.NormalIndex == -1)
            {
                normal = file.ComputeNormal(key, adjacent1, adjacent2);
            }
            else
            {
                normal = file.Normals[key.NormalIndex - 1];
            }

            Vector2 textureCoordinate = key.TexCoordIndex == -1 ? Vector2.Zero : file.TexCoords[key.TexCoordIndex - 1];

            return new VertexPositionNormalTextureTangent(position, normal, textureCoordinate, tangentBasis.Tangent, tangentBasis.Bitangent);
        }

        private static Vector3 ComputeNormal(this Veldrid.Utilities.ObjFile file, FaceVertex v1, FaceVertex v2, FaceVertex v3)
        {
            Vector3 pos1 = file.Positions[v1.PositionIndex - 1];
            Vector3 pos2 = file.Positions[v2.PositionIndex - 1];
            Vector3 pos3 = file.Positions[v3.PositionIndex - 1];

            return Vector3.Normalize(Vector3.Cross(pos1 - pos2, pos1 - pos3));
        }

        private static VertexPositionNormalTextureTangent AverageTangentBasis(VertexPositionNormalTextureTangent vertex, TangentBasis tangentBasis)
        {
          return new VertexPositionNormalTextureTangent(
              vertex.Position,
              vertex.Normal,
              vertex.TextureCoordinates,
              vertex.Tangent + tangentBasis.Tangent,
              vertex.Bitangent + tangentBasis.Bitangent);
        }

        private static TangentBasis ComputeTangentBasis(this Veldrid.Utilities.ObjFile file, Face face)
        {
                // Shortcuts for vertices
                var v0 = file.Positions[face.Vertex0.PositionIndex - 1];
                var v1 = file.Positions[face.Vertex1.PositionIndex - 1];
                var v2 = file.Positions[face.Vertex2.PositionIndex - 1];

                // Shortcuts for UVs
                var uv0 = face.Vertex0.TexCoordIndex == -1 ? Vector2.Zero : file.TexCoords[face.Vertex0.TexCoordIndex - 1];
                var uv1 = face.Vertex1.TexCoordIndex == -1 ? Vector2.Zero : file.TexCoords[face.Vertex1.TexCoordIndex - 1];
                var uv2 = face.Vertex2.TexCoordIndex == -1 ? Vector2.Zero : file.TexCoords[face.Vertex2.TexCoordIndex - 1];

                var deltaPos1 = v1 - v0;
                var deltaPos2 = v2 - v0;

                // UV delta
                var deltaUV1 = uv1 - uv0;
                var deltaUV2 = uv2 - uv0;

                float r = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X);
                var tangent = (deltaPos1 * deltaUV2.Y - deltaPos2 * deltaUV1.Y) * r;
                var bitangent = (deltaPos2 * deltaUV1.X - deltaPos1 * deltaUV2.X) * r;

                return new TangentBasis(tangent, bitangent);
        }
    }
}
