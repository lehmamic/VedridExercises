#version 330 core

in vec3 Position;
in vec4 Color;

void main()
{
    gl_Position.xyz = Position;
    gl_Position.w = 1.0;
}
