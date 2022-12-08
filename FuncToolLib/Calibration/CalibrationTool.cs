
using ParamDataLib;
using System;
using System.Diagnostics;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;
using System.Numerics;
using System.Collections.Generic;

namespace FuncToolLib.Calibration
{
    /// <summary>
    /// 标定工具
    /// </summary>
    public partial class CalibrationTool 
    {

        public CalibrationTool()
        {

        }
           
        /// <summary>
        /// //mat1是用来存九个像素坐标的，mat2用来存机器人坐标
        /// </summary>
        /// <param name="calib_img_pixel_coordinates">九个像素坐标</param>
        /// <param name="calib_img_rob_coordinates">机器人坐标</param>
        /// <returns>返回标定矩阵关系</returns>
        static public Mat VectorToHomMat2d(List<Point2d> calib_img_pixel_coordinates, List<Point2d> calib_img_rob_coordinates)
        {
            if (calib_img_pixel_coordinates.Count != 9 && calib_img_rob_coordinates.Count != 9)
            {
                return null;
            }

            Mat mat1 = new Mat(9, 2, MatType.CV_64F); //这里MatType的解
            mat1.Set<double>(0, 0, calib_img_pixel_coordinates[0].X);
            mat1.Set<double>(0, 1, calib_img_pixel_coordinates[0].Y);
            mat1.Set<double>(1, 0, calib_img_pixel_coordinates[1].X);
            mat1.Set<double>(1, 1, calib_img_pixel_coordinates[1].Y);
            mat1.Set<double>(2, 0, calib_img_pixel_coordinates[2].X);
            mat1.Set<double>(2, 1, calib_img_pixel_coordinates[2].Y);
            mat1.Set<double>(3, 0, calib_img_pixel_coordinates[3].X);
            mat1.Set<double>(3, 1, calib_img_pixel_coordinates[3].Y);
            mat1.Set<double>(4, 0, calib_img_pixel_coordinates[4].X);
            mat1.Set<double>(4, 1, calib_img_pixel_coordinates[4].Y);
            mat1.Set<double>(5, 0, calib_img_pixel_coordinates[5].X);
            mat1.Set<double>(5, 1, calib_img_pixel_coordinates[5].Y);
            mat1.Set<double>(6, 0, calib_img_pixel_coordinates[6].X);
            mat1.Set<double>(6, 1, calib_img_pixel_coordinates[6].Y);
            mat1.Set<double>(7, 0, calib_img_pixel_coordinates[7].X);
            mat1.Set<double>(7, 1, calib_img_pixel_coordinates[7].Y);
            mat1.Set<double>(8, 0, calib_img_pixel_coordinates[8].X);
            mat1.Set<double>(8, 1, calib_img_pixel_coordinates[8].Y);

            Mat mat2 = new Mat(9, 2, MatType.CV_64F);
            mat2.Set<double>(0, 0, calib_img_rob_coordinates[0].X);
            mat2.Set<double>(0, 1, calib_img_rob_coordinates[0].Y);
            mat2.Set<double>(1, 0, calib_img_rob_coordinates[1].X);
            mat2.Set<double>(1, 1, calib_img_rob_coordinates[1].Y);
            mat2.Set<double>(2, 0, calib_img_rob_coordinates[2].X);
            mat2.Set<double>(2, 1, calib_img_rob_coordinates[2].Y);
            mat2.Set<double>(3, 0, calib_img_rob_coordinates[3].X);
            mat2.Set<double>(3, 1, calib_img_rob_coordinates[3].Y);
            mat2.Set<double>(4, 0, calib_img_rob_coordinates[4].X);
            mat2.Set<double>(4, 1, calib_img_rob_coordinates[4].Y);
            mat2.Set<double>(5, 0, calib_img_rob_coordinates[5].X);
            mat2.Set<double>(5, 1, calib_img_rob_coordinates[5].Y);
            mat2.Set<double>(6, 0, calib_img_rob_coordinates[6].X);
            mat2.Set<double>(6, 1, calib_img_rob_coordinates[6].Y);
            mat2.Set<double>(7, 0, calib_img_rob_coordinates[7].X);
            mat2.Set<double>(7, 1, calib_img_rob_coordinates[7].Y);
            mat2.Set<double>(8, 0, calib_img_rob_coordinates[8].X);
            mat2.Set<double>(8, 1, calib_img_rob_coordinates[8].Y);

            Mat Hom_mat2d = Cv2.EstimateAffine2D(mat1, mat2);

            //Mat M = Cv2.GetAffineTransform(calib_img_pixel_coordinates, calib_img_rob_coordinates);
            return Hom_mat2d;
        }

        /// <summary>
        /// 计算RMS标定偏差
        /// </summary>
        /// <param name="Hom_mat2d">标定矩阵</param>
        /// <param name="points_camera">像素坐标点</param>
        /// <param name="points_robot">机器人坐标点</param>
        /// <returns></returns>
        static public double[] calRMS(Mat Hom_mat2d,List<Point2d> points_camera, List<Point2d> points_robot)
        {          
            double sumX = 0, sumY = 0;
            var A = Hom_mat2d.Get<double>(0, 0);
            var B = Hom_mat2d.Get<double>(0, 1);
            var C = Hom_mat2d.Get<double>(0, 2);
            var D = Hom_mat2d.Get<double>(1, 0);
            var E = Hom_mat2d.Get<double>(1, 1);
            var F = Hom_mat2d.Get<double>(1, 2);
            for (int i = 0; i < points_camera.Count; i++)
            {
                Point2d pt;
                pt.X = A * points_camera[i].X + B * points_camera[i].Y + C;
                pt.Y = D * points_camera[i].X + E * points_camera[i].Y + F;
               
                sumX += Math.Pow(points_robot[i].X - pt.X, 2);
                sumY += Math.Pow(points_robot[i].Y - pt.Y, 2);
            }
            double rmsX, rmsY;
            rmsX = Math.Sqrt(sumX / points_camera.Count);
            rmsY = Math.Sqrt(sumY / points_camera.Count);
            return new double[] {  rmsX, rmsY  };         
        }
        /// <summary>
        /// 获取矩阵系数
        /// </summary>
        /// <param name="Hom_mat2d"></param>
        /// <returns></returns>
        static public double[] GetMatrixCoefficient(Mat Hom_mat2d)
        {
            var A = Hom_mat2d.Get<double>(0, 0);
            var B = Hom_mat2d.Get<double>(0, 1);
            var C = Hom_mat2d.Get<double>(0, 2);
            var D = Hom_mat2d.Get<double>(1, 0);
            var E = Hom_mat2d.Get<double>(1, 1);
            var F = Hom_mat2d.Get<double>(1, 2);
            return  new double[]{ A,B,C,D,E,F};
        }
        /// <summary>
        /// 将像素坐标转为机器人坐标
        /// </summary>
        /// <param name="Hom_mat2d">标定关系矩阵</param>
        /// <param name="image_coordinates">像素坐标</param>
        /// <returns>机器人坐标</returns>
        static public Point2d AffineTransPoint2d(Mat Hom_mat2d, Point2d image_coordinates)
        {
            Point2d robot_coordinate;
            var A = Hom_mat2d.Get<double>(0, 0);
            var B = Hom_mat2d.Get<double>(0, 1);
            var C = Hom_mat2d.Get<double>(0, 2);    //Tx
            var D = Hom_mat2d.Get<double>(1, 0);
            var E = Hom_mat2d.Get<double>(1, 1);
            var F = Hom_mat2d.Get<double>(1, 2);    //Ty

            robot_coordinate.X =((A * image_coordinates.X) + (B * image_coordinates.Y) + C);
            robot_coordinate.Y = ((D * image_coordinates.X) + (E * image_coordinates.Y) + F);
            return robot_coordinate;
        }
        /// <summary>
        /// 将机器人坐标转为像素坐标
        /// </summary>
        /// <param name="Hom_mat2d">标定关系矩阵</param>
        /// <param name="image_coordinates">机器人坐标</param>
        /// <returns>机器人坐标</returns>
        static public Point2d AffineTransPoint2dINV(Mat Hom_mat2d, Point2d robot_coordinate)
        {
            Point2d image_coordinates;         
            Mat Hom_mat2d_inv= Hom_mat2d.Inv(method: DecompTypes.SVD);

            var A_inv = Hom_mat2d_inv.Get<double>(0, 0);
            var B_inv = Hom_mat2d_inv.Get<double>(0, 1);
            var C_inv = Hom_mat2d_inv.Get<double>(1, 0);
            var D_inv = Hom_mat2d_inv.Get<double>(1, 1);
            var E_inv = Hom_mat2d_inv.Get<double>(2, 0);
            var F_inv = Hom_mat2d_inv.Get<double>(2, 1);

            //var A_inv = Hom_mat2d_inv.Get<double>(0, 0);
            //var B_inv = Hom_mat2d_inv.Get<double>(1, 0);
            //var C_inv = Hom_mat2d_inv.Get<double>(2, 0);
            //var D_inv = Hom_mat2d_inv.Get<double>(0, 1);
            //var E_inv = Hom_mat2d_inv.Get<double>(1, 1);
            //var F_inv = Hom_mat2d_inv.Get<double>(2, 1);

            image_coordinates.X =( (A_inv * robot_coordinate.X) + (B_inv * robot_coordinate.Y) + C_inv);
            image_coordinates.Y = ((D_inv * robot_coordinate.X) + (E_inv * robot_coordinate.Y) + F_inv);
            return image_coordinates;
        }
    }
}
