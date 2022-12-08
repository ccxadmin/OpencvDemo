using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CVPoint = OpenCvSharp.Point;

namespace FuncToolLib
{
    /// <summary>
    /// 通用算法
    /// </summary>
    public class GeneralAlgorithm
    {
        /// <summary>
        /// 最小二乘法拟合直线
        /// </summary>
        /// <param name="parray">输入点集合</param>
        /// <returns>拟合直线后的2端点</returns>
        public static Point[] LeastSquareMethod_FitLine(Point[] parray)
        {
            //点数不能小于2
            if (parray.Length < 2)
            {
                Console.WriteLine("点的数量小于2，无法进行线性回归");
                return null;
            }
            //求出横纵坐标的平均值
            double averagex = 0, averagey = 0;
            foreach (Point p in parray)
            {
                averagex += p.X;
                averagey += p.Y;
            }
            averagex /= parray.Length;
            averagey /= parray.Length;
            //经验回归系数的分子与分母
            double numerator = 0;
            double denominator = 0;
            foreach (Point p in parray)
            {
                numerator += (p.X - averagex) * (p.Y - averagey);
                denominator += (p.X - averagex) * (p.X - averagex);
            }
            //回归系数b（Regression Coefficient）
            double RCB = numerator / denominator;
            //回归系数a
            double RCA = averagey - RCB * averagex;
            Console.WriteLine("回归系数A： " + RCA.ToString("0.0000"));
            Console.WriteLine("回归系数B： " + RCB.ToString("0.0000"));
            Console.WriteLine(string.Format("方程为： y = {0} + {1} * x",
              RCA.ToString("0.0000"), RCB.ToString("0.0000")));
            //剩余平方和与回归平方和
            double residualSS = 0;  //（Residual Sum of Squares）
            double regressionSS = 0; //（Regression Sum of Squares）
            foreach (Point p in parray)
            {
                residualSS +=
                  (p.Y - RCA - RCB * p.X) *
                  (p.Y - RCA - RCB * p.X);
                regressionSS +=
                  (RCA + RCB * p.X - averagey) *
                  (RCA + RCB * p.X - averagey);
            }
            Console.WriteLine("剩余平方和： " + residualSS.ToString("0.0000"));
            Console.WriteLine("回归平方和： " + regressionSS.ToString("0.0000"));

            List<int> temList = new List<int>();
            foreach (var s in parray)
                temList.Add(s.X);
            int p1_x = temList.Min();
            int p2_x = temList.Max();

            int p1_y = (int)(RCA + RCB * p1_x);
            int p2_y = (int)(RCA + RCB * p2_x);

            return new Point[2] { new Point(p1_x, p1_y), new Point(p2_x, p2_y) };


        }
        /// <summary>
        /// 最小二乘法拟合直线
        /// </summary>
        /// <param name="parray">输入点集合</param>
        /// <returns>拟合直线后的2端点</returns>
        public static PointF[] LeastSquareMethod_FitLine(PointF[] parray)
        {
            //点数不能小于2
            if (parray.Length < 2)
            {
                Console.WriteLine("点的数量小于2，无法进行线性回归");
                return null;
            }
            //求出横纵坐标的平均值
            double averagex = 0, averagey = 0;
            foreach (PointF p in parray)
            {
                averagex += p.X;
                averagey += p.Y;
            }
            averagex /= parray.Length;
            averagey /= parray.Length;
            //经验回归系数的分子与分母
            double numerator = 0;
            double denominator = 0;
            foreach (PointF p in parray)
            {
                numerator += (p.X - averagex) * (p.Y - averagey);
                denominator += (p.X - averagex) * (p.X - averagex);
            }
            //回归系数b（Regression Coefficient）
            double RCB = numerator / denominator;
            //回归系数a
            double RCA = averagey - RCB * averagex;
            Console.WriteLine("回归系数A： " + RCA.ToString("0.0000"));
            Console.WriteLine("回归系数B： " + RCB.ToString("0.0000"));
            Console.WriteLine(string.Format("方程为： y = {0} + {1} * x",
              RCA.ToString("0.0000"), RCB.ToString("0.0000")));
            //剩余平方和与回归平方和
            double residualSS = 0;  //（Residual Sum of Squares）
            double regressionSS = 0; //（Regression Sum of Squares）
            foreach (PointF p in parray)
            {
                residualSS +=
                  (p.Y - RCA - RCB * p.X) *
                  (p.Y - RCA - RCB * p.X);
                regressionSS +=
                  (RCA + RCB * p.X - averagey) *
                  (RCA + RCB * p.X - averagey);
            }
            Console.WriteLine("剩余平方和： " + residualSS.ToString("0.0000"));
            Console.WriteLine("回归平方和： " + regressionSS.ToString("0.0000"));

            List<float> temList = new List<float>();
            foreach (var s in parray)
                temList.Add(s.X);
            float p1_x = temList.Min();
            float p2_x = temList.Max();

            float p1_y = (float)(RCA + RCB * p1_x);
            float p2_y = (float)(RCA + RCB * p2_x);

            return new PointF[2] { new PointF(p1_x, p1_y), new PointF(p2_x, p2_y) };

        }

        /// <summary>
        /// 最小二乘法拟合直线
        /// </summary>
        /// <param name="parray">输入点集合</param>
        /// <returns>拟合直线后的2端点</returns>
        public static CVPoint[] LeastSquareMethod_FitLine(CVPoint[] parray)
        {
            //点数不能小于2
            if (parray.Length < 2)
            {
                Console.WriteLine("点的数量小于2，无法进行线性回归");
                return null;
            }
            //求出横纵坐标的平均值
            double averagex = 0, averagey = 0;
            foreach (CVPoint p in parray)
            {
                averagex += p.X;
                averagey += p.Y;
            }
            averagex /= parray.Length;
            averagey /= parray.Length;
            //经验回归系数的分子与分母
            double numerator = 0;
            double denominator = 0;
            foreach (CVPoint p in parray)
            {
                numerator += (p.X - averagex) * (p.Y - averagey);
                denominator += (p.X - averagex) * (p.X - averagex);
            }
            //回归系数b（Regression Coefficient）
            double RCB = numerator / denominator;
            //回归系数a
            double RCA = averagey - RCB * averagex;
            Console.WriteLine("回归系数A： " + RCA.ToString("0.0000"));
            Console.WriteLine("回归系数B： " + RCB.ToString("0.0000"));
            Console.WriteLine(string.Format("方程为： y = {0} + {1} * x",
              RCA.ToString("0.0000"), RCB.ToString("0.0000")));
            //剩余平方和与回归平方和
            double residualSS = 0;  //（Residual Sum of Squares）
            double regressionSS = 0; //（Regression Sum of Squares）
            foreach (CVPoint p in parray)
            {
                residualSS +=
                  (p.Y - RCA - RCB * p.X) *
                  (p.Y - RCA - RCB * p.X);
                regressionSS +=
                  (RCA + RCB * p.X - averagey) *
                  (RCA + RCB * p.X - averagey);
            }
            Console.WriteLine("剩余平方和： " + residualSS.ToString("0.0000"));
            Console.WriteLine("回归平方和： " + regressionSS.ToString("0.0000"));

            List<float> temList = new List<float>();
            foreach (var s in parray)
                temList.Add(s.X);
            float p1_x = temList.Min();
            float p2_x = temList.Max();
            float d_x = p2_x - p1_x;
            temList.Clear();
            foreach (var s in parray)
                temList.Add(s.Y);
            float p1_y = temList.Min();
            float p2_y = temList.Max();
            float d_y = p2_y - p1_y;

            if (Math.Abs(d_x) >= Math.Abs(d_y))
            {
                p1_y = (float)(RCA + RCB * p1_x);
                p2_y = (float)(RCA + RCB * p2_x);
            }
            else
            {


                p1_x = (float)((p1_y - RCA) / RCB);
                p2_x = (float)((p2_y - RCA) / RCB);
            }
            return new CVPoint[2] { new CVPoint(p1_x, p1_y), new CVPoint(p2_x, p2_y) };

        }


        public static CVPoint[] lineFit(CVPoint[] parray,int width,int height)
        {
            double a, b, c;
            int size = parray.Length;
            if (size < 2)
            {
                a = 0;
                b = 0;
                c = 0;
                return new CVPoint[2];
            }
            double x_mean = 0;
            double y_mean = 0;
            for (int i = 0; i < size; i++)
            {
                x_mean += parray[i].X;
                y_mean += parray[i].Y;
            }
            x_mean /= size;
            y_mean /= size; //至此，计算出了 x y 的均值

            double Dxx = 0, Dxy = 0, Dyy = 0;

            for (int i = 0; i < size; i++)
            {
                Dxx += (parray[i].X - x_mean) * (parray[i].X - x_mean);
                Dxy += (parray[i].X - x_mean) * (parray[i].Y - y_mean);
                Dyy += (parray[i].Y - y_mean) * (parray[i].Y - y_mean);
            }
            double lambda = ((Dxx + Dyy) - Math.Sqrt((Dxx - Dyy) * (Dxx - Dyy) + 4 * Dxy * Dxy)) / 2.0;
            double den = Math.Sqrt(Dxy * Dxy + (lambda - Dxx) * (lambda - Dxx));
            a = Dxy / den;
            b = (lambda - Dxx) / den;
            c = -a * x_mean - b * y_mean;
            /*---将离散点拟合为  a x + b y + c = 0 型直线-----*/
            List<float> temList = new List<float>();
            foreach (var s in parray)
                temList.Add(s.X);
            float p1_x = temList.Min();
            float p2_x = temList.Max();
            float d_x = p2_x - p1_x;
            temList.Clear();
            foreach (var s in parray)
                temList.Add(s.Y);
            float p1_y = temList.Min();
            float p2_y = temList.Max();
            float d_y = p2_y - p1_y;

            if(b!=0&&a!=0)
            {
                p1_y = (float)((-c - a * p1_x)/b);
                if(p1_y>height)
                {
                    p1_y = height;
                    p1_x = (float)((-c - b * p1_y) / a);
                }
                else if(p1_y<0)
                {
                    p1_y = 0;
                    p1_x = (float)((-c - b * p1_y) / a);
                }
              

                p2_y = (float)((-c - a * p2_x) / b);
                if (p2_y > height)
                {
                    p2_y = height;
                    p2_x = (float)((-c - b * p2_y) / a);
                }
                else if (p2_y < 0)
                {
                    p2_y = 0;
                    p2_x = (float)((-c - b * p2_y) / a);
                }
            }
            
         
            return new CVPoint[2] { new CVPoint(p1_x, p1_y), new CVPoint(p2_x, p2_y) };

        } 


    }

}
