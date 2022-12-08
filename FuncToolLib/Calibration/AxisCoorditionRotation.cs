using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace FuncToolLib.Calibration
{
   public  class AxisCoorditionRotation
    {
      /// <summary>
      /// 旋转中心计算
      /// </summary>
        /// <param name="point1">旋转前Mark坐标</param>
        /// <param name="point2">旋转后Mark坐标</param>
        /// <param name="RarotionAngle">旋转角度</param>
      /// <returns></returns>
       public static Point2f getRotateCenter(Point2f point1, Point2f point2, double RarotionAngle)
        {
            double sita = RarotionAngle * Math.PI / 180;
            double temx = ((point1.X + point2.X) - (Math.Sin(sita) / (1 - Math.Cos(sita)) * (point2.Y - point1.Y))) / 2;
            double temy = ((point1.Y + point2.Y) + (Math.Sin(sita) / (1 - Math.Cos(sita)) * (point2.X - point1.X))) / 2;
            return new Point2f((float)temx,(float)temy);
        }

      /// <summary>
       /// 旋转前坐标点位计算
      /// </summary>
       /// <param name="point1">旋转后Mark坐标</param>
       /// <param name="pointC">旋转中心</param>
       /// <param name="RarotionAngle">旋转角度</param>
      /// <returns></returns>
       public static Point2f get_befor_RotatedPoint(Point2f point2, Point2f pointC, double RarotionAngle)
       {
           double sita = RarotionAngle * Math.PI / 180;
           double temx = (point2.X - pointC.X) * Math.Cos(sita) + (point2.Y - pointC.Y) * Math.Sin(sita) + pointC.X;
           double temy = (point2.Y - pointC.Y) * Math.Cos(sita) - (point2.X - pointC.X) * Math.Sin(sita) + pointC.Y;
           return new Point2f((float)temx, (float)temy);
       }

        /// <summary>
        /// 旋转后坐标点位计算
        /// </summary>
        /// <param name="point1">旋转前Mark坐标</param>
        /// <param name="pointC">旋转中心</param>
        /// <param name="RarotionRad">旋转角度</param>
        /// <returns></returns>
        public static Point2f get_after_RotatePoint(Point2f point1, Point2f pointC, double RarotionAngle)
       {
           double sita = RarotionAngle * Math.PI / 180;
           double temx = (point1.X - pointC.X) * Math.Cos(sita) - (point1.Y - pointC.Y) * Math.Sin(sita) + pointC.X;
           double temy = (point1.Y - pointC.Y) * Math.Cos(sita) + (point1.X - pointC.X) * Math.Sin(sita) + pointC.Y;
           return new Point2f((float)temx, (float)temy);
       }

        /// <summary>
        /// 多点拟合圆
        /// </summary>
        /// <param name="points"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static float FitCircle(IEnumerable<OpenCvSharp.Point> points, ref Point2d center, ref double radius)
        {
            radius = 0.0f;
            double sum_x = 0.0f, sum_y = 0.0f;
            double sum_x2 = 0.0f, sum_y2 = 0.0f;
            double sum_x3 = 0.0f, sum_y3 = 0.0f;
            double sum_xy = 0.0f, sum_x1y2 = 0.0f, sum_x2y1 = 0.0f;
            int N = points.Count();
            for (int i = 0; i < N; i++)
            {
                double x = points.ElementAt(i).X;
                double y = points.ElementAt(i).Y;
                double x2 = x * x;
                double y2 = y * y;
                sum_x += x;
                sum_y += y;
                sum_x2 += x2;
                sum_y2 += y2;
                sum_x3 += x2 * x;
                sum_y3 += y2 * y;
                sum_xy += x * y;
                sum_x1y2 += x * y2;
                sum_x2y1 += x2 * y;
            }
            double C, D, E, G, H;
            double a, b, c;
            C = N * sum_x2 - sum_x * sum_x;
            D = N * sum_xy - sum_x * sum_y;
            E = N * sum_x3 + N * sum_x1y2 - (sum_x2 + sum_y2) * sum_x;
            G = N * sum_y2 - sum_y * sum_y;
            H = N * sum_x2y1 + N * sum_y3 - (sum_x2 + sum_y2) * sum_y;
            a = (H * D - E * G) / (C * G - D * D);
            b = (H * C - E * D) / (D * D - G * C);
            c = -(a * sum_x + b * sum_y + sum_x2 + sum_y2) / N;
            center.X = a / (-2);
            center.Y = b / (-2);
            radius = Math.Sqrt(a * a + b * b - 4 * c) / 2;
            if (double.IsNaN(radius))
                return -1f;

            double sum = 0;
            for (int i = 0; i < N; i++)
            {
                OpenCvSharp.Point pti = points.ElementAt(i);
                double ri = Math.Sqrt(Math.Pow(pti.X - center.X, 2) + Math.Pow(pti.Y - center.Y, 2));

                // sum += Math.Abs(ri - radius) / (ri + radius);
                sum += ri / radius > 1 ? ri / radius - 1 : 1 - ri / radius;
            }
            double sorce = 1 - sum / N;
            return (float)sorce;
        }

        /// <summary>
        /// 圆自动校验
        /// </summary>
        /// <param name="points"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
       static    public bool CheckCircle( IEnumerable<OpenCvSharp.Point> points, ref Point2d center, ref double  radius)
        {
            List<OpenCvSharp.Point> pointList = new List<OpenCvSharp.Point>();
            List<OpenCvSharp.Point> pointList2 = new List<OpenCvSharp.Point>();
            pointList2.AddRange(points);
            foreach (var s in points)
            {
                pointList.Clear();
                pointList.AddRange(points);
                pointList.Remove(s);
                Point2d temcenter = new Point2d(); double temradius=0;
                FitCircle(pointList,ref temcenter,ref temradius);
                if (Math.Abs(temcenter.X - center.X) >= 0.3 ||
                    Math.Abs(temcenter.Y - center.Y) >= 0.3 ||
                    Math.Abs(temradius - radius) >= 0.3)
                {
                    pointList2.Remove(s);  //移除干扰点
                }
            }
            return FitCircle(pointList2,ref center,ref radius)>0.2;
        }
       /// <summary>
       /// 多点拟合圆
       /// </summary>
       /// <param name="pts"></param>
       /// <param name="epsilon"></param>
       /// <returns></returns>
        public static PointF FitCenter(List<PointF> pts, double epsilon = 0.1)
        {
            double totalX = 0, totalY = 0;
            int setCount = 0;

            for (int i = 0; i < pts.Count; i++)
            {
                for (int j = 1; j < pts.Count; j++)
                {
                    for (int k = 2; k < pts.Count; k++)
                    {
                        double delta = (pts[k].X - pts[j].X) * (pts[j].Y - pts[i].Y) - (pts[j].X - pts[i].X) * (pts[k].Y - pts[j].Y);

                        if (Math.Abs(delta) > epsilon)
                        {
                            double ii = Math.Pow(pts[i].X, 2) + Math.Pow(pts[i].Y, 2);
                            double jj = Math.Pow(pts[j].X, 2) + Math.Pow(pts[j].Y, 2);
                            double kk = Math.Pow(pts[k].X, 2) + Math.Pow(pts[k].Y, 2);

                            double cx = ((pts[k].Y - pts[j].Y) * ii + (pts[i].Y - pts[k].Y) * jj + (pts[j].Y - pts[i].Y) * kk) / (2 * delta);
                            double cy = -((pts[k].X - pts[j].X) * ii + (pts[i].X - pts[k].X) * jj + (pts[j].X - pts[i].X) * kk) / (2 * delta);

                            totalX += cx;
                            totalY += cy;

                            setCount++;
                        }
                    }
                }
            }

            if (setCount == 0)
            {
                //failed
                return PointF.Empty;
            }

            return new PointF((float)totalX / setCount, (float)totalY / setCount);
        }
    }
   
}
