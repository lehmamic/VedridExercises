#version 330 core

in vec3 Position;
in vec4 Color;

uniform mat4 MVP;

void main()
{
    // mat4 MVP = Projection * View * Model;
    gl_Position = MVP * vec4(Position, 1);
    //gl_Position.xyz = Position;
    //gl_Position.w = 1.0;
}
