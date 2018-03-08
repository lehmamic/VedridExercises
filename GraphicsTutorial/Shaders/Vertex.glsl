#version 330 core

in vec3 Position;
in vec4 Color;

layout(std140) uniform Projection
{
    mat4 ProjectionMatrix;
};

layout(std140) uniform View
{
    mat4 ViewMatrix;
};

layout(std140) uniform Model
{
    mat4 ModelMatrix;
};

out vec4 FragmentColor;

void main()
{
    mat4 MVP = ProjectionMatrix * ViewMatrix * ModelMatrix;

    gl_Position = MVP * vec4(Position, 1);
    FragmentColor = Color;
}
