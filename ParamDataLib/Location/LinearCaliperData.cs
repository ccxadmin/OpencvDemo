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
    /// 卡尺直线参数
    /// </summary>
    public  class LinearCaliperData : ParmasDataBase, IParmaData
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
        public byte edgeThreshold { get; set; } = 30;

        /// <summary>
        /// 拟合方法
        /// </summary>
        [DefaultValue(typeof(EumFittingMethod), "Least_square_method")]
        public EumFittingMethod fitMethod { get; set; } = EumFittingMethod.Least_square_method;
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

                string configPath = fileDir + "\\卡尺直线参数";
                if (File.Exists(configPath))
                {
                    LinearCaliperData dat = GeneralUse.ReadSerializationFile<LinearCaliperData>(configPath);

                    this.caliperWidth = dat.caliperWidth;
                    this.caliperNum = dat.caliperNum;
                    this.edgeThreshold = dat.edgeThreshold;
                    this.fitMethod = dat.fitMethod;
                    this.edgePolarity = dat.edgePolarity;
                    this.FileDir = dat.FileDir;
                    this.ROI = dat.ROI;                
                }
                else
                    errInfoList.Add("卡尺直线参数配置文件不存在");
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
                string configPath = fileDir + "\\卡尺直线参数";
                GeneralUse.WriteSerializationFile<LinearCaliperData>(configPath, this);
              
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
    /// 边缘极性
    /// </summary>
    public enum EumEdgePolarity
    {
        positive,
        negtive,
        all
    }

    /// <summary>
    /// 拟合方式
    /// </summary>
    public enum EumFittingMethod
    {
        /// <summary>
        /// 最小二乘法
        /// </summary>
        Least_square_method,
        /// <summary>
        /// 随机采样一致性
        /// </summary>
        Random_sampling_consistency

    }
    
}
