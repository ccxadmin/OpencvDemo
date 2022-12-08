using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FilesRAW.Common;
using System.ComponentModel;

namespace ParamDataLib.Location
{
    /// <summary>
    ///模板匹配数据
    /// </summary>
    [Serializable]
   public  class TemplateMatchData:ParmasDataBase, IParmaData
    {
        public TemplateMatchData()
        {
          
            tpl = new Mat();
            mask = new Mat();
            StartAngle = -50;
            EndAngle = 50;
            MatchScore = 0.5;
        }
        public TemplateMatchData(string filePath) :base(filePath)
        {
           
            tpl = new Mat();
            mask = new Mat();
            StartAngle = -50;
            EndAngle = 50;
            MatchScore = 0.5;
        }
  
        /// <summary>
        /// 模板图像
        /// </summary>
        public Mat tpl { get; set; }
        /// <summary>
        /// 掩膜
        /// </summary>
        public Mat mask { get; set; }
        [DefaultValue(-50)]
        /// <summary>
        /// 起始角度
        /// </summary>
        public double StartAngle { get; set; } = -50;
        [DefaultValue(50)]
        /// <summary>
        /// 终止角度
        /// </summary>
        public double EndAngle { get; set; } = 50;
        /// <summary>
        /// 匹配得分
        /// </summary>
        public double MatchScore { get; set; } = 0.5;
        /// <summary>
        /// 参数加载
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileDir"></param>
        /// <returns></returns>
        public override bool LoadData(string fileDir = "")
        {
            try
            {
                if (fileDir == "") fileDir = base.FileDir;
                if (!Directory.Exists(fileDir))
                {
                    Directory.CreateDirectory(fileDir);
                    errInfoList.Add(fileDir+"路径不存在");
                }               
                string templatePath = fileDir + "\\模板.png";
                if (File.Exists(templatePath))
                    this.tpl = MatDataWriteRead.ReadImage(templatePath);
                else
                    errInfoList.Add("模板图像不存在");
                string maskPath = fileDir + "\\掩膜.png";
                if (File.Exists(maskPath))
                    this.mask = MatDataWriteRead.ReadImage(maskPath);
                else
                    errInfoList.Add("未创建掩膜");
                string configPath = fileDir + "\\模板参数.ini";
                if (File.Exists(configPath))
                {
                    this.StartAngle = double.Parse(GeneralUse.ReadValue("模板", "起始角度", "模板参数",
                                              "-50", fileDir));
                    this.EndAngle = double.Parse(GeneralUse.ReadValue("模板", "终止角度", "模板参数",
                                              "50", fileDir));
                    this.MatchScore = double.Parse(GeneralUse.ReadValue("模板", "匹配得分", "模板参数",
                                              "0.5", fileDir));              
                }
                else
                   errInfoList.Add("模板参数配置文件不存在");
            }
            catch (Exception er)
            {
                errInfoList.Clear();
                errInfoList.Add(er.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 参数保存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="fileDir"></param>
        /// <returns></returns>
        public override bool SaveData( string fileDir = "")
        {
            try
            {
                if (fileDir == "") fileDir = base.FileDir;
                if (!Directory.Exists(fileDir))
                {
                    Directory.CreateDirectory(fileDir);
                    errInfoList.Add(fileDir + "路径不存在");
                }
                string templatePath = fileDir + "\\模板.png";
                if (!this.tpl.Empty())
                    MatDataWriteRead.WriteImage(templatePath, this.tpl);
                string maskPath = fileDir + "\\掩膜.png";
                if(!this.mask.Empty())
                 MatDataWriteRead.WriteImage(maskPath, this.mask);
                GeneralUse.WriteValue("模板", "起始角度", this.StartAngle.ToString("f3"),
                          "模板参数", fileDir);
                GeneralUse.WriteValue("模板", "终止角度", this.EndAngle.ToString("f3"),
                        "模板参数", fileDir);
                GeneralUse.WriteValue("模板", "匹配得分", this.MatchScore.ToString("f3"),
                      "模板参数", fileDir);
            }
            catch (Exception er)
            {
                errInfoList.Clear();
                errInfoList.Add(er.Message);
                return false;
            }
            return true;
        }     
        /// <summary>
        /// 回去当前实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetData<T>() where T : class, IParmaData
        {
            return this as T;
        }
    }
}
