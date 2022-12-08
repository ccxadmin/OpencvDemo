
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
using ParamDataLib;
using ParamDataLib.Location;

namespace FuncToolLib.Location
{
    /// <summary>
    /// 拟合直线
    /// </summary>
    public class FitLineTool : ToolBase
    {
        public override Result Run<T>(Mat inputImg, T obj)
        {
            FitLineData fitLineData = obj as FitLineData;
            Result fitLineResult = new FitLineResult();
        
            try
            {
                //获取检测掩膜图像
                Mat temimg = MatExtension.Reducue_Mask_Mat(inputImg,(CVRRect)fitLineData.ROI);
                CVPoint[] contour = Detect(temimg, fitLineData.Thddown, fitLineData.Thdup);

                Mat Dst = new Mat();
                Cv2.CvtColor(inputImg, Dst, ColorConversionCodes.GRAY2BGR);
                CVPoint p1, p2;
                Line2D line2D = Cv2.FitLine(contour, DistanceTypes.L1, 0, 0.01, 0.01);
                line2D.FitSize(Dst.Width, Dst.Height, out p1, out p2);
                //检测区域             
                Dst.DrawRotatedRect((CVRRect)fitLineData.ROI);
                Cv2.Line(Dst, p1, p2, Scalar.Green, 1);

                (fitLineResult as FitLineResult).positionData.Add(new Point2f(p1.X, p1.Y));
                (fitLineResult as FitLineResult).positionData.Add(new Point2f(p2.X, p2.Y) );
              
                fitLineResult.resultToShow = Dst;
                fitLineResult.runStatus = true;
            }
            catch (Exception er)
            {
                Cv2.CvtColor(inputImg, fitLineResult.resultToShow, ColorConversionCodes.GRAY2BGR);
                //检测区域
                fitLineResult.resultToShow.DrawRotatedRect((CVRRect)fitLineData.ROI);
                fitLineResult.exceptionInfo = er.Message;
                fitLineResult.runStatus = false;
            }
            return fitLineResult;
        }

        private CVPoint[] Detect(Mat gray, double Thddown,double Thdup)
        {
                    
            Mat binary = new Mat();
            CVPoint[][] contours;
            Cv2.Threshold(gray, binary, Thddown, Thdup, ThresholdTypes.Binary );
            //Cv2.Threshold(temimg, binary,60, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            //获得轮廓
            Cv2.FindContours(binary, out contours, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
            //绘制轮廓
            int index = 0; List<CVPoint[]> points = new List<CVPoint[]>();
            foreach (var contour in contours)
            {
                if (contour.Length < 5)
                {
                    index++;
                    continue;
                }
                points.Add(contour);
                //Cv2.DrawContours(Dst, contours, index, Scalar.Green, 2);
                index++;
            }
         
            int cn = 0, idex = 0;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Length >= cn)
                {
                    cn = points[i].Length;
                    idex = i;
                }
            }
            return points[idex];
           
        }
    }
    /// <summary>
    ///  直线拟合结果
    /// </summary>
    public class FitLineResult : Result
    {
        public FitLineResult()
        {
            positionData = new List<Point2f>();
           
        }
        public List<Point2f> positionData;

    }
}
