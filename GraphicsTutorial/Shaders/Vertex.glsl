#version 330 core

in vec3 Position;
in vec4 Color;

layout(std140) uniform MVP
{
    mat4 MvpMatrix;
};

void main()
{
    // mat4 MVP = Projection * View * Model;
    gl_Position = MvpMatrix * vec4(Position, 1);
    //gl_Position.xyz = Position;
    //gl_Position.w = 1.0;
}
