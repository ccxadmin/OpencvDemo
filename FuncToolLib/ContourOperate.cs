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

namespace FuncToolLib
{
    public  class ContourOperate
    {

        /// <summary>
        /// 轮廓合并
        /// </summary>
        /// <param name="contour1"></param>
        /// <param name="contour2"></param>
        /// <returns></returns>
        static public CVPoint[] ContourCombine(CVPoint[] contour1, CVPoint[] contour2)
        {
            int length1 = contour1.Length;
            int length2 = contour2.Length;
            CVPoint[] outputArray = new CVPoint[length1 + length2];
            Array.ConstrainedCopy(contour1, 0, outputArray, 0, length1);
            Array.ConstrainedCopy(contour2, 0, outputArray, length1, length2);
           return outputArray;
        }
        /// <summary>
        /// 轮廓合并
        /// </summary>
        /// <param name="contour1"></param>
        /// <param name="contour2"></param>
        /// <returns></returns>
        static public CVPoint[] ContourCombine(CVPoint[][] contour1)
        {
            List<CVPoint> outputArray = new List<CVPoint>();
            List<CVPoint[]> contours = contour1.ToList<CVPoint[]>();
            int length1 = contours.Count;
            //Parallel.ForEach(contours, new Action<CVPoint[]>(p => {
            //    outputArray.AddRange(p.ToList());
            //}));
            //
            foreach(var p in contours)
                outputArray.AddRange(p.ToList());
            return outputArray.ToArray();
        }

        /// <summary>
        /// 轮廓合并并且提出公共部分
        /// </summary>
        /// <param name="contour1"></param>
        /// <param name="contour2"></param>
        /// <returns></returns>
        static public CVPoint[] ContourCombineWithoutCommon(CVPoint[] contour1, CVPoint[] contour2)
        {
            //int length1 = contour1.Length;
            //int length2 = contour2.Length;
            //CVPoint[] outputArray = new CVPoint[length1+ length2];
            //Array.ConstrainedCopy(contour1, 0, outputArray, 0, length1);
            //Array.ConstrainedCopy(contour2, 0, outputArray, length1, length2);

            CVPoint[] IntersectPoints = contour1.Intersect(contour2).ToArray();
            CVPoint[] ExceptPoints = contour1.Except(IntersectPoints).ToArray();
            CVPoint[] ExceptPoints2 = contour2.Except(IntersectPoints).ToArray();

            int length1 = ExceptPoints.Length;
            int length2 = ExceptPoints2.Length;
            CVPoint[] outputArray = new CVPoint[length1 + length2];
            Array.ConstrainedCopy(ExceptPoints, 0, outputArray, 0, length1);
            Array.ConstrainedCopy(ExceptPoints2, 0, outputArray, length1, length2);

            return outputArray;
        }




        /// <summary>
        /// 轮廓相减
        /// </summary>
        /// <param name="contour1"></param>
        /// <param name="contour2"></param>
        /// <returns></returns>
        static public CVPoint[] ContourSubtract(CVPoint[] contour1, CVPoint[] contour2)
        {
            //CVRect rect1 = Cv2.BoundingRect(contour1);
            //CVRect rect2 = Cv2.BoundingRect(contour2);
            //CVPoint[] intersectionPoints;//交集
            //Cv2.IntersectConvexConvex(contour1, contour2,out intersectionPoints);

            CVPoint[] IntersectPoints= contour1.Intersect(contour2).ToArray();
            CVPoint[] ExceptPoints = contour1.Except(IntersectPoints).ToArray();
          //  CVPoint[] ExceptPoints2 = contour2.Except(IntersectPoints).ToArray();
            return ExceptPoints;
        }

        static public CVPoint[][] ExceptBoundPoints( CVRect  rect,  CVPoint[][] contour1)
        {
            List<CVPoint[]> outputPoints = new List<CVPoint[]>();

           
            foreach (var p in contour1)
            {
                CVRect cVRect = Cv2.BoundingRect(p);
                if (cVRect!=rect)
                    outputPoints.Add(p);
            }
            return outputPoints.ToArray();


        }

        /// <summary>
        /// 轮廓剔除外接矩形边界点
        /// </summary>
        /// <param name="contour1"></param>
        /// <returns></returns>
        static public CVPoint[] ExceptBoundPoints(CVPoint[] contour1)
        {
            List<CVPoint> outputPoints = new List<CVPoint>();
            CVRect rect1 = Cv2.BoundingRect(contour1);
            int left = rect1.Left;
            int top = rect1.Top;
            int right = rect1.Right;
            int bottom = rect1.Bottom;

            foreach(var p in contour1)
                if ((p.X > left + 2 && p.X < right - 2) && (p.Y > top + 2 && p.Y < bottom - 2))
                    outputPoints.Add(p);
            //Parallel.ForEach(contour1, new Action<CVPoint>(p => {
            //    if ((p.X > left+2 && p.X < right-2) && (p.Y > top+2 && p.Y < bottom-2))
            //         outputPoints.Add(p);
            //}));
            return outputPoints.ToArray();
        }

    }
}
