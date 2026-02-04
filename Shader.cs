using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

public class Shader
{
    public int Handle;

    public Shader()
    {
        string vertexShaderSource = @"#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;

out vec2 texCoord;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec2 uvOffset;
uniform vec2 uvSize;

void main(void)
{
    texCoord = aTexCoord * uvSize + uvOffset;
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
}";

        string fragmentShaderSource = @"#version 330 core
out vec4 outputColor;
in vec2 texCoord;

uniform sampler2D texture0;
uniform vec4 overrideColor;
uniform float useOverrideColor;

void main()
{
    if (useOverrideColor > 0.5) {
        outputColor = overrideColor;
    } else {
        vec4 texColor = texture(texture0, texCoord);
        if (texColor.a < 0.1) discard;
        outputColor = texColor;
    }
}";

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        GL.LinkProgram(Handle);

        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    public void Use()
    {
        GL.UseProgram(Handle);
    }

    public void SetInt(string name, int data)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform1(location, data);
    }

    public void SetMatrix4(string name, Matrix4 data)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.UniformMatrix4(location, false, ref data);
    }

    public void SetFloat(string name, float data)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform1(location, data);
    }

    public void SetVector4(string name, Vector4 data)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform4(location, data);
    }

    public void SetVector2(string name, Vector2 data)
    {
        int location = GL.GetUniformLocation(Handle, name);
        if (location != -1) GL.Uniform2(location, data);
    }
}