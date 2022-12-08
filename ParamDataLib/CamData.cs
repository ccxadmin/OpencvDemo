using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ParamDataLib
{
    /// <summary>
    /// 相机参数
    /// </summary>
    [Serializable]
    public class CamData : IParmaData
    {
        public CamData()
        {
            
        }
      
        public T GetData<T>() where T : class, IParmaData
        {
            return this as T;
        }

        //[DefaultValue(typeof(EumCamType), "cam1")]
        //public EumCamType eumCamType { get; set; } = EumCamType.左上;//相机型号选择
        [DefaultValue(5000)]
        public int exposure { get; set; } = 5000;//相机曝光
        [DefaultValue(1000)]
        public int gain { get; set; } = 0;//相机增益
 


    } 
}
