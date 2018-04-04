#version 330 core

// Interpolated values from the vertex shaders
in vec2 UV;
in vec3 Position_Worldspace;
in vec3 Normal_Cameraspace;
in vec3 EyeDirection_Cameraspace;
in vec3 LightDirection_Cameraspace;
in vec3 LightDirection_Tangentspace;
in vec3 EyeDirection_Tangentspace;

layout(std140) uniform LightPosition
{
    vec3 LightPosition_Worldspace;
};

uniform sampler2D DiffuseTexture;
uniform sampler2D SpecularTexture;
uniform sampler2D NormalTexture;

out vec3 Color;

void main()
{
    // Light emission properties
    // You probably want to put them as uniforms
    vec3 LightColor = vec3(1, 1, 1);
    float LightPower = 50.0f;

    // Material properties
    vec3 MaterialDiffuseColor = texture(DiffuseTexture, UV).rgb;
    vec3 MaterialAmbientColor = vec3(0.1, 0.1, 0.1) * MaterialDiffuseColor;
    vec3 MaterialSpecularColor = vec3(0.3, 0.3, 0.3);

    // Local normal, in tangent space. V tex coordinate is inverted because normal map is in TGA (not in DDS) for better quality
    vec3 TextureNormal_Tangentspace = normalize(texture(NormalTexture, vec2(UV.x, -UV.y)).rgb * 2.0 - 1.0);

    // Distance to the light
    float distance = length(LightPosition_Worldspace - Position_Worldspace);

    // Normal of the computed fragment, in camera space (instead of normalize(Normal_Cameraspace))
    vec3 n = TextureNormal_Tangentspace;
    // Direction of the light (from the fragment to the light)
    vec3 l = normalize(LightDirection_Cameraspace);
    // Cosine of the angle between the normal and the light direction, 
    // clamped above 0
    //  - light is at the vertical of the triangle -> 1
    //  - light is perpendicular to the triangle -> 0
    //  - light is behind the triangle -> 0
    float cosTheta = clamp(dot(n, l), 0, 1);

    // Eye vector (towards the camera)
    vec3 E = normalize(EyeDirection_Cameraspace);
    // Direction in which the triangle reflects the light
    vec3 R = reflect(-l, n);
    // Cosine of the angle between the Eye vector and the Reflect vector,
    // clamped to 0
    //  - Looking into the reflection -> 1
    //  - Looking elsewhere -> < 1
    float cosAlpha = clamp(dot(E, R), 0, 1);

    Color =
      // Ambient : simulates indirect lighting
      MaterialAmbientColor +
      // Diffuse : "color" of the object
      MaterialDiffuseColor * LightColor * LightPower * cosTheta / (distance * distance) +
      // Specular : reflective highlight, like a mirror
      MaterialSpecularColor * LightColor * LightPower * pow(cosAlpha, 5) / (distance * distance);
}
