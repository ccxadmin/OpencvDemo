using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

using CVPoint = OpenCvSharp.Point;
using CVPointF = OpenCvSharp.Point2f;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using CVSize = OpenCvSharp.Size;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using VisionShowLib;
using ParamDataLib.Location;

namespace FuncToolLib
{
    public static class MatExtension
    {

        /// <summary>
        /// 图像子区域
        /// </summary>
        /// <param name="src"></param>
        /// <param name="cVRect"></param>
        /// <returns></returns>
        public static Mat  SubMat(Mat src,CVRect cVRect)
        {
            return src.SubMat(cVRect); //裁剪图像
        }

        /// <summary>
        /// 创建掩膜图像：与输入图大小一致
        /// </summary>
        /// <param name="src">输入图</param>
        /// <param name="ROI">掩膜区域</param>
        /// <returns></returns>
        public static Mat Reducue_Mask_Mat(Mat src, CVRect roi)
        {          
            Mat ImageROI = new Mat(src, roi);
            Mat des = new Mat(src.Size(), src.Type(), Scalar.All(0));
            Mat pos = new Mat(des, roi);
            // Mat pos = des[ROI];
            Mat mask = new Mat(ImageROI.Size(), ImageROI.Type(), Scalar.All(255));
            ImageROI.CopyTo(pos, mask);         
            return des;
        }
        /// <summary>
        /// 创建掩膜图像：与输入图大小一致
        /// </summary>
        /// <param name="src"></param>
        /// <param name="roi"></param>
        /// <returns></returns>
        public static Mat Reducue_Mask_Mat(Mat src, CVRRect roi)
        {        
            Mat mask = Mat.Zeros(src.Size(), MatType.CV_8UC1);
           
            Point2f[] pts = roi.Points();
          
            List<CVPoint> listt = new List<CVPoint>();
            for (int i = 0; i < pts.Count(); i++)
            {
                listt.Add(new CVPoint(pts[i].X, pts[i].Y));
            }

            List<List<CVPoint>> pp = new List<List<CVPoint>>() { listt };

            Cv2.FillPoly(mask, pp, new Scalar(255, 255, 255));
         
            Mat dst = new Mat();
            src.CopyTo(dst, mask);           
            return dst;
        }
        /// <summary>
        /// 创建掩膜图像：与输入图大小一致
        /// </summary>
        /// <param name="src"></param>
        /// <param name="roi"></param>
        /// <returns></returns>
        public static Mat Reducue_Mask_Mat(Mat src, CVCircle roi)
        {
            Mat mask = Mat.Zeros(src.Size(), MatType.CV_8UC3);
            Cv2.Circle(mask, new CVPoint(roi.Center.X, roi.Center.Y), (int)roi.Radius, Scalar.Red, 1, LineTypes.AntiAlias);
            Cv2.FloodFill(mask, new CVPoint(roi.Center.X, roi.Center.Y), Scalar.Red);
            //Mat maskRoi = new Mat();
            //mask.ConvertTo(maskRoi, MatType.CV_8UC1);
            Cv2.CvtColor(mask, mask, ColorConversionCodes.BGR2GRAY);

             Mat dst = new Mat();
            src.CopyTo(dst, mask);
         
            return dst;
        
        }

        /// <summary>
        /// 创建掩膜图像：与输入图大小一致
        /// </summary>
        /// <param name="src"></param>
        /// <param name="roi"></param>
        /// <returns></returns>
        public static Mat Reducue_Mask_Mat(Mat src, CVPoint[] roi)
        {
            Mat mask = Mat.Zeros(src.Size(), MatType.CV_8UC1);
      
            List<List<CVPoint>> pp = new List<List<CVPoint>>() { roi.ToList<CVPoint>() };

            Cv2.FillPoly(mask, pp, new Scalar(255, 255, 255));

            Mat dst = new Mat();
            src.CopyTo(dst, mask);
            return dst;
        }
     
        /// <summary>
        /// 裁切掩膜图像
        /// </summary>
        /// <param name="src">输入图</param>
        /// <param name="roi">ROI区域</param>
        /// <returns></returns>
        public static Mat Crop_Mask_Mat(Mat src, CVRect roi)
        {
           return new Mat(src, roi);
        }
   
        /// <summary>
        /// 裁切掩膜图像
        /// </summary>
        /// <param name="src"></param>
        /// <param name="roi"></param>
        /// <param name="BoundingRect"></param>
        /// <param name="eumWhiteOrBlack"></param>
        /// <returns></returns>
        public static Mat Crop_Mask_Mat(Mat src, CVRRect roi, out CVRect BoundingRect,
                 EumWhiteOrBlack eumWhiteOrBlack = EumWhiteOrBlack.White)
        {
                     
            BoundingRect = Cv2.BoundingRect(roi.Points());
            Mat mask = new Mat(src.Size(), MatType.CV_8UC3,Scalar.Black);
            mask.DrawRotatedRect(roi, Scalar.White, 1);           
            Cv2.FloodFill(mask, new CVPoint(roi.Center.X, roi.Center.Y), Scalar.White);
            mask.ConvertTo(mask, MatType.CV_8UC1);
            Mat _maskRoI = new Mat(mask, BoundingRect);
            Cv2.CvtColor(_maskRoI, _maskRoI, ColorConversionCodes.BGR2GRAY);
            Mat buf = new Mat();
            _maskRoI.CopyTo(buf);

            if(eumWhiteOrBlack == EumWhiteOrBlack.Black)
                //# 黑白反转
                buf =  Image_Reverse2(_maskRoI);
          
            Mat src2 = new Mat(src, BoundingRect);        
            Mat dst = new Mat();
            if(eumWhiteOrBlack == EumWhiteOrBlack.White)
            {
                Cv2.BitwiseAnd(src2, src2, dst, _maskRoI);
                return dst;
            }
            else
            {
                Cv2.BitwiseAnd(src2, src2, dst, _maskRoI);
                Mat dst2 = new Mat();
                Cv2.BitwiseOr(buf, dst, dst2);
                return dst2;
            }
           
        }

        /// <summary>
        /// 裁切多边形
        /// </summary>
        /// <param name="src"></param>
        /// <param name="roi"></param>
        /// <param name="BoundingRect"></param>
        /// <returns></returns>
        public static Mat Crop_Mask_Mat(Mat src, CVPoint[] roi, out CVRect BoundingRect)
        {
            Mat mask = Mat.Zeros(src.Size(), MatType.CV_8UC1);
            
            List<List<CVPoint>> pp = new List<List<CVPoint>>() { roi.ToList<CVPoint>() };

            Cv2.FillPoly(mask, pp, new Scalar(255, 255, 255));
            BoundingRect = Cv2.BoundingRect(roi);
            if((BoundingRect.X+ BoundingRect.Width<= src.Width)&&
                    (BoundingRect.Y+ BoundingRect.Height<= src.Height))
            {
                Mat src2 = new Mat(src, BoundingRect);

                Mat maskROI = new Mat(mask, BoundingRect);
                Mat dst = new Mat();
                Cv2.BitwiseAnd(src2, src2, dst, maskROI);
                return dst;
            }
            return mask;
        }
        /// <summary>
        /// 裁切掩膜图像
        /// </summary>
        /// <param name="src"></param>
        /// <param name="roi"></param>
        /// <returns></returns>
        public static Mat Crop_Mask_Mat(Mat src, CVCircle roi)
        {
            Mat mask = Mat.Zeros(src.Size(), MatType.CV_8UC3);
            Cv2.Circle(mask, new CVPoint(roi.Center.X, roi.Center.Y), (int)roi.Radius, Scalar.Red, 1, LineTypes.AntiAlias);
            Cv2.FloodFill(mask, new CVPoint(roi.Center.X, roi.Center.Y), Scalar.Red);

            mask.ConvertTo(mask, MatType.CV_8UC1);
            CVRect cVRect = new CVRect((int)(roi.Center.X - roi.Radius), (int)(roi.Center.Y - roi.Radius),
              (int)roi.Radius * 2, (int)roi.Radius * 2);
            Mat src2 = new Mat(src, cVRect);
            Mat maskRoI = new Mat(mask, cVRect);

            Cv2.CvtColor(maskRoI, maskRoI, ColorConversionCodes.BGR2GRAY);

            Mat dst = new Mat();
            Cv2.BitwiseAnd(src2, src2, dst, maskRoI);

            return dst;
        }


        /// <summary>
        /// 黑白反转
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Mat Image_Reverse(Mat src)
        {
            Mat dst = new Mat(src.Size(), MatType.CV_8UC1);
           
            for (int i = 0; i < src.Height; i++)
                for (int j = 0; j < src.Width; j++)
                {
                    int pixelvalue = src.At<int>(i, j);
                    dst.Set<int>(i, j, 255 - pixelvalue) ;
                }

            return dst;
        }
        /// <summary>
        ///  黑白反转2
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Mat Image_Reverse2(Mat src)
        {
            Mat dst = new Mat();
            Cv2.BitwiseNot(src,dst);
            return dst;
          
        }

        /// <summary>
        /// 获取有效区域
        /// </summary>
        /// <param name="TotalWith">图像宽度</param>
        /// <param name="TotalHeight">图像高度</param>
        /// <param name="detectROI">检测区域</param>
        /// <returns></returns>
        static public CVRect GetRegional(int TotalWith, int TotalHeight, CVRect detectROI)
        {
            var TotalRect = new CVRect(0, 0, TotalWith, TotalHeight);
            TotalRect = TotalRect.Intersect(new CVRect((int)detectROI.Left, (int)detectROI.Top,
                (int)detectROI.Width, (int)detectROI.Height));
            if (TotalRect.Width == 0 || TotalRect.Height == 0)
                TotalRect = new CVRect(0, 0, TotalWith, TotalHeight);
            return TotalRect;
        }

        static public CVPoint[] GetSectorF( SectorF  sectorF, SectorF sectorF2)
        {
            CVPoint[] points= Cv2.Ellipse2Poly(new CVPoint(sectorF.centreP.X, sectorF.centreP.Y),
                  new CVSize(sectorF.width/2, sectorF.height/2),0,
                  (int)sectorF.startAngle, (int)sectorF.getEndAngle, 1);
            CVPoint[]  points1 = Cv2.Ellipse2Poly(new CVPoint(sectorF2.centreP.X, sectorF2.centreP.Y),
                  new CVSize(sectorF2.width/2, sectorF2.height/2), 0,
                  (int)sectorF2.startAngle, (int)sectorF2.getEndAngle, 1);
            List<CVPoint> points3 = new List<CVPoint>();
            points3.AddRange(points.ToList<CVPoint>());
            points3.Reverse();
            points3.AddRange(points1.ToList<CVPoint>());
       //     CVPoint[]  points2=  Cv2.ApproxPolyDP(points3, 1,true);

       
            return points3.ToArray<CVPoint>();
           //   Cv2.FillConvexPoly;
          //Cv2.Polylines
        }
        static public Mat ExtractROI(Mat src,Mat mask, CVPoint Center, CVRect rect)
        { 
            //水漫的种子点 要求在ROI区域内部
           CVPoint pt = new CVPoint(Center.X, Center.Y);
            Cv2.FloodFill(mask, pt, Scalar.Red);
            mask.ConvertTo(mask, MatType.CV_8UC1);
                 
            Mat tem1 = new Mat(src, rect);
            Mat maskRoI = new Mat(mask, rect);
            Cv2.CvtColor(maskRoI, maskRoI, ColorConversionCodes.BGR2GRAY);
            Mat dstImg = new Mat();
            Cv2.BitwiseAnd(tem1, tem1, dstImg, maskRoI);
            return dstImg;
        }
 
        /// <summary>
        /// 绘制十字
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="point">绘制中心点</param>
        /// <param name="color">绘制颜色</param>
        /// <param name="size">绘制尺寸</param>
        /// <param name="thickness">线条厚度</param>
        static public void drawCross(this Mat img, CVPoint point, Scalar color, int size, int thickness)
        {
            //绘制横线
            CVPoint P11 = new CVPoint(point.X - size / 2, point.Y);
            CVPoint P12 = new CVPoint(point.X + size / 2, point.Y);
            Cv2.Line(img, P11, P12, color, thickness, LineTypes.Link8, 0);
            //绘制竖线
            CVPoint P21 = new CVPoint(point.X, point.Y - size / 2);
            CVPoint P22 = new CVPoint(point.X, point.Y + size / 2);
            Cv2.Line(img, P21, P22, color, thickness, LineTypes.Link8, 0);

        }
              
        /// <summary>
        /// 绘制旋转矩形
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="rect">绘制的旋转矩形</param>
        public static void DrawRotatedRect(this Mat img, RotatedRect rrect)
        {
            var pts = rrect.Points();
            CVPoint pt1 = new CVPoint(pts[pts.Length - 1].X, pts[pts.Length - 1].Y);
            for (int i = 0; i < pts.Length; i++)
            {
                CVPoint pt2 = new CVPoint(pts[i].X, pts[i].Y);
                Cv2.Line(img, pt1, pt2, new Scalar(255, 0, 0),1);
                pt1 = pt2;
            }
        }

        /// <summary>
        /// 绘制旋转矩形
        /// </summary>
        /// <param name="img"></param>
        /// <param name="rrect"></param>
        /// <param name="color"></param>
        /// <param name="thickness"></param>
        /// <param name="lineType"></param>
        public static void DrawRotatedRect(this Mat img, RotatedRect rrect,
              Scalar color, int thickness , LineTypes lineType = LineTypes.Link8)
        {
            var pts = rrect.Points();
            CVPoint pt1 = new CVPoint(pts[pts.Length - 1].X, pts[pts.Length - 1].Y);
            for (int i = 0; i < pts.Length; i++)
            {
                CVPoint pt2 = new CVPoint(pts[i].X, pts[i].Y);
                Cv2.Line(img, pt1, pt2, color, thickness, lineType);
                pt1 = pt2;
            }
        }
        /// <summary>
        /// 获取旋转矩形的外接矩形
        /// </summary>
        /// <param name="roi"></param>
        public static CVRect GetRect(RotatedRect roi)
        {
            Point2f[] pts = roi.Points();

            List<CVPoint> listt = new List<CVPoint>();
            for (int i = 0; i < pts.Count(); i++)
            {
                listt.Add(new CVPoint(pts[i].X, pts[i].Y));
            }

            List<List<CVPoint>> pp = new List<List<CVPoint>>() { listt };
            
           return Cv2.BoundingRect(pts);
        }

        /// <summary>
        /// 获取圆形的外接矩形
        /// </summary>
        /// <param name="roi"></param>
        public static CVRect GetRect(CVCircle roi)
        {
            CVRect cVRect = new CVRect((int)(roi.Center.X - roi.Radius), (int)(roi.Center.Y - roi.Radius),
             (int)roi.Radius * 2, (int)roi.Radius * 2);

            return cVRect;
        }
        /// <summary>
        /// 图像深度复制
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        static public Bitmap DeepClone(Bitmap bitmap)
        {
            Bitmap dstBitmap = null;
            using (MemoryStream mStream = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(mStream, bitmap);
                mStream.Seek(0, SeekOrigin.Begin);//指定当前流的位置为流的开头。
                dstBitmap = (Bitmap)bf.Deserialize(mStream);
                mStream.Close();
            }
            return dstBitmap;
        }
        /// <summary>
        /// bitmap转gray mat
        /// </summary>
        /// <param name="bitmap">输入bitmap</param>
        /// <returns></returns>
        public static Mat BitmapToGrayMat(System.Drawing.Bitmap bitmap)
        {
            Mat mat = BitmapConverter.ToMat(bitmap);
            if (mat.Channels() == 3)
            {
                Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2GRAY);
            }
            else if (mat.Channels() == 4)
            {
                Cv2.CvtColor(mat, mat, ColorConversionCodes.BGRA2GRAY);
            }
            return mat;
        }

       
        /// <summary>
        /// 将点ps绕0，0点旋转和平移后与点位pt重合并返回相应矩阵
        /// </summary>
        /// <param name="ps"></param>
        /// <param name="pt"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Mat getMat(CVPointF ps, CVPointF pt, double angle=0)
        {
           
            CVPointF _ps = Calibration.AxisCoorditionRotation.get_after_RotatePoint(ps,new CVPointF(0,0), angle);
            float Tx = pt.X - _ps.X;
            float Ty = pt.Y - _ps.Y;
            Mat m = Cv2.GetRotationMatrix2D(new CVPointF(0, 0), -angle, 1.0);
            var mIndex = m.GetGenericIndexer<double>();
            mIndex[0, 2] += Tx;
            mIndex[1, 2] += Ty;
            return m;
        }

        /// <summary>
        /// 画旋转矩形
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="rr"></param>
        /// <param name="scalar"></param>
        /// <param name="thickness"></param>
        public static void DrawRotatedRect(this Mat mat, RotatedRect rr, Scalar scalar, int thickness = 2)
        {
            ////通过4个个顶点画4条线形成矩形
            var P = rr.Points();
            for (int j = 0; j <= 3; j++)
            {
                Cv2.Line(mat, (CVPoint)P[j], (CVPoint)P[(j + 1) % 4], scalar, thickness);
            }
        }
        /// <summary>
        /// 多边形区域填充
        /// </summary>
        /// <param name="src"></param>
        /// <param name="points"></param>
        public static void FillPolygon(this Mat src, List<CVPoint> points)
        {
            List<List<CVPoint>> polygons = new List<List<CVPoint>>() { points };
            Cv2.FillPoly(src, polygons, Scalar.White);
        }
        /// <summary>
        ///  多边形区域填充
        /// </summary>
        /// <param name="src"></param>
        /// <param name="rr"></param>
        public static void FillPolygon(this Mat src, RotatedRect rr)
        {
            var P = rr.Points().Select(p => new CVPoint(p.X, p.Y)).ToArray();
            var pp = new CVPoint[1][] { P };
            Cv2.FillPoly(src, pp, Scalar.White);
        }
        /// <summary>
        /// 多边形绘制
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="points"></param>
        /// <param name="thickness"></param>
        public static void DrawPolygon(this Mat mat, Point2f[] points, int thickness = 1)
        {
            if (points.Length < 2) return;

            for (int j = 0; j <= points.Length - 1; j++)
            {
                Cv2.Line(mat, (CVPoint)points[j], (CVPoint)points[(j + 1) % points.Length], Scalar.RandomColor(), thickness);
            }
        }
        /// <summary>
        /// 多边形绘制
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="points"></param>
        /// <param name="thickness"></param>
        public static void DrawPolygon(this Mat mat, List<CVPoint> points, int thickness = 1)
        {
            if (points.Count < 2) return;

            for (int j = 0; j <= points.Count - 1; j++)
            {
                Cv2.Line(mat, points[j], points[(j + 1) % points.Count], Scalar.RandomColor(), thickness);
            }
        }
        /// <summary>
        /// 图像绕点0，0旋转
        /// </summary>
        /// <param name="src">输入</param>     
        /// <param name="angle">角度</param>
        /// <returns> 返回仿射变换后的完整图形 </returns>
        public static Mat Rotate(this Mat src, float angle)
        {
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            Mat dst = new Mat();

            //变换矩阵
            //  cos (angle)  sin(angle)
            //  -sin(angle)  cos(angle)

            Mat rot = Cv2.GetRotationMatrix2D(new Point2f(0, 0), angle, 1);

            //X==0 Y==0
            var w1 = rot.At<double>(0, 2);
            var h1 = rot.At<double>(1, 2);

            //Y==0
            var w2 = src.Width * rot.At<double>(0, 0) + rot.At<double>(0, 2);
            var h2 = src.Width * rot.At<double>(1, 0) + rot.At<double>(1, 2);

            //x==0
            var w3 = src.Height * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var h3 = src.Height * rot.At<double>(1, 1) + rot.At<double>(1, 2);

            var w4 = src.Width * rot.At<double>(0, 0) + src.Height * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var h4 = src.Width * rot.At<double>(1, 0) + src.Height * rot.At<double>(1, 1) + rot.At<double>(1, 2);

            CVPoint[] points = { new CVPoint(w1, h1), new CVPoint(w2, h2), new CVPoint(w3, h3), new CVPoint(w4, h4) };

            CVRect rRect = Cv2.BoundingRect(points);

            if(angle >= 0 && angle <= 90)
            {
                rot.Set<double>(0, 2, 0);
                rot.Set<double>(1, 2, h1 - h2);
            }
            else if(angle > 90 && angle <= 180)
            {
                rot.Set<double>(0, 2, -w2 + w1);
                rot.Set<double>(1, 2, rRect.Height);
            }
            else if(angle > 180 && angle <= 270)
            {
                rot.Set<double>(0, 2, rRect.Width);
                rot.Set<double>(1, 2, h1 - h3);
            }
            else if (angle > 270)
            {
                rot.Set<double>(0, 2, w1 - w3);
                rot.Set<double>(1, 2, 0);
            }         
            Cv2.WarpAffine(src, dst, rot, rRect.Size);
            return dst;
        }
        public static Mat RotateOfSizeNoChange(this Mat src, float angle)
        {
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            Mat dst = new Mat();

            //变换矩阵
            //  cos (angle)  sin(angle)
            //  -sin(angle)  cos(angle)

            Mat rot = Cv2.GetRotationMatrix2D(new Point2f(0, 0), angle, 1);

          
            Cv2.WarpAffine(src, dst, rot, src.Size());
            return dst;
        }
        public static Mat Rotate(this Mat src, float angle, ref Point2f point)
        {
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            Mat dst = new Mat();

            //变换矩阵
            //  cos (angle)  sin(angle)
            //  -sin(angle)  cos(angle)

            Mat rot = Cv2.GetRotationMatrix2D(new Point2f(0, 0), angle, 1);

            //X==0 Y==0
            var w1 = rot.At<double>(0, 2);
            var h1 = rot.At<double>(1, 2);

            //Y==0
            var w2 = src.Width * rot.At<double>(0, 0) + rot.At<double>(0, 2);
            var h2 = src.Width * rot.At<double>(1, 0) + rot.At<double>(1, 2);

            //x==0
            var w3 = src.Height * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var h3 = src.Height * rot.At<double>(1, 1) + rot.At<double>(1, 2);

            var w4 = src.Width * rot.At<double>(0, 0) + src.Height * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var h4 = src.Width * rot.At<double>(1, 0) + src.Height * rot.At<double>(1, 1) + rot.At<double>(1, 2);

            CVPoint[] points = { new CVPoint(w1, h1), new CVPoint(w2, h2), new CVPoint(w3, h3), new CVPoint(w4, h4) };

            CVRect rRect = Cv2.BoundingRect(points);

            if(angle >= 0 && angle <= 90)
            {
                rot.Set<double>(0, 2, 0);
                rot.Set<double>(1, 2, h1 - h2);
            }
            else if(angle > 90 && angle <= 180)
            {
                rot.Set<double>(0, 2, -w2 + w1);
                rot.Set<double>(1, 2, rRect.Height);
            }
            else if (angle > 180 && angle <= 270)
            {
                rot.Set<double>(0, 2, rRect.Width);
                rot.Set<double>(1, 2, h1 - h3);
            }
            else if(angle > 270)
            {
                rot.Set<double>(0, 2, w1 - w3);
                rot.Set<double>(1, 2, 0);
            }
          
            var x = point.X * rot.At<double>(0, 0) + point.Y * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var y = point.X * rot.At<double>(1, 0) + point.Y * rot.At<double>(1, 1) + rot.At<double>(1, 2);
            point.X = (int)Math.Round(x, 0);
            point.Y = (int)Math.Round(y, 0);
            Cv2.WarpAffine(src, dst, rot, rRect.Size);
            return dst;
        }
        public static Mat RotateOfSizeNoChange(this Mat src, float angle, ref Point2f point)
        {
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            Mat dst = new Mat();

            //变换矩阵
            //  cos (angle)  sin(angle)
            //  -sin(angle)  cos(angle)

            Mat rot = Cv2.GetRotationMatrix2D(new Point2f(0, 0), angle, 1);

       
            var x = point.X * rot.At<double>(0, 0) + point.Y * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var y = point.X * rot.At<double>(1, 0) + point.Y * rot.At<double>(1, 1) + rot.At<double>(1, 2);
            point.X = (int)Math.Round(x, 0);
            point.Y = (int)Math.Round(y, 0);
            Cv2.WarpAffine(src, dst, rot, src.Size());
            return dst;
        }
        public static Mat Rotate(this Mat src, float angle, ref CVPoint point)
        {
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            Mat dst = new Mat();

            //变换矩阵
            //  cos (angle)  sin(angle)
            //  -sin(angle)  cos(angle)

            Mat rot = Cv2.GetRotationMatrix2D(new Point2f(0, 0), angle, 1);

            //X==0 Y==0
            var w1 = rot.At<double>(0, 2);
            var h1 = rot.At<double>(1, 2);

            //Y==0
            var w2 = src.Width * rot.At<double>(0, 0) + rot.At<double>(0, 2);
            var h2 = src.Width * rot.At<double>(1, 0) + rot.At<double>(1, 2);

            //x==0
            var w3 = src.Height * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var h3 = src.Height * rot.At<double>(1, 1) + rot.At<double>(1, 2);

            var w4 = src.Width * rot.At<double>(0, 0) + src.Height * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var h4 = src.Width * rot.At<double>(1, 0) + src.Height * rot.At<double>(1, 1) + rot.At<double>(1, 2);

            CVPoint[] points = { new CVPoint(w1, h1), new CVPoint(w2, h2), new CVPoint(w3, h3), new CVPoint(w4, h4) };

            CVRect rRect = Cv2.BoundingRect(points);

            if (angle >= 0 && angle <= 90)
            {
                rot.Set<double>(0, 2, 0);
                rot.Set<double>(1, 2, h1 - h2);
            }
            else if (angle > 90 && angle <= 180)
            {
                rot.Set<double>(0, 2, -w2 + w1);
                rot.Set<double>(1, 2, rRect.Height);
            }
            else if (angle > 180 && angle <= 270)
            {
                rot.Set<double>(0, 2, rRect.Width);
                rot.Set<double>(1, 2, h1 - h3);
            }
            else if (angle > 270)
            {
                rot.Set<double>(0, 2, w1 - w3);
                rot.Set<double>(1, 2, 0);
            }

            var x = point.X * rot.At<double>(0, 0) + point.Y * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var y = point.X * rot.At<double>(1, 0) + point.Y * rot.At<double>(1, 1) + rot.At<double>(1, 2);
            point.X = (int)Math.Round(x, 0);
            point.Y = (int)Math.Round(y, 0);
            Cv2.WarpAffine(src, dst, rot, rRect.Size);
            return dst;
        }
        public static Mat RotateOfSizeNoChange(this Mat src, float angle, ref CVPoint point)
        {
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            Mat dst = new Mat();

            //变换矩阵
            //  cos (angle)  sin(angle)
            //  -sin(angle)  cos(angle)

            Mat rot = Cv2.GetRotationMatrix2D(new Point2f(0, 0), angle, 1);

     
            var x = point.X * rot.At<double>(0, 0) + point.Y * rot.At<double>(0, 1) + rot.At<double>(0, 2);
            var y = point.X * rot.At<double>(1, 0) + point.Y * rot.At<double>(1, 1) + rot.At<double>(1, 2);
            point.X = (int)Math.Round(x, 0);
            point.Y = (int)Math.Round(y, 0);
            Cv2.WarpAffine(src, dst, rot, src.Size());
            return dst;
        }

        /// <summary>
        /// 将图像按像素坐标系，绕图像中心旋转指定角度
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="angle">单位度</param>
        /// <returns></returns>
        public static Mat RotateAffine(this Mat img, double angle)
        { 
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            // 计算旋转后的图像尺寸
            int w0 = img.Width, h0 = img.Height;
            double arc = -Math.PI * angle / 180f;
            double c = Math.Cos(arc);
            double s = Math.Sin(arc);
            double minx = Math.Min(Math.Min(w0 * c - h0 * s, 0), Math.Min(w0 * c, -h0 * s));
            double maxx = Math.Max(Math.Max(w0 * c - h0 * s, 0), Math.Max(w0 * c, -h0 * s));
            double miny = Math.Min(Math.Min(w0 * s + h0 * c, 0), Math.Min(w0 * s, h0 * c));
            double maxy = Math.Max(Math.Max(w0 * s + h0 * c, 0), Math.Max(w0 * s, h0 * c));
            float w = (float)(maxx - minx);
            float h = (float)(maxy - miny);

            Point2f center = new Point2f(w0 / 2f, h0 / 2f);
            Mat m = Cv2.GetRotationMatrix2D(center, angle, 1.0);
            // 将原图像中心平移到旋转后的图像中心
            var mIndex = m.GetGenericIndexer<double>();
            mIndex[0, 2] += (w - w0) / 2f;
            mIndex[1, 2] += (h - h0) / 2f;
        
            Mat rotated = new Mat();
            Cv2.WarpAffine(img, rotated, m, new CVSize(w, h));
            return rotated;
        }

        public static void RotateAffine2(this Mat img, double angle, ref Point2f point, ref float Tx, ref float Ty)
        {
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            // 计算旋转后的图像尺寸
            int w0 = img.Width, h0 = img.Height;
            double arc = -Math.PI * angle / 180f;
            double c = Math.Cos(arc);
            double s = Math.Sin(arc);
            double minx = Math.Min(Math.Min(w0 * c - h0 * s, 0), Math.Min(w0 * c, -h0 * s));
            double maxx = Math.Max(Math.Max(w0 * c - h0 * s, 0), Math.Max(w0 * c, -h0 * s));
            double miny = Math.Min(Math.Min(w0 * s + h0 * c, 0), Math.Min(w0 * s, h0 * c));
            double maxy = Math.Max(Math.Max(w0 * s + h0 * c, 0), Math.Max(w0 * s, h0 * c));
            float w = (float)(maxx - minx);
            float h = (float)(maxy - miny);

            Point2f center = new Point2f(w0 / 2f, h0 / 2f);
            Mat m = Cv2.GetRotationMatrix2D(center, angle, 1.0);
            // 将原图像中心平移到旋转后的图像中心
            var mIndex = m.GetGenericIndexer<double>();
            Tx = (w - w0) / 2f;
            Ty = (h - h0) / 2f;
            mIndex[0, 2] += (w - w0) / 2f;
            mIndex[1, 2] += (h - h0) / 2f;

            var x = point.X * m.At<double>(0, 0) + point.Y * m.At<double>(0, 1) + m.At<double>(0, 2);
            var y = point.X * m.At<double>(1, 0) + point.Y * m.At<double>(1, 1) + m.At<double>(1, 2);
            point.X = (float)Math.Round(x, 3);
            point.Y = (float)Math.Round(y, 3);
        }
        /// <summary>
        /// 图像不裁剪方式旋转
        /// </summary>
        /// <param name="img"></param>
        /// <param name="angle"></param>
        /// <param name="point"></param>
        /// <param name="Tx"></param>
        /// <param name="Ty"></param>
        /// <returns></returns>
        public static Mat RotateAffine(this Mat img, double angle, ref Point2f point,ref float Tx,ref float Ty)
        {
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            // 计算旋转后的图像尺寸
            int w0 = img.Width, h0 = img.Height;
            double arc = -Math.PI * angle / 180f;
            double c = Math.Cos(arc);
            double s = Math.Sin(arc);
            double minx = Math.Min(Math.Min(w0 * c - h0 * s, 0), Math.Min(w0 * c, -h0 * s));
            double maxx = Math.Max(Math.Max(w0 * c - h0 * s, 0), Math.Max(w0 * c, -h0 * s));
            double miny = Math.Min(Math.Min(w0 * s + h0 * c, 0), Math.Min(w0 * s, h0 * c));
            double maxy = Math.Max(Math.Max(w0 * s + h0 * c, 0), Math.Max(w0 * s, h0 * c));
            float w = (float)(maxx - minx);
            float h = (float)(maxy - miny);

            Point2f center = new Point2f(w0/ 2f, h0/ 2f);
            Mat m = Cv2.GetRotationMatrix2D(center, angle, 1.0);
            // 将原图像中心平移到旋转后的图像中心
            var mIndex = m.GetGenericIndexer<double>();
            Tx= (w - w0) / 2f;
            Ty= (h - h0) / 2f;
            mIndex[0, 2] += (w - w0) / 2f;
            mIndex[1, 2] += (h - h0) / 2f;

            var x = point.X * m.At<double>(0, 0) + point.Y * m.At<double>(0, 1) + m.At<double>(0, 2);
            var y = point.X * m.At<double>(1, 0) + point.Y * m.At<double>(1, 1) + m.At<double>(1, 2);
            point.X = (float)Math.Round(x, 3);
            point.Y = (float)Math.Round(y, 3);
        
            Mat rotated = new Mat();
         //   int t2 = Environment.TickCount;
            Cv2.WarpAffine(img, rotated, m, new CVSize(w, h));
          //  Console.WriteLine(string.Format("LinearCaliperTool:{0}", Environment.TickCount - t2));
            return rotated;
        }
        /// <summary>
        /// 旋转复位
        /// </summary>
        /// <param name="img"></param>
        /// <param name="angle"></param>
        /// <param name="Tx"></param>
        /// <param name="Ty"></param>
        /// <param name="point"></param>
        public static void RotateAffineINV(this Mat img, double angle,float Tx,float Ty, ref Point2f point)
        {
            int w0 = img.Width, h0 = img.Height;
            Point2f center = new Point2f(w0 / 2f, h0 / 2f);
            Mat m = Cv2.GetRotationMatrix2D(center, angle, 1.0);
            var mIndex = m.GetGenericIndexer<double>();
            mIndex[0, 2] += Tx;
            mIndex[1, 2] += Ty;
            var x = point.X * m.At<double>(0, 0) + point.Y * m.At<double>(0, 1) + m.At<double>(0, 2);
            var y = point.X * m.At<double>(1, 0) + point.Y * m.At<double>(1, 1) + m.At<double>(1, 2);
            point.X = (float)Math.Round(x, 3);
            point.Y = (float)Math.Round(y, 3);
        }
        /// <summary>
        /// 旋转复位
        /// </summary>
        /// <param name="img"></param>
        /// <param name="angle"></param>
        /// <param name="Tx"></param>
        /// <param name="Ty"></param>
        /// <param name="point"></param>
        public static void RotateAffineINV(this Mat img, double angle, float Tx, float Ty, ref CVPoint point)
        {
            int w0 = img.Width, h0 = img.Height;
            Point2f center = new Point2f(w0 / 2f, h0 / 2f);
            Mat m = Cv2.GetRotationMatrix2D(center, angle, 1.0);
            var mIndex = m.GetGenericIndexer<double>();
            mIndex[0, 2] += Tx;
            mIndex[1, 2] += Ty;
            var x = point.X * m.At<double>(0, 0) + point.Y * m.At<double>(0, 1) + m.At<double>(0, 2);
            var y = point.X * m.At<double>(1, 0) + point.Y * m.At<double>(1, 1) + m.At<double>(1, 2);
            point.X = (int)Math.Round(x, 0);
            point.Y = (int)Math.Round(y, 0);
        }
        public static Mat RotateAffineOfSizeNoChange(this Mat img, double angle)
        {   
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            // 计算旋转后的图像尺寸
            int w0 = img.Width, h0 = img.Height;
         
            Point2f center = new Point2f(w0 / 2f, h0 / 2f);
            Mat m = Cv2.GetRotationMatrix2D(center, angle, 1.0);
  
            Mat rotated = new Mat();
            Cv2.WarpAffine(img, rotated, m, img.Size());
            return rotated;
        }
        public static Mat RotateAffineOfSizeNoChange(this Mat img, double angle,ref CVPoint point)
        {
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            // 计算旋转后的图像尺寸
            int w0 = img.Width, h0 = img.Height;
            float w = img.Width, h = img.Height;
            //double arc = -Math.PI * angle / 180f;
            //double c = Math.Cos(arc);
            //double s = Math.Sin(arc);
            //double minx = Math.Min(Math.Min(w0 * c - h0 * s, 0), Math.Min(w0 * c, -h0 * s));
            //double maxx = Math.Max(Math.Max(w0 * c - h0 * s, 0), Math.Max(w0 * c, -h0 * s));
            //double miny = Math.Min(Math.Min(w0 * s + h0 * c, 0), Math.Min(w0 * s, h0 * c));
            //double maxy = Math.Max(Math.Max(w0 * s + h0 * c, 0), Math.Max(w0 * s, h0 * c));
            //float w = (float)(maxx - minx);
            //float h = (float)(maxy - miny);
            Point2f center = new Point2f(w0 / 2f, h0 / 2f);
            Mat m = Cv2.GetRotationMatrix2D(center, angle, 1.0);
            if (angle == 90 || angle == 270)
            {
                w = img.Height;
                h = img.Width;
                var mIndex = m.GetGenericIndexer<double>();
                mIndex[0, 2] += (w - w0) / 2f;
                mIndex[1, 2] += (h - h0) / 2f;
            }

            var x = point.X * m.At<double>(0, 0) + point.Y * m.At<double>(0, 1) + m.At<double>(0, 2);
            var y = point.X * m.At<double>(1, 0) + point.Y * m.At<double>(1, 1) + m.At<double>(1, 2);
            point.X = (int)Math.Round(x, 0);
            point.Y = (int)Math.Round(y, 0);

            Mat rotated = new Mat();
            Cv2.WarpAffine(img, rotated, m, new CVSize(w, h));
            return rotated;
        }
        public static Mat RotateAffineOfSizeNoChange(this Mat img, double angle, ref Point2f point)
        {   
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            // 计算旋转后的图像尺寸
            int w0 = img.Width, h0 = img.Height;
            float w = img.Width, h = img.Height;
            //double arc = -Math.PI * angle / 180f;
            //double c = Math.Cos(arc);
            //double s = Math.Sin(arc);
            //double minx = Math.Min(Math.Min(w0 * c - h0 * s, 0), Math.Min(w0 * c, -h0 * s));
            //double maxx = Math.Max(Math.Max(w0 * c - h0 * s, 0), Math.Max(w0 * c, -h0 * s));
            //double miny = Math.Min(Math.Min(w0 * s + h0 * c, 0), Math.Min(w0 * s, h0 * c));
            //double maxy = Math.Max(Math.Max(w0 * s + h0 * c, 0), Math.Max(w0 * s, h0 * c));
            //float w = (float)(maxx - minx);
            //float h = (float)(maxy - miny);
            Point2f center = new Point2f(w0 / 2f, h0 / 2f);
            Mat m = Cv2.GetRotationMatrix2D(center, angle, 1.0);
            if (angle == 90 || angle == 270)
            {
                w = img.Height;
                h = img.Width;
                var mIndex = m.GetGenericIndexer<double>();
                mIndex[0, 2] += (w - w0) / 2f;
                mIndex[1, 2] += (h - h0) / 2f;
            }

            var x = point.X * m.At<double>(0, 0) + point.Y * m.At<double>(0, 1) + m.At<double>(0, 2);
            var y = point.X * m.At<double>(1, 0) + point.Y * m.At<double>(1, 1) + m.At<double>(1, 2);
            point.X = (int)Math.Round(x, 0);
            point.Y = (int)Math.Round(y, 0);

            Mat rotated = new Mat();
            Cv2.WarpAffine(img, rotated, m, new CVSize(w, h));
            return rotated;
        }
        public static Mat RotateAffineOfSizeNoChange(this Mat img, double angle, ref List<Point2f> point2Fs)
        {
                  
            // angle 0-360
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            // 计算旋转后的图像尺寸
            int w0 = img.Width, h0 = img.Height;
            float w =img.Width, h = img.Height;
          
            Point2f center = new Point2f(w0 / 2f, h0 / 2f);
            Mat m = Cv2.GetRotationMatrix2D(center, angle, 1.0);
            if (angle == 90 || angle == 270)
            {
                w = img.Height;
                h = img.Width;
                var mIndex = m.GetGenericIndexer<double>();
                mIndex[0, 2] += (w - w0) / 2f;
                mIndex[1, 2] += (h - h0) / 2f;
            }
            
            //坐标点位集合转换
            if (point2Fs.Count>0)
            {
                Point2f[] point2Fs1 = new CVPointF[point2Fs.Count];
                point2Fs.CopyTo(point2Fs1);
                for (int i = 0; i < point2Fs.Count; i++)
                {
                    var x = point2Fs1[i].X * m.At<double>(0, 0) + point2Fs1[i].Y * m.At<double>(0, 1) + m.At<double>(0, 2);
                    var y = point2Fs1[i].X * m.At<double>(1, 0) + point2Fs1[i].Y * m.At<double>(1, 1) + m.At<double>(1, 2);
                    point2Fs1[i].X = (int)Math.Round(x, 0);
                    point2Fs1[i].Y = (int)Math.Round(y, 0);

                }
                point2Fs = point2Fs1.ToList<Point2f>();
            }
               
            Mat rotated = new Mat();
         
            Cv2.WarpAffine(img, rotated, m, new CVSize(w, h));
            return rotated;
        }


        
        /// <summary>
        /// 直线交点
        /// </summary>
        /// <param name="Line1"></param>
        /// <param name="Line2"></param>
        /// <param name="crossPoint"></param>
        public static void IntersectionPoint(Line2D Line1, Line2D Line2, out Point2d crossPoint)
        {
            // Vx Vy 与直线共线的归一化向量的XY分量,可以理解为线段的一个端点
            // X1 Y1 直线上某点的坐标 可以理解为线段的另一个端点

            //  如果是一条垂直线，计算斜率会发生除0错误，所以对线稍加修改，对结果影响不大
            if (Line1.X1 - Line1.Vx == 0)
            {
                Line1 = new Line2D(Line1.Vx, Line1.Vy, Line1.X1 + 0.1, Line1.Y1);
            }

            if (Line2.X1 - Line2.Vx == 0)
            {
                Line2 = new Line2D(Line2.Vx, Line2.Vy, Line2.X1 + 0.1, Line2.Y1);
            }
            //对于过两个点(Vx，Vy) 和 (X1，Y1)的直线，斜率为k=(Y1-Vy)/(X1-Vx)。
            double k1 = (Line1.Y1 - Line1.Vy) / (Line1.X1 - Line1.Vx);
            double k2 = (Line2.Y1 - Line2.Vy) / (Line2.X1 - Line2.Vx);

            //交点
            crossPoint.X = (k1 * Line1.Vx - Line1.Vy - k2 * Line2.Vx + Line2.Vy) / (k1 - k2);
            crossPoint.Y = (k1 * k2 * (Line1.Vx - Line2.Vx) + k1 * Line2.Vy - k2 * Line1.Vy) / (k1 - k2);
        }
        /// <summary>
        /// 直线
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static Line2D LineSegmentPoint2Line2D(LineSegmentPoint line)
        {
            return new Line2D(line.P1.X, line.P1.Y, line.P2.X, line.P2.Y);
        }


        /// <summary>
        /// 是否同直线
        /// </summary>
        /// <param name="Segment1"></param>
        /// <param name="Segment2"></param>
        /// <returns></returns>
        public static bool CoverSegments(LineSegmentPoint Segment1, LineSegmentPoint Segment2)
        {
            float distance=   getDist_P2L(Segment1.P1, Segment2.P1, Segment2.P2);
            float distance2 = getDist_P2L(Segment1.P2, Segment2.P1, Segment2.P2);
            //   double value1 = GetK(Segment1);
            //   double value2 = GetK(Segment2);

            return (Math.Round(distance, 0) <= 5 && Math.Round(distance2, 0) <= 5);
                    
        }

        /// <summary>
        /// 是否同直线
        /// </summary>
        /// <param name="p11"></param>
        /// <param name="p12"></param>
        /// <param name="p21"></param>
        /// <param name="p22"></param>
        /// <returns></returns>
        public static bool CoverSegments(Point2f p11, Point2f p12,
                                             Point2f p21, Point2f p22)
        {
            float distance = getDist_P2L(p11, p21, p22);
            float distance2 = getDist_P2L(p12, p21, p22);
           
            double ang=   GetAngle(p11, p12);
            double ang2 = GetAngle(p21, p22);
            bool flag = (Math.Abs(ang - ang2) <= 1)||
                 Math.Abs(Math.Abs(ang)+Math.Abs(ang2)-180)<=1;
            return (Math.Round(distance, 0) <= 5 && Math.Round(distance2, 0) <= 5)&& flag;

        }

        /// <summary>
        /// 旋转矩形剔除直线边界点
        /// </summary>
        /// <param name="lineSegmentPointArray"></param>
        /// <param name="rrect"></param>
        /// <param name="BoundingRect"></param>
        /// <returns></returns>
        public static LineSegmentPoint[] ExceptBoundOfRRect(LineSegmentPoint[] lineSegmentPointArray,
                                CVRRect rrect, CVRect BoundingRect)
        { 
            /*原始直线轮廓集合*/
            List<LineSegmentPoint> temList = lineSegmentPointArray.ToList<LineSegmentPoint>();
            /*获取边界线段*/
            Point2f[] CVRRect_vertex = rrect.Points();

           // float offsetX = rrect.Center.X - (BoundingRect.X + BoundingRect.Width / 2);
          //  float offsetY = rrect.Center.Y - (BoundingRect.Y + BoundingRect.Height / 2);
            for (int i = 0; i < CVRRect_vertex.Length; i++)
            {
                CVRRect_vertex[i].X -= BoundingRect.X;
                CVRRect_vertex[i].Y -= BoundingRect.Y;
            }
            /*边界投影*/
            LineSegmentPoint[] boundarySeg = new LineSegmentPoint[4] {
                new LineSegmentPoint( new CVPoint( CVRRect_vertex[0].X,CVRRect_vertex[0].Y),
                          new CVPoint( CVRRect_vertex[1].X,CVRRect_vertex[1].Y)),
                 new LineSegmentPoint( new CVPoint( CVRRect_vertex[1].X,CVRRect_vertex[1].Y),
                          new CVPoint( CVRRect_vertex[2].X,CVRRect_vertex[2].Y)),
                  new LineSegmentPoint( new CVPoint( CVRRect_vertex[2].X,CVRRect_vertex[2].Y),
                          new CVPoint( CVRRect_vertex[3].X,CVRRect_vertex[3].Y)),
                   new LineSegmentPoint( new CVPoint( CVRRect_vertex[3].X,CVRRect_vertex[3].Y),
                          new CVPoint( CVRRect_vertex[0].X,CVRRect_vertex[0].Y))
                };

            /*剔除与边界重合的线段*/
            for (int i = 0; i < lineSegmentPointArray.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    //如果有重合
                    if (MatExtension.CoverSegments(lineSegmentPointArray[i], boundarySeg[j]))
                    {
                        temList.Remove(lineSegmentPointArray[i]); //移除与边界相交的线段
                        break;
                    }
                }
            }

            return temList.ToArray<LineSegmentPoint>();

        }

        /// <summary>
        /// 剔除提取的轮廓与外接旋转矩形边界的重合点
        /// </summary>
        /// <param name="lineSegmentPointArray"></param>
        /// <param name="rrect"></param>
        /// <param name="BoundingRect"></param>
        /// <returns></returns>
        public static CVPoint[][] ExceptBoundOfRRect(CVPoint[][] contours, CVRRect rrect)
        {
         
            /*获取边界线段*/
            Point2f[] CVRRect_vertex = rrect.Points();
            List<Point2f> points = new List<Point2f>();
            points.AddRange(CVRRect_vertex);
            points.Add(CVRRect_vertex[0]);
            List<List<CVPoint>> contoursList = new List<List<CVPoint>>();
            List<CVPoint[]> contoursBuf = contours.ToList<CVPoint[]>();
            foreach(var s in contoursBuf)
                contoursList.Add(s.ToList<CVPoint>());

            for (int i = 0; i < contours.Length; i++)
            {
                for(int j=0;j< contours[i].Length;j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                       float dis=   getDist_P2L(contours[i][j], points[k], points[k + 1]);
                        if (dis <= 2)
                            contoursList[i].Remove(contours[i][j]);//移除
                    }
                }
            }
            List<CVPoint[]> temBuf = new List<CVPoint[]>();
            foreach (var s in contoursList)
                temBuf.Add(s.ToArray<CVPoint>());

            return temBuf.ToArray<CVPoint[]>();
          
        }
        /// <summary>
        /// 扇形剔除圆弧边界点
        /// </summary>
        /// <param name="Contours"></param>
        /// <param name="sectorF"></param>
        /// <param name="BoundingRect"></param>
        /// <returns></returns>
        public static CVPoint[][] ExceptBoundOfSectorF(CVPoint[][] Contours, SectorF sectorF, CVRect BoundingRect)
        {
            List<List<CVPoint>> contourList = new List<List<CVPoint>>();
            int count = Contours.ToList<CVPoint[]>().Count;//轮廓数量
            foreach (var s in Contours)
                contourList.Add(s.ToList<CVPoint>());

            double outerR = sectorF.getOuterSector().getRadius;//内径
            double innerR = sectorF.getInnerSector().getRadius;//外径
            double centrePx = sectorF.centreP.X- BoundingRect.X; //中心点
            double centrePy = sectorF.centreP.Y - BoundingRect.Y; //中心点
            CVPoint[] RegionSectorF = MatExtension.GetSectorF(sectorF.getInnerSector(),
                sectorF.getOuterSector());

            for (int i=0;i< count;i++)
                foreach (CVPoint t in Contours[i])
                {
                    double distance = Math.Sqrt(Math.Pow(t.X - centrePx, 2) + Math.Pow(t.Y - centrePy, 2));
                    if (!(distance > innerR + 5 && distance < outerR -5))
                    {
                        contourList[i].Remove(t);
                        continue;
                    }
                   
                     {
                        CVPoint p1 = new CVPoint(t.X, t.Y);

                        CVPoint p21 = new CVPoint(sectorF.getEndpoint(sectorF.startAngle).X - BoundingRect.X,
                            sectorF.getEndpoint(sectorF.startAngle).Y - BoundingRect.Y);
                        CVPoint p22 = new CVPoint(sectorF.centreP.X - BoundingRect.X,
                           sectorF.centreP.Y - BoundingRect.Y);

                        CVPoint p31 = new CVPoint(sectorF.getEndpoint(sectorF.getEndAngle).X - BoundingRect.X,
                                                  sectorF.getEndpoint(sectorF.getEndAngle).Y - BoundingRect.Y);
                        CVPoint p32 = new CVPoint(sectorF.centreP.X - BoundingRect.X,
                           sectorF.centreP.Y - BoundingRect.Y);

                        float distance2 = getDist_P2L(p1, p21, p22);
                        float distance3= getDist_P2L(p1, p31, p32);
                    
                        if ((distance2 >= 0 && distance2 <=2)||(distance3 >= 0 && distance3 <= 2))
                        {
                            contourList[i].Remove(t);
                            continue;
                        }
                    }                                         
                }
       
            List<CVPoint[]> dstContour = new List<CVPoint[]>();
            foreach (var s in contourList)
                if(s.Count>0)
                  dstContour.Add(s.ToArray<CVPoint>());

            return dstContour.ToArray<CVPoint[]>();
        }

        /// <summary>
        /// 直线合并
        /// </summary>
        /// <param name="Segment1"></param>
        /// <param name="Segment2"></param>
        /// <param name="Angledeviation"></param>
        /// <returns></returns>
        static public Line2D UnionLines(LineSegmentPoint Segment1, LineSegmentPoint Segment2,
          double Angledeviation)
        {
            List<CVPoint> linePoints = new List<CVPoint>();
       
            double angle1 = GetAngle(Segment1);

            double angle2 = GetAngle(Segment2);

            double deviation = Math.Abs(angle1 - angle2);

            if (deviation > Angledeviation)
                return LineSegmentPoint2Line2D(Segment1);

            linePoints.Add(Segment1.P1);
            linePoints.Add(Segment1.P2);
            linePoints.Add(Segment2.P1);
            linePoints.Add(Segment2.P2);

           return  Cv2.FitLine(linePoints, DistanceTypes.L2, 0, 0.01, 0.01);

        }

        static public LineSegmentPoint  calMaxDistance (LineSegmentPoint lineSegmentPoint, LineSegmentPoint lineSegmentPoint1)
        {
            double distance = Math.Sqrt(Math.Pow((lineSegmentPoint.P2.Y - lineSegmentPoint.P1.Y), 2) +
                Math.Pow((lineSegmentPoint.P2.X - lineSegmentPoint.P1.X), 2)
                );

            double distance2 = Math.Sqrt(Math.Pow((lineSegmentPoint1.P2.Y - lineSegmentPoint1.P1.Y), 2) +
               Math.Pow((lineSegmentPoint1.P2.X - lineSegmentPoint1.P1.X), 2)
               );

            return distance > distance2 ? lineSegmentPoint : lineSegmentPoint1;
        }
        /// <summary>
        /// 多直线合并 
        /// </summary>
        /// <param name="SegmentArray"></param>
        /// <param name="Angledeviation"></param>
        /// <returns></returns>
        static public LineSegmentPoint UnionLines2(LineSegmentPoint[] SegmentArray, 
            int width,int height,
            double Angledeviation=5)
        {
            List<CVPoint> linePoints = new List<CVPoint>();
            double maxLength=0; int maxIndex=0;
             for(int i=0;i< SegmentArray.Length;i++)
            {
                if(  SegmentArray[i].Length()> maxLength)
                {
                    maxLength = SegmentArray[i].Length();
                    maxIndex = i;
                }
            }
            /* 先计算均值角度*/

            double averageAngle = GetAngle(SegmentArray[maxIndex]);


            /*然后逐一比较角度偏差值*/
            for (int j = 0; j < SegmentArray.Length; j++)
                if (Math.Abs(GetAngle(SegmentArray[j]) - averageAngle) <= Angledeviation)
                {
                    linePoints.Add(SegmentArray[j].P1);
                    linePoints.Add(SegmentArray[j].P2);
                }

            /*最后进行点位拟合*/
           CVPoint[] points=    GeneralAlgorithm.lineFit(linePoints.ToArray(), width,height);
            //Line2D line2D= Cv2.FitLine(linePoints, DistanceTypes.L1, 0, 0.01, 0.01);
            
            //line2D.FitSize(width, height, out CVPoint linep1,out CVPoint linep2);
        
            return new LineSegmentPoint(points[0], points[1]);
        }
        /// <summary>
        /// 计算点到直线的距离
        /// </summary>
        /// <param name="pointP"></param>
        /// <param name="pointA"></param>
        /// <param name="pointB"></param>
        /// <returns></returns>
        public static   float getDist_P2L(CVPoint pointP, CVPoint pointA, CVPoint pointB)
        {
            int A = 0, B = 0, C = 0;
            A = pointA.Y - pointB.Y;
            B = pointB.X - pointA.X;
            C = pointA.X * pointB.Y - pointA.Y * pointB.X;

            float distance = 0;
            distance = ((float)Math.Abs(A * pointP.X + B * pointP.Y + C)) / ((float)Math.Sqrt(A * A + B * B));
            return distance;
        }

        /// <summary>
        /// 计算点到直线的距离
        /// </summary>
        /// <param name="pointP"></param>
        /// <param name="pointA"></param>
        /// <param name="pointB"></param>
        /// <returns></returns>
        public static float getDist_P2L(CVPointF pointP, CVPointF pointA, CVPointF pointB)
        {
            float A = 0, B = 0, C = 0;
            A = pointA.Y - pointB.Y;
            B = pointB.X - pointA.X;
            C = pointA.X * pointB.Y - pointA.Y * pointB.X;

            float distance = 0;
            distance = ((float)Math.Abs(A * pointP.X + B * pointP.Y + C)) / ((float)Math.Sqrt(A * A + B * B));
            return distance;
        }

        /// <summary>
        /// 计算直线斜率
        /// </summary>
        /// <param name="Segment"></param>
        /// <returns></returns>
        static public   double GetK(LineSegmentPoint Segment)
        {
            if (Segment.P1.X == Segment.P2.X)
                return double.MaxValue;
            else if (Segment.P1.Y == Segment.P2.Y)
                return 0;
            else
                return Math.Round((double)(Segment.P2.Y - Segment.P1.Y) / (Segment.P2.X - Segment.P1.X),2);

        }
        /// <summary>
        /// 计算直线斜率
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        static public double GetK(CVPointF p1,CVPointF p2)
        {
            if (p1.X == p2.X)
                return double.MaxValue;
            else if (p1.Y == p2.Y)
                return 0;
            else
                return Math.Round((double)(p2.Y - p1.Y) / (p2.X - p1.X), 2);

        }
        /// <summary>
        /// 计算直线角度
        /// </summary>
        /// <param name="Segment"></param>
        /// <returns></returns>
        static public double GetAngle(LineSegmentPoint Segment)
        {
            double angle1;
            double k1 = GetK(Segment);
         
            if (k1 == double.MaxValue)
                angle1 = 90;
            else if (k1 == 0)
                angle1 = 0;
            else
                angle1 = Math.Atan(k1) * 180 / Math.PI;

            return angle1;
        }
        /// <summary>
        /// 计算直线角度
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        static public double GetAngle(CVPointF p1,CVPointF p2)
        {
            double angle1;
            double k1 = GetK(p1, p2);

            if (k1 == double.MaxValue)
                angle1 = 90;
            else if (k1 == 0)
                angle1 = 0;
            else
                angle1 = Math.Atan(k1) * 180 / Math.PI;

            return angle1;
        }
        /// <summary>
        /// 绘制箭头
        /// </summary>
        /// <param name="inputMat"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="dSize"></param>
        /// <param name="color"></param>
        /// <param name="nThickness"></param>
        public static void DrawArrow(this Mat inputMat, Point2d p1, Point2d p2,
                                        int dSize, Scalar color, int nThickness = 1)
        {
            if (inputMat.Empty())
            {
                return;
            }
            double dK = ((double)p2.Y - (double)p1.Y) / ((double)p2.X - (double)p1.X);
            double dAngle = Math.Atan(dK) * 180 / Math.PI;
            //绘制直线
            Cv2.Line(inputMat, (int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, color,
                                                            nThickness, LineTypes.AntiAlias);
            RotatedRect rotateRect = new CVRRect(new CVPointF((float)p2.X, (float)p2.Y),
                                                    new Size2f(dSize, dSize * 0.5), (float)dAngle);

            CVPointF[] point2Fs = rotateRect.Points();

            if ((dAngle >= 0 && p1.X <= p2.X) || (dAngle < 0 && p1.X <= p2.X))
            {
                Cv2.Line(inputMat, new CVPoint(p2.X, p2.Y), new CVPoint(point2Fs[0].X, point2Fs[0].Y),
                         color, nThickness, LineTypes.AntiAlias);
                Cv2.Line(inputMat, new CVPoint(p2.X, p2.Y), new CVPoint(point2Fs[1].X, point2Fs[1].Y),
                          color, nThickness, LineTypes.AntiAlias);
            }
            else
            {
                Cv2.Line(inputMat, new CVPoint(p2.X, p2.Y), new CVPoint(point2Fs[2].X, point2Fs[2].Y),
                                                 color, nThickness, LineTypes.AntiAlias);
                Cv2.Line(inputMat, new CVPoint(p2.X, p2.Y), new CVPoint(point2Fs[3].X, point2Fs[2].Y),
                                                 color, nThickness, LineTypes.AntiAlias);
            }
        }

        /// <summary>
        /// 绘制角度
        /// </summary>
        /// <param name="img"></param>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="radius"></param>
        public static void DrawAngle(this Mat img, Point2d p0, Point2d p1, Point2d p2, double radius)
        {
            // 计算直线的角度
            double angle1 = Math.Atan2(-(p1.Y - p0.Y), p1.X - p0.X) * 180 / Cv2.PI;
            double angle2 = Math.Atan2(-(p2.Y - p0.Y), p2.X - p0.X) * 180 / Cv2.PI;
            // 计算主轴的角度
            double angle = angle1 <= 0 ? -angle1 : 360 - angle1;
            // 计算圆弧的结束角度
            double end_angle = (angle2 < angle1) ? (angle1 - angle2) : (360 - (angle2 - angle1));
            // 画圆弧
            Cv2.Ellipse(img, (CVPoint)p0, new CVSize(radius, radius), angle, 0, end_angle, Scalar.RandomColor(), 2);
            //string a = (angle-end_angle).ToString();
            string a = end_angle.ToString("F3");
            Cv2.PutText(img, a, (CVPoint)p0, HersheyFonts.HersheyDuplex, 0.8d, Scalar.Red);
        }
        /// <summary>
        /// 获取灰色和彩色图像
        /// </summary>
        /// <param name="src"></param>
        /// <param name="gray"></param>
        /// <param name="bgr"></param>
        /// <returns></returns>
        public static bool GetGrayAndBgr(this Mat src, out Mat gray, out Mat bgr)
        {
            bgr = new Mat(0, 0, MatType.CV_8UC3, Scalar.Black);
            gray = new Mat(0, 0, MatType.CV_8UC1, Scalar.Black);
            if (src.Empty()) return false;
            if (src.Type() != MatType.CV_8UC3 && src.Type() != MatType.CV_8UC1) return false;

            if (src.Type() == MatType.CV_8UC3)
            {
                gray = src.CvtColor(ColorConversionCodes.BGR2GRAY);

                bgr = src.Clone();
            }
            else
            {
                gray = src.Clone();
                bgr = src.CvtColor(ColorConversionCodes.GRAY2BGR);
            }
            return true;
        }
        /// <summary>
        /// 获取灰色图像
        /// </summary>
        /// <param name="src"></param>
        /// <param name="gray"></param>
        /// <returns></returns>
        public static bool GetGray(this Mat src, out Mat gray)
        {
            gray = new Mat(0, 0, MatType.CV_8UC1, Scalar.Black);
            if (src.Empty()) return false;
            if (src.Type() != MatType.CV_8UC3 && src.Type() != MatType.CV_8UC1) return false;

            if (src.Type() == MatType.CV_8UC3)
            {
                gray = src.CvtColor(ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                gray = src.Clone();
            }
            return true;
        }
        /// <summary>
        /// 获取彩色图像
        /// </summary>
        /// <param name="src"></param>
        /// <param name="bgr"></param>
        /// <returns></returns>
        public static bool GetBgr(this Mat src, out Mat bgr)
        {
            bgr = new Mat(0, 0, MatType.CV_8UC3, Scalar.Black);

            if (src.Empty()) return false;
            if (src.Type() != MatType.CV_8UC3 && src.Type() != MatType.CV_8UC1) return false;

            if (src.Type() == MatType.CV_8UC3)
            {
                bgr = src.Clone();
            }
            else
            {
                bgr = src.CvtColor(ColorConversionCodes.GRAY2BGR);
            }
            return true;
        }

        // Checks if a matrix is a valid rotation matrix.
        // 检查一个矩阵是否是一个有效的旋转矩阵。
        private static bool IsRotationMatrix(Mat R)
        {
            Mat Rt = new Mat();
            Cv2.Transpose(R, Rt);
            Mat shouldBeIdentity = Rt * R;
            Mat I = Mat.Eye(3, 3, shouldBeIdentity.Type());
            return Cv2.Norm(I, shouldBeIdentity) < 1e-6;
        }

        // https://blog.csdn.net/xiangxianghehe/article/details/102481769
        // Calculates rotation matrix to euler angles  计算旋转矩阵到欧拉角
        public static bool RotationMatrixToEulerAngles(Mat R, out Vec3d euler)
        {
            euler = new Vec3d();

            if (IsRotationMatrix(R)) return false;

            var sy = Math.Sqrt(R.At<double>(0, 0) * R.At<double>(0, 0) + R.At<double>(1, 0) * R.At<double>(1, 0));

            bool singular = sy < 1e-6;

            if (!singular)
            {
                euler.Item0 = Math.Atan2(R.At<double>(2, 1), R.At<double>(2, 2));
                euler.Item1 = Math.Atan2(-R.At<double>(2, 0), sy);
                euler.Item2 = Math.Atan2(R.At<double>(1, 0), R.At<double>(0, 0));
            }
            else
            {
                euler.Item0 = Math.Atan2(-R.At<double>(1, 2), R.At<double>(1, 1));
                euler.Item1 = Math.Atan2(-R.At<double>(2, 0), sy);
                euler.Item2 = 0;
            }

            return true;
        }

        //基于傅里叶变换的角度检测 https://blog.csdn.net/CSDN131137/article/details/103008744
        public static (double angle, Mat DFT) GetDFTAngle(this Mat src)
        {
            //OpenCV中的DFT对图像尺寸有一定要求，
            //需要用GetOptimalDFTSize方法来找到合适的大小，
            //根据这个大小建立新的图像，把原图像拷贝过去，多出来的部分直接填充0。

            src.GetGray(out Mat gray);

            int width = Cv2.GetOptimalDFTSize(src.Width);
            int height = Cv2.GetOptimalDFTSize(src.Height);
            var padded = new Mat(height, width, MatType.CV_8UC1, Scalar.Black);//扩展后的图像，单通道

            padded[0, src.Height, 0, src.Width] = gray.Clone();

            padded.ConvertTo(padded, MatType.CV_32FC1);

            Mat zeros = new Mat(padded.Size(), MatType.CV_32FC1) * 0f;
            Mat comImg = new Mat();

            Mat[] paddeds = new[] { padded, zeros };
            //Merge into a double-channel image 合并成一个双通道图像
            Cv2.Merge(paddeds, comImg);

            Cv2.Dft(comImg, comImg);

            Cv2.Split(comImg, out Mat[] planes);
            Cv2.Magnitude(planes[0], planes[1], planes[0]);

            //计算幅值，转换到对数尺度(logarithmic scale)
            //Switch to logarithmic scale, for better visual results
            //M2=log(1+M1)
            Mat magMat = planes[0];
            magMat += Scalar.All(1);
            Cv2.Log(magMat, magMat);

            //Crop the spectrum
            //Width and height of magMat should be even, so that they can be divided by 2
            //-2 is 11111110 in binary system, operator & make sure width and height are always even
            magMat = magMat[new CVRect(0, 0, magMat.Cols & -2, magMat.Rows & -2)];

            //Rearrange the quadrants of Fourier image,
            //so that the origin is at the center of image,
            //and move the high frequency to the corners
            int cx = magMat.Cols / 2;
            int cy = magMat.Rows / 2;

            Mat q0 = new Mat(magMat, new CVRect(0, 0, cx, cy));
            Mat q1 = new Mat(magMat, new CVRect(0, cy, cx, cy));
            Mat q2 = new Mat(magMat, new CVRect(cx, cy, cx, cy));
            Mat q3 = new Mat(magMat, new CVRect(cx, 0, cx, cy));

            Mat tmp = new Mat();
            q0.CopyTo(tmp);
            q2.CopyTo(q0);
            tmp.CopyTo(q2);

            q1.CopyTo(tmp);
            q3.CopyTo(q1);
            tmp.CopyTo(q3);

            //MatType, then to[0,255]
            Cv2.Normalize(magMat, magMat, 0, 1, NormTypes.MinMax);
            Mat magImg = new Mat(magMat.Size(), MatType.CV_8UC1);
            magMat.ConvertTo(magImg, MatType.CV_8UC1, 255, 0);
            //Cv2.ImShow("test", magImg);

            //Turn into binary image
            Mat magThresh = new Mat();
            Cv2.Threshold(magImg, magThresh, 150, 255, ThresholdTypes.Binary);
            //Cv2.ImShow("Threshold", magImg);

            //Find lines with Hough Transformation

            //Mat linImg = new(magImg.Size(), MatType.CV_8UC3);
            var lines = Cv2.HoughLines(magThresh, 1, Cv2.PI / 180, 100, 0, 0);
            int numLines = lines.Length;
            //for (int l = 0; l < numLines; l++)
            //{
            //    float rho = lines[l].Rho, theta = lines[l].Theta;
            //    Point pt1, pt2;
            //    double a = Math.Cos(theta), b = Math.Sin(theta);
            //    double x0 = a * rho, y0 = b * rho;
            //    pt1.X = (int)Math.Round(x0 + 1000 * (-b));
            //    pt1.Y = (int)Math.Round(y0 + 1000 * (a));
            //    pt2.X = (int)Math.Round(x0 - 1000 * (-b));
            //    pt2.Y = (int)Math.Round(y0 - 1000 * (a));
            //    Cv2.Line(linImg, pt1.X, pt1.Y, pt2.X, pt2.Y, Scalar.Blue);
            //}

            //从三个角度中找到真正的角度
            double angel = 0;
            var piThresh = Cv2.PI / 90;
            float pi2 = (float)Cv2.PI / 2;
            for (int l = 0; l < numLines; l++)
            {
                float theta = lines[l].Theta;
                if (Math.Abs(theta) < piThresh || Math.Abs(theta - pi2) < piThresh)
                    continue;
                else
                {
                    angel = theta;
                    break;
                }
            }
            //计算旋转角度
            //图像必须是正方形，
            //使旋转角度可以计算右
            angel = angel < pi2 ? angel : angel - (float)Cv2.PI;
            if (angel != pi2)
            {
                double angelT = src.Rows * Math.Tan(angel) / src.Cols;
                angel = Math.Atan(angelT);
            }
            double angelD = angel * 180 / Cv2.PI;

            return (angelD, magImg);
        }
        /// <summary>
        /// 画中文
        /// </summary>
        /// <param name="src"></param>
        /// <param name="text"></param>
        /// <param name="point"></param>
        /// <param name="emSize"></param>
        /// <param name="color"></param>
        public static void PutTextZh(this Mat src, string text, CVPoint point, float emSize = 36, System.Drawing.Brush color = null)
        {
            if (string.IsNullOrEmpty(text)) return ;
            if(color==null) color = System.Drawing.Brushes.Lime;
            //color ??= System.Drawing.Brushes.Lime;
            var font = new System.Drawing.Font(new System.Drawing.FontFamily("微软雅黑"), emSize);


            var  bitmap = src[0,1,0,1].ToBitmap();
            var graphics = System.Drawing.Graphics.FromImage(bitmap);
 
            var size = graphics.MeasureString(text, font).ToSize();
            graphics.Dispose();
            bitmap.Dispose();

            var rect = new CVRect { Left=point.X,Width=size.Width,Top = point.Y,Height=size.Height };
            rect.Height = rect.Bottom > src.Height ? src.Height - rect.Y : rect.Height;
            rect.Width = rect.Right > src.Width ? src.Width - rect.X : rect.Width;
            bitmap = src[rect].ToBitmap();
            graphics = System.Drawing.Graphics.FromImage(bitmap);
            graphics.DrawString(text, font, color, new System.Drawing.PointF(0, 0));
            graphics.Flush();
            graphics.Dispose();
            src[rect] = bitmap.ToMat();

            bitmap.Dispose();
            font.Dispose();
        }
        /// <summary>
        ///图像中点
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Point GetCenter(this Mat src) => new Point(src.Width / 2, src.Height / 2);
    }
}