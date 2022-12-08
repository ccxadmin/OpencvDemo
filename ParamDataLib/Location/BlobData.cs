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
    /// Blob检测
    /// </summary>
   public  class BlobData : ParmasDataBase, IParmaData
    {
        [DefaultValue(150)]
        /// <summary>
        /// 二值化阈值下限
        /// </summary>
        public float MinThreshold { get; set; } =150;
        [DefaultValue(255)]
        /// <summary>
        /// 二值化阈值上限
        /// </summary>
        public float MaxThreshold { get; set; } = 255;

        /// <summary>
        /// Blob粒子筛选条件
        /// </summary>
        public StuBlobFilter stuBlobFilter { get; set; }

        /// <summary>
        /// 参数加载
        /// </summary>
        /// <param name="fileDir"></param>
        /// <returns></returns>
        public override bool LoadData(string fileDir = "")
        {
            try
            {
                string configPath = fileDir + "\\Blob参数";
                if (File.Exists(configPath))
                {
                    BlobData dat = GeneralUse.ReadSerializationFile<BlobData>(configPath);

                    this.MinThreshold = dat.MinThreshold;
                    this.MaxThreshold = dat.MaxThreshold;
                    this.stuBlobFilter = dat.stuBlobFilter;
                    this.FileDir = dat.FileDir;
                    this.ROI = dat.ROI;
                }
                else
                    errInfoList.Add("Blob参数配置文件不存在");
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
                string configPath = fileDir + "\\Blob参数";
                GeneralUse.WriteSerializationFile<BlobData>(configPath, this);
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
    public struct StuBlobFilter
    {
        //根据圆度过滤；圆度表示斑点距圆的距离，例如圆的圆度为1，正方形的圆度为0.785
        public bool isFilterByCircularity;
        public float MinCircularity;
        public float MaxCircularity;
        //根据惯性比过滤；衡量形状的伸长程度，相当于（短轴 / 长轴），例如对于一个圆，则惯性比是1，对于椭圆的惯性比在0和1之间，而对于线是0；  0≤  minInertiaRatio  ≤1  ，  maxInertiaRatio  ≤ 1 
        public bool isFilterByInertia;
        public float MinInertiaRatio;
        public float MaxInertiaRatio;
        //根据凹凸性进行过滤；凸度定义为：斑点的面积/该斑点的凸包面积；凸度小于等于1，越接近1表示该斑点凸性越强
        public bool isFilterByConvexity;
        public float MinConvexity;
        public float MaxConvexity;

        //根据灰度或颜色值过滤
        public bool isFilterByColor;
        public byte BlobColor;

        //根据BLOB面积过滤
        public bool isFilterByArea;
        /// <summary>
        /// 面积下限
        /// </summary>
        public int areaLow;
        /// <summary>
        /// 面积上限
        /// </summary>
        public int areaHigh;
       
    }
}
