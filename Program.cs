using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
namespace XDOtoGLTF2
{
    class Program
    {
        // 텍스쳐가없으면 로딩이 되지 않음, 임시 텍스쳐 파일(노란색)
        public static String no_texture_temp_jpg = @"C:\Users\KimDoHoon\Desktop\C++_Project\XDOtoGLTF2\data\No_Texture.JPG";

        static void Main(string[] args)
        {
            // new GLTFtoSIMPLEGLB(@"C:\Users\KimDoHoon\Desktop\C++_Project\XDOtoGLTF2\data\dokdo\212\dokdo");       -> gltf 파일 하나로 external glb 만듬


            /**
             * A ------ A_1 ----- a1.xdo
             *       \         \- a2.xdo
             *       \         \- a3.xdo
             *       \         \- a4.xdo
             *       \- A_2 ----- b1.xdo
             *       \- A_3 ----- c1.xdo
             *       \- A_4 ----- d1.xdo
             *       
             *       file_path = A 폴더의 절대경로 입력
             **/
            
            String file_path = @"C:\Users\KimDoHoon\Desktop\C++_Project\XDOtoGLTF2\data\dokdo\";   // 해당 폴더를 입력받아 "하위 폴더들"을 참조하여 진행

            Queue<XDOInfo> XDO_queue = new Queue<XDOInfo>();
            HashSet<string> key = new HashSet<string>();
            var GLTF_queue_list = new Dictionary<string, Queue<GLTFInfo>>();

            // Queue<GLTFInfo> GLTF_queue = new Queue<GLTFInfo>();
            Queue<singleGLBInfo> GLB_queue = new Queue<singleGLBInfo>();
            DirSearchAndQueue(file_path, XDO_queue, key);
            int xdoSize = XDO_queue.Count;
            MakeGLTF(XDO_queue, GLTF_queue_list);

            MakeGLB(GLTF_queue_list, GLB_queue, key);
            MakeB3DM(GLB_queue);

            Console.ReadKey();
            
        }

        static void DirSearchAndQueue(string dir, Queue<XDOInfo> queue, HashSet<string> key)
        {
            // xdo 파일들을 찾아 queue에 집어넣음(나중에 xdo 파일들을 하나의 파일로 만들어야함)
            try
            {
                foreach (string d in Directory.GetDirectories(dir))
                {
                    DirectoryInfo di = new DirectoryInfo(d);
                    foreach(string f in Directory.GetFiles(d, "*.xdo"))
                    {
                        Console.WriteLine("Dir:\t{0} \nFile:\t{1}", di.Name, f);
                        key.Add(di.Name);
                        XDOInfo xdo = new XDOInfo();
                        xdo.xdo = new XDO(new BinaryReader(File.Open(f, FileMode.Open)));
                        xdo.folderName = di.Name;
                        xdo.fileName = Path.GetFileNameWithoutExtension(f);
                        xdo.filePath = Path.GetDirectoryName(f) + @"\";
                        queue.Enqueue(xdo);
                    }
                    DirSearchAndQueue(d, queue, key);  
                }
            }
            catch(System.Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        static void MakeGLTF(Queue<XDOInfo> xdo_queue, Dictionary<string, Queue<GLTFInfo>> gltf_hashtable)
        {
            // 집어넣어진 xdo 파일들을 queue에서 빼내면서 gltf, bin 생성(텍스쳐파일이 없다면 임시 텍스쳐(no_texture_temp_jpg)를 사용
            // DIctionary 구조를 사용하여 [폴더이름] queue에 gltf파일 집어 넣음
            while (xdo_queue.Count != 0)
            {
                XDOInfo item = xdo_queue.Dequeue();
                if (!gltf_hashtable.ContainsKey(item.folderName))
                {
                    gltf_hashtable.Add(item.folderName, new Queue<GLTFInfo>());
                }
                GLTFInfo gltf_item = new GLTFInfo();
                gltf_item.gltf = new XDOtoGLTF2.GLTF(item.xdo, item.fileName, item.filePath);
                gltf_item.filePath = item.filePath;
                gltf_item.fileName = item.fileName;
                gltf_item.folderName = item.folderName;
                gltf_hashtable[item.folderName].Enqueue(gltf_item);
            }
        }
        static void MakeGLB(Dictionary<string, Queue<GLTFInfo>> gltf_queue, Queue<singleGLBInfo> glb_merge_queue, HashSet<string> hashset_key)
        {
            // gltf -> embeded glb로 만듬
            foreach(var key in hashset_key)
            {
                int i = 0;
                GLBInfo[] glb_array = new GLBInfo[gltf_queue[key].Count];
                Console.WriteLine("Queue[{0}]:\tSize:{1} --------------", key, gltf_queue[key].Count);

                String filePath, fileName, folderName;
                filePath = fileName = folderName = "";
                while (gltf_queue[key].Count != 0)
                {
                    GLTFInfo item = gltf_queue[key].Dequeue();

                    GLBInfo glb_item = new GLBInfo();
                    glb_item.glb = new XDOtoGLTF2.GLB(item, key);
                    glb_item.filePath = item.filePath;
                    glb_item.fileName = item.fileName;
                    glb_item.folderName = item.folderName;
                    glb_array[i] = glb_item;
                    i++;


                    // temporary
                    filePath = glb_item.filePath;
                    fileName = glb_item.fileName;
                    folderName = glb_item.folderName;
                }

                // 1 gltf, bin, jpg -> 1 embeded glb
                singleGLBInfo q_item = new singleGLBInfo();
                
                // n (gltf, bin, jpg) -> 1 embeded glb
                //q_item.glb = new GLBMerge(glb_array, key);

                // n (gltf, bin, jpg) -> 1 mesh per xdo & embeded & batchID glb    but no texture.(only temporary texture)
                //new GLBSingleMesh(glb_array, key);

                // n (gltf, bin, jpg) -> 1 mesh per xdo & embeded & batchID, multi texture glb
                //new GLBSingleMeshWithMultiTexture(glb_array, key);

                

                // b3dm을 만들기위해 glb 파일을 큐에 집어 넣음
                q_item.filePath = filePath;
                q_item.fileName = fileName;
                q_item.folderName = folderName;

                glb_merge_queue.Enqueue(q_item);
            }
        }
        static void MakeB3DM(Queue<singleGLBInfo> q)
        {
            while(q.Count != 0)
            {
                singleGLBInfo item = q.Dequeue();
                new B3DM(item.glb, item.folderName, item.filePath);

            }
        }
    }
    public class XDOInfo : Info
    {
        public XDO xdo;
    }
    public class GLTFInfo : Info
    {
        public GLTF gltf;
    }
    public class GLBInfo : Info
    {
        public GLB glb;
    }
    public class singleGLBInfo : Info
    {
        public GLBMerge glb;
    }
    public class Info
    {
        public string filePath;
        public string fileName;
        public string folderName;
    }
}
