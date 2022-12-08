using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using ParamDataLib.Location;
using VisionShowLib;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using Rect = System.Drawing.RectangleF;
using Point = System.Drawing.PointF;

namespace FuncToolLib.Location
{
    /// <summary>
    /// 圆形卡尺工具
    /// </summary>
    public class CircularCaliperTool : ToolBase
    {
        public override Result Run<T>(Mat gray, T obj)
        {
            CircularCaliperData circularCaliperData = obj as CircularCaliperData;
            Result circularCaliperResult = new CircularCaliperResult();
            List<RotatedCaliperRectF> rotatedRectFs = new List<RotatedCaliperRectF>();
           
            if(circularCaliperData.ROI is SectorF)
            {
                SectorF sectorF = (SectorF)circularCaliperData.ROI;
                rotatedRectFs = VisionShowControl.GetCircularCalipersRegions(sectorF,
                                  circularCaliperData.caliperWidth,
                                  circularCaliperData.caliperNum,
                                  circularCaliperData.circleDir);
            }
            else
            {
                CircleF circleF = (CircleF)circularCaliperData.ROI;
                rotatedRectFs = VisionShowControl.GetCircularCalipersRegions(circleF,
                                  circularCaliperData.caliperWidth, circularCaliperData.caliperNum);
            }
            (circularCaliperResult as CircularCaliperResult).rotatedRectFs = rotatedRectFs;

            try
            {
                if (rotatedRectFs.Count <= 0)
                {
                    circularCaliperResult.exceptionInfo = "卡尺圆工具数量小于0";
                    circularCaliperResult.runStatus = false;
                    return circularCaliperResult;
                }
              
                List<Point2f> point2Fs1 = new List<Point2f>();
                foreach (var s in rotatedRectFs)
                    point2Fs1.Add(new Point2f(s.centerP.X, s.centerP.Y));//添加卡尺矩形中心点

                Mat dst = new Mat();
                //外加高斯滤波
                Mat GaussMat = Filter.ImageFilter.GaussImage(gray, new Size(3, 3), 0);
                Cv2.CvtColor(GaussMat, dst, ColorConversionCodes.GRAY2BGR);
             
                List<Point2f> edges = new List<Point2f>();//边缘点集合
                List<Point2f> vpdExceptPoints = new List<Point2f>();
             ///   CVRect BoundingRect = new CVRect();

                for (int i = 0; i < rotatedRectFs.Count; i++)
                {
                    float roAngle = rotatedRectFs[i].angle;
                    Point2f temP = point2Fs1[i];
                    float tx = 0, ty = 0;

                    /*先裁剪在旋转提升检测速度*/
                    //Mat cropRRectMat = MatExtension.Crop_Mask_Mat(GaussMat,
                    //    new CVRRect(new Point2f(temP.X, temP.Y),
                    //new Size2f(rotatedRectFs[0].size.Width, rotatedRectFs[0].size.Height), roAngle),
                    //          out BoundingRect);

                    //Mat roMat = cropRRectMat.RotateAffine(roAngle, ref temP, ref tx, ref ty);
                    /***********************/

                    Mat roMat = dst.RotateAffine(roAngle, ref temP, ref tx, ref ty);
                    CVRRect cVRRect = new CVRRect(new Point2f(temP.X, temP.Y),
                        new Size2f(rotatedRectFs[0].size.Width, rotatedRectFs[0].size.Height), 0);

                    Mat roMat2 = new Mat();
                    Cv2.CvtColor(roMat, roMat2, ColorConversionCodes.BGR2GRAY);
                    CVRect rect = cVRRect.BoundingRect();
                    //区域超界限判断
                    if (rect.X < 0 || rect.Y < 0 || (rect.X + rect.Width > roMat2.Width) || (rect.Y + rect.Height > roMat2.Height))
                        continue;

                    Mat tem = MatExtension.Crop_Mask_Mat(roMat2, rect);
                    /***********************/

                    List<Point2f> point2Fs = new List<Point2f>();
                    CircularCaliperTool.FindEdges(tem, circularCaliperData.edgeThreshold, ref point2Fs,
                         circularCaliperData.edgePolarity);

                    if (point2Fs.Count > 0)
                    {
                        CVPoint cVPoint = new CVPoint(point2Fs[0].X + rect.X, point2Fs[0].Y + rect.Y);

                      // Point2f point2f = new Point2f(point2Fs[0].X, point2Fs[0].Y);
                        roMat.RotateAffineINV(-roAngle, -tx, -ty, ref cVPoint);
                      //  Point2f dp = new Point2f(point2f.X + rect.X, point2f.Y + rect.Y);
                   
                        edges.Add(cVPoint);
                    }
                }
               
                if (edges.Count >= 3)
                {
                    //亚像素角点
                    /// 角点位置精准化参数
                 //   Size winSize = new Size(5, 5);
                 //   Size zeroZone = new Size(-1, -1);
                    //迭代的终止条件
                    //TermCriteria criteria = new TermCriteria(
                    //    CriteriaTypes.Eps & CriteriaTypes.MaxIter,
                    //    40, //maxCount=40
                    //    0.001);    //epsilon=0.001
                 
                 //   Point2f[] SubCorners = Cv2.CornerSubPix(gray, edges, winSize, zeroZone, criteria);

                  //  edges = SubCorners.ToList<Point2f>();
                  //  if (circularCaliperData.fitMethod == EumFittingMethod.Least_square_method)
                    {
                        M_CIRCLE circle1 = CircularCaliperTool.fitCircle(edges);
                        dst.drawCross(circle1.centreP.ToPoint(),Scalar.LimeGreen,20,2);
                        dst.Circle(circle1.centreP.ToPoint(), (int)circle1.radius, Scalar.LimeGreen);

                        (circularCaliperResult as CircularCaliperResult).centreP= circle1.centreP;
                        (circularCaliperResult as CircularCaliperResult).radius= circle1.radius;
                    }
                    //else
                    //{
                    //    M_CIRCLE circle2 = CircularCaliperTool.RansacCircleFiler(edges,
                    //        out vpdExceptPoints);
                    //    dst.drawCross(circle2.centreP.ToPoint(), Scalar.LimeGreen, 20, 2);
                    //    dst.Circle(circle2.centreP.ToPoint(), (int)circle2.radius, Scalar.Blue);
                    //    (circularCaliperResult as CircularCaliperResult).centreP = circle2.centreP;
                    //    (circularCaliperResult as CircularCaliperResult).radius = circle2.radius;
                    //}
                    circularCaliperResult.runStatus = true;
                }
                else
                    circularCaliperResult.runStatus = false;

                (circularCaliperResult as CircularCaliperResult).edges = edges;
                //绘制边缘点
                foreach (var s in edges)
                    dst.drawCross(s.ToPoint(), Scalar.LimeGreen, 10, 1);
                //绘制卡尺检测区域
                foreach (var s in rotatedRectFs)
                    dst.DrawRotatedRect(new CVRRect(new Point2f(s.centerP.X, s.centerP.Y),
                        new Size2f(s.size.Width, s.size.Height), s.angle));
                circularCaliperResult.resultToShow = dst;
            }
            catch (Exception er)
            {
                Cv2.CvtColor(gray, circularCaliperResult.resultToShow, ColorConversionCodes.GRAY2BGR);
                //检测区域
                //绘制卡尺检测区域
                foreach (var s in rotatedRectFs)
                    circularCaliperResult.resultToShow.DrawRotatedRect(new CVRRect(new Point2f(s.centerP.X, s.centerP.Y),
                        new Size2f(s.size.Width, s.size.Height), s.angle));
                circularCaliperResult.exceptionInfo = er.Message;
                circularCaliperResult.runStatus = false;
            }
            return circularCaliperResult;
        }


        static Mat on_Canny(Mat g_srcImage, double canThddown, double canThdup)
        {
            //Mat GaussMat = Filter.ImageFilter.GaussImage(g_srcImage, new Size(3, 3), 0);
            Mat cannyMat = new Mat(g_srcImage.Size(), g_srcImage.Type());
            Cv2.Canny(g_srcImage, cannyMat, canThddown, canThdup);
            //cannny。参数：1：src_img：8 bit 输入图像；2：dst输出边缘图像，一般是二值图像，背景是黑色；3：tkBarCannyMin.Value低阈值。值越大，找到的边缘越少；4：tkBarCannyMax.Value高阈值；5:hole表示应用Sobel算子的孔径大小，其有默认值3；6:rbBtnTrue.Checked计算图像梯度幅值的标识，有默认值false。
            //低于阈值1的像素点会被认为不是边缘；
            //高于阈值2的像素点会被认为是边缘；
            //在阈值1和阈值2之间的像素点,若与一阶偏导算子计算梯度得到的边缘像素点相邻，则被认为是边缘，否则被认为不是边缘。
            return cannyMat;

        }
        /// <summary>
        /// 垂直线条检测
        /// </summary>
        /// <param name="srcImage"></param>
        /// <returns></returns>
        static Mat VerticalLine(Mat srcImage)
        {

            List<byte> array = new List<byte>();

            for (int i = 0; i < srcImage.Cols; i++)
            {
                int sumvalue = 0;int c_row = 0;
                for (int j = 0; j < srcImage.Rows; j++)
                {
                    byte temvalue = srcImage.At<byte>(j, i);
                    /*裁剪后的图像，进行旋转会留下很多的无效区域，像素值为0*/
                    if (temvalue > 0)//摒除像素值为0的点
                    {
                        sumvalue += srcImage.At<byte>(j, i);//每一列的像素和
                        c_row++;
                    }
                    
                }
                byte Average_strength = (byte)(sumvalue / (c_row==0? srcImage.Rows: c_row));//每一列的平均强度,摒除像素值为0的点

                array.Add(Average_strength);

            }
            Mat lineImage = new Mat(1, array.Count, MatType.CV_8UC1, new Scalar(0, 0, 0));

            int count = array.Count();
            //恢复直线
            for (int n = 0; n < lineImage.Rows; n++)
            {
                for (int w = 0; w < count; w++)
                {
                    lineImage.Set<byte>(n, w, array[w]);

                }
            }

            return lineImage;
        }

        /// <summary>
        /// 边缘点查找
        /// </summary>
        /// <param name="srcImage">输入图</param>
        /// <param name="eumProjection_Direction">投影方向</param>
        /// <param name="edgeThreshold">边缘阈值</param>
        /// <param name="zeroPList">边缘点坐标集合</param>
        /// <returns></returns>
        static public Mat FindEdges(Mat srcImage, byte edgeThreshold, ref List<Point2f> zeroPList,
             EumEdgePolarity edgePolarity = EumEdgePolarity.all)
        {
         //   CVRect boundary = srcImage.BoundingRect();
            CVRect boundary = new CVRect(0, 0, srcImage.Width, srcImage.Height);
                 float cenY = (boundary.Y + boundary.Height) / 2;
            Mat projectMat = VerticalLine(srcImage);//平均值灰度投影
            zeroPList = new List<Point2f>();//极值点坐标集合
            Mat sobelMat = new Mat();
            Cv2.Sobel(projectMat, sobelMat, MatType.CV_64F, 1, 0, 1, 1, 0);

            Mat gxAbs = new Mat();
            Cv2.ConvertScaleAbs(sobelMat, gxAbs);
            int wid = sobelMat.Width;
            //Console.WriteLine("卡尺：");
            for (int i = 0; i < wid; i++)
            {
                //目标边缘梯度
                byte _sdx = gxAbs.At<byte>(0, i);

                double value = sobelMat.At<double>(0, i);

                //Console.WriteLine(string.Format("{0},{1}", _sdx, value));
                if (_sdx >= edgeThreshold)
                {
                    if ((edgePolarity == EumEdgePolarity.all) ||
                        (edgePolarity == EumEdgePolarity.positive && value > 0) ||
                        (edgePolarity == EumEdgePolarity.negtive && value < 0))
                    {
                        zeroPList.Add(new Point2f(i, cenY));
                        break;
                    }

                }

            }

            //Mat thdMat = new Mat();
            //Cv2.Threshold(projectMat, thdMat, edgeThreshold, 255, ThresholdTypes.Binary);
            //Mat CannyMat = on_Canny(thdMat, edgeThreshold, 255);//二阶求导零点
            //Mat mat = CannyMat.FindNonZero();
            //CannyMat.GetArray<byte>(out byte[] bytearray);

            //for (int i = 0; i < CannyMat.Cols; i++)
            //{
            //    if (CannyMat.At<byte>(0, i) == 255
            //        && i > 5 && i < CannyMat.Cols - 5)//去掉首位干扰点
            //        zeroPList.Add(new Point2f(i, cenY));
            //}
            return gxAbs;

        }

       /// <summary>
       /// 最小二乘法圆拟合
       /// </summary>   
       /// <param name="fitPList"></param>
       /// <returns></returns>
        static public M_CIRCLE fitCircle( List<Point2f> fitPList)
        {
            if (fitPList.Count < 3) return default;

            float  Radius = 0.0f;
            Point2f center = new Point2f();
            double sum_x = 0.0f, sum_y = 0.0f;
            double sum_x2 = 0.0f, sum_y2 = 0.0f;
            double sum_x3 = 0.0f, sum_y3 = 0.0f;
            double sum_xy = 0.0f, sum_x1y2 = 0.0f, sum_x2y1 = 0.0f;
            int N = fitPList.Count();
            for (int i = 0; i < N; i++)
            {
                double x = fitPList.ElementAt(i).X;
                double y = fitPList.ElementAt(i).Y;
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
            center.X = (float)a / (-2);
            center.Y = (float)b / (-2);
            Radius = (float)Math.Sqrt(a * a + b * b - 4 * c) / 2;
            if (double.IsNaN(Radius))
                return default;

            return new M_CIRCLE { centreP= center,radius= Radius };
            //得分验证
            //double sum = 0;
            //for (int i = 0; i < N; i++)
            //{
            //    Point2f pti = fitPList.ElementAt(i);
            //    double ri = Math.Sqrt(Math.Pow(pti.X - center.X, 2) + Math.Pow(pti.Y - center.Y, 2));             
            //    sum += ri / radius > 1 ? ri / radius - 1 : 1 - ri / radius;
            //}
            //double sorce = 1 - sum / N;
            //return (float)sorce;         
        }

        /// <summary>
        /// 中心
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        static public Point2f GetPPCenter(Point2f p1, Point2f p2)
        {
            return new Point2f((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        }
        /// <summary>
        /// 斜率
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        static public float GetLineSlope(Point2f p1, Point2f p2)
        {
            return (p2.Y - p1.Y) / (p2.X - p1.X);
        }
        /// <summary>
        /// 距离
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        static public float GetPPDistance(Point2f p1, Point2f p2)
        {
            return (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        /// <summary>
        /// 随机采样一致性
        /// </summary>
        /// <param name="points"></param>
        /// <param name="vpdExceptPoints"></param>
        /// <param name="sigma"></param>
        static public void RansacCircleFiler(Point2f[] points, out List<Point2f> vpdExceptPoints, double sigma )
        {
            int n = points.Length;
            vpdExceptPoints = new List<Point2f>();

            if (n < 3)
            {
                return;
            }

            Random random = new Random();
            double bestScore = -1.0;
            List<Point2f> vpdTemp = new List<Point2f>();
            int iterations = (int)(Math.Log(1 - 0.99) / (Math.Log(1 - (1.00 / n))) * 10);

            for (int k = 0; k < iterations; k++)
            {
                int i1 = 0, i2 = 0, i3 = 0;
                Point2f p1 = new Point2f(0, 0), p2 = new Point2f(0, 0), p3 = new Point2f(0, 0);
                while (true)
                {
                    i1 = random.Next(n);
                    i2 = random.Next(n);
                    i3 = random.Next(n);
                    if ((i1 != i2 && i1 != i3 && i2 != i3))
                    {
                        if ((points[i1].Y != points[i2].Y) && (points[i1].Y != points[i3].Y))
                        {
                            break;
                        }
                    }
                }
                p1 = points[i1];
                p2 = points[i2];
                p3 = points[i3];

                //use three points to caculate a circle
                Point2f pdP12 = GetPPCenter(p1, p2);
                double dK1 = -1 / GetLineSlope(p1, p2);
                double dB1 = pdP12.Y - dK1 * pdP12.X;
                Point2f pdP13 = GetPPCenter(p1, p3);
                double dK2 = -1 / GetLineSlope(p1, p3);
                double dB2 = pdP13.Y - dK2 * pdP13.X;
                Point2f pdCenter = new Point2f(0, 0);
                pdCenter.X = (float)((dB2 - dB1) / (dK1 - dK2));
                pdCenter.Y = (float)(dK1 * pdCenter.X + dB1);
                double dR = GetPPDistance(pdCenter, p1);
                double score = 0;
                vpdTemp.Clear();
                for (int i = 0; i < n; i++)
                {
                    double d = dR - GetPPDistance(points[i], pdCenter);
                    if (Math.Abs(d) < sigma)
                    {
                        score += 1;
                    }
                    else
                    {
                        vpdTemp.Add(points[i]);
                    }
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    vpdExceptPoints = vpdTemp;
                }
            }
        }

        /// <summary>
        /// Ransac随机一致性圆拟合
        /// </summary>
        /// <param name="points"></param>
        /// <param name="vpdExceptPoints"></param>
        /// <param name="iterations"></param>
        /// <param name="sigma"></param>
        /// <returns></returns>
        static public M_CIRCLE RansacCircleFiler(List<Point2f> points, out List<Point2f> vpdExceptPoints,
           int iterations = 1000, double sigma = 1.0)
        {
            int n = points.Count;
            vpdExceptPoints = new List<Point2f>();

            if (n < 3)
            {
                return default;
            }

            Random random = new Random();
            double bestScore = -1.0;
            //List<Point2f> vpdTemp = new List<Point2f>();
            //int iterations = (int)(Math.Log(1 - 0.99) / (Math.Log(1 - (1.00 / n))) * 10);
            M_CIRCLE m_CIRCLE1=new M_CIRCLE () ;
            for (int k = 0; k < iterations; k++)
            {
                int i1 = 0, i2 = 0, i3 = 0;
                Point2f p1 = new Point2f(0, 0), p2 = new Point2f(0, 0), p3 = new Point2f(0, 0);
                while (true)
                {
                    i1 = random.Next(n);
                    i2 = random.Next(n);
                    i3 = random.Next(n);
                    if ((i1 != i2 && i1 != i3 && i2 != i3))
                    {
                        if ((points[i1].Y != points[i2].Y) && (points[i1].Y != points[i3].Y))
                        {
                            break;
                        }
                    }
                }
                p1 = points[i1];
                p2 = points[i2];
                p3 = points[i3];

                //use three points to caculate a circle
                Point2f pdP12 = GetPPCenter(p1, p2);
                double dK1 = -1 / GetLineSlope(p1, p2);
                double dB1 = pdP12.Y - dK1 * pdP12.X;
                Point2f pdP13 = GetPPCenter(p1, p3);
                double dK2 = -1 / GetLineSlope(p1, p3);
                double dB2 = pdP13.Y - dK2 * pdP13.X;
                Point2f pdCenter = new Point2f(0, 0);
                pdCenter.X = (float)((dB2 - dB1) / (dK1 - dK2));
                pdCenter.Y = (float)(dK1 * pdCenter.X + dB1);
                double dR = GetPPDistance(pdCenter, p1);
                double score = 0;
                //vpdTemp.Clear();
                for (int i = 0; i < n; i++)
                {
                    double d = dR - GetPPDistance(points[i], pdCenter);
                    if (Math.Abs(d) < sigma)
                    {
                        score += 1;
                    }
                    //else
                    //{
                    //    vpdTemp.Add(points[i]);
                    //}
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    //vpdExceptPoints = vpdTemp;
                    m_CIRCLE1 = new M_CIRCLE {centreP= pdCenter ,radius= (float)dR };
                }
            }
            return m_CIRCLE1;
        }


    }
    /// <summary>
    /// 圆形
    /// </summary>
    public struct M_CIRCLE
    {
        public Point2f centreP;
        public float radius;
    
    }
    public class CircularCaliperResult: Result
    {
        public Point2f centreP;
        public float radius;
        public List<Point2f> edges = new List<Point2f>();//边缘点集合
        public List<RotatedCaliperRectF> rotatedRectFs = new List<RotatedCaliperRectF>();

    }
}
