using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace visionForm
{
    [Serializable]
    public abstract class BaseInfo
    {
       
    }
    /// <summary>
    /// 区域设定信息
    /// </summary>
    [Serializable]
    public   class DatagridOfRegionInfo: BaseInfo
    {
        public DatagridOfRegionInfo(string _name,bool _isUse, string _regionInfo="已设定" )
        {

            isUse = _isUse;
            name = _name;
            regionInfo = _regionInfo;
         
        }
      
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool isUse { get; set; }
        /// <summary>
        /// 序号
        /// </summary>
        public string name { get; set; } = "Posi1";
        /// <summary>
        /// 区域信息
        /// </summary>
        public string regionInfo { get; set; }

    
        public void dataToRow(ref DataGridViewRow dgrow)
        {
           
            dgrow.SetValues(
                    isUse,
                    name,
                    regionInfo,
                  new Button() { Text = "删除" ,BackColor=System.Drawing.Color.Red,
                  ForeColor=System.Drawing.Color.Black});
            
        }

        static public string[] dataToRegionInfo(DataGridViewRow dgrow)
        {

            string[] d_RegionInfo = new string[3]
            {
                    dgrow.Cells[0].EditedFormattedValue.ToString(),
                    dgrow.Cells[1].EditedFormattedValue.ToString(),
                    dgrow.Cells[2].EditedFormattedValue.ToString(),
                   // dgrow.Cells[3].EditedFormattedValue.ToString()
            };
            return d_RegionInfo;
        }
    }
   
    /// <summary>
    /// 胶水检测信息
    /// </summary>
    [Serializable]
    public class DatagridOfGlueCheckInfo : BaseInfo
    {
        public DatagridOfGlueCheckInfo()
        {

        }
        public DatagridOfGlueCheckInfo(DateTime _checkTime, bool _checkReult,
             string spotNum,   string _id)
        {
            时间 = _checkTime.ToString("HH:mm:ss");
            测试结果 = _checkReult ? "OK" : "NG";
            斑点数量 = spotNum;
            ID = _id;
        }
        public DatagridOfGlueCheckInfo(DateTime _checkTime, bool _checkReult,int spotNum, int _id)
        {
            时间 = _checkTime.ToString("HH:mm:ss");
            测试结果 = _checkReult ? "OK" : "NG";
            斑点数量 = spotNum.ToString();
            ID = _id.ToString();
        }
        public DatagridOfGlueCheckInfo(string _id,string _checkTime, string _checkReult)
        {
                             
            时间 = _checkTime;
            测试结果 = _checkReult;
            ID = _id;
        }
        public DatagridOfGlueCheckInfo(string _checkTime, string _checkReult)
        {
            int index = int.Parse(ID);
            时间 = _checkTime;
            测试结果 = _checkReult;
            ID = (index + 1).ToString();
        }

        public override string ToString()
        {
            return string.Format("ID：{0}，时间：{1}，测试结果：{2}", ID, 时间, 测试结果);
        }
        /// <summary>
        /// 序号
        /// </summary>
        public string ID { get; set; } = "0";
        /// <summary>
        /// 测试时间
        /// </summary>
        public string 时间 { get; set; }

        public string 斑点数量 { get; set; }
        /// <summary>
        /// 测试结果
        /// </summary>
        public string 测试结果 { get; set; }

    }
}
