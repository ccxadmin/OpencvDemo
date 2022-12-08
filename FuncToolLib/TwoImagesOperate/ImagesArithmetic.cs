
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

namespace FuncToolLib.TwoImagesOperate
{
    /// <summary>
    /// 图像数学运算
    /// </summary>
    public class ImagesArithmetic
    {
        /// <summary>
        /// 图像相加
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img2"></param>
        /// <returns></returns>
        static public Mat AddOfTwoImages(Mat img,Mat img2)
        {
            if (img.Empty() || img2.Empty())
                return null;
            if (img.Width != img2.Width ||
                img.Height != img2.Height)
                return null;
            Mat dst = new Mat();
            Cv2.Add(img, img2, dst);
            return dst;

        }
        /// <summary>
        /// computes weighted sum of two arrays (dst = alpha*src1 + beta*src2 + gamma)
        /// </summary>
        /// <param name="img"></param>
        /// <param name="weight"></param>
        /// <param name="img2"></param>
        /// <param name="weight2"></param>
        /// <param name="offvalue"></param>
        /// <returns></returns>
        static public Mat AddOfTwoImages(Mat img,double weight, Mat img2,double weight2,
            double offvalue=0)
        {
            if (img.Empty() || img2.Empty())
                return null;
            if (img.Width != img2.Width ||
                img.Height != img2.Height)
                return null;
            Mat dst = new Mat();
            Cv2.AddWeighted(img, weight, img2, weight2, offvalue, dst);
            return dst;

        }
        /// <summary>
        /// adds scaled array to another one (dst = alpha*src1 + src2)
        /// </summary>
        /// <param name="img"></param>
        /// <param name="weight"></param>
        /// <param name="img2"></param>
        /// <returns></returns>
        static public Mat AddOfTwoImages(Mat img, double weight, Mat img2)
        {
            if (img.Empty() || img2.Empty())
                return null;
            if (img.Width != img2.Width ||
                img.Height != img2.Height)
                return null;
            Mat dst = new Mat();
            Cv2.ScaleAdd(img, weight, img2,  dst);
            return dst;

        }

        /// <summary>
        /// 图像直接相减
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img2"></param>
        /// <returns></returns>
        static public Mat SubOfTwoImages(Mat img, Mat img2)
        {
            if (img.Empty() || img2.Empty())
                return null;
            if (img.Width != img2.Width ||
                img.Height != img2.Height)
                return null;
            Mat dst = new Mat();
            Cv2.Subtract(img, img2, dst);
            return dst;

        }

        /// <summary>
        /// 图像相减后取绝对值
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img2"></param>
        /// <returns></returns>
        static public Mat AbsSubOfTwoImages(Mat img, Mat img2)
        {
            if (img.Empty() || img2.Empty())
                return null;
            if (img.Width != img2.Width ||
                img.Height != img2.Height)
                return null;
            Mat dst = new Mat();
            Cv2.Absdiff(img, img2, dst);
            return dst;

        }
        /// <summary>
        /// 图像相乘
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img2"></param>
        /// <returns></returns>
        static public Mat MulOfTwoImages(Mat img, Mat img2)
        {
            if (img.Empty() || img2.Empty())
                return null;
            if (img.Width != img2.Width ||
                img.Height != img2.Height)
                return null;
            Mat dst = new Mat();
            Cv2.Multiply(img, img2, dst);
            return dst;

        }
        /// <summary>
        /// 图像相除
        /// </summary>
        /// <param name="img"></param>
        /// <param name="img2"></param>
        /// <returns></returns>
        static public Mat DivOfTwoImages(Mat img, Mat img2)
        {
            if (img.Empty() || img2.Empty())
                return null;
            if (img.Width != img2.Width ||
                img.Height != img2.Height)
                return null;
            Mat dst = new Mat();
            Cv2.Divide(img, img2, dst);
            return dst;

        }
    }
}
