using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParamDataLib
{
    [Serializable]
    public abstract class ParmasDataBase
    {
        public ParmasDataBase()
        {

        }
        public ParmasDataBase(string fileDir)
        {
            FileDir = fileDir;
        }
        public ParmasDataBase(Rect rOI)
        {
            ROI = rOI;
        }
        public ParmasDataBase(string fileDir, Rect rOI)
        {
            FileDir = fileDir;
            ROI = rOI;
        }
        public List<string> errInfoList = new List<string>();
        /// <summary>
        /// 存储文件路径
        /// </summary>
        public string FileDir { get; set; }
        /// <summary>
        /// ROI区域
        /// </summary>
        public object ROI { get; set; }

        /// <summary>
        /// 模板搜索区域
        /// </summary>
        public RectangleF matchSearchRegion { get; set; } = new RectangleF(0, 0, 1000, 1000);

        /// <summary>
        /// 模板搜索方式
        /// </summary>
        public EumMatchSearchMethod MatchSearchMethod { get; set; } = EumMatchSearchMethod.AllRegion;

        /// <summary>
        /// 加载检测参数数据
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="filePath">参数文件路径</param>
        /// <returns>返回加载的参数对象</returns>
        public virtual bool LoadData(string fileDir) { return true; }

        /// <summary>
        /// 保存检测参数数据
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="filePath">参数文件路径</param> 
        public virtual bool SaveData(string fileDir) { return true; }


    }
    /// <summary>
    /// 模板匹配搜索方式
    /// </summary>
    public  enum EumMatchSearchMethod
    {
        //全区域
        AllRegion,
        //局部区域
        PartRegion

    }
    public class MatDataWriteRead
    {
        /// <summary>
        /// 矩阵(Mat)关系写入
        /// </summary>
        /// <param name="matri">写入的矩阵</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="nodeName">节点名称</param>
        public static void WriteMatri(Mat matri, string filePath, string nodeName)
        {
            using (FileStorage fs = new FileStorage(filePath, FileStorage.Modes.Write))
            {
                fs.Write(nodeName, matri);
            }
        }
        /// <summary>
        ///  矩阵(Mat)关系读取
        /// </summary>
        /// <param name="filePath">文件路径.xml</param>
        /// <param name="nodeName">节点名称</param>
        /// <returns>返回读取的矩阵</returns>
        public static Mat ReadMatri(string filePath, string nodeName)
        {
            using (FileStorage fs = new FileStorage(filePath, FileStorage.Modes.Read))
            {
                return (Mat)fs[nodeName];
            }
        }
        /// <summary>
        /// 读取图像
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="nodeName">节点名称</param>
        /// <returns>返回读取的图像</returns>
        public static Mat ReadImage( string filePath)
        {
            return Cv2.ImRead(filePath,ImreadModes.AnyColor);
          
        }
        /// <summary>
        /// 保存图像
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="image">图像</param>
        public static void  WriteImage(string filePath, Mat image)
        {
            Cv2.ImWrite(filePath, image);             
        }
    }
}
