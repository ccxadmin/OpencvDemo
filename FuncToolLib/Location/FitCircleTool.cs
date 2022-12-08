using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenCvSharp;
using OpenCvSharp.Extensions;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using Rect = System.Drawing.RectangleF;
using Point = System.Drawing.PointF;
using VisionShowLib;
using ParamDataLib;
using ParamDataLib.Location;

namespace FuncToolLib.Location
{
    /// <summary>
    /// 圆拟合
    /// </summary>
    public  class FitCircleTool : ToolBase
    {
      
        public override Result Run<T>(Mat gray, T obj)
        {
            FitCircleData fitCircleData2 = obj as FitCircleData;
            Result fitCircleResult = new FitCircleResult();
            try
            {
                //获取检测掩膜图像
                Mat temimg = MatExtension.Crop_Mask_Mat(gray, (CVPoint[])fitCircleData2.ROI,
                            out CVRect BoundingRect);

                //阈值操作 阈值参数可以用一些可视化工具来调试得到
                Mat ThresholdImg = temimg.Threshold(fitCircleData2.EdgeThreshold, 255, ThresholdTypes.Binary);
                //  Cv2.ImShow("Threshold", ThresholdImg);

                //降噪
                //方法一：高斯变化
                //Mat gaussImg= ThresholdImg.GaussianBlur(new Size(5, 5), 0.8);
                //Cv2.ImShow("GaussianBlur", gaussImg);
                //方法二：中值滤波降噪
                //Mat medianImg = ThresholdImg.MedianBlur(5);
                //Cv2.ImShow("MedianBlur", medianImg);

                //方法三：膨胀+腐蚀
                ////膨胀处理
                //Mat kernel = new Mat(15, 15, MatType.CV_8UC1);
                //Mat DilateImg = ThresholdImg.Dilate(kernel);
                ////腐蚀处理
                //Mat binary = DilateImg.Erode(kernel);
                ////显示中间结果
                //Cv2.ImShow("Dilate & Erode", binary);
                Mat element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));
                Mat openImg = ThresholdImg.MorphologyEx(MorphTypes.Open, element);
                //   Cv2.ImShow("Dilate & Erode", openImg);        

                //寻找图像轮廓
                CVPoint[][] contours;
                HierarchyIndex[] hierachy;
                Cv2.FindContours(openImg, out contours, out hierachy, RetrievalModes.List, ContourApproximationModes.ApproxTC89KCOS);

                CVPoint[][] dstcontours = MatExtension.ExceptBoundOfSectorF(contours, fitCircleData2.sectorF, BoundingRect);
                List<CVPoint[]> contoursList = new List<CVPoint[]>();
                List<Point2d> centrepList = new List<Point2d>();
                List<double> radiusList = new List<double>();


                Mat dst = new Mat();
                Cv2.CvtColor(gray, dst, ColorConversionCodes.GRAY2BGR);
                //根据找到的轮廓点，拟合圆
                for (int i = 0; i < dstcontours.Length; i++)
                {
                    //拟合函数必须至少5个点，少于则不拟合
                    if (dstcontours[i].Length < 5) continue;

                    Point2d centrep = new Point2d(0, 0);
                    double radius = 0;

                    //拟合圆
                    double fitscore = Calibration.AxisCoorditionRotation.FitCircle(dstcontours[i], ref centrep, ref radius);
                    if (fitscore < 0.2 || radius < fitCircleData2.minRadius ||
                           radius > fitCircleData2.maxRadius) continue;

                    //圆自动校验
                    if (!Calibration.AxisCoorditionRotation.CheckCircle(dstcontours[i], ref centrep, ref radius))
                        continue;

                    contoursList.Add(dstcontours[i]);
                    centrepList.Add(centrep);
                    radiusList.Add(radius);
                }

                //圆弧联合
                int num = centrepList.Count;
                double cx = centrepList[0].X;
                double cy = centrepList[0].Y;
                double r = radiusList[0];

                List<CVPoint> CVPointList = new List<CVPoint>();
                CVPointList.AddRange(contoursList[0]);
                if(num>=2)
                    for (int k = 1; k < num; k++)
                    {
                        if (Math.Abs(cx - centrepList[k].X) <= 5 &&
                            Math.Abs(cy - centrepList[k].Y) <= 5 &&
                            Math.Abs(r - radiusList[k]) <= 5)
                            CVPointList.AddRange(contoursList[k]);
                    }

                //拟合圆
                Point2d centrep2 = new Point2d(0, 0);
                double radius2 = 0;               
                Calibration.AxisCoorditionRotation.FitCircle(CVPointList, ref centrep2, ref radius2);

                //圆自动校验
                Calibration.AxisCoorditionRotation.CheckCircle(CVPointList, ref centrep2, ref radius2);

                //坐标恢复
                centrep2.X += BoundingRect.X;
                centrep2.Y += BoundingRect.Y;
                //结果添加
                (fitCircleResult as FitCircleResult).positionData.Add(new Point2d(centrep2.X, centrep2.Y));

                (fitCircleResult as FitCircleResult).radiusArray.Add(radius2);
                //绘制结果圆
                Cv2.Circle(dst, new CVPoint(centrep2.X, centrep2.Y), (int)radius2, Scalar.Lime);
                //绘制圆中心
                dst.drawCross(new CVPoint(centrep2.X, centrep2.Y), Scalar.Red, 20, 2);

                //检测区域
                List<CVPoint[]> points = new List<CVPoint[]>();
                points.Add((CVPoint[])fitCircleData2.ROI);
                Cv2.Polylines(dst, points, true, Scalar.Blue);

                fitCircleResult.resultToShow = dst;
                fitCircleResult.runStatus = true;
            }
            catch (Exception er)
            {
                Cv2.CvtColor(gray, fitCircleResult.resultToShow, ColorConversionCodes.GRAY2BGR);
                //检测区域
                List<CVPoint[]> points = new List<CVPoint[]>();
                points.Add((CVPoint[])fitCircleData2.ROI);
                Cv2.Polylines(fitCircleResult.resultToShow, points, true, Scalar.Blue);

                fitCircleResult.exceptionInfo = er.Message;
                fitCircleResult.runStatus = false;
            }
            return fitCircleResult;
        }
    }

    /// <summary>
    /// 圆拟合结果
    /// </summary>
    public class FitCircleResult : Result
    {
        public FitCircleResult()
        {
            positionData = new List<Point2d>();
            radiusArray = new List<double>();
        }
        public List<Point2d> positionData;

        public List<double> radiusArray;


    }
}
