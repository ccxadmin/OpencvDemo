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

namespace FuncToolLib.Filter
{

    /*
     * 1.方框滤波(当normalize为True)与均值滤波:
     * 就是利用区域内像素的均值替换掉原有的像素值,在去噪的同时也会破坏掉原有图像的细节部分.
     * 
     * 
     * 2.高斯滤波:
     * 每个像素点的值都由本身与和邻近区域的其他像素值经过加权平均后得到,加权系数越靠近中心越大,越远离中心越小
     *    
     * 3.中值滤波:
     * 是一种非线性滤波,使用像素点领近点的的灰度值的中值代替改点的灰度值,可以去除脉冲噪声跟椒盐噪声
     * 
     * 
     * 4.双边滤波:
     * 是一种非线性滤波器,是结合图像空间邻近度和像素值相似度的一种折中处理,尽量在降噪的同时保存边缘
     */

    /// <summary>
    /// 图像滤波器
    /// </summary>
    public class ImageFilter
    {
        /// <summary>
        /// 方框滤波
        /// </summary>
        /// <param name="src">输入图像</param>
        /// <param name="kernelSize">内核尺寸</param>
        /// <returns>返回滤波后图像</returns>
        static public Mat BoxFilter(Mat src, Size kernelSize)
        {
            if (src.Empty())
                return null;

            Mat dst = new Mat();
            /*方框滤波:BoxFilter
             * src:输入图像
             * dst:输出图像
             * ddepth:MatType类型,图像深度,如果为-1则为源图像深度,例:src.Depth
             * ksize:平滑核大小,决定图像的质量
             * anchor:锚点。默认值Point(-1，-1)表示锚位于内核中心
             * normalize:指示内核是否归一化
             * borderType :输出图像的边框样式,一般用默认样式
             * 当normalize为true时,方框滤波也就成了均值滤波
             */
            Cv2.BoxFilter(src, dst, dst.Depth(), kernelSize,
                new CVPoint(-1, -1), false);
            return dst;
        }
        /// <summary>
        /// 均值滤波
        /// </summary>
        /// <param name="src">输入图像</param>
        /// <param name="kernelSize">内核尺寸</param>
        /// <returns>返回滤波后图像</returns>
        static public Mat MeanImage(Mat src, Size kernelSize)
        {
            if (src.Empty())
                return null;

            Mat dst = new Mat();

            /*均值滤波Blur:
             * src:输入图像
             * dst:输出图像
             * ksize:内核大小,size(X,Y),假如size(3,3)则表示3*3的内核大小
             * anchor: 锚点。默认值Point(-1，-1)表示锚位于内核中心
             * borderType: 输出图像的边框样式,一般用默认样式
             */
            Cv2.Blur(src, dst, kernelSize);

            return dst;
        }

        /// <summary>
        /// 高斯滤波
        /// </summary>
        /// <param name="src">输入图像</param>
        /// <param name="kernelSize">内核尺寸</param>
        /// <returns>返回滤波后图像</returns>
        static public Mat GaussImage(Mat src, Size kernelSize,double sigmaX,
            double sigmaY=0)
        {
            if (src.Empty())
                return null;

            Mat dst = new Mat();

            /*高斯滤波GaussianBlur:
             * src:输入图像
             * dst:输出图像
             * ksize:内核大小,size(X,Y),假如size(3,3)则表示3*3的内核大小
             * sigmaX:表示高斯核在X轴方向的标准偏差
             * sigmaY :表示高斯核在Y轴方向的标准偏差值,如果sigmaY 为0,则sigmaY =sigmaX,如果两个sigma都为零，则从ksize计算
             * borderType :一般用默认值
             */
            Cv2.GaussianBlur(src, dst, kernelSize, sigmaX, sigmaY);

            return dst;
        }


        /// <summary>
        /// 中值滤波
        /// 内核尺寸必须大于1,且为奇数
        /// </summary>
        /// <param name="src">输入图像</param>
        /// <param name="kernelSize">内核尺寸必须大于1,且为奇数</param>
        /// <returns>返回滤波后图像</returns>
        static public Mat MedianImage(Mat src, int kernelSize)
        {
            if (src.Empty())
                return null;

            Mat dst = new Mat();

            /*均值滤波Blur:
             * src:输入图像
             * dst:输出图像
             * ksize:内核大小,必须大于1,且为奇数                  
             */
            Cv2.MedianBlur(src, dst, kernelSize);

            return dst;
        }

        /// <summary>
        /// 双边滤波
        /// </summary>
        /// <param name="src">输入图像</param>
        /// <param name="d">在滤波过程中使用的每个像素邻域的直径</param>
        /// <param name="sigmaColor">在颜色空间中过滤sigma</param>
        /// <param name="sigmaSpace">在坐标空间中过滤,且为奇数</param>
        /// <returns>返回滤波后图像</returns>
        static public Mat BilateralImage(Mat src, 
            int d,double sigmaColor,double sigmaSpace)
        {
            if (src.Empty())
                return null;

            Mat dst = new Mat();

            /*双边滤波BilateralFilter:
             * src:输入图像
             * dst:输出图像
             * d:在滤波过程中使用的每个像素邻域的直径   
             * sigmaColor:在颜色空间中过滤sigma。该参数值越大，表示像素邻域内更多的颜色会混合在一起，从而产生更大的半均等颜色区域
             * sigmaSpace:在坐标空间中过滤。参数值越大，越远的像素会相互影响(只要它们的颜色足够接近;见sigmaColor)。然后d>0，
             *            它指定的邻域大小与sigmspace无关，否则d与sigmspace成比例
             * borderType :一般是用默认值             
             */
            Cv2.BilateralFilter(src, dst,  d, sigmaColor, sigmaSpace);

            return dst;
        }


    }

    
}
