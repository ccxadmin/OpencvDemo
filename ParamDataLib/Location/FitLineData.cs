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
    /// 拟合直线参数
    /// </summary>
    public class FitLineData : ParmasDataBase, IParmaData
    {
        [DefaultValue(150)]
        /// <summary>
        /// 阈值下限
        /// </summary>
        public double Thddown { get; set; } = 150;
        [DefaultValue(255)]
        /// <summary>
        /// 阈值上限
        /// </summary>
        public double Thdup { get; set; } = 255;
       

        /// <summary>
        /// 参数加载
        /// </summary>
        /// <param name="fileDir"></param>
        /// <returns></returns>
        public override bool LoadData(string fileDir = "")
        {
            try
            {

                string configPath = fileDir + "\\拟合直线参数";
                if (File.Exists(configPath))
                {
                    FitLineData dat = GeneralUse.ReadSerializationFile<FitLineData>(configPath);

                    this.Thddown = dat.Thddown;
                    this.Thdup = dat.Thdup;
                 
                    this.FileDir = dat.FileDir;
                    this.ROI = dat.ROI;
                    //this.canThddown = double.Parse(GeneralUse.ReadValue("查找直线", "Canny阈值下限", "直线参数",
                    //                          "100", fileDir));
                    //this.canThdup = double.Parse(GeneralUse.ReadValue("查找直线", "Canny阈值上限", "直线参数",
                    //                          "200", fileDir));
                    //this.ThresholdP = int.Parse(GeneralUse.ReadValue("查找直线", "直线阈值", "直线参数",
                    //                       "30", fileDir));
                    //this.MinLinLenght = double.Parse(GeneralUse.ReadValue("查找直线", "最小线长度", "直线参数",
                    //                          "1", fileDir));
                    //this.MaxLineGap = double.Parse(GeneralUse.ReadValue("查找直线", "最大允许断线", "直线参数",
                    //                          "10", fileDir));        
                }
                else
                    errInfoList.Add("拟合直线参数配置文件不存在");
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
                string configPath = fileDir + "\\拟合直线参数";
                GeneralUse.WriteSerializationFile<FitLineData>(configPath, this);
                //GeneralUse.WriteValue("查找直线", "Canny阈值下限", this.canThddown.ToString("f3"),
                //          "直线参数", fileDir);
                //GeneralUse.WriteValue("查找直线", "Canny阈值上限", this.canThdup.ToString("f3"),
                //        "直线参数", fileDir);
                //GeneralUse.WriteValue("查找直线", "直线阈值", this.ThresholdP.ToString("f3"),
                //        "直线参数", fileDir);
                //GeneralUse.WriteValue("查找直线", "最小线长度", this.MinLinLenght.ToString("f3"),
                //        "直线参数", fileDir);
                //GeneralUse.WriteValue("查找直线", "最大允许断线", this.MaxLineGap.ToString("f3"),
                //      "直线参数", fileDir);
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
