using System;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace XDOtoGLTF2
{
    public class GLBMerge
    {
        // 한 폴더내에 여러개의 glb가 만들어지는데 이를 통합하여 한 파일로 만들게 된다.
        // 따라서 한 폴더내에는 한 GLB가 있게 되며 이 glb는 모든 xdo object를 표현할 수 있게 된다.
        // GLTF, GLB에 있던 attribute(ObjBox, key, color, altitude 등의 값은 가져오지 않았다. 추가할거면 기존의 것을 가져와서 추가시키기만 하면 끝)
        private bool debugmode = true;

        JObject superJSON = JObject.Parse(@"{}");
        public GLBMerge(GLBInfo[] gltf_array, string key)
        {
            makeScene(superJSON, debugmode);
            int meshCount = makeScenes(superJSON, gltf_array, false);
            makeNodes(superJSON, gltf_array, false, meshCount);
            int _batchID_Number = makeMeshes(superJSON, gltf_array, false, meshCount);
            makeMaterials(superJSON, gltf_array, false, meshCount);
            makeTextures(superJSON, gltf_array, false, meshCount);
            makeSamplers(superJSON, false);
            makeImages(superJSON, gltf_array, false, _batchID_Number, meshCount);
            makeAccessors(superJSON, gltf_array, false, meshCount);
            int bytelength = makeBufferViews(superJSON, gltf_array, false, meshCount);
            makeBuffers(superJSON, bytelength, false);
            makeAsset(superJSON, false);
            makeGLB(superJSON, gltf_array, key, bytelength);
            int index = 0;
            for(int i = 0; i < gltf_array.Length; i++)
            {
                var item = gltf_array[i];
                var file_path = item.filePath;
                var file_name = item.fileName;
                var folder_name = item.folderName;
                var json = item.glb.json;

                Console.WriteLine("{0})\t\tFilePath:\t" + item.filePath, ++index);
                Console.WriteLine("\t\tFileName:\t" + item.fileName);
                Console.WriteLine("\t\tFolderName:\t" + item.folderName);
                
            }
        }
        private void makeScene(JObject superjson, bool debug)
        {
            superjson["scene"] = 0;

            if (debug) Console.WriteLine("#DEBUG\t" + superjson["scene"].ToString());
        }
        private int makeScenes(JObject superjson, GLBInfo[] gltf_array, bool debug)
        {
            int meshNo = 0;
            for (int i = 0; i < gltf_array.Length; i++)
            {
                var gltf_scenes = gltf_array[i].glb.json["scenes"][0];
                var gltf_scenes_nodes = (JArray)gltf_scenes["nodes"];
                meshNo += gltf_scenes_nodes.Count;
            }

            JArray scenes_nodes = new JArray();
            int[] scenes_nodes_array = new int[meshNo];
            for(int i = 0; i < meshNo; i++)
            {
                scenes_nodes_array[i] = i;
            }

            JObject scenesToken = JObject.FromObject(new
            {
                name = "Scene",
                nodes = scenes_nodes_array
            });
            scenes_nodes.Add(scenesToken);

            superjson["scenes"] = scenes_nodes;

            if (debug) Console.WriteLine("#DEBUG\tTotal xdo mesh count: " + meshNo);
            if (debug) Console.WriteLine("#DEBUG\t" + scenesToken.ToString());

            return meshNo;
        }
        private void makeNodes(JObject superjson, GLBInfo[] gltf_array, bool debug, int meshcount)
        {
            JArray nodes_nodes = new JArray();
            for(int i = 0; i < meshcount; i++)
            {
                JObject nodesToken = JObject.FromObject(new
                {
                    mesh = i,
                    name = "node_" + i
                });
                nodes_nodes.Add(nodesToken);
            }
            superjson["nodes"] = nodes_nodes;

            if (debug) Console.WriteLine("#DEBUG\t" + nodes_nodes.ToString());
        }
        private int makeMeshes(JObject superjson, GLBInfo[] gltf_array, bool debug, int meshcount)
        {
            JArray meshes_nodes = new JArray();
            for(int i = 0; i < meshcount; i++)
            {
                JArray primitives_element = new JArray();
                JToken attribute_token = JObject.FromObject(new
                {
                    POSITION = 1 + i * 4,
                    NORMAL = 2 + i * 4,
                    TEXCOORD_0 = 3 + i * 4//,
                    //_BATCHID = 4 * meshcount
                });
                JToken primitives_token = JObject.FromObject(new
                {
                    attributes = attribute_token,
                    indices = 0 + i * 4,
                    material = i
                });

                primitives_element.Add(primitives_token);

                JObject meshesToken = JObject.FromObject(new
                {
                    name = "mesh_" + i,
                    primitives = primitives_element 
                });
                meshes_nodes.Add(meshesToken);
            }
            superjson["meshes"] = meshes_nodes;

            if (debug) Console.WriteLine("#DEBUG\t" + meshes_nodes);
            return 4 * meshcount;
        }

        private void makeMaterials(JObject superjson, GLBInfo[] gltf_array, bool debug, int meshcount)
        {
            JArray materials_nodes = new JArray();
            for(int i = 0; i < meshcount; i++)
            {
                JToken index = JToken.FromObject(new
                {
                    index = i,
                    texCoord = 0 // temporary
                });
                JToken baseColorTexture = JToken.FromObject(new
                {
                    baseColorTexture = index,
                    metallicFactor = 0.0,
                    roughnessFactor = 1.0
                });
                JToken materialsElement = JToken.FromObject(new
                {
                    pbrMetallicRoughness = baseColorTexture
                });
                materials_nodes.Add(materialsElement);
            }
            superjson["materials"] = materials_nodes;

            if (debug) Console.WriteLine("#DEBUG\t" + materials_nodes.ToString());
        }
        
        private void makeTextures(JObject superjson, GLBInfo[] gltf_array, bool debug, int meshcount)
        {
            JArray texture_nodes = new JArray();
            for(int i = 0; i < meshcount; i++)
            {
                JToken textureToken = JObject.FromObject(new
                {
                    sampler = 0,
                    source = i
                });
                texture_nodes.Add(textureToken);
            }
            superjson["textures"] = texture_nodes;

            if (debug) Console.WriteLine("#DEBUG\t" + texture_nodes.ToString());
        }
        private void makeSamplers(JObject superjson, bool debug)
        {
            JArray samplers_nodes = new JArray();
            samplers_nodes.Add(JObject.FromObject(new
            {
               // empty value
            }));
            superjson["samplers"] = samplers_nodes;

            if (debug) Console.WriteLine("#DEBUG\t" + samplers_nodes);
        }
        private void makeImages(JObject superjson, GLBInfo[] gltf_array, bool debug, int BATCHIDindex, int meshcount)
        {
            JArray images_nodes = new JArray();
            for(int i = 0; i < gltf_array.Length; i++)
            {
                var gltf_json = gltf_array[i].glb.json;
                var gltf_json_image = (JArray)gltf_json["images"];
                for(int j = 0; j < gltf_json_image.Count; j++)
                {
                    images_nodes.Add(gltf_json_image[j]);
                }
            }

            for(int i = 0; i < images_nodes.Count; i++)
            {
                var element = images_nodes[i];
                element["bufferView"] = (BATCHIDindex /* +1 */) + i;
            }
            superjson["images"] = images_nodes;

            if (debug) Console.WriteLine("#DEBUG\t" + images_nodes.ToString());
        }
        private void makeAccessors(JObject superjson, GLBInfo[] gltf_array, bool debug, int meshcount)
        {
            JArray accessors_nodes = new JArray();
            int index = 0;
            for(int i = 0; i < gltf_array.Length; i++)
            {
                var gltf_json = gltf_array[i].glb.json;
                var gltf_json_accessor = (JArray)gltf_json["accessors"];
                for(int j = 0; j < gltf_json_accessor.Count; j++)
                {
                    var accessor_element = (JToken)gltf_json_accessor[j];
                    accessor_element["bufferView"] = index++;
                    accessors_nodes.Add(gltf_json_accessor[j]);
                }
            }
            superjson["accessors"] = accessors_nodes;

            if (debug) Console.WriteLine("#DEBUG\t" + accessors_nodes.ToString());
        }
        private int makeBufferViews(JObject superjson, GLBInfo[] gltf_array, bool debug, int meshcount)
        {
            int byteLength = 0;
            JArray bufferviews_nodes = new JArray();
            List<int[]> imageBufferViewsIndexList = new List<int[]>();
            for (int i = 0; i < gltf_array.Length; i++)
            {
                var gltf_json = gltf_array[i].glb.json;
                var gltf_json_bufferViews = (JArray)gltf_json["bufferViews"];
                for(int j = 0; j < gltf_json_bufferViews.Count; j++)
                {
                    // bufferView가 이미지를 나타낼때는 나중에 처리
                    if (gltf_json_bufferViews[j]["target"] == null)
                    {
                        imageBufferViewsIndexList.Add(new int[2] { i, j });
                        continue;
                    }

                    // bufferView가 target을 가지고 있을경우
                    JToken bufferview_element = JToken.FromObject(new
                    {
                        buffer = 0,
                        byteLength = (int)gltf_json_bufferViews[j]["byteLength"],
                        byteOffset = byteLength
                    });
                    if ((int)gltf_json_bufferViews[j]["byteLength"] % 4 != 0) byteLength += 2;  // fit mul of 4 (because of indice list)
                    byteLength += (int)gltf_json_bufferViews[j]["byteLength"];

                    if (j == gltf_json_bufferViews.Count-1) byteLength += (int)gltf_json_bufferViews[j]["byteOffset"] + (int)gltf_json_bufferViews[j]["byteLength"];
                    if(gltf_json_bufferViews[j]["target"] != null) bufferview_element["target"] = gltf_json_bufferViews[j]["target"];
                    bufferviews_nodes.Add(bufferview_element);
                }
            }

            for(int i = 0; i < imageBufferViewsIndexList.Count; i++)
            {
                int vi = imageBufferViewsIndexList[i][0];
                int vj = imageBufferViewsIndexList[i][1];

                var bufferviewElement = (JArray)gltf_array[vi].glb.json["bufferViews"];

                JToken bufferview_element = JToken.FromObject(new
                {
                    buffer = 0,
                    byteLength = (int)bufferviewElement[vj]["byteLength"],
                    byteOffset = byteLength
                });
                if ((int)bufferviewElement[vj]["byteLength"] % 4 != 0)
                {
                    byteLength += 4 - ((int)bufferviewElement[vj]["byteLength"] % 4);
                }
                byteLength += (int)bufferviewElement[vj]["byteLength"];
                bufferviews_nodes.Add(bufferview_element);
            }

            superjson["bufferViews"] = bufferviews_nodes;
            if (debug) Console.WriteLine("#DEBUG\t" + bufferviews_nodes.ToString());

            return byteLength;
        }
        private void makeBuffers(JObject superjson, int bytelength, bool debug)
        {
            JArray buffers_nodes = new JArray();
            JToken buffers_element = JToken.FromObject(new
            {
                byteLength = bytelength
            });
            buffers_nodes.Add(buffers_element);

            superjson["buffers"] = buffers_nodes;
            if (debug) Console.WriteLine("#DEBUG\t" + buffers_nodes.ToString());
        }
        private void makeAsset(JObject superjson, bool debug)
        {
            JToken asset_token = JObject.FromObject(new
            {
                generator = "GLB* -> GLB test",
                version = "2.0"
            });
            superjson["asset"] = asset_token;
        }
        private void makeGLB(JObject superjson, GLBInfo[] gltf, string key, int total_offset)
        {
            Console.WriteLine(gltf[0].filePath + key + ".glb");
            BinaryWriter bw = new BinaryWriter(File.Open(gltf[0].filePath + key + ".glb", FileMode.Create));
            bw.Write((uint)0x46546c67); // magic    :   glTF
            bw.Write((uint)2);          // version  :   2

            int bias = 0;
            if (superjson.ToString().Length % 4 != 0)
                bias = 4 - (superjson.ToString().Length % 4);

            bw.Write((uint)(12 + superjson.ToString().Length + bias + 8 + total_offset + 8));   // this file's total length
                            // json + bias = 4 byte padding, total_offset = always padding, 8 + 8 + 12 = 4 byte padding
            bw.Write((uint)(superjson.ToString().Length + bias));   // JSON 4 bytes padding
            bw.Write((uint)0x4e4f534a); // magic    :   JSON
            bw.Close();

            StreamWriter sw = new StreamWriter(gltf[0].filePath + key + ".glb", append: true);
            sw.Write(superjson.ToString());     // JSON write(Binarywriter always writes 2 bytes for representing its length)
            sw.Close();

            bw = new BinaryWriter(File.Open(gltf[0].filePath + key + ".glb", FileMode.Append));
            for (int i = 0; i < bias; i++) { bw.Write((byte)0x20); }    // for 4 byte padding. 0x20

            bw.Write((uint)total_offset);   // bin total length
            bw.Write((uint)0x004e4942); // magic    :   BIN
            for(int i = 0; i < gltf.Length; i++)
            {
                for(int j = 0; j < gltf[i].glb.bin_buffer.Count; j++)
                {
                    bw.Write(gltf[i].glb.bin_buffer[j]);
                }
            }
            for (int i = 0; i < gltf.Length; i++)
            {
                for (int j = 0; j < gltf[i].glb.img_buffer.Count; j++)
                {
                    bw.Write(gltf[i].glb.img_buffer[j]);
                }
            }
            bw.Close();
        }
    }
}