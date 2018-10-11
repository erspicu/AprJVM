using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using AprilJVM;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Collections;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading;

namespace JVMDemo_UI
{
    public partial class AprJVMUI : Form
    {
        public AprJVMUI()
        {
            InitializeComponent();
            parse_bytecode_list();
        }

        protected static AprJVMUI instance;
        public static AprJVMUI GetInstance()
        {
            if (instance == null || instance.IsDisposed)
                instance = new AprJVMUI();
            return instance;
        }

        ZipFile zip;
        AprJVM myjvm = new AprJVM();
        private void btn_open_Click(object sender, EventArgs e)
        {

            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "class file (*.class)|*.class|jar file (*.jar)|*.jar";

            if (fd.ShowDialog() != DialogResult.OK)
                return;

            if (fd.FileName.EndsWith(".class"))
            {
                jsonExplorer.Nodes.Clear();
                richTextBox1.Clear();

                
                bool loadclass = myjvm.LoadClass(fd.FileName);
                if (!loadclass)
                {
                    MessageBox.Show("load classfile fail !");
                    return;
                }
                BuildTree(JsonConvert.SerializeObject(myjvm.classfile), fd.SafeFileName);
            }

            if (fd.FileName.EndsWith(".jar"))
            {

                jsonExplorer.Nodes.Clear();
                richTextBox1.Clear();
                jar_file = fd.FileName;
                button1.Text = "載入中...";
                button1.Enabled = false;
                jsonExplorer.Enabled = false;
                new Thread(() =>
                {
                    parse_jar_struct();

                    Invoke(new MethodInvoker(() =>
                    {
                        button1.Text = "開啟";
                        button1.Enabled = true;
                        jsonExplorer.Enabled = true;
                    }));

                }).Start();

            }
        }

        public string jar_file = "";
        public void parse_jar_struct()
        {


            zip = new ZipFile( jar_file );
            List<string> items = new List<string>();

            Dictionary<string, bool> attr = new Dictionary<string, bool>();
            foreach (ZipEntry entry in zip)
            {
                if (entry.IsFile)
                {
                    attr.Add(entry.Name, false);
                    items.Add(entry.Name);
                }
                if (entry.IsDirectory)
                {
                    attr.Add(entry.Name, true);
                    items.Add(entry.Name);
                }
            }
            items.Sort();

            #region build tree
            Dictionary<string, object> folder_tree = new Dictionary<string, object>();
            foreach (string i in items)
            {

                if (attr[i] == true) // folder
                {
                    List<string> folder_items = i.Split(new char[] { '/' }).ToList();

                    Dictionary<string, object> folder_tree_temp = new Dictionary<string, object>();

                    folder_tree_temp = folder_tree as Dictionary<string, object>;
                    folder_tree_temp = folder_tree;

                    foreach (string f in folder_items)
                    {
                        if (f == "")
                            break;
                        if (!folder_tree_temp.ContainsKey(f))
                        {
                            folder_tree_temp.Add(f, new Dictionary<string, object>());
                        }
                        folder_tree_temp = folder_tree_temp[f] as Dictionary<string, object>;
                    }
                }
                else //file
                {
                    List<string> folder_items = i.Split(new char[] { '/' }).ToList();
                    Dictionary<string, object> folder_tree_temp = new Dictionary<string, object>();
                    folder_tree_temp = folder_tree as Dictionary<string, object>;

                    string file = folder_items[folder_items.Count() - 1];

                    folder_items.RemoveAt(folder_items.Count() - 1);

                    foreach (string f in folder_items)
                    {
                        if (!folder_tree_temp.ContainsKey(f))
                            folder_tree_temp.Add(f, new Dictionary<string, object>());
                        folder_tree_temp = folder_tree_temp[f] as Dictionary<string, object>;
                    }
                    folder_tree_temp.Add(file, null);
                }
            }

            BuildTreeJAR(JsonConvert.SerializeObject(folder_tree), new FileInfo(jar_file).Name );

            #endregion
        }

        struct bytecode
        {
            public string opcode_name;
            public int byte_length;
        }

        Dictionary<int, bytecode> bytecode_table = new Dictionary<int, bytecode>();
        public void parse_bytecode_list()
        {
            string table_file = Application.StartupPath + "\\bytecodelist.csv";
            List<string> file_line = File.ReadAllLines(table_file).ToList();

            foreach (string i in file_line)
            {
                bytecode b = new bytecode();
                List<string> inf = i.Split(new char[] { ',' }).ToList();
                b.opcode_name = inf[1].Replace("\"", "");
                b.byte_length = int.Parse(inf[2]);
                int op_index = Convert.ToInt32(inf[0], 16);
                bytecode_table.Add(op_index, b);
            }
        }

        public void BuildTreeJAR(string json_str, string name)
        {
            Stopwatch st = new Stopwatch();
            st.Restart();
            jar_name = name;

            TreeNode t = Json2TreeJar(JObject.Parse(json_str), name);

            Invoke(new MethodInvoker(() =>
            {
                jsonExplorer.Nodes.Clear();
                jsonExplorer.Nodes.Add(t);
                jsonExplorer.Nodes[0].Text = name;

            }));


            JarClassParese(jsonExplorer.Nodes);


            st.Stop();
            Console.WriteLine("Jar tree build cost time : " + st.ElapsedMilliseconds + " ms");
        }

        public void BuildTree(string json_str, string name)
        {
            Stopwatch st = new Stopwatch();
            st.Restart();
            jsonExplorer.Nodes.Clear();
            jsonExplorer.Nodes.Add(Json2Tree(JObject.Parse(json_str)));
            jsonExplorer.Nodes[0].Text = name;
            st.Stop();
            Console.WriteLine("tree build cost time : " + st.ElapsedMilliseconds + " ms");
        }

        //ref http://stackoverflow.com/questions/1080442/how-to-convert-an-stream-into-a-byte-in-c
        public static byte[] ReadToEnd(Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }
        string jar_name = "";
        public void JarClassParese(TreeNodeCollection tnc)
        {
            foreach (TreeNode t in tnc)
            {
                if (t.FullPath.EndsWith(".class"))
                {
                    AprJVM myjvm_ = new AprJVM();
                    ZipEntry ze = zip.GetEntry(t.FullPath.Remove(0, jar_name.Length + 1).Replace("\\", "/"));

                    myjvm_.parse_class_struct(ReadToEnd(zip.GetInputStream(ze)));

                    string json_str = JsonConvert.SerializeObject(myjvm_.classfile);

                    TreeNode tnode = Json2Tree(JObject.Parse(json_str));
                    
                    Invoke(new MethodInvoker(() =>
                    {
                        foreach (TreeNode s in tnode.Nodes)
                            t.Nodes.Add(s);
                    }));

                }
                JarClassParese(t.Nodes);
            }
        }
        private TreeNode Json2TreeJar(JObject obj, string name)
        {
            TreeNode parent = new TreeNode();
            parent.Text = name;
            foreach (var token in obj)
            {
                TreeNode child = new TreeNode();
                child.Text = token.Key.ToString();
                if (token.Value.Type.ToString() == "Object")
                {
                    JObject o = (JObject)token.Value;
                    child = Json2TreeJar(o, token.Key);
                    parent.Nodes.Add(child);
                }
                else if (token.Value.Type.ToString() == "Array")
                {
                    int ix = -1;
                    foreach (var itm in token.Value)
                    {
                        if (itm.Type.ToString() == "Object")
                        {
                            TreeNode objTN = new TreeNode();
                            ix++;
                            JObject o = (JObject)itm;
                            objTN = Json2TreeJar(o, token.Key);
                            objTN.Text = token.Key.ToString() + " [" + ix + "]";
                            child.Nodes.Add(objTN);
                        }
                        else if (itm.Type.ToString() == "Array")
                        {
                            ix++;
                            TreeNode dataArray = new TreeNode();
                            foreach (var data in itm)
                            {
                                dataArray.Text = token.Key.ToString() + "[" + ix + "]";
                                dataArray.Nodes.Add(data.ToString());
                            }
                            child.Nodes.Add(dataArray);
                        }
                        else
                            child.Nodes.Add(itm.ToString());

                    }
                    parent.Nodes.Add(child);
                }
                else
                    parent.Nodes.Add(child);

            }
            return parent;
        }

        // http://stackoverflow.com/questions/18769634/creating-tree-view-dynamically-according-to-json-text-in-winforms
        private TreeNode Json2Tree(JObject obj)
        {
            TreeNode parent = new TreeNode();
            foreach (var token in obj)
            {
                parent.Text = token.Key;
                TreeNode child = new TreeNode();
                child.Text = token.Key.ToString();
                if (token.Value.Type.ToString() == "Object")
                {
                    JObject o = (JObject)token.Value;
                    child = Json2Tree(o);
                    parent.Nodes.Add(child);
                }
                else if (token.Value.Type.ToString() == "Array")
                {
                    int ix = -1;
                    if (token.Value.Count() == 0)                    
                        child.Text = child.Text + " : N/A";
                    
                    foreach (var itm in token.Value)
                    {
                        if (itm.Type.ToString() == "Object")
                        {
                            TreeNode objTN = new TreeNode();
                            ix++;
                            JObject o = (JObject)itm;
                            objTN = Json2Tree(o);
                            objTN.Text = token.Key.ToString() + "[" + ix + "]";
                            child.Nodes.Add(objTN);
                        }
                        else if (itm.Type.ToString() == "Array")
                        {
                            ix++;
                            TreeNode dataArray = new TreeNode();
                            foreach (var data in itm)
                            {
                                dataArray.Text = token.Key.ToString() + "[" + ix + "]";
                                dataArray.Nodes.Add(data.ToString());
                            }
                            child.Nodes.Add(dataArray);
                        }

                        else
                        {
                            child.Nodes.Add(itm.ToString());
                        }
                    }
                    parent.Nodes.Add(child);
                }
                else
                {
                    string v = "";

                    if (token.Value.ToString() == "")
                        v = child.Text + " : N/A";
                    else
                        v = child.Text + " : " + token.Value.ToString();
                    child.Text = v;
                    parent.Nodes.Add(child);
                }
            }
            return parent;
        }

        private void btn_ExpandAll_Click(object sender, EventArgs e)
        {
            jsonExplorer.ExpandAll();
        }

        private void btn_CollapseAll_Click(object sender, EventArgs e)
        {
            jsonExplorer.CollapseAll();
        }


        private void jsonExplorer_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!e.Node.Text.StartsWith("code : ") && !e.Node.Text.StartsWith("methods["))
            {
                richTextBox1.Clear();
                return;
            }

            string temp = "";
            string method_name = "";

            if (e.Node.Text.StartsWith("methods["))
            {
                try
                {
                    if (e.Node.Nodes[4].Nodes[0].Nodes[2].Nodes[5].Text != null)
                        temp = e.Node.Nodes[4].Nodes[0].Nodes[2].Nodes[5].Text;
                }
                catch
                {
                    richTextBox1.Text = "No code part found !";
                    return;
                }
                method_name = e.Node.Parent.Parent.Nodes[4].Nodes[int.Parse(e.Node.Nodes[1].Text.Remove(0, "name_index : ".Length)) - 1].Nodes[3].Text;
                method_name = method_name.Remove(0, "bytes_str : ".Length);
            }

            if (e.Node.Text.StartsWith("code : "))
            {
                method_name = e.Node.Parent.Parent.Parent.Parent.Parent.Parent.Nodes[4].Nodes[int.Parse(e.Node.Parent.Parent.Parent.Parent.Nodes[1].Text.Remove(0, "name_index : ".Length)) - 1].Nodes[3].Text;
                method_name = method_name.Remove(0, "bytes_str : ".Length);
                temp = e.Node.Text;
            }

            string byte_str = temp.Remove(0, "code : ".Length);
            byte[] array = Convert.FromBase64String(byte_str);

            string dis_str = "Method Name : " + method_name + "\n\n";
            for (int i = 0; i < array.Length; )
            {
                if (bytecode_table.Keys.Contains(array[i]))
                {
                    switch (bytecode_table[array[i]].byte_length)
                    {
                        case 1:
                            dis_str += i.ToString("x8") + "    " + bytecode_table[array[i]].opcode_name + "\n";
                            break;

                        case 2:
                            dis_str += i.ToString("x8") + "    " + bytecode_table[array[i]].opcode_name + " " + array[i + 1].ToString("x2") + "\n";
                            break;

                        case 3:
                            dis_str += i.ToString("x8") + "    " + bytecode_table[array[i]].opcode_name + " " + array[i + 1].ToString("x2") + " , " + array[i + 2].ToString("x2") + "\n";
                            break;

                        case 4:
                            dis_str += i.ToString("x8") + "    " + bytecode_table[array[i]].opcode_name + " " + array[i + 1].ToString("x2") + " , " + array[i + 2].ToString("x2") + " , " + array[i + 3].ToString("x2") + "\n";
                            break;

                        case 5:
                            dis_str += i.ToString("x8") + "    " + bytecode_table[array[i]].opcode_name + " " + array[i + 1].ToString("x2") + " , " + array[i + 2].ToString("x2") + " , " + array[i + 3].ToString("x2") + " , " + array[i + 4].ToString("x2") + "\n";
                            break;
                    }
                    i += bytecode_table[array[i]].byte_length;
                }
                else
                {
                    dis_str += i.ToString("x8") + "    " + "unknow opcde" + "\n";
                    i++;
                }
            }
            richTextBox1.Text = dis_str;
        }

        private void btn_exec_Click(object sender, EventArgs e)
        {
            if (myjvm.parse_classfile_ok)
                myjvm.Execute();
            else
                MessageBox.Show("JVM 尚未載入有效Class內容 !");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            char a = 'a';
            Console.WriteLine(a);

        }


    }
}
