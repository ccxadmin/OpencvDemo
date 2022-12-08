using FilesRAW.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParamDataLib.Location
{
    [Serializable]
    /// <summary>
    /// 霍夫圆参数
    /// </summary>
    public   class HoughCircleData : ParmasDataBase, IParmaData
    {
        [DefaultValue(50)]
        /// <summary>
        /// 最小同心圆距
        /// </summary>
        public double MinDist { get; set; } = 50;
        [DefaultValue(60)]
        /// <summary>
        /// canny边缘检测阈值低
        /// </summary>
        public double Param1 { get; set; } = 60;
        [DefaultValue(70)]
        /// <summary>
        /// 中心点累加器阈值
        /// </summary>
        public double Param2 { get; set; } = 70;
        [DefaultValue(20)]
        /// <summary>
        /// 最小半径
        /// </summary>
        public int MinRadius { get; set; } = 20;
        [DefaultValue(300)]
        /// <summary>
        /// 最大半径
        /// </summary>
        public int MaxRadius { get; set; } = 300;

        /// <summary>
        /// 参数加载
        /// </summary>
        /// <param name="fileDir"></param>
        /// <returns></returns>
        public override bool LoadData(string fileDir = "")
        {
            try
            {
                string configPath = fileDir + "\\霍夫圆参数";
                if (File.Exists(configPath))
                {
                    HoughCircleData dat = GeneralUse.ReadSerializationFile<HoughCircleData>(configPath);

                    this.MinDist = dat.MinDist;
                    this.Param1 = dat.Param1;
                    this.Param2 = dat.Param2;
                    this.MinRadius = dat.MinRadius;
                    this.MaxRadius = dat.MaxRadius;
                    this.FileDir = dat.FileDir;
                    this.ROI = dat.ROI;

                    //this.MinDist = double.Parse(GeneralUse.ReadValue("查找圆", "最小同心圆距", "圆参数",
                    //                          "10", fileDir));
                    //this.Param1 = double.Parse(GeneralUse.ReadValue("查找圆", "canny边缘检测阈值低", "圆参数",
                    //                          "100", fileDir));
                    //this.Param2 = double.Parse(GeneralUse.ReadValue("查找圆", "中心点累加器阈值", "圆参数",
                    //                       "100", fileDir));
                    //this.MinRadius = int.Parse(GeneralUse.ReadValue("查找圆", "最小半径", "圆参数",
                    //                          "1", fileDir));
                    //this.MaxRadius = int.Parse(GeneralUse.ReadValue("查找圆", "最大半径", "圆参数",
                    //                          "999", fileDir));
                }
                else
                    errInfoList.Add("霍夫圆参数配置文件不存在");
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
        /// <param name="fileDir"></param>
        /// <returns></returns>
        public override bool SaveData(string fileDir = "")
        {
            try
            {
                if (fileDir == "") fileDir = base.FileDir;

                string configPath = fileDir + "\\霍夫圆参数";
                GeneralUse.WriteSerializationFile<HoughCircleData>(configPath, this);

                //GeneralUse.WriteValue("查找圆", "最小同心圆距", this.MinDist.ToString("f3"),
                //          "圆参数", fileDir);
                //GeneralUse.WriteValue("查找圆", "canny边缘检测阈值低", this.Param1.ToString("f3"),
                //        "圆参数", fileDir);
                //GeneralUse.WriteValue("查找圆", "中心点累加器阈值", this.Param2.ToString("f3"),
                //        "圆参数", fileDir);
                //GeneralUse.WriteValue("查找圆", "最小半径", this.MinRadius.ToString(),
                //        "圆参数", fileDir);
                //GeneralUse.WriteValue("查找圆", "最大半径", this.MaxRadius.ToString(),
                //      "圆参数", fileDir);
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
