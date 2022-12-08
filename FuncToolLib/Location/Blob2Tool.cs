using OpenCvSharp;
using ParamDataLib;
using ParamDataLib.Location;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OpenCvSharp.ConnectedComponents;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using Rect = System.Drawing.RectangleF;
using Point = System.Drawing.PointF;

namespace FuncToolLib.Location
{
    /// <summary>
    /// Blob2检测
    /// </summary>
    public class Blob2Tool : ToolBase
    {

        public List<Blob> BlobList = new List<Blob>();
             
        /// <summary>
        /// Blob工具运行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gray"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Result Run<T>(Mat gray, T obj)
        {
            Blob2Data blob2Data = obj as Blob2Data;
            Result blob2Result = new Blob2Result();
          
            try
            {
                //获取检测掩膜图像
                Mat temimg=  MatExtension.Reducue_Mask_Mat(gray, (CVRect)blob2Data.ROI);
                ConnectedComponents Cc = Blob(temimg, blob2Data.minthd, blob2Data.maxthd);
                if (Cc is null || Cc.LabelCount <= 1)
                {
                    gray.CopyTo(blob2Result.resultToShow);
                    blob2Result.exceptionInfo = "未找到Blob粒子";
                    blob2Result.runStatus = false;
                    return blob2Result;
                }

                BlobList.Clear();

                //得到去除背景的blob列表
                List<Blob> Blobs = Cc.Blobs.Skip(1).OrderBy(b => b.Area).ToList();

                int AreaLargest = Blobs.LastOrDefault().Area;

              
                foreach (var blob in Blobs)
                {
                    if (blob.Area < blob2Data.stuBlobFilter.areaLow ||
                        blob.Area > blob2Data.stuBlobFilter.areaHigh ||
                       blob.Width < blob2Data.stuBlobFilter.widthLow ||
                       blob.Width > blob2Data.stuBlobFilter.widthHigh ||
                       blob.Height < blob2Data.stuBlobFilter.heightLow ||
                        blob.Height > blob2Data.stuBlobFilter.heightHigh)
                        continue;
                    BlobList.Add( blob); //添加满足条件的Blob粒子
                  
                }
                Mat Dst = new Mat();
               
                CVPoint[][] contours;
                Cv2.CvtColor(gray, Dst, ColorConversionCodes.GRAY2BGR);
                //检测区域
                Dst.Rectangle((CVRect)blob2Data.ROI, new Scalar(255, 0, 0), 2);
                Mat binary = new Mat();
                Cv2.Threshold(temimg, binary, blob2Data.minthd, blob2Data.maxthd, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                //Cv2.Threshold(temimg, binary,230, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                //获得轮廓
                Cv2.FindContours(binary, out contours, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
                //绘制轮廓
                int index = 0;
                foreach (var contour in contours)
                {
                    Cv2.DrawContours(Dst, contours, index, Scalar.Green, 2);
                    index++;
                }
            
                foreach (Blob s in BlobList)
                {
                   
                    ///绘制粒子的外接矩形
                   Dst.Rectangle(s.Rect, Scalar.Red,2);
                    ///绘制十字中心
                    Dst.drawCross(new CVPoint(s.Centroid.X, s.Centroid.Y), Scalar.Red,10, 2);
                    (blob2Result as Blob2Result).positionData.Add(new Point2d(
                        s.Centroid.X,s.Centroid.Y));
                
                }

                blob2Result.resultToShow = Dst;
                blob2Result.runStatus = true;
                //画出总Mask
                //if (BlobList.Count > 0)
                //{
                //    Dst = new Mat();
                //    Cc.FilterByBlobs(gray, Dst, BlobList);

                //}
                //else
                //{
                //    Dst = gray.EmptyClone();
                //}

            }
            catch(Exception er )
            {
                Cv2.CvtColor(gray, blob2Result.resultToShow, ColorConversionCodes.GRAY2BGR);
                //检测区域
                blob2Result.resultToShow.Rectangle((CVRect)blob2Data.ROI, new Scalar(255, 0, 0), 2);
                blob2Result.exceptionInfo = er.Message;
                blob2Result.runStatus = false;
            }
            return blob2Result;         
        }

        /// <summary>
        /// Blob检测
        /// </summary>
        /// <param name="gray"></param>
        /// <param name="minthd"></param>
        /// <param name="macthd"></param>
        /// <returns></returns>
        private ConnectedComponents Blob(Mat gray,double minthd=0,double maxthd=255)
        {
          
            Mat binary = gray.Threshold(minthd, maxthd, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            return  Cv2.ConnectedComponentsEx(binary);
          
        }

      
    }
    /// <summary>
    ///  Blob结果
    /// </summary>
    public class Blob2Result : Result
    {
        public Blob2Result()
        {
            positionData = new List<Point2d>();
        }
        public List<Point2d> positionData;
    }
}
