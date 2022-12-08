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
    public   class Blob2Data : ParmasDataBase, IParmaData
    {
        [DefaultValue(150)]
        /// <summary>
        /// 最小阈值
        /// </summary>
        public double minthd { get; set; } = 150;
        [DefaultValue(255)]
        /// <summary>
        /// 最大阈值
        /// </summary>
        public double maxthd { get; set; } = 255;    
        /// <summary>
        /// Blob粒子筛选条件
        /// </summary>
        public StuBlobFilter2 stuBlobFilter { get; set; }
        /// <summary>
        /// 参数加载
        /// </summary>
        /// <param name="fileDir"></param>
        /// <returns></returns>
        public override bool LoadData(string fileDir = "")
        {
            try
            {
                string configPath = fileDir + "\\Blob2参数";
                if (File.Exists(configPath))
                {
                    Blob2Data dat = GeneralUse.ReadSerializationFile<Blob2Data>(configPath);

                    this.minthd = dat.minthd;
                    this.maxthd = dat.maxthd;
                    this.stuBlobFilter = dat.stuBlobFilter;
                    this.FileDir = dat.FileDir;
                    this.ROI = dat.ROI;
                }            
                else
                    errInfoList.Add("Blob2参数配置文件不存在");
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
                string configPath = fileDir + "\\Blob2参数";                   
                GeneralUse.WriteSerializationFile<Blob2Data>(configPath, this);             
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
    /// <summary>
    /// Blob粒子特征筛选条件
    /// </summary>
    public struct StuBlobFilter2
    {
        /// <summary>
        /// 面积下限
        /// </summary>
        public int areaLow;
        /// <summary>
        /// 面积上限
        /// </summary>
        public int areaHigh;
        /// <summary>
        /// 宽度下限
        /// </summary>
        public int widthLow;
        /// <summary>
        /// 宽度上限
        /// </summary>
        public int widthHigh;
        /// <summary>
        /// 高度下限
        /// </summary>
        public int heightLow;
        /// <summary>
        /// 高度上限
        /// </summary>
        public int heightHigh;
    }
}
