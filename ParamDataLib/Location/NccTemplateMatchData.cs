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
    ///Ncc模板匹配数据
    /// </summary>
    [Serializable]
   public  class NccTemplateMatchData : ParmasDataBase, IParmaData
    {
        public NccTemplateMatchData()
        {
            tpl = new Mat();
            mask = new Mat();
            StartAngle = -90;
            AngleRange = 180;
            AngleStep = 1;
            NumLevels = 3;
            MatchScore = 0.5;
        }
        public NccTemplateMatchData(string filePath) :base(filePath)
        {
            FileDir = filePath;
            tpl = new Mat();
            mask = new Mat();
            StartAngle = -90;
            AngleRange = 180;
            AngleStep = 1;
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

        [DefaultValue(1)]
        /// <summary>
        /// 角度步长
        /// </summary>
        public double AngleStep { get; set; } = 1;

        [DefaultValue(3)]
        /// <summary>
        /// 金字塔层数
        /// </summary>
        public int NumLevels { get; set; } = 3;
        

        [DefaultValue(0.5)]
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
      
                string configPath = fileDir + "\\Ncc模板参数.ini";
                if (File.Exists(configPath))
                {
                    this.StartAngle = double.Parse(GeneralUse.ReadValue("Ncc模板", "起始角度", "Ncc模板参数",
                                              "-90", fileDir));
                    this.AngleRange = double.Parse(GeneralUse.ReadValue("Ncc模板", "角度范围", "Ncc模板参数",
                                              "180", fileDir));
                    this.AngleStep = double.Parse(GeneralUse.ReadValue("Ncc模板", "角度步长", "Ncc模板参数",
                                              "1", fileDir));
                    this.NumLevels = int.Parse(GeneralUse.ReadValue("Ncc模板", "金字塔层数", "Ncc模板参数",
                                            "3", fileDir));
                    this.MatchScore = double.Parse(GeneralUse.ReadValue("Ncc模板", "匹配得分", "Ncc模板参数",
                                              "0.5", fileDir));              
                }
                else
                   errInfoList.Add("Ncc模板参数配置文件不存在");
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
   
                GeneralUse.WriteValue("Ncc模板", "起始角度", this.StartAngle.ToString("f3"),
                          "Ncc模板参数", fileDir);
                GeneralUse.WriteValue("Ncc模板", "角度范围", this.AngleRange.ToString("f3"),
                        "Ncc模板参数", fileDir);
                GeneralUse.WriteValue("Ncc模板", "角度步长", this.AngleStep.ToString("f3"),
                      "Ncc模板参数", fileDir);
                GeneralUse.WriteValue("Ncc模板", "金字塔层数", this.NumLevels.ToString("f3"),
                      "Ncc模板参数", fileDir);
                GeneralUse.WriteValue("Ncc模板", "匹配得分", this.MatchScore.ToString("f3"),
                      "Ncc模板参数", fileDir);
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
