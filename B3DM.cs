using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace XDOtoGLTF2
{
    class B3DM
    {
        // batchId = 0인 가장 간단한 b3dm을 만듬
        public B3DM(GLBMerge glb, string file_name, string file_path)
        {

            BinaryReader br = new BinaryReader(File.Open(file_path + file_name + ".glb", FileMode.Open));
            int many = (int)br.BaseStream.Length;
            Byte[] temp = br.ReadBytes(many);
            br.Close();

            // b3dm은 8 bytes align을 해야하는데 glb를 수정하지않고 featureTable json에 쓸모없는 값(0x20)을 넣어 8 byte를 맞춤
            int byte_8_align = 0;
            if((28 + 20 + many % 8) != 0)
            {
                byte_8_align = (28 + 20 + many) % 8;
            }
            int total_size = (28 + 20 + byte_8_align + many);
            Console.WriteLine(file_name + ".b3dm ByteLength: " + total_size + "(origin: " + (28 + 20 + many) + "), mod 8: " + byte_8_align);
            StreamWriter sw = new StreamWriter(file_path + file_name + ".b3dm");
            sw.Write("b3dm");
            sw.Close();

            BinaryWriter bw = new BinaryWriter(File.Open(file_path + file_name + ".b3dm", FileMode.Append));
            bw.Write((uint)1);
            bw.Write((uint)total_size);  // 8 byte padding
            bw.Write((uint)(20 + byte_8_align));
            bw.Write((uint)0);
            bw.Write((uint)0);
            bw.Write((uint)0);

            bw.Close();

            sw = new StreamWriter(file_path + file_name + ".b3dm", append: true);
            sw.Write("{\"BATCH_LENGTH\":0}");
            sw.Close();

            bw = new BinaryWriter(File.Open(file_path + file_name + ".b3dm", FileMode.Append));
            bw.Write((byte)0x20);
            bw.Write((byte)0x20);
            for(int i = 0; i < byte_8_align; i++)
            {
                bw.Write((byte)0x20);
            }

            bw.Write(temp);
            bw.Close();

        }
    }
}
