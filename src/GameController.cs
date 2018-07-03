using System;
using System.Drawing;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SS14.Noise
{
    class GameController : GameWindow
    {
        uint VAO;
        uint VBO;
        uint EBO;
        int ShaderProgram;
        int Texture;

        public GameController() : base(800, 600,
                                       GraphicsMode.Default,
                                       "Noise!",
                                       GameWindowFlags.Default,
                                       DisplayDevice.Default,
                                       3, 3, GraphicsContextFlags.Debug)
        {
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            GL.DebugMessageCallback(DebugMessage, IntPtr.Zero);
            GL.ClearColor(Color4.Black);

            {
                // VAO.
                GL.GenVertexArrays(1, out VAO);
                GL.BindVertexArray(VAO);
            }

            {
                // VBO & EBO.
                var tri = new float[]
                {
                    -1, -1, 0, 1,
                    1,  -1, 1, 1,
                    1,   1, 1, 0,
                    -1,  1, 0, 0,
                };

                unsafe
                {
                    // Gotta spare that ONE GL call.
                    var buffers = stackalloc uint[2];
                    GL.GenBuffers(2, buffers);
                    VBO = buffers[0];
                    EBO = buffers[1];
                }
                
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * tri.Length, tri, BufferUsageHint.StaticDraw);
            
                var indices = new uint[]
                {
                    0, 1, 2,
                    0, 2, 3,
                };

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
                GL.BufferData(BufferTarget.ElementArrayBuffer, sizeof(uint) * indices.Length, indices, BufferUsageHint.StaticDraw);
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

            { 
                // Vertex Attribs.
                GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), sizeof(float) * 2);
                GL.EnableVertexAttribArray(0);
                GL.EnableVertexAttribArray(1);
            }

            {
                // Texture.
                GL.GenTextures(1, out Texture);
                GL.BindTexture(TextureTarget.Texture2D, Texture);

                int param = (int)TextureWrapMode.Repeat;
                GL.TextureParameterI((int)All.Texture2D, TextureParameterName.TextureWrapS, ref param);
                GL.TextureParameterI((int)All.Texture2D, TextureParameterName.TextureWrapT, ref param);

                param = (int)TextureMagFilter.Nearest;
                GL.TextureParameterI((int)All.Texture2D, TextureParameterName.TextureMagFilter, ref param);
                GL.TextureParameterI((int)All.Texture2D, TextureParameterName.TextureMinFilter, ref param);

                ReloadImage();
            }
        }

        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);

            GL.DeleteProgram(ShaderProgram);
            GL.DeleteVertexArray(VAO);

            unsafe
            {
                var buffers = stackalloc uint[2]
                {
                    VBO,
                    EBO,
                };
                GL.DeleteBuffers(2, buffers);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(ShaderProgram);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Texture);
            GL.BindVertexArray(VAO);
            GL.Uniform1(GL.GetUniformLocation(ShaderProgram, "ourTexture"), 0);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, IntPtr.Zero);

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

        void ReloadImage()
        {
            LoadImageToTexture("src/background.png");
        }

        void LoadImageToTexture(string path)
        {
            using (var file = File.OpenRead(path))
            using (var img = new Bitmap(file))
            {
                var data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height),
                                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.BindTexture(TextureTarget.Texture2D, Texture);
                GL.TexImage2D(TextureTarget.Texture2D,
                              0,
                              PixelInternalFormat.Rgba,
                              data.Width,
                              data.Height,
                              0,
                              PixelFormat.Bgra,
                              PixelType.UnsignedByte,
                              data.Scan0);

                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                img.UnlockBits(data);
            }
        }

        private void DebugMessage(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            Console.WriteLine("ah fuck");
        }

        const string VERTEX_SHADER = @"
#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aUV;

out vec2 uv;

void main()
{
    uv = aUV;
    gl_Position = vec4(aPos.x, aPos.y, 0.0, 1.0);
}";

        const string FRAGMENT_SHADER = @"
#version 330 core
out vec4 FragColor;
in vec2 uv;

uniform sampler2D ourTexture;

void main()
{
    FragColor = vec4(texture(ourTexture,uv));
}";
    }
}