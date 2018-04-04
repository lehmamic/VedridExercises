#version 330 core

in vec3 Position;
in vec3 Normal;
in vec2 TextureCoordinate;
in vec3 Tangent;
in vec3 Bitangent;

layout(std140) uniform Projection
{
    mat4 ProjectionMatrix;
};

layout(std140) uniform View
{
    mat4 ViewMatrix;
};

layout(std140) uniform LightPosition
{
    vec3 LightPosition_Worldspace;
};

layout(std140) uniform Model
{
    mat4 ModelMatrix;
};

// Output data ; will be interpolated for each fragment.
out vec2 UV;
out vec3 Position_Worldspace;
out vec3 Normal_Cameraspace;
out vec3 EyeDirection_Cameraspace;
out vec3 LightDirection_Cameraspace;

out vec3 LightDirection_Tangentspace;
out vec3 EyeDirection_Tangentspace;

void main()
{
    mat4 MVP = ProjectionMatrix * ViewMatrix * ModelMatrix;
    mat3 MV3x3 = mat3(ViewMatrix * ModelMatrix);

    // Output position of the vertex, in clip space : MVP * position
    gl_Position = MVP * vec4(Position, 1);

    // Position of the vertex, in worldspace : M * position
    Position_Worldspace = (ModelMatrix * vec4(Position, 1)).xyz;

    // Vector that goes from the vertex to the camera, in camera space.
    // In camera space, the camera is at the origin (0,0,0).
    vec3 VertexPosition_Cameraspace = ( ViewMatrix * ModelMatrix * vec4(Position, 1)).xyz;
    EyeDirection_Cameraspace = vec3(0, 0, 0) - VertexPosition_Cameraspace;

    // Vector that goes from the vertex to the light, in camera space. M is ommited because it's identity.
    vec3 LightPosition_Cameraspace = ( ViewMatrix * vec4(LightPosition_Worldspace, 1)).xyz;
    LightDirection_Cameraspace = LightPosition_Cameraspace + EyeDirection_Cameraspace;

    // Normal of the the vertex, in camera space
    Normal_Cameraspace = ( ViewMatrix * ModelMatrix * vec4(Normal, 0)).xyz; // Only correct if ModelMatrix does not scale the model ! Use its inverse transpose if not.

    UV = TextureCoordinate;

    // model to camera = ModelView
    vec3 vertexTangent_Cameraspace = MV3x3 * Tangent;
    vec3 vertexBitangent_Cameraspace = MV3x3 * Bitangent;
    vec3 vertexNormal_Cameraspace = MV3x3 * Normal;

    mat3 TBN = transpose(mat3(
      vertexTangent_Cameraspace,
      vertexBitangent_Cameraspace,
      vertexNormal_Cameraspace
    )); // You can use dot products instead of building this matrix and transposing it. See References for details.

    LightDirection_Tangentspace = TBN * LightDirection_Cameraspace;
    EyeDirection_Tangentspace =  TBN * EyeDirection_Cameraspace;
}
