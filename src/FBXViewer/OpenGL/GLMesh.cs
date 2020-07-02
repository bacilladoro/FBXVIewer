using System;
using OpenGL;
using Matrix4x4 = System.Numerics.Matrix4x4;
using PrimitiveType = OpenGL.PrimitiveType;

namespace FBXViewer.OpenGL
{
    public class GLMesh
    {
        private readonly Buffers _buffers;
        private readonly int _indexCount;


        public GLMesh(Buffers buffers, int indexCount)
        {
            _indexCount = indexCount;
            _buffers = buffers;
            ModelMatrix = Matrix4x4.Identity;
        }

        public Matrix4x4 ModelMatrix { get; set; }
        public Texture? DiffuseTexture { get; set; }

        public void Render(int diffuseTextureId)
        {
            Gl.EnableVertexAttribArray(0);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _buffers.VertexBuffer);
            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 0, IntPtr.Zero);
            
            Gl.EnableVertexAttribArray(1);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _buffers.UvBuffer);
            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false, 0, IntPtr.Zero);
            
            Gl.EnableVertexAttribArray(2);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _buffers.NormalBuffer);
            Gl.VertexAttribPointer(2, 3, VertexAttribType.Float, false, 0, IntPtr.Zero);
            
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, _buffers.IndexBuffer);

            if (DiffuseTexture != null)
            {
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.BindTexture(TextureTarget.Texture2d, DiffuseTexture.Buffer);
                Gl.Uniform1i(diffuseTextureId, 1, 0);
            }
            
            Gl.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
            
            Gl.DisableVertexAttribArray(0);
        }
    }
}