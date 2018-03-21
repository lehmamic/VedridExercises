#version 330 core

in vec2 Position;
in vec2 TextureCoordinate;

out vec2 UV;

void main()
{
    // Output position of the vertex, in clip space
    // map [0..800][0..600] to [-1..1][-1..1]
    vec2 vertexPosition_homoneneousspace = Position - vec2(400, 300); // [0..800][0..600] -> [-400..400][-300..300]
    vertexPosition_homoneneousspace /= vec2(400,300);
    gl_Position =  vec4(vertexPosition_homoneneousspace, 0, 1);

    UV = TextureCoordinate;
}
