using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.IO;

namespace GestureRecognition
{
    class GraphicsViewer: GameWindow, GraphicsManipulator
    {
        private int pgmID;
        private int vsID;
        private int fsID;

        private int attribute_vcol;
        private int attribute_vpos;
        private int uniform_mview;

        private int vbo_position;
        private int vbo_color;
        private int vbo_mview;
        private int ibo_elements;

        private Vector3[] vertdata;
        private Vector3[] coldata;
        private Matrix4[] mviewdata;

        private int[] indicedata; // To hold vertices for the cube

        private float time = 0.0f;
        private float xrotation = 0.0f;
        private float yrotation = 0.0f;
        private float zrotation = 0.0f;
        private float xshift = 0.0f;
        private float yshift = 0.0f;
        private float zshift = -10.0f;
        private float scale = 0.5f;

        private GestureRecognizer gestureRecognizer;

        public GraphicsViewer() : base(512, 512, new GraphicsMode(32, 24, 0, 4))
        {

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Initialize program
            initProgram();

            // Define vertices and initial view matrix
            vertdata = new Vector3[] {
                new Vector3(-0.8f, -0.8f,  -0.8f),
                new Vector3(0.8f, -0.8f,  -0.8f),
                new Vector3(0.8f, 0.8f,  -0.8f),
                new Vector3(-0.8f, 0.8f,  -0.8f),
                new Vector3(-0.8f, -0.8f,  0.8f),
                new Vector3(0.8f, -0.8f,  0.8f),
                new Vector3(0.8f, 0.8f,  0.8f),
                new Vector3(-0.8f, 0.8f,  0.8f),
            };

            // Define color data for vertices
            coldata = new Vector3[] {
                new Vector3(0f, 0f, 0f),
                new Vector3( 1f, 1f, 1f),
                new Vector3( 0f,  0f, 0f),
                new Vector3(1f, 1f, 1f),
                new Vector3( 0f, 0f, 0f),
                new Vector3( 1f,  1f, 1f),
                new Vector3(0f, 0f, 0f),
                new Vector3( 1f, 1f, 1f)
            };

            mviewdata = new Matrix4[]{
                Matrix4.Identity
            };

            indicedata = new int[]
            {
                //front
                0, 7, 3,
                0, 4, 7,
                //back
                1, 2, 6,
                6, 5, 1,
                //left
                0, 2, 1,
                0, 3, 2,
                //right
                4, 5, 6,
                6, 7, 4,
                //top
                2, 3, 6,
                6, 3, 7,
                //bottom
                0, 1, 5,
                0, 5, 4
            };

            Title = "Kinect Graphics Interaction";

            GL.ClearColor(0.5f, 0.5f, 0.5f, 0.1f);

            GL.PointSize(5f);

            gestureRecognizer = new GestureRecognizer(this);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Viewport(0, 0, Width, Height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.DepthTest);

            // Set desired variables
            GL.EnableVertexAttribArray(attribute_vpos);
            GL.EnableVertexAttribArray(attribute_vcol);

            // Draw
            //GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            GL.DrawElements(BeginMode.Triangles, indicedata.Length, DrawElementsType.UnsignedInt, 0);
            
            // Clean
            GL.DisableVertexAttribArray(attribute_vpos);
            GL.DisableVertexAttribArray(attribute_vcol);

            // Flush the buffer
            GL.Flush();

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            time += (float)e.Time;
            //Console.WriteLine(time);

            /* vbo_position */
            // Define buffer to send information to
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_position);
            // Define information that is being sent
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(vertdata.Length * Vector3.SizeInBytes), vertdata, BufferUsageHint.StaticDraw);
            // Use buffer
            GL.VertexAttribPointer(attribute_vpos, 3, VertexAttribPointerType.Float, false, 0, 0);

            /* vbo_color */
            // Define buffer to send information to
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_color);
            // Define information that is being sent
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(coldata.Length * Vector3.SizeInBytes), coldata, BufferUsageHint.StaticDraw);
            // Use Buffer
            GL.VertexAttribPointer(attribute_vcol, 3, VertexAttribPointerType.Float, true, 0, 0);

            /* ibo_elements */
            // Define buffer to send information to
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo_elements);
            // Define information that is being sent
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indicedata.Length * sizeof(int)), indicedata, BufferUsageHint.StaticDraw);


            if (Keyboard[Key.Escape])
            {
                Exit();
            }

            mviewdata[0] = Matrix4.CreateScale(scale);
            mviewdata[0] = mviewdata[0] * Matrix4.CreateRotationZ(0.15f * zrotation) * Matrix4.CreateRotationY(0.15f * yrotation) * Matrix4.CreateRotationX(0.15f * xrotation);
            mviewdata[0] = mviewdata[0] * Matrix4.CreateTranslation(0.15f * xshift, 0.15f * yshift, 0.15f * zshift);
            mviewdata[0] = mviewdata[0] * Matrix4.CreatePerspectiveFieldOfView(1.3f, ClientSize.Width / (float)ClientSize.Height, 1.0f, 40.0f);


            // Send model view matrix
            GL.UniformMatrix4(uniform_mview, false, ref mviewdata[0]);

            // Clear buffer for future use
            GL.UseProgram(pgmID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


        }

        protected void initProgram()
        {
            // Create Program
            pgmID = GL.CreateProgram();

            // Load Shaders
            loadShader("vs.glsl", ShaderType.VertexShader, pgmID, out vsID);
            loadShader("fs.glsl", ShaderType.FragmentShader, pgmID, out fsID);

            // Link Shaders and Write Errors
            GL.LinkProgram(pgmID);
            Console.WriteLine(GL.GetProgramInfoLog(pgmID));

            // Get attributes
            attribute_vpos = GL.GetAttribLocation(pgmID, "vPosition");
            attribute_vcol = GL.GetAttribLocation(pgmID, "vColor");
            uniform_mview = GL.GetUniformLocation(pgmID, "modelview");

            // Check for errors
            if (attribute_vpos == -1 || attribute_vcol == -1 || uniform_mview == -1)
            {
                Console.WriteLine("Error binding attributes");
            }

            GL.GenBuffers(1, out vbo_position);
            GL.GenBuffers(1, out vbo_color);
            GL.GenBuffers(1, out vbo_mview);
            GL.GenBuffers(1, out ibo_elements);
        }

        protected void loadShader(String filename, ShaderType type, int program, out int address)
        {
            address = GL.CreateShader(type);
            using (StreamReader sr = new StreamReader("../../" + filename))
            {
                GL.ShaderSource(address, sr.ReadToEnd());
            }

            GL.CompileShader(address);
            GL.AttachShader(program, address);
            Console.WriteLine(GL.GetShaderInfoLog(address));
        }

        public void Translate(float x, float y, float z)
        {
            xshift += x * 10;
            yshift += y * 10;
            zshift += z * 10;
        }

        public void Rotate(float x, float y, float z)
        {
            xrotation += x * 50;
            yrotation += y * 50;
            zrotation += z * 50;
        }

        public void Scale(float factor)
        {
            scale += factor;
        }
    }
}
