#version 330 core

// Interpolated values from the vertex shaders
in vec2 UV;

// Values that stay constant for the whole mesh.
uniform sampler2D SurfaceTexture;

// Ouput data
out vec4 Color;

void main()
{
    Color = texture(SurfaceTexture, UV);
}
