#version 330 core

in vec2 UV;

uniform sampler2D SurfaceTexture;

out vec4 Color;

void main()
{
    Color = texture(SurfaceTexture, UV).rgba;
}
