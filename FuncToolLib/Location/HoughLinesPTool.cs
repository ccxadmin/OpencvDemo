using OpenCvSharp;
using ParamDataLib;
using ParamDataLib.Location;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using Rect = System.Drawing.RectangleF;
using Point = System.Drawing.PointF;

namespace FuncToolLib.Location
{
    /// <summary>
    /// 查找直线（霍夫变换）
    /// </summary>
    public class HoughLinesPTool : ToolBase
    {
        /// <summary>
        /// 直线检测运行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputImg"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Result Run<T>(Mat gray, T obj)
        {
            HoughLinesPData houghLinesPData = obj as HoughLinesPData;
            Result houghLinesPResult = new HoughLinesPResult();
                  
            try
            {
                CVRect BoundingRect = new CVRect();
                //获取检测掩膜图像
                Mat temimg = MatExtension.Crop_Mask_Mat(gray, (CVRRect)houghLinesPData.ROI,out BoundingRect);

                /*获取霍夫直线*/
                LineSegmentPoint[] lineSegmentPointArray = HoughLinesP(temimg, houghLinesPData.canThddown, houghLinesPData.canThdup,
                             houghLinesPData.ThresholdP, houghLinesPData.MinLineLenght, houghLinesPData.MaxLineGap);

                //剔除边界重合点
                LineSegmentPoint[] lineSegmentPoints= MatExtension.ExceptBoundOfRRect(lineSegmentPointArray, (CVRRect)houghLinesPData.ROI,
                   BoundingRect);
    
                /*多直线合并成单直线*/
                LineSegmentPoint lineSegment= MatExtension.UnionLines2(lineSegmentPoints,
                    BoundingRect.Width, BoundingRect.Height,
                    2);

                Mat dst = new Mat();
                Cv2.CvtColor(gray, dst, ColorConversionCodes.GRAY2BGR);
            
                /*拟合成单直线*/
                //if (c_idex>=0)
                if (lineSegment!=null&& lineSegment.P1!= lineSegment.P2)
                {
                    Cv2.Line(dst, lineSegment.P1.X + BoundingRect.X,
                      lineSegment.P1.Y + BoundingRect.Y,
                      lineSegment.P2.X + BoundingRect.X,
                    lineSegment.P2.Y + BoundingRect.Y,                
                     Scalar.Lime, 1);   //依据点集画线

                    (houghLinesPResult as HoughLinesPResult).positionData.Add(
                        new Point2d(
                        lineSegment.P1.X + BoundingRect.X, lineSegment.P1.Y + BoundingRect.Y)
                        );

                    (houghLinesPResult as HoughLinesPResult).positionData.Add(
                        new Point2d(
                         lineSegment.P2.X + BoundingRect.X, lineSegment.P2.Y + BoundingRect.Y)
                        );

                    houghLinesPResult.runStatus = true;
                }  
                else
                    houghLinesPResult.runStatus = false;
                //检测区域
                 dst.DrawRotatedRect((CVRRect)houghLinesPData.ROI, new Scalar(255, 0, 0), 2);
                houghLinesPResult.resultToShow = dst;
                
                
            }
            catch(Exception er)
            {
                Cv2.CvtColor(gray, houghLinesPResult.resultToShow, ColorConversionCodes.GRAY2BGR);
                //检测区域
                houghLinesPResult.resultToShow.DrawRotatedRect((CVRRect)houghLinesPData.ROI, new Scalar(255, 0, 0), 2);
                houghLinesPResult.exceptionInfo = er.Message;
                houghLinesPResult.runStatus = false;
            }
            return houghLinesPResult;
        }



        /****************************霍夫变换-直线检测******************/
        /// <summary>
        /// 霍夫直线
        /// </summary>
        /// <param name="src_img">输入图</param>
        /// <param name="ThresholdP">阈值</param>
        /// <param name="MinLinLenght">最小线长度</param>
        /// <param name="MaxLineGap">最大允许间隙</param>
        /// <returns></returns>
        private LineSegmentPoint[] HoughLinesP(Mat src_img, double canThddown, double canThdup,
           int ThresholdP,double MinLinLenght,  double MaxLineGap)
        {
            try
            {
                Mat CannyImg= Contour.EdgeTool.Canny(src_img, canThddown, canThdup);
                //LineSegmentPoint[] lineSegmentPointArray;
                //Mat dst = new Mat();
                //CannyImg.CopyTo(dst);
                return  Cv2.HoughLinesP(CannyImg, 1, Cv2.PI / 180, ThresholdP, MinLinLenght,
                          MaxLineGap);
                /*
                    *  HoughLinesP:使用概率霍夫变换查找二进制图像中的线段。
                    *  参数：
                    *      1； image: 输入图像 （只能输入单通道图像）
                    *      2； rho:   累加器的距离分辨率(以像素为单位) 生成极坐标时候的像素扫描步长
                    *      3； theta: 累加器的角度分辨率(以弧度为单位)生成极坐标时候的角度步长，一般取值CV_PI/180 ==1度
                    *      4； threshold: 累加器阈值参数。只有那些足够的行才会返回 投票(>阈值)；设置认为几个像素连载一起才能被看做是直线。
                    *      5； minLineLength: 最小线长度，设置最小线段是有几个像素组成。
                    *      6； maxLineGap: 同一条线上的点之间连接它们的最大允许间隙。(默认情况下是0）：设置你认为像素之间间隔多少个间隙也能认为是直线
                    *      返回结果:
                    *     返回值类型：LineSegmentPoint[], 输出线,每条线由一个4元向量(x1, y1, x2，y2)
                    */
                //Mat ndst = new Mat();
                //src_img.CopyTo(ndst);
                //for (int i = 0; i < lineSegmentPointArray.Length; i++)
                //{
                //    Cv2.Line(ndst, lineSegmentPointArray[i].P1, lineSegmentPointArray[i].P2, Scalar.Red,2);   //依据点集画线
                //}
               
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }

        }
    }

    /// <summary>
    ///  直线拟合结果
    /// </summary>
    public class HoughLinesPResult : Result
    {
        public HoughLinesPResult()
        {
            positionData = new List<Point2d>();

        }
        public List<Point2d> positionData;

    }
}
