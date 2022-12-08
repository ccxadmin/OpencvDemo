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
    /// 卡尺圆检测参数
    /// </summary>
    [Serializable]
    public class CircularCaliperData : ParmasDataBase, IParmaData
    {
        /// <summary>
        /// 卡尺宽度
        /// </summary>
        [DefaultValue(20)]
        public int caliperWidth { get; set; } = 20;

        /// <summary>
        /// 卡尺数量
        /// </summary>
        [DefaultValue(6)]
        public int caliperNum { get; set; } = 6;

        /// <summary>
        /// 边缘阈值
        /// </summary>
        [DefaultValue(100)]
        public byte edgeThreshold { get; set; } = 100;

        /// <summary>
        ///  寻找方向  
        /// </summary>
        [DefaultValue(typeof(EumDirectionOfCircle), "Outer")]
        public EumDirectionOfCircle circleDir { get; set; } = EumDirectionOfCircle.Outer;
        /// <summary>
        /// 边缘极性
        /// </summary>
        [DefaultValue(typeof(EumEdgePolarity), "all")]
        public EumEdgePolarity edgePolarity { get; set; } = EumEdgePolarity.all;


        /// <summary>
        /// 参数加载
        /// </summary>
        /// <param name="fileDir"></param>
        /// <returns></returns>
        public override bool LoadData(string fileDir = "")
        {
            try
            {
                string configPath = fileDir + "\\卡尺圆参数";
                if (File.Exists(configPath))
                {
                    CircularCaliperData dat = GeneralUse.ReadSerializationFile<CircularCaliperData>(configPath);

                    this.caliperWidth = dat.caliperWidth;
                    this.caliperNum = dat.caliperNum;
                    this.edgeThreshold = dat.edgeThreshold;
                    this.edgePolarity = dat.edgePolarity;     
                    
                    this.FileDir = dat.FileDir;
                    this.ROI = dat.ROI;
                }
                else
                    errInfoList.Add("卡尺圆参数配置文件不存在");
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
                string configPath = fileDir + "\\卡尺圆参数";
                GeneralUse.WriteSerializationFile<CircularCaliperData>(configPath, this);
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
