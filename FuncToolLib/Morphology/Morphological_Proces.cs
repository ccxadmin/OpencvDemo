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

namespace FuncToolLib.Morphology
{
    /// <summary>
    /// 图像形态学处理
    /// </summary>
    public class Morphological_Proces
    {
        /// <summary>
        /// 图像膨胀:
        /// 对图片的高亮度部分(白色)进行操作,膨胀是对高亮度部分进行"领域扩张"
        /// </summary>
        /// <param name="src">输入图像</param>
        /// <param name="morphShape">结构元素类型</param>
        /// <param name="kernelSize">内核尺寸</param>
        /// <param name="iterations">迭代次数</param>
        /// <returns>返回膨胀后的图像</returns>
        static public Mat DilateImage(Mat src, MorphShapes morphShape, Size kernelSize, int iterations = 1)
        {
            if (src.Empty())
                return null;

            Mat dst = new Mat();
            Mat element = Cv2.GetStructuringElement(morphShape, kernelSize, new CVPoint(-1, -1));
            /*膨胀dilate:
             * src:输入图像(建议二值图)
             * dst:输出图像
             * element:用于膨胀的结构单元。如果element=new Mat()[为空的意思]，则使用一个3x3的矩形结构单元
             * anchor :锚点位置,默认为(-1,-1)表示位于中心
             * iterations :膨胀次数
             * borderType :边界模式,一般使用默认值
             * borderValue :边界值,一般采用默认值
             */
            Cv2.Dilate(src, dst, element, new CVPoint(-1, -1), iterations);

            return dst;
        }


        /// <summary>
        /// 图像腐蚀：
        /// 对图片的高亮度部分(白色)进行操作,腐蚀是对高亮度部分进行"领域蚕食""
        /// </summary>
        /// <param name="src">输入原图</param>
        /// <param name="morphShape">结构元素</param>
        /// <param name="kernelSize">内核尺寸</param>
        /// <param name="iterations">迭代次数</param>
        /// <returns></returns>
        static public Mat ErodeImage(Mat src, MorphShapes morphShape, Size kernelSize, int iterations = 1)
        {
            if (src.Empty())
                return null;

            Mat dst = new Mat();
            Mat element = Cv2.GetStructuringElement(morphShape, kernelSize, new CVPoint(-1, -1));
            /*腐蚀erode:
             * src:输入图像(建议二值图)
             * dst:输出图像
             * element:用于腐蚀的结构单元。如果element=new Mat()[为空的意思]，则使用一个3x3的矩形结构单元
             * anchor :锚点位置,默认为(-1,-1)表示位于中心
             * iterations :腐蚀次数
             * borderType :边界模式,一般使用默认值
             * borderValue :边界值,一般采用默认值
             */
            Cv2.Erode(src, dst, element, new CVPoint(-1, -1), iterations);

            return dst;
        }

        /// <summary>
        /// 形态学处理
        /// </summary>
        /// <param name="src">输入原图</param>
        /// <param name="morphShape">形态结构元素</param>
        /// <param name="kernelSize">内核尺寸</param>
        /// <param name="morphType">形态操作类型</param>
        /// <param name="iterations">迭代次数</param>
        /// <returns></returns>
        static public Mat MorphologyEx(Mat src, MorphShapes morphShape, Size kernelSize,
            MorphTypes morphType,   int iterations = 1)
        {
            if (src.Empty())
                return null;

            Mat dst = new Mat();
            Mat element = Cv2.GetStructuringElement(morphShape, kernelSize, new CVPoint(-1, -1));
            /*腐蚀：对高亮度部分进行"领域蚕食""
             * 膨胀：对高亮度部分进行"领域扩张"
             * 开运算：先腐蚀,后膨胀的过程,可以去掉小的对象
             * 闭运算：先膨后腐蚀的顺序 ,可以填充图像中细小的空洞
             * 形态学梯度：用膨胀图减去腐蚀图,可以将图像边缘凸现出来,可以用其来保留边缘轮廓
             * 顶帽：源图像与开运算的图像相减,用来分离比领近点亮一些的斑块
             * 黑帽：闭运算图像与源图像做差,用来分离比领近点暗一些的斑块
             * 击中击不中：击中击不中变换是形态学中用来检测特定形状所处位置的一个基本工具。它的原理就是使用腐蚀，
             *             如果要在一幅图像A上找到B形状的目标   
             */
            Cv2.MorphologyEx(src, dst, morphType, element, new CVPoint(-1, -1), iterations);

            return dst;
        }


    }
}
