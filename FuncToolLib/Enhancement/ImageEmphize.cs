using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenCvSharp;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using Rect = System.Drawing.RectangleF;
using Point = System.Drawing.PointF;
using FuncToolLib.Filter;

namespace FuncToolLib.Enhancement
{
    /*
     * 直方图均衡化：
          直方图均衡化是通过调整图像的灰阶分布，使得在0~255灰阶上的分布更加均衡，
          提高了图像的对比度，达到改善图像主观视觉效果的目的。对比度较低的图像适合使用直方图均衡化方法来增强图像细节。
     * 拉普拉斯：
          拉普拉斯算子可以增强局部的图像对比度
     * 对数Log变换
     * 伽马变换
     * 
     */

    /// <summary>
    /// 图像增强
    /// </summary>
    public class ImageEmphize
    {
        /// <summary>
        /// 图像直方图均衡
        /// </summary>
        /// <param name="src">输入原图：黑白/彩色</param>
        /// <returns>返回直方图均衡后的图像</returns>
        static public Mat Histogram_equalization(Mat src)
        {
            if (src.Empty())
                return null;    
            Mat dst = new Mat();
            if (src.Channels() == 1)
                Cv2.EqualizeHist(src, dst);   
            else if(src.Channels() == 3)
            {
                Mat[] matRGB = new Mat[3];
                Cv2.Split(src, out matRGB);
                for (int i = 0; i < 3; i++)
                {
                    Cv2.EqualizeHist(matRGB[i], matRGB[i]);
                }
                Cv2.Merge(matRGB,  dst);
            }
            return dst;
        }
        /// <summary>
        /// 拉普拉斯算子
        /// </summary>
        /// <param name="src"></param>
        /// <param name="kernel"></param>
        /// <returns></returns>
        static public Mat Laplace_operator(Mat src, Mat kernel)
        {
            if (src.Empty())
                return null;
            Mat dst = new Mat();
      
          //  Mat kernel = new Mat(3,3,MatType.CV_32FC1,new float[] { 0, -1, 0, 0, 5, 0, 0, -1, 0 });
                             
            Cv2.Filter2D(src, dst, src.Channels() == 1 ? MatType.CV_8UC1:MatType.CV_8UC3, kernel);
            return dst;
        }

        /// <summary>
        /// 图像伽马变换:
        ///    伽马变换主要用于图像的校正，将灰度过高或者灰度过低的图片进行修正，增强对比度。
        ///    变换公式就是对原图像上每一个像素值做乘积运算
        /// </summary>
        /// <param name="src"></param>
        /// <param name="gamaValue"></param>
        /// <returns>伽马变换后的图像</returns>
        static public Mat Gamma(Mat src,double gamaValue)
        {
            Mat imageGamma = new Mat(src.Size(), src.Channels() == 1? MatType.CV_32FC1:MatType.CV_32FC3);
            for (int i = 0; i < src.Rows; i++)
            {
                for (int j = 0; j < src.Cols; j++)
                {
                    if (src.Channels() == 1)
                    {
                        imageGamma.At<Vec3f>(i, j)[0] = (float)Math.Pow((src.At<Vec3b>(i, j)[0]), gamaValue);
                            //(src.At<Vec3b>(i, j)[0]) * (src.At<Vec3b>(i, j)[0]) * (src.At<Vec3b>(i, j)[0]);
                    }
                    else if(src.Channels() == 3)
                    {
                        imageGamma.At<Vec3f>(i, j)[0] = (float)Math.Pow((src.At<Vec3b>(i, j)[0]), gamaValue);
                        imageGamma.At<Vec3f>(i, j)[1] = (float)Math.Pow((src.At<Vec3b>(i, j)[1]), gamaValue);
                        imageGamma.At<Vec3f>(i, j)[2] = (float)Math.Pow((src.At<Vec3b>(i, j)[2]), gamaValue);
                    }
                }
                       
            }
            //归一化到0~255    
            Cv2.Normalize(imageGamma, imageGamma, 0, 255, NormTypes.MinMax);
            //转换成8bit图像显示
            Cv2.ConvertScaleAbs(imageGamma, imageGamma);
            return imageGamma;
        }

        /// <summary>
        /// 图像增强
        /// </summary>
        /// <param name="src"></param>
        /// <param name="MaskWidth"></param>
        /// <param name="MaskHeight"></param>
        /// <param name="Factor"></param>
        /// <returns></returns>
        static public Mat Emphasize(Mat src,int MaskWidth,int MaskHeight,int Factor=5)
        {
            if (src.Channels() == 3)
                Cv2.CvtColor(src, src, ColorConversionCodes.BGR2GRAY);
            Mat imgMean=  ImageFilter.MeanImage(src,new Size(MaskWidth, MaskHeight));
            Mat imgEmphasize = new Mat(src.Size(), MatType.CV_8UC1);
            for (int i = 0; i < src.Rows; i++)
            {
                for (int j = 0; j < src.Cols; j++)
                {

                    byte srcValue = src.At<byte>(i, j);
                    byte meanValue = imgMean.At<byte>(i, j);
                    double temvalue = Math.Round((double)(srcValue - meanValue) * Factor) + srcValue;
                    double newValue = (temvalue >= 255 ? 255 : temvalue);
                    double newValue2 = (newValue <= 0 ? 0 : newValue);
                    imgEmphasize.Set<byte>(i, j, (byte)newValue2);

                    //imgEmphasize.At<Vec3f>(i, j)[0] = (float)Math.Round((double)(src.At<Vec3b>(i, j)[0]- imgMean.At<Vec3b>(i, j)[0])* Factor)+
                    //     src.At<Vec3b>(i, j)[0];
                    //(src.At<Vec3b>(i, j)[0]) * (src.At<Vec3b>(i, j)[0]) * (src.At<Vec3b>(i, j)[0]);

                }

            }
            //归一化到0~255    
            //Cv2.Normalize(imgEmphasize, imgEmphasize, 0, 255, NormTypes.MinMax);
            //转换成8bit图像显示
           // Cv2.ConvertScaleAbs(imgEmphasize, imgEmphasize);
            return imgEmphasize;
        }
        /// <summary>
        /// 图像像素缩放
        /// </summary>
        /// <param name="src"></param>
        /// <param name="Mult"></param>
        /// <param name="Add"></param>
        /// <returns></returns>
        static public  Mat scale_image(Mat src,  float Mult, int  Add )
        {
            if (src.Channels() == 3)
                Cv2.CvtColor(src, src, ColorConversionCodes.BGR2GRAY);
            Mat imageScale = new Mat(src.Size(),  MatType.CV_8UC1);
              
            for (int i = 0; i < src.Rows; i++)
            {
                for (int j = 0; j < src.Cols; j++)
                {
                    float temvalue = src.At<byte>(i, j) * Mult + Add;
                    float newValue = (temvalue >= 255?255: temvalue);
                    float newValue2 = (newValue <= 0 ? 0 : newValue);
                    imageScale.Set<byte>(i, j, (byte)newValue2);                  
                }

            }         
            return imageScale;
        }
        /// <summary>
        /// 图像像素缩放
        /// </summary>
        /// <param name="src"></param>
        /// <param name="minGray"></param>
        /// <param name="maxGray"></param>
        /// <returns></returns>
        static public Mat scale_image_range(Mat src, byte minGray, byte maxGray)
        {
            if (src.Channels() == 3)
                Cv2.CvtColor(src, src, ColorConversionCodes.BGR2GRAY);
            Mat imageScale = new Mat(src.Size(), MatType.CV_8UC1);

            for (int i = 0; i < src.Rows; i++)
            {
                for (int j = 0; j < src.Cols; j++)
                {
                    float temvalue = src.At<byte>(i, j);
                    float newValue = (temvalue >= maxGray ? 255 : temvalue);
                    float newValue2 = (newValue <= minGray ? 0 : newValue);
                    if (newValue2 >= minGray && newValue2 <= maxGray)
                        newValue2 = (newValue2 - minGray) / (maxGray - minGray) * 255;

                    imageScale.Set<byte>(i, j, (byte)newValue2);
                }

            }
         
            return imageScale;
        }
     

    }
}
