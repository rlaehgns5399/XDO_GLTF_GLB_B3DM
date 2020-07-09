using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace XDOtoGLTF2
{

    // External glb를 만들기 위한 클래스. -> gltf, bin, jpg(텍스쳐) 파일만 있으면 만들 수 있음.
    class GLTFtoSIMPLEGLB
    {
        public GLTFtoSIMPLEGLB(String filename)
        {
            StreamReader sr = new StreamReader(filename + ".gltf");
            
            filename = filename + "_test";
            string json = sr.ReadToEnd();
            int jsonlength = (int)sr.BaseStream.Length;
            sr.Close();

            int bias = 4 - jsonlength % 4;
            BinaryWriter bw = new BinaryWriter(File.Open(filename + ".glb", FileMode.Create));
            bw.Write((uint)0x46546c67);
            bw.Write((uint)2);
            bw.Write((uint)(12 + jsonlength + bias + 8));
            bw.Write((uint)(jsonlength + bias));
            bw.Write((uint)0x4e4f534a);
            bw.Close();

            StreamWriter sw = new StreamWriter(filename + ".glb", append: true);
            sw.Write(json);
            sw.Close();

            bw = new BinaryWriter(File.Open(filename + ".glb", FileMode.Append));
            for(int i = 0; i < bias; i++)
            bw.Write((byte)0x20);
            bw.Close();
        }
    }
}
