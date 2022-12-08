using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using FilesRAW.Common;
using FilesRAW.xml;
using System.IO;


namespace visionForm
{
    public partial class frmrecipe : Form
    {

        private frmrecipe()
        {
            InitializeComponent();

        }
        //单例模式
        private static frmrecipe _MDIParent1 = null;
        public static frmrecipe CreateInstance()
        {
            if (_MDIParent1 == null)
                _MDIParent1 = new frmrecipe();

            return _MDIParent1;
        }

        XmlFile xf;
        //string filepath = AppDomain.CurrentDomain.BaseDirectory + "Config\\配方文件.recp";

        //新建默认记录
        void CreateDefaultRecord()
        {
            dataRecipe newdataRecipe = new dataRecipe();
            DataGridViewRow dr = new DataGridViewRow();
            dr.CreateCells(this.dataGridView1);//给行添加单元格            
            dataRecipe.dataToRow(newdataRecipe, ref dr);
            dataGridView1.Rows.Add(dr);
            dataGridView1.Update();
        }
        //删除记录
        void DeleteRecord(int RowIndex)
        {
            if (RowIndex < 0) return;
            dataGridView1.Rows.Remove(dataGridView1.Rows[RowIndex]);
            dataGridView1.Update();
        }

        void LoadRecipeFile()
        {
            bool flag = xf.LoadXml();
            if (!flag)
            {
                xf = new XmlFile();
                MessageBox.Show("配方配置xml文件不存在！");
                return;
            }
            XmlNodeList readList = xf.GetNodeList("root");
            if (readList.Count > 0) dataGridView1.Rows.Clear();

            List<string> recipeNameList = new List<string>();
            for (int i = 0; i < readList.Count; i++)
            {
                var s = readList[i].ChildNodes;
                //防止重名
                if (recipeNameList.Contains(s[0].InnerText))
                    continue;
                recipeNameList.Add(s[0].InnerText);

                DataGridViewRow dr = new DataGridViewRow();
                dr.CreateCells(this.dataGridView1);//给行添加单元格            
                dataRecipe.dataToRow(new string[3] {s[0].InnerText,
                s[1].InnerText,
                s[2].InnerText

                }, ref dr);
                dataGridView1.Rows.Add(dr);
            }
            dataGridView1.Update();

        }

        //新建
        private void CreateANewRecipe_Click(object sender, EventArgs e)
        {
            CreateDefaultRecord();
        }

        public bool CreateRecipe(string recipeName, bool isReplace)
        {
            if (dataGridView1.InvokeRequired)
            {
                dataGridView1.Invoke(new Func<string, bool, bool>(CreateRecipe), recipeName, isReplace);
            }
            else
            {

                for (int j = 0; j < dataGridView1.RowCount; j++)
                {
                    if (dataGridView1.Rows[j].Cells[0].Value.ToString() == recipeName)
                    {
                        if (isReplace)
                            return true;
                        else
                            return false;
                    }
                }
                dataRecipe newdataRecipe = new dataRecipe();
                newdataRecipe.recipeName = recipeName;
                DataGridViewRow dr = new DataGridViewRow();
                dr.CreateCells(this.dataGridView1);//给行添加单元格            
                dataRecipe.dataToRow(newdataRecipe, ref dr);
                dataGridView1.Rows.Add(dr);
                dataGridView1.Update();
            }
            return true;
        }
        //打开配方文件夹
        private void OpenFile_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();     
            dialog.Description = "请选择需要加载的配方文件";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string foldPath = dialog.SelectedPath;
                string[] Add_recipeNames = foldPath.Split('\\');
                string Add_recipeName = Add_recipeNames[Add_recipeNames.Length - 1];
               bool flag= AddRecipeFile(foldPath, Add_recipeName,true);
                if (flag)
                {
                    string usedRecipeName = SaveRecipe();
                    if (usedRecipeName != "")
                        MessageBox.Show("配方加载成功！");
                    else
                        MessageBox.Show("配方加载失败或已被取消！");
                }
                else
                    MessageBox.Show("配方加载失败或已被取消！");
            }    
        }

      
        public bool AddRecipeFile(string path,string recipeName, 
            bool IsUse = true, bool isReplace=true)
        {
            try
            {
                if (!Directory.Exists(path)) return false;
                //string[] Add_recipeNames = path.Split('\\');
                //string Add_recipeName = Add_recipeNames[Add_recipeNames.Length - 1];
                List<string> DirectoryNameList = new List<string>();
                List<string> FileNameList = new List<string>();
                DirectoryInfo di = new DirectoryInfo(path);
                DirectoryInfo[] diList = di.GetDirectories();
                FileInfo[] fiList = di.GetFiles();
                foreach (var s in diList)
                    DirectoryNameList.Add(s.Name);
                foreach (var s in fiList)
                    FileNameList.Add(s.Name);


                if (!Directory.Exists(path))
                {
                    MessageBox.Show("原配方文件不存在！");
                    return false;
                }

                bool createflag = CreateRecipe(recipeName, isReplace);
                if (!createflag)
                {
                    MessageBox.Show("添加配方失败，存在同名配方！");
                    return false;
                }

                string dircpath = AppDomain.CurrentDomain.BaseDirectory + "配方";
                //如果需要替换
                if (isReplace)
                {
                    bool flag = CopyDirectory(path, dircpath + "\\" + recipeName, true);
                    if (!flag) return false;
                }
                return true;

            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取配方名称
        /// </summary>
        /// <returns></returns>
        public List<string> GetRecipeName(ref string msg)
        {
            List<string> temNameList = new List<string>();
            bool flag = xf.LoadXml();
            if (!flag)
            {
                //xf = new XmlFile();
                //  MessageBox.Show("文件不存在！");
                msg = string.Format("配方文件：{0}加载失败！", xf._Path);
                return temNameList;
            }
            XmlNodeList readList = xf.GetNodeList("root");

            for (int i = 0; i < readList.Count; i++)
            {
                var s = readList[i].ChildNodes;
                temNameList.Add(s[0].InnerText);
            }
            return temNameList;
        }


        /// <summary>
        /// 配方切换
        /// recipename=配方名称
        /// </summary>
        /// <param name="recipename"></param>
        public void SwitchRecipe(string recipename)
        {
            if (dataGridView1.InvokeRequired)
            {
                dataGridView1.Invoke(new Action<string>(SwitchRecipe), recipename);
            }
            else
            {
                int index = -1;
                for (int j = 0; j < dataGridView1.RowCount; j++)
                {
                    if (dataGridView1.Rows[j].Cells[0].Value.ToString() == recipename)
                    {
                        index = j;
                        break;
                    }
                }
                if (index == -1)
                {
                    MessageBox.Show("需要切换的配方名称不存在，请确认！");
                    return;
                }
                for (int j = 0; j < dataGridView1.RowCount; j++)
                {
                    dataGridView1.Rows[j].Cells[1].Value = false;

                }
                dataGridView1.Rows[index].Cells[1].Value = true;

                //string dircpath = AppDomain.CurrentDomain.BaseDirectory + "配方";
                //string saveToUsePath = string.Empty;
                //for (int i = 0; i < dataGridView1.RowCount; i++)
                //{
                //    string parentName = string.Format("配方{0}", i + 1);
                //    string[] recipeContent = dataRecipe.dataToRecipe2(dataGridView1.Rows[i]);
                //    // xf.AddNode(parentName, "序号", recipeContent[0]);
                //    xf.AddNode(parentName, "配方名称", recipeContent[0]);
                //    if (i != 0)
                //    {
                //        if (!Directory.Exists(dircpath + "\\" + recipeContent[0]))
                //        {
                //            Directory.CreateDirectory(dircpath + "\\" + recipeContent[0]);
                //            CopyDirectory(dircpath + "\\default", dircpath + "\\" + recipeContent[0], true);
                //        }

                //    }

                //    xf.AddNode(parentName, "启用", recipeContent[1]);
                //    xf.AddNode(parentName, "是否删除", recipeContent[2]);
                //    if (bool.Parse(recipeContent[1]))
                //        saveToUsePath = dircpath + "\\" + recipeContent[0];
                //}
                //xf._Path = AppDomain.CurrentDomain.BaseDirectory + "Config\\配方文件.xml";
                //xf.SaveXmlFile();
                //GeneralUse.WriteValue("配方", "使用路径", saveToUsePath, "config");             
            }
        }

        public EventHandler RecipeSaveHandle;
        List<string> recipeList = new List<string>();
        //保存
        private void SaveRecipes_Click(object sender, EventArgs e)
        {
            xf.clear();
            List<bool> falglist = new List<bool>();
            for (int j = 0; j < dataGridView1.RowCount; j++)
            {
                if ((bool)dataGridView1.Rows[j].Cells[1].EditedFormattedValue)
                    falglist.Add(true);
            }
            if (falglist.Count != 1)
            {
                MessageBox.Show("保存失败：启用配方只允许选择一个！", "Information", MessageBoxButtons.OK,
                       MessageBoxIcon.Error);
                return;
            }
            string dircpath = AppDomain.CurrentDomain.BaseDirectory + "配方";
            string saveToUsePath = string.Empty;
            recipeList.Clear();
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {

                string parentName = string.Format("配方{0}", i);
                string[] recipeContent = dataRecipe.dataToRecipe2(dataGridView1.Rows[i]);
                // xf.AddNode(parentName, "序号", recipeContent[0]);
                xf.AddNode(parentName, "配方名称", recipeContent[0]);
                if (!recipeList.Contains(recipeContent[0]))
                    recipeList.Add(recipeContent[0]);
                else
                {
                    MessageBox.Show("存在相同配方名称请确认！", "Information",
                                 MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!Directory.Exists(dircpath + "\\" + recipeContent[0]))
                {
                    Directory.CreateDirectory(dircpath + "\\" + recipeContent[0]);
                    CopyDirectory(dircpath + "\\default", dircpath + "\\" + recipeContent[0], true);
                }

                xf.AddNode(parentName, "启用", recipeContent[1]);
                xf.AddNode(parentName, "是否删除", recipeContent[2]);
                if (bool.Parse(recipeContent[1]))
                    saveToUsePath = dircpath + "\\" + recipeContent[0];
            }
            xf._Path = AppDomain.CurrentDomain.BaseDirectory + "Config\\配方文件.xml";
            xf.SaveXmlFile();
                    
            DirectoryInfo di = new DirectoryInfo(dircpath);
            DirectoryInfo[] fi = di.GetDirectories();
            for (int i = 0; i < fi.Length; i++)
            {
                bool flag = false;
                foreach (var s in recipeList)
                {
                    if (fi[i].Name == s)
                    {
                        flag = true;
                        break;
                    }

                }
                if (flag)  
                    continue;
                else   
                {
                    if (Directory.Exists(dircpath + "\\" + fi[i].Name)&&
                                 !fi[i].Name.ToUpper().Contains("DEFAULT"))//default文件不可删除
                        Directory.Delete(dircpath + "\\" + fi[i].Name, true);
                }

            }
            MessageBox.Show("保存成功！");
            string[] arraystring = saveToUsePath.Split('\\');
            GeneralUse.WriteValue("配方", "使用配方名称", arraystring[arraystring.Length - 1], "config");
            if (RecipeSaveHandle != null)
                RecipeSaveHandle(saveToUsePath, null);
           
        }

        public string SaveRecipe()
        {
            string usedRecipePath = "";
            if (dataGridView1.InvokeRequired)
            {
                usedRecipePath = dataGridView1.Invoke(new Func<string>(SaveRecipe)).ToString();
                return usedRecipePath;
            }
            else
            {
                xf.clear();
                List<bool> falglist = new List<bool>();
                for (int j = 0; j < dataGridView1.RowCount; j++)
                {
                    if ((bool)dataGridView1.Rows[j].Cells[1].EditedFormattedValue)
                        falglist.Add(true);
                }
                if (falglist.Count != 1)
                {
                    //MessageBox.Show("保存失败：启用配方只允许选择一个！", "Information", MessageBoxButtons.OK,
                    //       MessageBoxIcon.Error);
                    return "";
                }
                string dircpath = AppDomain.CurrentDomain.BaseDirectory + "配方";
                //string saveToUsePath = string.Empty;
                recipeList.Clear();
                for (int i = 0; i < dataGridView1.RowCount; i++)
                {

                    string parentName = string.Format("配方{0}", i);
                    string[] recipeContent = dataRecipe.dataToRecipe2(dataGridView1.Rows[i]);
                    // xf.AddNode(parentName, "序号", recipeContent[0]);
                 
                    if (!recipeList.Contains(recipeContent[0]))
                        recipeList.Add(recipeContent[0]);
                    else
                    {
                        //MessageBox.Show("存在相同配方名称请确认！", "Information",
                        //             MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return "";
                    }

                    if (!Directory.Exists(dircpath + "\\" + recipeContent[0]))
                    {
                        Directory.CreateDirectory(dircpath + "\\" + recipeContent[0]);
                        CopyDirectory(dircpath + "\\default", dircpath + "\\" + recipeContent[0], true);
                    }
                    xf.AddNode(parentName, "配方名称", recipeContent[0]);
                    xf.AddNode(parentName, "启用", recipeContent[1]);
                    xf.AddNode(parentName, "是否删除", recipeContent[2]);
                    if (bool.Parse(recipeContent[1]))
                        usedRecipePath = dircpath + "\\" + recipeContent[0];
                }
                xf._Path = AppDomain.CurrentDomain.BaseDirectory + "\\Config\\配方文件.xml";
                xf.SaveXmlFile();
                //MessageBox.Show("保存成功！");
                string[] arraystring = usedRecipePath.Split('\\');
                GeneralUse.WriteValue("配方", "使用配方名称", arraystring[arraystring.Length - 1], "config");
              
                return usedRecipePath;

            }
        }
        /// <summary>
        /// 文件夹下所有内容copy
        /// </summary>
        /// <param name="SourcePath">要Copy的文件夹</param>
        /// <param name="DestinationPath">要复制到哪个地方</param>
        /// <param name="overwriteexisting">是否覆盖</param>
        /// <returns></returns>
        private static bool CopyDirectory(string SourcePath, string DestinationPath, bool overwriteexisting)
        {
            bool ret = false;
            try
            {
                SourcePath = SourcePath.EndsWith(@"\") ? SourcePath : SourcePath + @"\";
                DestinationPath = DestinationPath.EndsWith(@"\") ? DestinationPath : DestinationPath + @"\";

                if (Directory.Exists(SourcePath))
                {
                    if (Directory.Exists(DestinationPath) == false)
                        Directory.CreateDirectory(DestinationPath);

                    foreach (string fls in Directory.GetFiles(SourcePath))
                    {
                        FileInfo flinfo = new FileInfo(fls);
                        flinfo.CopyTo(DestinationPath + flinfo.Name, overwriteexisting);
                    }
                    foreach (string drs in Directory.GetDirectories(SourcePath))
                    {
                        DirectoryInfo drinfo = new DirectoryInfo(drs);
                        if (CopyDirectory(drs, DestinationPath + drinfo.Name, overwriteexisting) == false)
                            ret = false;
                    }
                }
                ret = true;
            }
            catch (Exception ex)
            {
                ret = false;
            }
            return ret;
        }

        public bool shouldHide { get; set; } = false;
        private void MDIParent1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //重复校验
            recipeList.Clear();
            List<bool> falglist = new List<bool>();
            for (int j = 0; j < dataGridView1.RowCount; j++)
            {
                if ((bool)dataGridView1.Rows[j].Cells[1].EditedFormattedValue)
                    falglist.Add(true);
                string[] recipeContent = dataRecipe.dataToRecipe2(dataGridView1.Rows[j]);
                if (!recipeList.Contains(recipeContent[0]))
                    recipeList.Add(recipeContent[0]);
                else
                {
                    MessageBox.Show("存在相同配方名称请确认！", "Information",
                                 MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }
            }

            if (falglist.Count != 1)
            {
                MessageBox.Show("请勾选一个配方并进行保存！", "Information", MessageBoxButtons.OK,
                       MessageBoxIcon.Error);
                e.Cancel = true;
                return;
            }
            if (shouldHide)
            {
                this.Dispose();
                _MDIParent1 = null;
            }
            else
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void dataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            //删除操作
            if (e.ColumnIndex == 2)
            {
                string[] recipeContent = dataRecipe.dataToRecipe2(dataGridView1.Rows[e.RowIndex]);
                if (recipeContent[0] == "00000")
                {
                    MessageBox.Show("配方名称[00000]为系统设置默认，无法被删除！");
                    return;
                }

                if (dataGridView1[e.ColumnIndex, e.RowIndex].GetType() != typeof(DataGridViewButtonCell))
                    return;
                //表格信息删除
                DeleteRecord(e.RowIndex);
            }
            //check操作
            else if (e.ColumnIndex == 1)
            {
                if (dataGridView1[e.ColumnIndex, e.RowIndex].GetType() != typeof(DataGridViewCheckBoxCell))
                    return;
                //dataGridView1[e.ColumnIndex, e.RowIndex].Value = true;


                ((DataGridViewCheckBoxCell)this.dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex]).Value = true;//选择当前行的CheckBox 并空置其他的行
                for (int i = 0; i < this.dataGridView1.Rows.Count; i++)
                {
                    if (i != e.RowIndex)
                    {
                        ((DataGridViewCheckBoxCell)this.dataGridView1.Rows[i].Cells[e.ColumnIndex]).Value = false;
                    }
                }
                this.dataGridView1.Rows[e.RowIndex].Selected = true;

                dataGridView1.Update();
            }
        
        }

        public void DeleteRecipe(string recipeName)
        {
            if (dataGridView1.InvokeRequired)
            {
                dataGridView1.Invoke(new Action<string>(DeleteRecipe), recipeName);
            }
            else
            {
                if (recipeName == "00000")
                {
                    MessageBox.Show("配方名称[00000]为系统设置默认，无法被删除！");
                    return;
                }
                int index = -1;
                for (int j = 0; j < dataGridView1.RowCount; j++)
                {
                    if (dataGridView1.Rows[j].Cells[0].Value.ToString() == recipeName)
                    {
                        index = j;
                        break;
                    }
                }
                if (index == -1)
                {
                    MessageBox.Show("需要删除的配方名称不存在，请确认！");
                    return;
                }

                //删除对应的文件夹
                string dircpath = AppDomain.CurrentDomain.BaseDirectory + "配方";
                if (Directory.Exists(dircpath + "\\" + recipeName))
                    Directory.Delete(dircpath + "\\" + recipeName, true);
                //表格信息删除
                DeleteRecord(index);
            }
        }

        private void frmrecipe_Load(object sender, EventArgs e)
        {
            //CreateDefaultRecord();

            xf = new XmlFile(AppDomain.CurrentDomain.BaseDirectory + "Config\\配方文件.xml");
            LoadRecipeFile();
            if (dataGridView1.RowCount > 0)
                //第一行为默认值不允许编辑
                dataGridView1.Rows[0].Cells[0].ReadOnly = true;
            this.dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        //另存为
        private void SaveAstoolStripButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择文件存放路径";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string foldPath = dialog.SelectedPath;
                string name = getUesRecipeName();
                if (name == string.Empty) return;
                bool flag = ExportRecipe(foldPath+"\\"+ name, name);//将当时使用得配方文件存储到foldPath下并同名
                if (flag)
                    MessageBox.Show("导出成功！");
                else
                    MessageBox.Show("导出失败！");
            }
        }

        string getUesRecipeName()
        {
            string name = string.Empty;
            for (int j = 0; j < dataGridView1.RowCount; j++)
            {
                if ((bool)dataGridView1.Rows[j].Cells[1].Value)
                {
                    name = dataRecipe.dataToRecipe2(dataGridView1.Rows[j])[0];
                    break;
                }

            }
            return name;
        }

        /// <summary>
        /// 将配方文件recipeName导出到特定文件路径path
        /// path:文件路径
        /// recipeName:配方文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="recipeName">配方文件</param>
        /// <returns></returns>
        public bool ExportRecipe(string path, string recipeName)
        {
            try
            {
                string dircpath = AppDomain.CurrentDomain.BaseDirectory + "配方";
                if (!Directory.Exists(dircpath + "\\" + recipeName)) return false;
                return  CopyDirectory(dircpath + "\\" + recipeName, path, true);
            }
           catch
            {
                return false;
            }                
        }
    }


    [Serializable]
    public class dataRecipe
    {
        public dataRecipe()
        {
          
        }
      //  public int index { get; set; } = 0;
        public string recipeName { get; set; } = "0";
        public bool IsUsing { get; set; } = false;
        public Button deleteButton { get; set; } = new Button() { Text = "删除" };

       static  public void  dataToRow( dataRecipe dr,ref  DataGridViewRow dgrow)
        {
            dgrow.SetValues(
                dr.recipeName,
                 dr.IsUsing,
                 dr.deleteButton);

            //dgrow.Cells[0].Value = dr.index;
            //dgrow.Cells[1].Value = dr.recipeName;
            //dgrow.Cells[2].Value = dr.IsUsing;
            //dgrow.Cells[3].Value = dr.deleteButton;                   

        }

        static public void dataToRow(string[] itemdata,  ref DataGridViewRow dgrow)
        {

            dgrow.SetValues(
                    itemdata[0],
                    bool.Parse(itemdata[1]),
                  new Button() { Text = itemdata[2] });
          
        }

        static public dataRecipe dataToRecipe(DataGridViewRow dgrow)
        {
            dataRecipe d_dataRecipe = null;
            try
            {
                d_dataRecipe = new dataRecipe
                {
                    //index = (int)dgrow.Cells[0].EditedFormattedValue,
                    recipeName = dgrow.Cells[0].EditedFormattedValue.ToString(),
                    IsUsing = (bool)dgrow.Cells[1].EditedFormattedValue,
                    deleteButton = new Button() { Text = dgrow.Cells[2].EditedFormattedValue.ToString() }
                   
                };
            }
            catch
            { }
            return d_dataRecipe;
        }

        static public string[] dataToRecipe2(DataGridViewRow dgrow)
        {

            string[] d_dataRecipe = new string[3]
            {
                    dgrow.Cells[0].EditedFormattedValue.ToString(),
                    dgrow.Cells[1].EditedFormattedValue.ToString(),
                    dgrow.Cells[2].EditedFormattedValue.ToString(),
                   // dgrow.Cells[3].EditedFormattedValue.ToString()
            };
            return d_dataRecipe;
        }
    }
}
