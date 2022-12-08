using FilesRAW.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionShowLib;

namespace ParamDataLib.Location
{
   
    /// <summary>
    /// 圆拟合检测参数
    /// </summary>
    [Serializable]
    public class FitCircleData : ParmasDataBase, IParmaData
    {
        [DefaultValue(10)]
        /// <summary>
        /// 最小半径
        /// </summary>
        public double minRadius { get; set; } = 10;

        [DefaultValue(150)]
        /// <summary>
        /// 最大半径
        /// </summary>
        public double maxRadius { get; set; } = 150;
              
        [DefaultValue(20)]
        /// <summary>
        /// 边缘阈值
        /// </summary>
        public int EdgeThreshold { get; set; } = 20;
        /// <summary>
        /// 扇形区域
        /// </summary>
        public SectorF sectorF { get; set; }

        /// <summary>
        /// 参数加载
        /// </summary>
        /// <param name="fileDir"></param>
        /// <returns></returns>
        public override bool LoadData(string fileDir = "")
        {
            try
            {
                string configPath = fileDir + "\\圆拟合参数";
                if (File.Exists(configPath))
                {
                    FitCircleData dat = GeneralUse.ReadSerializationFile<FitCircleData>(configPath);

                    this.minRadius = dat.minRadius;
                    this.maxRadius = dat.maxRadius;
                    this.sectorF = dat.sectorF;
                    this.EdgeThreshold = dat.EdgeThreshold;               
                    this.FileDir = dat.FileDir;
                    this.ROI = dat.ROI;
                }
                else
                    errInfoList.Add("圆拟合参数配置文件不存在");
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
                string configPath = fileDir + "\\圆拟合参数";
                GeneralUse.WriteSerializationFile<FitCircleData>(configPath, this);
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
