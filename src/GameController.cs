using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;

namespace SS14.Noise
{
    class GameController : GameWindow
    {
        uint VAO;
        uint VBO;
        uint EBO;
        int ShaderProgram;

        public GameController() : base(800, 600, GraphicsMode.Default, "Noise!", GameWindowFlags.Default, DisplayDevice.Default, 3, 3, GraphicsContextFlags.Default)
        {
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            GL.ClearColor(Color4.Black);

            {
                // VAO.
                GL.GenVertexArrays(1, out VAO);
                GL.BindVertexArray(VAO);
            }

            {
                // VBO & EBO.
                var tri = new Vector2[]
                {
                    new Vector2(-1, -1),
                    new Vector2(1, 1),
                    new Vector2(1, -1),
                    new Vector2(-1, 1)
                };

                var indices = new uint[]
                {
                    0, 1, 2,
                    1, 2, 3,
                };

                GL.GenBuffers(1, out VBO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 2 * tri.Length, tri, BufferUsageHint.StaticDraw);
            
                GL.GenBuffers(1, out EBO);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
                GL.BufferData(BufferTarget.ElementArrayBuffer, sizeof(uint), indices, BufferUsageHint.StaticDraw);
            }

            {
                // Shaders.
                int vertexShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vertexShader, VERTEX_SHADER);
                GL.CompileShader(vertexShader);

                int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragmentShader, FRAGMENT_SHADER);
                GL.CompileShader(fragmentShader);

                CheckShaderCompile(vertexShader);
                CheckShaderCompile(fragmentShader);

                ShaderProgram = GL.CreateProgram();
                GL.AttachShader(ShaderProgram, vertexShader);
                GL.AttachShader(ShaderProgram, fragmentShader);
                GL.LinkProgram(ShaderProgram);

                GL.GetProgram(ShaderProgram, GetProgramParameterName.LinkStatus, out var success);
                if (success == 0)
                {
                    GL.GetProgramInfoLog(ShaderProgram, out var info);
                    throw new Exception("Program link error: " + info);
                }

                GL.UseProgram(ShaderProgram);
                GL.DeleteShader(vertexShader);
                GL.DeleteShader(fragmentShader);
            }

            // Vertex Attribs.
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
        }

        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);


        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(ShaderProgram);
            GL.BindVertexArray(VAO);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, new IntPtr(0));

            SwapBuffers();
        }

        void CheckShaderCompile(int shader)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out var success);
            if (success == 0)
            {
                GL.GetShaderInfoLog(shader, out var error);
                throw new Exception("Shader compile error: " + error);
            }
        }

        const string VERTEX_SHADER = @"
#version 330 core
layout (location = 0) in vec2 aPos;

void main()
{
    gl_Position = vec4(aPos.x, aPos.y, 0.0, 1.0);
}";

        const string FRAGMENT_SHADER = @"
#version 330 core
out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
} 
";
    }
}