using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ParamDataLib;
using System.Diagnostics;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using CVSize = OpenCvSharp.Size;

namespace FuncToolLib
{
    /// <summary>
    /// 工具基类
    /// </summary>
    public abstract class ToolBase : IRunTool, IDisposable
    {
        public ToolBase()
        {

        }
        public void Dispose()
        {
            if (this != null)
                GC.Collect();
        }
        protected  Stopwatch st = new Stopwatch();
        public abstract Result Run<T>(Mat inputImg, T obj) where T : class, IParmaData;

        public T GetTool<T>() where T : class, IRunTool
        {
            return this as T;
        }
      
    }

  
    /// <summary>
    /// 位置数据
    /// </summary>
    public  struct PositionData
    {
        /// <summary>
        ///平面xy坐标
        /// </summary>
        public Point2d pointXY;
        /// <summary>
        /// 角度
        /// </summary>
        public double angle;
        //半径
        public double radius;
    }

     abstract  public class Result
    {
      public Result()
        {
            resultToShow = new Mat();
        }
        /// <summary>
        /// 运行状态
        /// </summary>
        public bool runStatus;
        /// <summary>
        /// 异常信息
        /// </summary>
        public string exceptionInfo;
        /// <summary>
        /// 检测时间ms
        /// </summary>
        public long detectTime;
        /// <summary>
        /// 结果显示对象
        /// </summary>
        public Mat resultToShow;
     
    }
}
