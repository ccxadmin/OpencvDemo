/*----------------------------------------------------------------
// Copyright (C) 2022 ORIGITECH
// 版权所有
// 创建标识: Xp.Yang 2022/6/8 10:15:47
//
// 文件名: BlobDetectorTool
// 文件件功能描述： Use For
//
// 修改标识: Xp.Yang 2022/6/8 10:15:47
// 修改描述: The initial version
//----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenCvSharp;
using ParamDataLib;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using Point = System.Drawing.PointF;
using Rect = System.Drawing.RectangleF;

namespace FuncToolLib
{
    public  class BlobDetectorTool : ToolBase
    {
        public override StuResultOfToolRun Run<T>(Mat inputImg, T obj)
        {
            st.Start();

            StuResultOfToolRun stuResultOfToolRun = new StuResultOfToolRun();

            if (inputImg == null || inputImg.Width <= 0)
            {
                stuResultOfToolRun.runStatus = false;
                stuResultOfToolRun.exceptionInfo = "输入图像为空.";
                stuResultOfToolRun.detectTime = 0;
                return stuResultOfToolRun;
            }

            BlobDetectorParam param = obj as BlobDetectorParam;

            //ROI img
            CVRect rect = new CVRect((int)param.ROI.X, (int)param.ROI.Y, 
                (int)param.ROI.Width, (int)param.ROI.Height);
            var roi = CommonTool.GetRegional(inputImg.Width, inputImg.Height,
                                        rect);
            stuResultOfToolRun.detectRegion = roi;
            Mat detImg = new Mat(inputImg, roi);

            stuResultOfToolRun.resultToShow = new Mat();
            inputImg.CopyTo(stuResultOfToolRun.resultToShow);

            //method1
            //var keyPoints = SimapleBlobDetect(detImg, param);
            //Cv2.CvtColor(stuResultOfToolRun.resultToShow, stuResultOfToolRun.resultToShow, ColorConversionCodes.GRAY2BGR);

            ////Cv2.DrawKeypoints(stuResultOfToolRun.resultToShow, keyPoints, stuResultOfToolRun.resultToShow, Scalar.HotPink, DrawMatchesFlags.DrawRichKeypoints);
            ////draw cross
            //if (keyPoints != null)
            //{
            //    CVPoint pt = new CVPoint();
            //    foreach (var keyPoint in keyPoints)
            //    {
            //        pt.X = keyPoint.Pt.ToPoint().X + roi.Left;
            //        pt.Y = keyPoint.Pt.ToPoint().Y + roi.Top;
            //        CommonTool.drawCross(stuResultOfToolRun.resultToShow, pt,
            //                                        new Scalar(0, 255, 0), 10, 2);
            //    }
            //}

            //method2
            var coutours = ContourBlobDetect(detImg, param);
            Cv2.CvtColor(stuResultOfToolRun.resultToShow, stuResultOfToolRun.resultToShow, ColorConversionCodes.GRAY2BGR);

            for (int i = 0;i < coutours.Length;i++)
            {
                double area = Cv2.ContourArea( coutours[i]);
                Cv2.MinEnclosingCircle(coutours[i], out Point2f center, out float radius);

                if (param.FilterByArea)
                {
                    if (area < param.MinArea || area > param.MaxArea)
                        continue;
                }
                CommonTool.drawCross(stuResultOfToolRun.resultToShow, 
                                    new CVPoint((int)center.X,(int)center.Y),
                                    new Scalar(0, 255, 0), 10, 2);
                Cv2.DrawContours(stuResultOfToolRun.resultToShow, coutours, i, new Scalar(0, 255, 0), 2);
            }

            st.Stop();
            stuResultOfToolRun.runStatus = true;
            stuResultOfToolRun.detectTime = st.ElapsedMilliseconds;

            return stuResultOfToolRun;
        }

        /// <summary>
        /// blob检测 
        /// </summary>
        /// <param name="inImg"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public KeyPoint[] SimapleBlobDetect(Mat inImg, BlobDetectorParam parms)
        {

            //param load
            circleParams.MinThreshold = parms.MinThreshold;
            circleParams.MaxThreshold = parms.MaxThreshold;

            circleParams.FilterByColor = parms.FilterByColor;
            circleParams.BlobColor = parms.BlobColor;

            circleParams.FilterByArea = parms.FilterByArea;
            circleParams.MinArea = parms.MinArea;
            circleParams.MaxArea = parms.MaxArea;

            circleParams.FilterByCircularity = parms.FilterByCircularity;
            circleParams.MinCircularity = parms.MinCircularity;

            circleParams.FilterByConvexity = parms.FilterByConvexity;
            circleParams.MinConvexity = parms.MinConvexity;

            circleParams.FilterByInertia = parms.FilterByInertia;
            circleParams.MinInertiaRatio = parms.MinInertiaRatio;

            //generat detector          
            var detector = SimpleBlobDetector.Create(circleParams);

            var keyPoints = detector.Detect(inImg, parms.mask);

            //detect
            return keyPoints;
        }

        ///<summary>
        /// blob检测
        /// </summary>
        /// <param name="inImg"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public Mat[] ContourBlobDetect(Mat inImg, BlobDetectorParam parms)
        {
            Mat binImg = new Mat();
            Cv2.Threshold(inImg, binImg, parms.MinThreshold, parms.MaxThreshold, 
                ThresholdTypes.Binary | ThresholdTypes.Otsu);

            //detect
            Mat[] contours = Cv2.FindContoursAsMat(binImg,RetrievalModes.External,
                ContourApproximationModes.ApproxNone,new CVPoint((int)parms.ROI.X, (int)parms.ROI.Y));

            return contours;
        }

        // Parameters tuned to detect only circles
        SimpleBlobDetector.Params circleParams = new SimpleBlobDetector.Params
        {
            //Threshold Range
            MinThreshold = 10,
            MaxThreshold = 230,
            ThresholdStep = 10,

            FilterByColor = true,
            BlobColor = 0,

            // Area Range.
            FilterByArea = true,
            MinArea = 500,
            MaxArea = 50000,

            // Circularity is a ratio of the area to the perimeter.
            // Polygons with more sides are more circular.
            FilterByCircularity = true,
            MinCircularity = 0.9f,

            // Convexity is the ratio of the area of the blob to the area of its convex hull.
            FilterByConvexity = true,
            MinConvexity = 0.95f,

            // A circle's inertia ratio is 1. A line's is 0. An oval is between 0 and 1.
            FilterByInertia = true,
            MinInertiaRatio = 0.95f
        };
    }
}
