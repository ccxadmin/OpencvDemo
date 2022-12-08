using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuncToolLib.Contour
{
    /// <summary>
    /// 边缘工具
    /// </summary>
    public  class EdgeTool
    {

        /// <summary>
        ///  Canny边缘检测 
        /// </summary>
        /// <param name="Gray">输入图</param>
        /// <param name="canThddown">阈值下限</param>
        /// <param name="canThdup">阈值上限</param>
        /// <returns></returns>
        static public Mat Canny(Mat Gray,double canThddown, double canThdup)
        {

            Mat canny = new Mat(Gray.Size(), Gray.Type());
            Cv2.Canny(Gray, canny, canThddown, canThdup);
            //cannny。参数：1：src_img：8 bit 输入图像；2：dst输出边缘图像，一般是二值图像，背景是黑色；3：tkBarCannyMin.Value低阈值。值越大，找到的边缘越少；4：tkBarCannyMax.Value高阈值；5:hole表示应用Sobel算子的孔径大小，其有默认值3；6:rbBtnTrue.Checked计算图像梯度幅值的标识，有默认值false。
            //低于阈值1的像素点会被认为不是边缘；
            //高于阈值2的像素点会被认为是边缘；
            //在阈值1和阈值2之间的像素点,若与一阶偏导算子计算梯度得到的边缘像素点相邻，则被认为是边缘，否则被认为不是边缘。
            return canny;

        }

      public  static Mat on_Sobel_x(Mat g_srcImage)
        {

            //   Mat GaussMat=  Filter.ImageFilter.GaussImage(g_srcImage,new Size(5,5),1);
            Mat sobelxMat = new Mat();
            Cv2.Sobel(g_srcImage, sobelxMat, MatType.CV_64F, 1, 0,
                               3, 1, 1, BorderTypes.Default);
            sobelxMat.ConvertTo(sobelxMat, MatType.CV_8UC1);

            return sobelxMat;

        }

        public static Mat on_Sobel(Mat g_srcImage)

        {

            // 求 X方向梯度
            Mat g_sobelGradient_X = new Mat();
            Mat g_sobelAbsGradient_X = new Mat();
            Cv2.Sobel(g_srcImage, g_sobelGradient_X, MatType.CV_16S, 1, 0,
                               3, 1, 1, BorderTypes.Default);

            Cv2.ConvertScaleAbs(g_sobelGradient_X, g_sobelAbsGradient_X);//计算绝对值，并将结果转换成8位

            // 求Y方向梯度
            Mat g_sobelGradient_Y = new Mat();
            Mat g_sobelAbsGradient_Y = new Mat();
            Cv2.Sobel(g_srcImage, g_sobelGradient_Y, MatType.CV_16S, 0, 1,
               3, 1, 1, BorderTypes.Default);

            Cv2.ConvertScaleAbs(g_sobelGradient_Y, g_sobelAbsGradient_Y);//计算绝对值，并将结果转换成8位

            // 合并梯度
            Mat g_dstImage = new Mat();
            Cv2.AddWeighted(g_sobelAbsGradient_X, 0.5, g_sobelAbsGradient_Y, 0.5, 0, g_dstImage);
            g_dstImage.ConvertTo(g_dstImage, MatType.CV_8UC1);
            return g_dstImage;
        }

        static Mat on_Laplacian(Mat g_srcImage)
        {

            //   Mat GaussMat=  Filter.ImageFilter.GaussImage(g_srcImage,new Size(5,5),1);
            Mat LaplacianMat = new Mat();
            Cv2.Laplacian(g_srcImage, LaplacianMat, MatType.CV_64F);
            Mat LaplacianMatAbs = new Mat();
            Cv2.ConvertScaleAbs(LaplacianMat, LaplacianMatAbs);
            return LaplacianMatAbs;

        }

    }
}
