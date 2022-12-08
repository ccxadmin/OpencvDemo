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
    ///Canny模板匹配数据
    /// </summary>
    [Serializable]
   public  class CannyTemplateMatchData : ParmasDataBase, IParmaData
    {
        public CannyTemplateMatchData()
        {         
            tpl = new Mat();
            mask = new Mat();
            StartAngle = -90;
            AngleRange = 180;
            SegmentThresh = 100;
            NumLevels = 3;
            MatchScore = 0.5;
        }
        public CannyTemplateMatchData(string filePath) :base(filePath)
        {
            FileDir = filePath;
            tpl = new Mat();
            mask = new Mat();
            StartAngle = -90;
            AngleRange = 180;
            SegmentThresh = 100;
            NumLevels = 3;
            MatchScore = 0.5;
        }

        /// <summary>
        /// 模板图像
        /// </summary>
        [NonSerialized]
        public Mat tpl;
        /// <summary>
        /// 掩膜
        /// </summary>
        [NonSerialized]
        public Mat mask;

        [DefaultValue(-90)]
        /// <summary>
        /// 起始角度
        /// </summary>
        public double StartAngle { get; set; } = -90;

        [DefaultValue(180)]
        /// <summary>
        /// 角度范围
        /// </summary>
        public double AngleRange { get; set; } = 180;

        /// <summary>
        /// 分割阈值
        /// </summary>
        [DefaultValue(100)]      
        public double SegmentThresh { get; set; } = 100;

        /// <summary>
        /// 金字塔层数
        /// </summary>
        [DefaultValue(3)]
        public int NumLevels { get; set; } = 3;

        /// <summary>
        /// 匹配得分
        /// </summary>
        [DefaultValue(0.5)]   
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
                string templatePath = fileDir + "\\Canny模板.png";
                if (File.Exists(templatePath))
                    this.tpl = MatDataWriteRead.ReadImage(templatePath);
                else
                    errInfoList.Add("Canny模板图像不存在");
                string maskPath = fileDir + "\\Canny掩膜.png";
                if (File.Exists(maskPath))
                    this.mask = MatDataWriteRead.ReadImage(maskPath);
                else
                    errInfoList.Add("未创建Canny掩膜");
                string configPath = fileDir + "\\Canny模板参数.ini";
                if (File.Exists(configPath))
                {
                    this.StartAngle = double.Parse(GeneralUse.ReadValue("Canny模板", "起始角度", "Canny模板参数",
                                              "-90", fileDir));
                    this.AngleRange = double.Parse(GeneralUse.ReadValue("Canny模板", "角度范围", "Canny模板参数",
                                              "180", fileDir));
                    this.SegmentThresh = double.Parse(GeneralUse.ReadValue("Canny模板", "分割阈值", "Canny模板参数",
                                            "100", fileDir));
                    this.NumLevels = int.Parse(GeneralUse.ReadValue("Canny模板", "金字塔层数", "Canny模板参数",
                          "3", fileDir));                
                    this.MatchScore = double.Parse(GeneralUse.ReadValue("Canny模板", "匹配得分", "Canny模板参数",
                                              "0.5", fileDir));              
                }
                else
                   errInfoList.Add("Canny模板参数配置文件不存在");
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
                string templatePath = fileDir + "\\Canny模板.png";
                if (!this.tpl.Empty())
                    MatDataWriteRead.WriteImage(templatePath, this.tpl);
                string maskPath = fileDir + "\\Canny掩膜.png";
                if(!this.mask.Empty())
                 MatDataWriteRead.WriteImage(maskPath, this.mask);
                GeneralUse.WriteValue("Canny模板", "起始角度", this.StartAngle.ToString("f3"),
                          "Canny模板参数", fileDir);
                GeneralUse.WriteValue("Canny模板", "角度范围", this.AngleRange.ToString("f3"),
                        "Canny模板参数", fileDir);
                GeneralUse.WriteValue("Canny模板", "分割阈值", this.SegmentThresh.ToString("f3"),
                     "Canny模板参数", fileDir);
                GeneralUse.WriteValue("Canny模板", "金字塔层数", this.NumLevels.ToString("f3"),
                         "Canny模板参数", fileDir);
                GeneralUse.WriteValue("Canny模板", "匹配得分", this.MatchScore.ToString("f3"),
                      "Canny模板参数", fileDir);
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
