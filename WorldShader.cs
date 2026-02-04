using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

public class WorldShader
{
    public int Handle;

    string vertexShaderSource = @"#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in float aTexLayer;

out vec2 texCoord;
out float texLayer;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main(void)
{
    texCoord = aTexCoord;
    texLayer = aTexLayer;
    gl_Position = projection * view * model * vec4(aPosition, 1.0);
}";

    string fragmentShaderSource = @"#version 330 core
out vec4 outputColor;
in vec2 texCoord;
in float texLayer;

uniform sampler2DArray textureArray;
uniform vec4 overrideColor;
uniform float useOverrideColor;

void main()
{
    if (useOverrideColor > 0.5) {
        outputColor = overrideColor;
    } else {
        // layer must be integer, but interpolated float is fine if flat or careful. 
        // usually we use 'flat out float texLayer' in VS and 'flat in' in FS to avoid interpolation issues.
        vec4 texColor = texture(textureArray, vec3(texCoord, texLayer));
        if (texColor.a < 0.1) discard;
        outputColor = texColor;
    }
}";

    public WorldShader()
    {
        // Add 'flat' qualifier modification if needed, but for now let's try.
        // Actually, for integer layers, 'flat' is safer.
        vertexShaderSource = vertexShaderSource.Replace("out float texLayer;", "flat out float texLayer;");
        fragmentShaderSource = fragmentShaderSource.Replace("in float texLayer;", "flat in float texLayer;");

        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        CheckError(vertexShader, "Vertex");

        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        CheckError(fragmentShader, "Fragment");

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        GL.LinkProgram(Handle);
        
        int linkStatus;
        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out linkStatus);
        if (linkStatus == 0)
        {
            string info = GL.GetProgramInfoLog(Handle);
            System.Console.WriteLine("WorldShader link error:\n" + info);
        }

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    private void CheckError(int shader, string type)
    {
        int status;
        GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
        if (status == 0)
        {
            string info = GL.GetShaderInfoLog(shader);
            System.Console.WriteLine($"{type} shader compile error:\n" + info);
        }
    }

    public void Use() => GL.UseProgram(Handle);

    public void SetMatrix4(string name, Matrix4 data)
    {
        int location = GL.GetUniformLocation(Handle, name);
        if (location != -1) GL.UniformMatrix4(location, false, ref data);
    }
    
    public void SetInt(string name, int data)
    {
        int location = GL.GetUniformLocation(Handle, name);
        if (location != -1) GL.Uniform1(location, data);
    }

    public void SetFloat(string name, float data)
    {
        int location = GL.GetUniformLocation(Handle, name);
        if (location != -1) GL.Uniform1(location, data);
    }
    
    public void SetVector4(string name, Vector4 data)
    {
        int location = GL.GetUniformLocation(Handle, name);
        if (location != -1) GL.Uniform4(location, data);
    }
}
