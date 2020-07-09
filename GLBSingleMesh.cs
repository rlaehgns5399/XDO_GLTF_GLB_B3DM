using System;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Linq;

namespace XDOtoGLTF2
{
    public class GLBSingleMesh
    {
        // Mesh가 너무 여러개라 난잡한 것을 xdo당 face수에 관계없이 mesh하나로 고정하고, batchId까지 부여한다. (텍스쳐는 없음)
        // GLTF, GLB에 있던 attribute(ObjBox, key, color, altitude 등의 값은 가져오지 않았다. 추가할거면 기존의 것을 가져와서 추가시키기만 하면 끝)
        private bool debugmode = true;

        JObject superJSON = JObject.Parse(@"{}");

        List<List<uint>> bin_indice = new List<List<uint>>();
        List<List<float>> bin_position = new List<List<float>>();
        List<List<float>> bin_normal = new List<List<float>>();
        List<List<float>> bin_texture = new List<List<float>>();
        List<List<uint>> bin_batchId = new List<List<uint>>();
        
        List<int[]> accessor_element_count_list = new List<int[]>();
        List<float[]> position_min_list = new List<float[]>();
        List<float[]> position_max_list = new List<float[]>();
        List<int[]> batchId_min_list = new List<int[]>();
        List<int[]> batchId_max_list = new List<int[]>();
        
        List<byte[]> img_buffer = new List<byte[]>();
        int byteLength = 0;
        public GLBSingleMesh(GLBInfo[] gltf_array, string key)
        {
            key = key + "_single";
            mergeBin(gltf_array);
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
            makeGLB(superJSON, gltf_array, key, bytelength, meshCount);

            int index = 0;
            for (int i = 0; i < gltf_array.Length; i++)
            {
                var item = gltf_array[i];
                var file_path = item.filePath;
                var file_name = item.fileName;
                var folder_name = item.folderName;
                var json = item.glb.json;

                Console.WriteLine("{0})\t\tFilePath:\t" + item.filePath, ++index);
                Console.WriteLine("\t\tFileName:\t" + item.fileName);
                Console.WriteLine("\t\tFolderName:\t" + item.folderName);
                Console.WriteLine("\tfaceNum:\t" + item.glb.gltf.xdo.faceNum);
            }
        }
        private void mergeBin(GLBInfo[] gltf_array)
        {

            // uint indices_max = 0;
            uint batchId = 0;
            for (int i = 0; i < gltf_array.Length; i++)
            {
                uint rememberBatchId = batchId;
                uint indices_max = 0;
                float[] position_min = new float[3];
                float[] position_max = new float[3];
                int[] accessor_element_count = { 0, 0, 0, 0, 0 };

                List<uint> bin_indices = new List<uint>();
                List<float> bin_positions = new List<float>();
                List<uint> bin_batchIds = new List<uint>();
                List<float> bin_normals = new List<float>();
                List<float> bin_textures = new List<float>();
                for (int j = 0; j < gltf_array[i].glb.gltf.xdo.faceNum; j++)
                {
                    List<ushort> indices = gltf_array[i].glb.gltf.xdo.Meshes[j].Index;
                    for (int k = 0; k < indices.Count; k++) { 
                        bin_indices.Add(indices[k] + indices_max);
                    }
                    

                    
                    indices_max += gltf_array[i].glb.gltf.xdo.Meshes[j].index_min_max[1] + (uint)1;
                    accessor_element_count[0] += indices.Count;

                    List<Vector3> positions = gltf_array[i].glb.gltf.xdo.Meshes[j].Vertex;
                    position_min[0] = (float)Math.Min(position_min[0], gltf_array[i].glb.gltf.xdo.Meshes[j].vertex_min_max[0].x);
                    position_min[1] = (float)Math.Min(position_min[1], gltf_array[i].glb.gltf.xdo.Meshes[j].vertex_min_max[0].y);
                    position_min[2] = (float)Math.Min(position_min[2], gltf_array[i].glb.gltf.xdo.Meshes[j].vertex_min_max[0].z);
                    position_max[0] = (float)Math.Max(position_max[0], gltf_array[i].glb.gltf.xdo.Meshes[j].vertex_min_max[1].x);
                    position_max[1] = (float)Math.Max(position_max[1], gltf_array[i].glb.gltf.xdo.Meshes[j].vertex_min_max[1].y);
                    position_max[2] = (float)Math.Max(position_max[2], gltf_array[i].glb.gltf.xdo.Meshes[j].vertex_min_max[1].z);


                    for (int k = 0; k < positions.Count; k++)
                    {
                        bin_positions.Add(positions[k].x);
                        bin_positions.Add(positions[k].y);
                        bin_positions.Add(positions[k].z);
                        
                        bin_batchIds.Add(batchId);
                    }
                    accessor_element_count[1] += positions.Count;
                    accessor_element_count[4] += positions.Count;

                    List<Vector3> normals = gltf_array[i].glb.gltf.xdo.Meshes[j].Normals;
                    for (int k = 0; k < normals.Count; k++)
                    {
                        bin_normals.Add(normals[k].x);
                        bin_normals.Add(normals[k].y);
                        bin_normals.Add(normals[k].z);
                    }
                    accessor_element_count[2] += normals.Count;

                    List<Vector2> textures = gltf_array[i].glb.gltf.xdo.Meshes[j].UV;
                    for (int k = 0; k < textures.Count; k++)
                    {
                        bin_textures.Add(textures[k].x);
                        bin_textures.Add(textures[k].y);
                    }
                    accessor_element_count[3] += textures.Count;

                    

                    batchId++;
                }

                bin_indice.Add(bin_indices);
                bin_position.Add(bin_positions);
                bin_batchId.Add(bin_batchIds);
                bin_normal.Add(bin_normals);
                bin_texture.Add(bin_textures);

                accessor_element_count_list.Add(accessor_element_count);
                position_min_list.Add(position_min);
                position_max_list.Add(position_max);

                int[] batchId_max = new int[1];
                int[] batchId_min = new int[1];

                batchId_max[0] = (int)batchId - 1;
                batchId_min[0] = (int)rememberBatchId;
                batchId_max_list.Add(batchId_max);
                batchId_min_list.Add(batchId_min);
            }
        }
        private void makeScene(JObject superjson, bool debug)
        {
            superjson["scene"] = 0;

            if (debug) Console.WriteLine("#DEBUG\t" + superjson["scene"].ToString());
        }
        private int makeScenes(JObject superjson, GLBInfo[] gltf_array, bool debug)
        {
            // Node는 gltf_array의 length만큼만 만든다. (기존은 gltf당 mesh들의 합으로 모두 이어버렸음)
            JArray scenes_nodes = new JArray();
            int[] scenes_nodes_array = new int[gltf_array.Length];
            for (int i = 0; i < gltf_array.Length; i++)
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
            
            if (debug) Console.WriteLine("#DEBUG\t" + scenesToken.ToString());

            return gltf_array.Length;
        }


        private void makeNodes(JObject superjson, GLBInfo[] gltf_array, bool debug, int meshcount)
        {
            JArray nodes_nodes = new JArray();
            for (int i = 0; i < meshcount; i++)
            {
                var xdo = gltf_array[i].glb.gltf.xdo;
                double[] translationMatrix = { (xdo.MinX + xdo.MaxX) / 2, (xdo.MinY + xdo.MaxY) / 2, (xdo.MinZ + xdo.MaxZ) / 2 };
                JObject nodesToken = JObject.FromObject(new
                {
                    mesh = i,
                    name = "node_" + i,
                    translation = translationMatrix
                });
                nodes_nodes.Add(nodesToken);
            }
            superjson["nodes"] = nodes_nodes;

            if (debug) Console.WriteLine("#DEBUG\t" + nodes_nodes.ToString());
        }
        private int makeMeshes(JObject superjson, GLBInfo[] gltf_array, bool debug, int meshcount)
        {
            JArray meshes_nodes = new JArray();
            for (int i = 0; i < meshcount; i++)
            {
                JArray primitives_element = new JArray();
                JToken attribute_token = JObject.FromObject(new
                {
                    POSITION = 1 + i * 5,
                    NORMAL = 2 + i * 5,
                    _BATCHID = 3 + i * 5,
                    TEXCOORD_0 = 4 + i * 5,
                });

                /*
                for(int j = 0; j < 111; j++)
                {
                    attribute_token["TEXCOORD_" + j] = j;
                }
                */
                JToken primitives_token = JObject.FromObject(new
                {
                    attributes = attribute_token,
                    indices = 0 + i * 5,
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
            return 5 * meshcount;
        }

        private void makeMaterials(JObject superjson, GLBInfo[] gltf_array, bool debug, int meshcount)
        {
            JArray materials_nodes = new JArray();
            for (int i = 0; i < meshcount; i++)
            {
                JToken index = JToken.FromObject(new
                {
                    index = i
                    // texCoord = 0 // temporary
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
            for (int i = 0; i < meshcount; i++)
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
            for(int i = 0; i < meshcount; i++)
            {
                JToken images_element = JToken.FromObject(new
                {
                    bufferView = BATCHIDindex++,
                    mimeType = "image/jpeg"
                });
                images_nodes.Add(images_element);
            }
            superjson["images"] = images_nodes;

            if (debug) Console.WriteLine("#DEBUG\t" + images_nodes.ToString());
        }
        private void makeAccessors(JObject superjson, GLBInfo[] gltf_array, bool debug, int meshcount)
        {
            JArray accessors_nodes = new JArray();
            int index = 0;
            for (int i = 0; i < meshcount; i++)
            {
                JToken accessors_indices = JToken.FromObject(new {
                    name = "indice_uint32",
                    bufferView = index++,
                    componentType = 5125,
                    count = accessor_element_count_list[i][0],
                    type = "SCALAR"
                });

                JToken accessors_positions = JToken.FromObject(new
                {
                    name = "positions_float",
                    bufferView = index++,
                    componentType = 5126,
                    count = accessor_element_count_list[i][1],
                    min = position_min_list[i],
                    max = position_max_list[i],
                    type = "VEC3"
                });

                JToken accessors_normals = JToken.FromObject(new
                {
                    name = "normals_float",
                    bufferView = index++,
                    componentType = 5126,
                    count = accessor_element_count_list[i][2],
                    type = "VEC3"
                });

                

                JToken accessors_batchid = JToken.FromObject(new
                {
                    name = "batchId_uint32",
                    bufferView = index++,
                    componentType = 5125,
                    count = accessor_element_count_list[i][4],
                    min = batchId_min_list[i],
                    max = batchId_max_list[i],
                    type = "SCALAR"
                });

                JToken accessors_textures = JToken.FromObject(new
                {
                    name = "textures_float",
                    bufferView = index++,
                    componentType = 5126,
                    count = accessor_element_count_list[i][3],
                    type = "VEC2"
                });

                accessors_nodes.Add(accessors_indices);
                accessors_nodes.Add(accessors_positions);
                accessors_nodes.Add(accessors_normals);
                accessors_nodes.Add(accessors_batchid);
                accessors_nodes.Add(accessors_textures);
            }
            superjson["accessors"] = accessors_nodes;

            if (debug) Console.WriteLine("#DEBUG\t" + accessors_nodes.ToString());
        }
        private int makeBufferViews(JObject superjson, GLBInfo[] gltf_array, bool debug, int meshcount)
        {
            
            JArray bufferviews_nodes = new JArray();

            for(int i = 0; i < meshcount; i++)
            {
                JToken bufferviews_element_indices = JToken.FromObject(new
                {
                    buffer = 0,
                    byteLength = accessor_element_count_list[i][0] * 4,
                    byteOffset = byteLength,
                    target = 34963  // arraybuffer
                });
                byteLength += accessor_element_count_list[i][0] * 4;

                JToken bufferviews_element_positions = JToken.FromObject(new
                {
                    buffer = 0,
                    byteLength = accessor_element_count_list[i][1] * 4 * 3,
                    byteOffset = byteLength,
                    target = 34962  // element arraybuffer (vec3, vec2...)
                });
                byteLength += accessor_element_count_list[i][1] * 4 * 3;

                JToken bufferviews_element_normals = JToken.FromObject(new
                {
                    buffer = 0,
                    byteLength = accessor_element_count_list[i][2] * 4 * 3,
                    byteOffset = byteLength,
                    target = 34962  // element arraybuffer (vec3, vec2...)
                });
                byteLength += accessor_element_count_list[i][2] * 4 * 3;
                
                JToken bufferviews_element_batchIds = JToken.FromObject(new
                {
                    buffer = 0,
                    byteLength = accessor_element_count_list[i][4] * 4,
                    byteOffset = byteLength,
                    target = 34962  // arraybuffer
                });
                byteLength += accessor_element_count_list[i][4] * 4;

                JToken bufferviews_element_textures = JToken.FromObject(new
                {
                    buffer = 0,
                    byteLength = accessor_element_count_list[i][3] * 4 * 2,
                    byteOffset = byteLength,
                    target = 34962  // element arraybuffer (vec3, vec2...)
                });
                byteLength += accessor_element_count_list[i][3] * 4 * 2;


                bufferviews_nodes.Add(bufferviews_element_indices);
                bufferviews_nodes.Add(bufferviews_element_positions);
                bufferviews_nodes.Add(bufferviews_element_normals);
                bufferviews_nodes.Add(bufferviews_element_batchIds);
                bufferviews_nodes.Add(bufferviews_element_textures);
            }
            for (int i = 0; i < meshcount; i++)
            {
                BinaryReader br = new BinaryReader(File.Open(Program.no_texture_temp_jpg, FileMode.Open));
                int bias = 4 - ((int)br.BaseStream.Length % 4);
                JToken bufferviews_temp_texture = JToken.FromObject(new
                {
                    buffer = 0,
                    byteLength = (int)br.BaseStream.Length,
                    byteOffset = byteLength
                });
                img_buffer.Add(br.ReadBytes((int)br.BaseStream.Length));
                byteLength += (int)br.BaseStream.Length + bias;

                br.Close();
                bufferviews_nodes.Add(bufferviews_temp_texture);
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
                generator = "XDO* -> GLB test",
                version = "2.0"
            });
            superjson["asset"] = asset_token;
        }
        private void makeGLB(JObject superjson, GLBInfo[] gltf, string key, int total_offset, int meshcount)
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
            for(int i = 0; i < meshcount; i++)
            {
                for (int j = 0; j < bin_indice[i].Count; j++)
                    bw.Write(bin_indice[i][j]);
                for (int j = 0; j < bin_position[i].Count; j++)
                    bw.Write(bin_position[i][j]);
                for (int j = 0; j < bin_normal[i].Count; j++)
                    bw.Write(bin_normal[i][j]);
                for (int j = 0; j < bin_batchId[i].Count; j++)
                    bw.Write(bin_batchId[i][j]);
                for (int j = 0; j < bin_texture[i].Count; j++)
                    bw.Write(bin_texture[i][j]);
                
            }

            for (int i = 0; i < img_buffer.Count; i++)
            {
                bw.Write(img_buffer[i]);
                for (int j = 0; j < 4 - (img_buffer[i].Length % 4); j++)
                    bw.Write((byte)0);
            }
            bw.Close();
        }
    }
}