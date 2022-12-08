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
    public class Blob3Data : ParmasDataBase, IParmaData
    {
        /// <summary>
        /// 边缘阈值
        /// </summary>
        [DefaultValue(150)]       
        public double edgeThreshold { get; set; } = 150;

        /// <summary>
        /// 粒子最小面积
        /// </summary>
        [DefaultValue(100)]
        public int minArea { get; set; } = 100;

        /// <summary>
        /// 粒子最大面积
        /// </summary>
        [DefaultValue(99999)]
        public int maxArea { get; set; } = 99999;

        /// <summary>
        /// 粒子极性
        /// </summary>
        [DefaultValue(typeof(EumWhiteOrBlack), "White")]
        public EumWhiteOrBlack eumWhiteOrBlack { get; set; } = EumWhiteOrBlack.White;

        /// <summary>
        /// 参数加载
        /// </summary>
        /// <param name="fileDir"></param>
        /// <returns></returns>
        public override bool LoadData(string fileDir = "")
        {
            try
            {
                string configPath = fileDir + "\\Blob3参数";
                if (File.Exists(configPath))
                {
                    Blob3Data dat = GeneralUse.ReadSerializationFile<Blob3Data>(configPath);

                    this.edgeThreshold = dat.edgeThreshold;
                    this.minArea = dat.minArea;
                    this.maxArea = dat.maxArea;
                    this.eumWhiteOrBlack = dat.eumWhiteOrBlack;

                    this.FileDir = dat.FileDir;
                    this.ROI = dat.ROI;
                }
                else
                    errInfoList.Add("Blob3参数配置文件不存在");
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
                string configPath = fileDir + "\\Blob3参数";
                GeneralUse.WriteSerializationFile<Blob3Data>(configPath, this);
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
    /// 粒子区域极性
    /// </summary>
    public enum EumWhiteOrBlack
    {
        /// <summary>
        /// 白色区域
        /// </summary>
        White,
        /// <summary>
        /// 黑色区域
        /// </summary>
        Black
    }
}
