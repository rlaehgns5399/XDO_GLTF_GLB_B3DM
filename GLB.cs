using System;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace XDOtoGLTF2
{
    public class GLB
    {
        public int total_offset = 0;
        public GLTF gltf;
        public JObject json;
        public List<byte[]> bin_buffer = new List<byte[]>();
        public List<byte[]> img_buffer = new List<byte[]>();

        // gltf, bin, texture를 glb 한 파일로 만들때에는
        // bin, texture 파일을 바이너리로 바꾸고, gltf(JSON)은 "buffers", "bufferViews", "images" 항목만 변경하면 된다
        // buffer를 여러개 쓰는 것이 아닌 1개만 씀으로써 그에 해당되는 내용을 모두 바꾸는 과정이 필요
        public GLB(GLTFInfo gltf_item, string key)
        {
            var item = gltf_item;
            var file_path = item.filePath;
            var file_name = item.fileName;
            gltf = gltf_item.gltf;
            json = gltf.container;

                var buffers = (JArray)json["buffers"];
                var bufferViews = (JArray)json["bufferViews"];
                List<byte[]> buffer = new List<byte[]>();
                
                total_offset = 0;
                int k = 0;
                for (int j = 0; j < buffers.Count; j++)
                {
                    var temp = buffers[j];
                    int binLength = (int)temp["byteLength"];
                    string binName = temp["uri"].ToString();


                    BinaryReader br = new BinaryReader(File.Open(file_path + file_name + "_" + j + ".bin", FileMode.Open));
                    byte[] forRead = br.ReadBytes((int)br.BaseStream.Length);
                    buffer.Add(forRead);
                    bin_buffer.Add(forRead);
                    br.Close();
                    for (k = 4 * j; k < 4 * j + 4; k++)
                    {
                        int bufview_buffer = (int)(bufferViews[k]["buffer"]);
                        int bufview_byteLength = (int)bufferViews[k]["byteLength"];
                        int bufview_byteOffset = (int)bufferViews[k]["byteOffset"];
                        int bufview_target = (int)bufferViews[k]["target"];

                        JToken bufview_element = JToken.FromObject(new
                        {
                            buffer = 0,
                            byteLength = bufview_byteLength,
                            byteOffset = total_offset,
                            target = bufview_target
                        });

                        if (bufview_byteLength % 4 != 0) bufview_byteLength += 2;   // fit mul of 4
                        total_offset += bufview_byteLength;

                        bufferViews[k] = bufview_element;
                        // Console.WriteLine(bufview_element.ToString());
                    }
                    // br.ReadBytes(binLength);
                }

                var images = (JArray)json["images"];
                for (int j = 0; j < images.Count; j++)
                {
                    string imgName = (string)images[j]["uri"];
                    BinaryReader ir;
                    try
                    {
                        ir = new BinaryReader(File.Open(file_path + imgName, FileMode.Open));
                    }
                    catch
                    {
                        ir = new BinaryReader(File.Open(Program.no_texture_temp_jpg, FileMode.Open));
                    }
                    var imageLength = (int)ir.BaseStream.Length;


                    JToken image_bufview_element = JToken.FromObject(new
                    {
                        buffer = 0,
                        byteLength = imageLength,
                        byteOffset = total_offset
                    });

                    byte[] temp = new byte[imageLength];

                    temp = ir.ReadBytes(imageLength);
                    ir.Close();
                    if (imageLength % 4 != 0)
                    {
                        byte[] newTemp = new byte[imageLength + 4 - (imageLength % 4)];
                        System.Array.Copy(temp, newTemp, temp.Length);
                        for (int i = 0; i < 4 - (imageLength % 4); i++)
                        {
                            newTemp[imageLength + i] = (byte)0x0;
                            total_offset++;
                        }
                    img_buffer.Add(newTemp);
                        buffer.Add(newTemp);
                    }
                    else
                    {
                    img_buffer.Add(temp);
                        buffer.Add(temp);
                    }

                    total_offset += imageLength;
                    // Console.WriteLine(image_bufview_element.ToString());


                    string mimetype = "image/jpeg";
                    if (imgName.ToLower().Contains(".png")) mimetype = "image/png";
                    JToken image_element = JToken.FromObject(new
                    {
                        bufferView = k + j,
                        mimeType = mimetype
                    });
                    // Console.WriteLine(image_element.ToString());

                    json["images"][j] = image_element;
                    bufferViews.Add(image_bufview_element);
                }
                JArray buffers_container = new JArray();
                JToken buffers_element = JToken.FromObject(new
                {
                    byteLength = total_offset
                });
                buffers_container.Add(buffers_element);
                json["buffers"] = buffers_container;

                // Console.WriteLine(json.ToString());
                // Console.WriteLine(json["asset"]["extras"].ToString());
                BinaryWriter bw = new BinaryWriter(File.Open(file_path + file_name + ".glb", FileMode.Create));
                bw.Write((uint)0x46546c67);
                bw.Write((uint)2);

                int bias = 0;
                if (json.ToString().Length % 4 != 0)
                    bias = 4 - (json.ToString().Length % 4);

                bw.Write((uint)(12 + json.ToString().Length + bias + 8 + total_offset + 8));

                bw.Write((uint)(json.ToString().Length + bias));
                bw.Write((uint)0x4e4f534a);
                bw.Close();

                StreamWriter sw = new StreamWriter(file_path + file_name + ".glb", append: true);
                sw.Write(json.ToString());
                sw.Close();

                bw = new BinaryWriter(File.Open(file_path + file_name + ".glb", FileMode.Append));
                for (int i = 0; i < bias; i++) { bw.Write((byte)0x20); }
                bw.Write((uint)total_offset);
                bw.Write((uint)0x004e4942);
                for (int i = 0; i < buffer.Count; i++)
                {
                    bw.Write(buffer[i]);
                }
                bw.Close();
            }
            
    }

}
