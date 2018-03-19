#version 330 core

// Interpolated values from the vertex shaders
in vec2 UV;
in vec3 Position_Worldspace;
in vec3 Normal_Cameraspace;
in vec3 EyeDirection_Cameraspace;
in vec3 LightDirection_Cameraspace;

layout(std140) uniform LightPosition
{
    vec3 LightPosition_Worldspace;
};

uniform sampler2D SurfaceTexture;

out vec3 Color;

void main()
{
    // Light emission properties
    // You probably want to put them as uniforms
    vec3 LightColor = vec3(1, 1, 1);
    float LightPower = 50.0f;

    // Material properties
    vec3 MaterialDiffuseColor = texture(SurfaceTexture, UV).rgb;

    // Distance to the light
    float distance = length(LightPosition_Worldspace - Position_Worldspace);

    // Normal of the computed fragment, in camera space
    vec3 n = normalize(Normal_Cameraspace);
    // Direction of the light (from the fragment to the light)
    vec3 l = normalize(LightDirection_Cameraspace);
    // Cosine of the angle between the normal and the light direction, 
    // clamped above 0
    //  - light is at the vertical of the triangle -> 1
    //  - light is perpendicular to the triangle -> 0
    //  - light is behind the triangle -> 0
    float cosTheta = clamp(dot(n, l), 0, 1);

    Color = MaterialDiffuseColor * LightColor * LightPower * cosTheta / (distance * distance);
}
