using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CVPoint = OpenCvSharp.Point;
using CVPointF = OpenCvSharp.Point2f;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using CVSize = OpenCvSharp.Size;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Blob;
using ParamDataLib.Location;
using VisionShowLib;

namespace FuncToolLib.Location
{
    public class Blob3Tool : ToolBase
    {
        public override Result Run<T>(Mat gray, T obj)
        {
            Blob3Data blob3Data = obj as Blob3Data;
            Result blob3Result = new Blob3Result();
            try
            {
                RotatedRectF rotatedRectF = (RotatedRectF)blob3Data.ROI;
                CVRRect cVRRect = new CVRRect(new CVPointF(rotatedRectF.cx, rotatedRectF.cy),
                    new Size2f (rotatedRectF.Width, rotatedRectF.Height), rotatedRectF.angle);
                  //获取检测掩膜图像
                  Mat temimg = MatExtension.Crop_Mask_Mat(gray, cVRRect,
                    out CVRect BoundingRect, blob3Data.eumWhiteOrBlack);
                CvBlobs blobs = RunBlob(temimg, blob3Data.edgeThreshold, blob3Data.minArea,
                    blob3Data.maxArea, blob3Data.eumWhiteOrBlack);
                
                Mat dst = new Mat();

                Cv2.CvtColor(gray, dst, ColorConversionCodes.GRAY2BGR);

                //检测区域
                dst.DrawRotatedRect(cVRRect, new Scalar(255, 0, 0), 2);
                dst.Rectangle((CVRect)BoundingRect, new Scalar(255, 0, 0), 2);
                //blobs.RenderBlobs(gray, dst);//渲染斑点

                int text = 1; //数字         
                foreach (var item in blobs)
                {
                    if (item.Value.Area >= blob3Data.minArea && 
                               item.Value.Area <= blob3Data.maxArea) // 检查标签区域
                    {
                        CvBlob blob = item.Value;

                        List<CVPoint> points = blob.Contour.ConvertToPolygon();
                        List<List<CVPoint>> pointsList = new List<List<CVPoint>>();
                        CVPoint[] temP= points.ToArray<CVPoint>();

                        for (int i= 0;i < temP.Length;i++)
                        {
                            int x = temP[i].X;
                            int y = temP[i].Y;
                            temP[i].X = x + BoundingRect.X;
                            temP[i].Y = y + BoundingRect.Y;

                        }                       
                           pointsList.Add(temP.ToList<CVPoint>());
                        //获取重心点
                        //Moments M;
                        //M = Cv2.Moments(points);
                        //double cX = (M.M10 / M.M00);
                        //double cY = (M.M01 / M.M00);

                        //float a = (float)(M.M20 / M.M00 - cX * cX);
                        //float b = (float)(M.M11 / M.M00 - cX * cY);
                        //float c = (float)(M.M02 / M.M00 - cY * cY);
                        //计算角度
                        //double tanAngle = Cv2.FastAtan2(2 * b, (a - c)) / 2;

                        Cv2.DrawContours(dst, pointsList, 0, Scalar.Red);

                        double cx = blob.Centroid.X+ BoundingRect.X;
                        double cy = blob.Centroid.Y + BoundingRect.Y;
                        dst.drawCross(new CVPoint(cx, cy),Scalar.LimeGreen,20,2);
                        //   Cv2.Circle(result, blob.Contour.StartingPoint, 8, Scalar.Red, 2, LineTypes.AntiAlias);
                        Cv2.PutText(dst, text.ToString(), new CVPoint(cx, cy),  //修改标签编号设置
                            HersheyFonts.HersheyComplex, 1, Scalar.Yellow, 2, LineTypes.AntiAlias);
                        text++;

                     (blob3Result as Blob3Result).positionData.Add(new Point2d(
                           cx, cy));
                        (blob3Result as Blob3Result).areas.Add(blob.Area);


                    }
                }

                blob3Result.resultToShow = dst;
                blob3Result.runStatus = true;

            }
            catch (Exception er)
            {
                Cv2.CvtColor(gray, blob3Result.resultToShow, ColorConversionCodes.GRAY2BGR);
                //检测区域
                //blob3Result.resultToShow.Rectangle((CVRect)blob3Data.ROI, new Scalar(255, 0, 0), 2);
                RotatedRectF rotatedRectF = (RotatedRectF)blob3Data.ROI;
                CVRRect cVRRect = new CVRRect(new CVPointF(rotatedRectF.cx, rotatedRectF.cy),
                    new Size2f(rotatedRectF.Width, rotatedRectF.Height), rotatedRectF.angle);
                blob3Result.resultToShow.DrawRotatedRect(cVRRect, new Scalar(255, 0, 0), 2);
               // blob3Result.resultToShow.Rectangle((CVRect)BoundingRect, new Scalar(255, 0, 0), 2);
                blob3Result.exceptionInfo = er.Message;
                blob3Result.runStatus = false;
            }
            return blob3Result;
        }
        /// <summary>
        /// Blob检测
        /// </summary>
        /// <param name="gray"></param>
        /// <param name="roi"></param>
        /// <param name="edgeThreshold"></param>
        /// <param name="minArea"></param>
        /// <param name="maxArea"></param>
        /// <param name="eumWhiteOrBlack"></param>
        /// <returns></returns>
        static   private CvBlobs RunBlob(Mat gray,  double edgeThreshold,
            int minArea,int maxArea, EumWhiteOrBlack eumWhiteOrBlack= EumWhiteOrBlack.White)
        {
          //  Mat cropMat = MatExtension.Crop_Mask_Mat(gray, roi);

            Mat binary = new Mat();

            if(eumWhiteOrBlack== EumWhiteOrBlack.White)
               Cv2.Threshold(gray, binary, edgeThreshold, 255, ThresholdTypes.Binary); //二值化
            else
                Cv2.Threshold(gray, binary, edgeThreshold, 255, ThresholdTypes.BinaryInv); //二值化      

            //Mat result = new Mat(cropMat.Size(), MatType.CV_8UC3);

            CvBlobs blobs = new CvBlobs();

            blobs.Label(binary);//斑点检测

            return blobs;
       
        }      
    
    
        /// <summary>
        /// Blob检测
        /// </summary>
        /// <param name="gray"></param>
        /// <param name="edgeThreshold"></param>
        /// <param name="minArea"></param>
        /// <param name="maxArea"></param>
        /// <param name="eumWhiteOrBlack"></param>
        /// <returns></returns>
        static public Blob3Result Run(Mat gray, 
                double edgeThreshold,int minArea,int maxArea, 
                        EumWhiteOrBlack eumWhiteOrBlack)

        {
            Blob3Result blob3Result = new Blob3Result();
            try
            {
              
                CvBlobs blobs = RunBlob(gray, edgeThreshold,minArea,
                                                   maxArea, eumWhiteOrBlack);

                Mat dst = new Mat();

                Cv2.CvtColor(gray, dst, ColorConversionCodes.GRAY2BGR);

                //blobs.RenderBlobs(gray, dst);//渲染斑点
                blob3Result.xlds.Clear();
                int text = 1; //数字         
                foreach (var item in blobs)
                {
                    if (item.Value.Area >= minArea &&
                               item.Value.Area <= maxArea) // 检查标签区域
                    {
                        CvBlob blob = item.Value;

                        List<CVPoint> points = blob.Contour.ConvertToPolygon();
                        List<List<CVPoint>> pointsList = new List<List<CVPoint>>();
                        CVPoint[] temP = points.ToArray<CVPoint>();

                        //for (int i = 0; i < temP.Length; i++)
                        //{
                        //    int x = temP[i].X;
                        //    int y = temP[i].Y;                         
                        //}
                        pointsList.Add(temP.ToList<CVPoint>());
                        blob3Result.xlds.Add(temP);
                       //获取重心点
                       //Moments M;
                       //M = Cv2.Moments(points);
                       //double cX = (M.M10 / M.M00);
                       //double cY = (M.M01 / M.M00);

                       //float a = (float)(M.M20 / M.M00 - cX * cX);
                       //float b = (float)(M.M11 / M.M00 - cX * cY);
                       //float c = (float)(M.M02 / M.M00 - cY * cY);
                       //计算角度
                       //double tanAngle = Cv2.FastAtan2(2 * b, (a - c)) / 2;

                       Cv2.DrawContours(dst, pointsList, 0, Scalar.Red);

                        double cx = blob.Centroid.X ;
                        double cy = blob.Centroid.Y ;
                        dst.drawCross(new CVPoint(cx, cy), Scalar.LimeGreen, 20, 2);

                        CVRRect cVRRect=    Cv2.MinAreaRect(pointsList[0]);
                        dst.DrawRotatedRect(cVRRect,Scalar.Orange);
                        //   Cv2.Circle(result, blob.Contour.StartingPoint, 8, Scalar.Red, 2, LineTypes.AntiAlias);
                        Cv2.PutText(dst, text.ToString(), new CVPoint(cx, cy),  //修改标签编号设置
                            HersheyFonts.HersheyComplex, 1, Scalar.Yellow, 2, LineTypes.AntiAlias);
                        text++;

                        blob3Result.positionData.Add(new Point2d(
                              cx, cy));
                        blob3Result.areas.Add(blob.Area);

                    }
                }

                blob3Result.resultToShow = dst;
                blob3Result.runStatus = true;

            }
            catch (Exception er)
            {
                Cv2.CvtColor(gray, blob3Result.resultToShow, ColorConversionCodes.GRAY2BGR);
               
                blob3Result.exceptionInfo = er.Message;
                blob3Result.runStatus = false;
            }
            return blob3Result;
        }

    }
    /// <summary>
    ///  Blob结果
    /// </summary>
    public class Blob3Result : Result
    {
        public Blob3Result()
        {
            positionData = new List<Point2d>();
            xlds = new List<CVPoint[]>();
            areas = new List<double>();
        }
        //中心点
        public List<Point2d> positionData;
        //轮廓点
        public List<CVPoint[]> xlds;
        //面积
        public List<double> areas;
    }

}
