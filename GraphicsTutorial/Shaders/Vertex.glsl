#version 330 core

in vec3 Position;
in vec4 Color;

layout(std140) uniform MVP
{
    mat4 MvpMatrix;
};

out vec4 FragmentColor;

void main()
{
    gl_Position = MvpMatrix * vec4(Position, 1);
    FragmentColor = Color;
}
