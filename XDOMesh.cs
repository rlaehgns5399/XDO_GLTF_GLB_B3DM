using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace XDOtoGLTF2
{
    public class XDOMesh
    {
        /// <summary>
        /// Vertex 목록
        /// </summary>
        private List<Vector3> m_vertex = new List<Vector3>();
        /// <summary>
        /// IndexList 목록
        /// </summary>
        private List<Vector3> m_normals = new List<Vector3>();
        /// <summary>
        /// UV 목록
        /// </summary>
        private List<Vector2> m_uvs = new List<Vector2>();
        /// <summary>
        /// Index 목록
        /// </summary>
        private List<ushort> m_index = new List<ushort>();

        public List<Vector3> vertex_min_max = new List<Vector3>();
        public List<Vector3> normal_min_max = new List<Vector3>();
        public List<Vector2> texture_min_max = new List<Vector2>();
        public List<ushort> index_min_max = new List<ushort>();
        /// <summary>
        /// Mesh Color
        /// </summary>
        public Color32 Color { get; set; }

        public byte ImageLevel { get; set; }
        /// <summary>
        /// Texture 이미지 파일명
        /// </summary>
        public string ImageName { get; set; }
        /// <summary>
        /// Texture Nail 이미지, jpg 바이너리
        /// </summary>
        public byte[] Image { get; set; }

        public XDOMesh(System.IO.BinaryReader reader)
        {
            // Vertex 읽기
            var vertexCount = reader.ReadUInt32();

            float vertex_min_x, vertex_min_y, vertex_min_z;
            float vertex_max_x, vertex_max_y, vertex_max_z;
            float normal_min_x, normal_min_y, normal_min_z;
            float normal_max_x, normal_max_y, normal_max_z;
            float texture_min_u, texture_min_v;
            float texture_max_u, texture_max_v;

            vertex_min_x = vertex_min_y = vertex_min_z = float.MaxValue;
            vertex_max_x = vertex_max_y = vertex_max_z = float.MinValue;
            normal_min_x = normal_min_y = normal_min_z = float.MaxValue;
            normal_max_x = normal_max_y = normal_max_z = float.MinValue;
            texture_min_u = texture_min_v = float.MaxValue;
            texture_max_u = texture_max_v = float.MinValue;
            var randomvalue = new System.Random().NextDouble() * 60 - 30;
            randomvalue = 0.0f;
            for (int i = 0; i < vertexCount; i++)
            {
                // Vertex
                float vx = reader.ReadSingle() + (float)randomvalue;
                float vy = reader.ReadSingle() + (float)randomvalue;
                float vz = reader.ReadSingle() + (float)randomvalue;
                vertex_min_x = Math.Min(vertex_min_x, vx);
                vertex_min_y = Math.Min(vertex_min_y, vy);
                vertex_min_z = Math.Min(vertex_min_z, vz);
                vertex_max_x = Math.Max(vertex_max_x, vx);
                vertex_max_y = Math.Max(vertex_max_y, vy);
                vertex_max_z = Math.Max(vertex_max_z, vz);
                m_vertex.Add(new Vector3(vx, vy, vz));
                // normal
                float nx = reader.ReadSingle();
                float ny = reader.ReadSingle();
                float nz = reader.ReadSingle();
                               
                if(nx == 0 && ny == 0 && nz == 0)
                {
                    nx = ny = nz = 0.5f;
                    Console.WriteLine("0, 0, 0 detected. fixed to 0.5, 0.5, 0.5");
                }

                float a = Math.Abs((float)Math.Sqrt(nx * nx + ny * ny + nz * nz));
                // Console.WriteLine(nx + ", " + ny + ", " + nz + ", " + a + "//" + normal_min_x + ", " + normal_min_y + ", " + normal_min_z);
                // Console.WriteLine(nx + ", " + ny + ", " + nz + ", " + a + "//" + normal_max_x + ", " + normal_max_y + ", " + normal_max_z);

                nx /= a;
                ny /= a;
                nz /= a;                

                normal_min_x = Math.Min(normal_min_x, nx);
                normal_min_y = Math.Min(normal_min_y, ny);
                normal_min_z = Math.Min(normal_min_z, nz);
                normal_max_x = Math.Max(normal_max_x, nx);
                normal_max_y = Math.Max(normal_max_y, ny);
                normal_max_z = Math.Max(normal_max_z, nz);
                
                m_normals.Add(new Vector3(nx, ny, nz));
                // uv
                float u = reader.ReadSingle();
                float v = reader.ReadSingle();
                texture_min_u = Math.Min(texture_min_u, u);
                texture_min_v = Math.Min(texture_min_v, v);
                texture_max_u = Math.Max(texture_max_u, u);
                texture_max_v = Math.Max(texture_max_v, v);
                m_uvs.Add(new Vector2(u, v));
            }
            vertex_min_max.Add(new Vector3(vertex_min_x, vertex_min_y, vertex_min_z));  // vertex min
            vertex_min_max.Add(new Vector3(vertex_max_x, vertex_max_y, vertex_max_z));  // vertex max
            normal_min_max.Add(new Vector3(normal_min_x, normal_min_y, normal_min_z));  // normal min
            normal_min_max.Add(new Vector3(normal_max_x, normal_max_y, normal_max_z));  // normal max
            texture_min_max.Add(new Vector2(texture_min_u, texture_min_v));             // texture min
            texture_min_max.Add(new Vector2(texture_max_u, texture_max_v));             // texture min
            // Index 읽기

            ushort vertex_min, vertex_max;

            vertex_min = vertex_max = 0;

            var indexCount = reader.ReadUInt32();
            for (int i = 0; i < indexCount; i++)
            {
                ushort t = reader.ReadUInt16();
                vertex_min = Math.Min(vertex_min, t);
                vertex_max = Math.Max(vertex_max, t);
                m_index.Add(t);
            }
            index_min_max.Add(vertex_min);
            index_min_max.Add(vertex_max);

            // Color 읽기
            var c = reader.ReadUInt32();
            byte A = (byte)((c >> 24) & 0xFF);
            byte R = (byte)((c >> 16) & 0xFF);
            byte G = (byte)((c >> 8) & 0xFF);
            byte B = (byte)((c) & 0xFF);
            this.Color = new Color32(R, G, B, A);

            // Image Level
            this.ImageLevel = reader.ReadByte();

            // Image Name
            var imgNameLen = reader.ReadByte();
            if (imgNameLen > 0)
            {
                this.ImageName = Encoding.UTF8.GetString(reader.ReadBytes(imgNameLen));

                // Nail Image 읽기
                var nailLen = reader.ReadUInt32();
                this.Image = reader.ReadBytes((int)nailLen);
            }
            else
            {
                this.ImageName = null;
            }
        }

        public List<Vector3> Vertex { get { return m_vertex; } }
        public List<Vector3> Normals { get { return m_normals; } }
        public List<Vector2> UV { get { return m_uvs; } }
        public List<ushort> Index { get { return m_index; } }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb
                .Append("Vertex Count : " + m_vertex.Count).Append(Environment.NewLine)
                .Append("Index Count : " + m_index.Count).Append(Environment.NewLine)
                .Append("Color : ").Append(this.Color).Append(Environment.NewLine)
                .Append("Image Level : ").Append(this.ImageLevel).Append(Environment.NewLine)
                .Append("Image Name : ").Append(this.ImageName).Append(Environment.NewLine)
                .Append("Nail Length : ").Append(this.Image.Length).Append(Environment.NewLine);

            return sb.ToString();
        }

        public void Serialize(System.IO.BinaryWriter writer)
        {
            if (string.IsNullOrEmpty(this.ImageName))
                throw new InvalidOperationException();
            var imgNameBuf = Encoding.UTF8.GetBytes(this.ImageName);
            if ((int)byte.MaxValue < imgNameBuf.Length)
                throw new InvalidOperationException();

            if (this.Vertex.Count != this.Normals.Count ||
                this.Vertex.Count != this.UV.Count)
                throw new InvalidOperationException();

            if (this.Vertex.Count == 0 || this.Index.Count == 0)
                throw new InvalidOperationException();

            if (string.IsNullOrEmpty(this.ImageName) ||
                this.Image.Length == 0)
                throw new InvalidOperationException();

            writer.Write((uint)this.Vertex.Count);
            for (int i = 0; i < this.Vertex.Count; i++)
            {
                var v = this.Vertex[i];
                var n = this.Normals[i];
                var uv = this.UV[i];

                writer.Write(v.x);
                writer.Write(v.y);
                writer.Write(v.z);

                writer.Write(n.x);
                writer.Write(n.y);
                writer.Write(n.z);

                writer.Write(uv.x);
                writer.Write(uv.y);
            }

            writer.Write((uint)this.Index.Count);
            foreach (var idx in this.Index)
            {
                writer.Write((ushort)idx);
            }

            // Color
            var color = ((uint)this.Color.a << 24) + ((uint)this.Color.r << 16) +
                ((uint)this.Color.g << 8) + (uint)this.Color.b;
            writer.Write(color);

            // Image Level
            writer.Write(this.ImageLevel);
            // Image Name Length
            writer.Write((byte)imgNameBuf.Length);
            // Image Name
            writer.Write(imgNameBuf);

            // Nail Byte Length
            writer.Write((uint)this.Image.Length);
            // Nail Byte
            writer.Write(this.Image);
        }

    }

}