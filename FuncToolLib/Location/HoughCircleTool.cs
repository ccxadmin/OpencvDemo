using OpenCvSharp;
using ParamDataLib;
using ParamDataLib.Location;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using Rect = System.Drawing.RectangleF;
using Point = System.Drawing.PointF;

namespace FuncToolLib.Location
{
    /// <summary>
    /// 查找圆（霍夫变换）
    /// </summary>
    public class HoughCircleTool : ToolBase
    {
        /// <summary>
        /// 圆检测运行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputImg"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Result Run<T>(Mat gray, T obj)
        {
            HoughCircleData houghCircleData = obj as HoughCircleData;
            Result houghCircleResult = new HoughCircleResult();
          
            try
            {
                //获取检测掩膜图像
             //   Mat temimg = MatExtension.Reducue_Mask_Mat(gray, (CVCircle)houghCircleData.ROI);

                CircleSegment[] circleSegment = HoughCircle(gray, houghCircleData.MinDist,
                               houghCircleData.Param1, houghCircleData.Param2, houghCircleData.MinRadius, houghCircleData.MaxRadius);
               
                //用来显示绘制效果
                Mat dst = new Mat();
                Cv2.CvtColor(gray, dst, ColorConversionCodes.GRAY2BGR);
                //检测区域
                dst.Circle((int)((CVCircle)houghCircleData.ROI).Center.X,
                   (int)((CVCircle)houghCircleData.ROI).Center.Y,
                   (int)((CVCircle)houghCircleData.ROI).Radius,
                   new Scalar(255, 0, 0), 2);

                for (int i = 0; i < circleSegment.Count(); i++)
                {
                    //画圆
                    Cv2.Circle(dst, (int)circleSegment[i].Center.X, (int)circleSegment[i].Center.Y,
                                            (int)circleSegment[i].Radius, Scalar.Green, 2, LineTypes.AntiAlias);
                    //画圆心
                    dst.drawCross(new CVPoint((int)circleSegment[i].Center.X, (int)circleSegment[i].Center.Y),
                            Scalar.Green, 20, 2);

                    (houghCircleResult as HoughCircleResult).positionData.Add(new Point2f(
                           circleSegment[i].Center.X, circleSegment[i].Center.Y));
                    (houghCircleResult as HoughCircleResult).radiusArray.Add(circleSegment[i].Radius);
                    
                }
                houghCircleResult.resultToShow = dst;
                houghCircleResult.runStatus = true;
                CircleCount = circleSegment.Length;
            }
            catch(Exception er)
            {
                Cv2.CvtColor(gray, houghCircleResult.resultToShow, ColorConversionCodes.GRAY2BGR);
                houghCircleResult.resultToShow.Circle((int)((CVCircle)houghCircleData.ROI).Center.X,
                                        (int)((CVCircle)houghCircleData.ROI).Center.Y,
                                               (int)((CVCircle)houghCircleData.ROI).Radius,
                                                   new Scalar(255, 0, 0), 2);
                houghCircleResult.exceptionInfo = er.Message;
                houghCircleResult.runStatus = false;
            }
            return houghCircleResult;
        }
        public int CircleCount = 0;//结果圆的个数
        /****************************霍夫变换-圆检测******************/
        /// <summary>
        /// 霍夫圆检测
        /// </summary>
        /// <param name="Gray">输入图</param>
        /// <param name="MinDist">最小同心圆距</param>
        /// <param name="Param1">canny边缘检测阈值低</param>
        /// <param name="Param2">中心点累加器阈值</param>
        /// <param name="MinRadius">最小半径</param>
        /// <param name="MaxRadius">最大半径</param>
        /// <returns></returns>
        private CircleSegment[] HoughCircle(Mat Gray,double MinDist,double Param1,
           double Param2, int MinRadius, int MaxRadius)
        {         
            return  Cv2.HoughCircles(Gray, HoughModes.Gradient,
                dp: 1, minDist: MinDist, Param1, Param2, MinRadius, MaxRadius);
            //霍夫圆检测：使用霍夫变换查找灰度图像中的圆。
            /*
             * 参数：
             *      1：输入参数： 8位、单通道、灰度输入图像
             *      2：实现方法：目前，唯一的实现方法是HoughCirclesMethod.Gradient
             *      3: dp      :累加器分辨率与图像分辨率的反比。默认=1
             *      4：minDist: 检测到的圆的中心之间的最小距离。(最短距离-可以分辨是两个圆的，否则认为是同心圆-                            src_gray.rows/8)
             *      5:param1:   第一个方法特定的参数。[默认值是100] canny边缘检测阈值低
             *      6:param2:   第二个方法特定于参数。[默认值是100] 中心点累加器阈值 – 候选圆心
             *      7:minRadius: 最小半径
             *      8:maxRadius: 最大半径
             *
             */
        }

    }

    /// <summary>
    /// 霍夫圆结果
    /// </summary>
    public class HoughCircleResult : Result
    {
        public HoughCircleResult()
        {
            positionData = new List<Point2f>();
            radiusArray = new List<double>();
        }
        public List<Point2f> positionData;

        public List<double> radiusArray;


    }
}

