using OpenCvSharp;
using ParamDataLib.Location;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VisionShowLib;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using Rect = System.Drawing.RectangleF;
using Point = System.Drawing.PointF;
using FuncToolLib.Contour;

namespace FuncToolLib.Location
{
    /// <summary>
    /// 直线卡尺工具
    /// </summary>
    public class LinearCaliperTool : ToolBase
    {
        public override Result Run<T>(Mat gray, T obj)
        {
            LinearCaliperData linearCaliperData = obj as LinearCaliperData;
            Result linearCaliperResult = new LinearCaliperResult();
            RotatedCaliperRectF RegionRRect = (RotatedCaliperRectF)linearCaliperData.ROI;
            List<RotatedCaliperRectF> rotatedRectFs = VisionShowControl.GetLinearCalipersRegions(RegionRRect,
                                   linearCaliperData.caliperWidth, linearCaliperData.caliperNum);
            (linearCaliperResult as LinearCaliperResult).rotatedRectFs = rotatedRectFs;
            try
            {            
                if (rotatedRectFs.Count <= 0)
                {
                    linearCaliperResult.exceptionInfo = "卡尺直线工具数量小于0";
                    linearCaliperResult.runStatus = false;
                    return linearCaliperResult;
                }
                //float roAngle = rotatedRectFs[0].angle;
                List<Point2f> point2Fs1 = new List<Point2f>();
                foreach (var s in rotatedRectFs)
                    point2Fs1.Add(new Point2f(s.centerP.X, s.centerP.Y));//添加卡尺矩形中心点

                Mat dst = new Mat();
                //高斯过滤
          
                Mat GaussMat = Filter.ImageFilter.GaussImage(gray, new Size(3, 3), 0);
            
                Cv2.CvtColor(GaussMat, dst, ColorConversionCodes.GRAY2BGR);
              
                //Mat roMat = dst.RotateAffineOfSizeNoChange(roAngle, ref point2Fs1);//根据图像旋转的角度更新中心点的坐标
                           
                List<Point2f> edges = new List<Point2f>();//边缘点集合
                Mat roMat = dst.RotateAffine(rotatedRectFs[0].angle);
                for (int i = 0; i < point2Fs1.Count; i++)
                {
                   
                    float roAngle = rotatedRectFs[i].angle;
                    Point2f temP = point2Fs1[i];
                    float tx = 0, ty = 0;

                    // Mat roMat = dst.RotateAffine(roAngle, ref temP, ref tx, ref ty);
                    dst.RotateAffine2(roAngle, ref temP, ref tx, ref ty);

                    ///根据新中心点位左边重新生成新的检测卡尺区域
                    CVRRect cVRRect = new CVRRect(new Point2f(temP.X, temP.Y),
                       new Size2f(rotatedRectFs[0].size.Width, rotatedRectFs[0].size.Height), 0);
                    Mat roMat2 = new Mat();
                    Cv2.CvtColor(roMat, roMat2, ColorConversionCodes.BGR2GRAY);
                    ///
                    //roMat.DrawRotatedRect(cVRRect);
                    //visionShowControl2.clearAll();
                    //visionShowControl2.dispImage(roMat);
                    ///
                    CVRect rect = cVRRect.BoundingRect();
                    //区域超界限判断
                    if (rect.X < 0 || rect.Y < 0 || (rect.X + rect.Width > roMat2.Width) || (rect.Y + rect.Height > roMat2.Height))
                        continue;

                    Mat tem = MatExtension.Crop_Mask_Mat(roMat2, rect);

                    List<Point2f> point2Fs = new List<Point2f>();
                    LinearCaliperTool.FindEdges(tem, linearCaliperData.edgeThreshold, ref point2Fs,
                        EumProjection_direction.vertical, linearCaliperData.edgePolarity);
                    if (point2Fs.Count > 0)
                    {
                        Point2f point2f = new CVPoint(point2Fs[0].X + rect.X, point2Fs[0].Y + rect.Y);

                        // roMat.RotateAffineOfSizeNoChange(-roAngle, ref cVPoint);
                        roMat.RotateAffineINV(-roAngle, -tx, -ty, ref point2f);

                        edges.Add(point2f);
                    }
                  
                }
                 
                if (edges.Count >= 2)
                {
                    //亚像素角点
                    /// 角点位置精准化参数
                    //Size winSize = new Size(5, 5);
                    //Size zeroZone = new Size(-1, -1);
                    //迭代的终止条件
                    //TermCriteria criteria = new TermCriteria(
                    //    CriteriaTypes.Eps & CriteriaTypes.MaxIter,
                    //    40, //maxCount=40
                    //    0.001);    //epsilon=0.001

              
              //      Point2f[] SubCorners = Cv2.CornerSubPix(gray, edges, winSize, zeroZone, criteria);
                
                //    edges = SubCorners.ToList<Point2f>();

                    if (linearCaliperData.fitMethod== EumFittingMethod.Least_square_method)
                    {
                        M_LINE line1 = LinearCaliperTool.fitLine(dst, edges);
                        dst.Line(line1.sp, line1.ep, Scalar.LimeGreen);
                        (linearCaliperResult as LinearCaliperResult).sp=line1.sp;
                        (linearCaliperResult as LinearCaliperResult).ep=line1.ep;
                    }
                    else
                    {
                        Vector2[] vector2s = new Vector2[edges.Count];
                        for (int i = 0; i < edges.Count; i++)
                        {
                            vector2s[i].X = edges[i].X;
                            vector2s[i].Y = edges[i].Y;
                        }
                        M_LINE line2 = LinearCaliperTool.FitLineRansac(dst, vector2s);
                        dst.Line(line2.sp, line2.ep, Scalar.Blue);
                        (linearCaliperResult as LinearCaliperResult).sp=line2.sp;
                        (linearCaliperResult as LinearCaliperResult).ep=line2.ep;
                    }
                    linearCaliperResult.runStatus = true;
                }
                else
                    linearCaliperResult.runStatus = false;

                (linearCaliperResult as LinearCaliperResult).edges = edges;
                //绘制边缘点
                foreach (var s in edges)
                    dst.drawCross(s.ToPoint(), Scalar.LimeGreen, 10, 1);
                //绘制卡尺检测区域
                foreach (var s in rotatedRectFs)
                    dst.DrawRotatedRect(new CVRRect(new Point2f(s.centerP.X, s.centerP.Y),
                        new Size2f(s.size.Width, s.size.Height), s.angle));
                linearCaliperResult.resultToShow = dst;
            }
            catch(Exception er)
            {
                Cv2.CvtColor(gray, linearCaliperResult.resultToShow, ColorConversionCodes.GRAY2BGR);
                //检测区域
                //绘制卡尺检测区域
                foreach (var s in rotatedRectFs)
                    linearCaliperResult.resultToShow.DrawRotatedRect(new CVRRect(new Point2f(s.centerP.X, s.centerP.Y),
                        new Size2f(s.size.Width, s.size.Height), s.angle));
                linearCaliperResult.exceptionInfo = er.Message;
                linearCaliperResult.runStatus = false;
            }
            return linearCaliperResult;
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
         static Mat VerticalLine(Mat srcImage)//垂直线条检测 
        {

            List<byte> array=new List<byte> ();
                 
            for (int i = 0; i < srcImage.Cols; i++)
            {
                int sumvalue = 0;
                for (int j = 0; j < srcImage.Rows; j++)
                {
                    sumvalue += srcImage.At<byte>(j, i);//每一列的像素和
                }
                byte Average_strength =(byte)(sumvalue / srcImage.Rows);//每一列的平均强度

                array.Add(Average_strength);

            }
            Mat lineImage = new Mat(1, array.Count, MatType.CV_8UC1,new Scalar(0,0,0));
          
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

        static Mat HorizonLine(Mat srcImage)//水平线条检测
        {
            List<byte> array1=new List<byte> ();
              
            for (int i = 0; i < srcImage.Rows; i++)
            {
                int sumvalue = 0;
                for (int j = 0; j < srcImage.Cols; j++)
                {
                    sumvalue += srcImage.At<byte>(i, j);//每一行的像素和
                }
                byte Average_strength = (byte)(sumvalue / srcImage.Cols);//每一行的平均强度

                array1.Add(Average_strength);
            }
            Mat lineImage = new Mat(srcImage.Rows, 1, MatType.CV_8UC1, new Scalar(0, 0, 0));
            //Mat lineImage(srcImageBin.rows, srcImageBin.cols, CV_8UC1, cv::Scalar(0, 0, 0));
            
            int count = array1.Count();

            //恢复水平线
            for (int h = 0; h < lineImage.Cols; h++)
            {
                for (int m = 0; m < count; m++)
                {
                    lineImage.Set<byte>(m, h, array1[m]);

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
             EumProjection_direction eumProjection_Direction= EumProjection_direction.vertical ,
             EumEdgePolarity edgePolarity= EumEdgePolarity.all)
        {
            CVRect boundary = srcImage.BoundingRect();
          
            if (eumProjection_Direction== EumProjection_direction.vertical)
            {
                float cenY = (boundary.Y + boundary.Height) / 2;             
                Mat projectMat = VerticalLine(srcImage);//平均值灰度投影
                zeroPList = new List<Point2f>();//极值点坐标集合

       //         Mat GaussianMat = new Mat();
       //         Cv2.GaussianBlur(projectMat, GaussianMat,new Size(3,1),0);
                Mat sobelMat = new Mat();
                Cv2.Sobel(projectMat, sobelMat, MatType.CV_64F, 1, 0, 1, 1, 0);

                Mat gxAbs = new Mat();
                Cv2.ConvertScaleAbs(sobelMat, gxAbs);
                //梯度变化
                //Mat gx = new Mat();
                //Cv2.Sobel(projectMat, gx, MatType.CV_16S, 1, 0);
                //Mat gxAbs = new Mat();
                //Cv2.ConvertScaleAbs(sobelMat, gxAbs);
                //    gxAbs.ConvertTo(gxAbs, MatType.CV_8UC1);

                int wid = sobelMat.Width;
             //   Console.WriteLine("卡尺：");
                for (int i=0;i< wid;i++)
                {
                    //目标边缘梯度
                    byte _sdx = gxAbs.At<byte>(0, i);

                    double value = sobelMat.At<double>(0, i);

               //     Console.WriteLine(string.Format("{0},{1}", _sdx, value));
                    if (_sdx >= edgeThreshold)
                    {     
                        if((edgePolarity== EumEdgePolarity.all)||
                            (edgePolarity == EumEdgePolarity.positive && value > 0)||
                            (edgePolarity == EumEdgePolarity.negtive && value < 0))
                        {
                            zeroPList.Add(new Point2f(i, cenY));
                            break;
                        }
                                           
                    }

                }

               // Mat thdMat = new Mat();             
               // Cv2.Threshold(projectMat,thdMat, edgeThreshold,255,ThresholdTypes.Binary);
               // Mat CannyMat=on_Canny(thdMat, edgeThreshold, 255);//二阶求导零点
                //Mat mat = CannyMat.FindNonZero();
                //CannyMat.GetArray<byte>(out byte[] bytearray);
          
                //for (int i = 0; i < CannyMat.Cols; i++)
                //{
                //    if (CannyMat.At<byte>(0, i) == 255
                //        && i > 5 && i < CannyMat.Cols - 5)//去掉首位干扰点
                //        //zeroPList.Add(new Point2f(i, cenY));
                //    ;
                   
                //}
                return gxAbs;
            }
           else
            {
                float cenX = (boundary.X + boundary.Width) / 2;
                Mat projectMat = HorizonLine(srcImage);//平均值灰度投影
                //projectMat.GetArray<byte>(out byte[] values);
                Mat thdMat = new Mat();
                Cv2.Threshold(projectMat, thdMat, edgeThreshold, 255, ThresholdTypes.Binary);
                //thdMat.GetArray<byte>(out byte[] values);
                Mat CannyMat = on_Canny(thdMat, edgeThreshold, 255);//二阶求导零点
                //Mat mat = CannyMat.FindNonZero();
                zeroPList = new List<Point2f>();//极值点坐标集合
                for (int i = 0; i < CannyMat.Rows; i++)
                {
                    //byte ss = CannyMat.Get<byte>(0, i);
                    if (CannyMat.At<byte>(i, 0) == 255)
                        zeroPList.Add(new Point2f(cenX, i));
                }
                return CannyMat;
            }
        }

        /// <summary>
        /// 最小二乘法直线拟合
        /// </summary>
        /// <param name="srcImage"></param>
        /// <param name="fitPList"></param>
        /// <returns></returns>
        static public M_LINE fitLine( Mat srcImage,   List<Point2f> fitPList)
        {
            if (fitPList.Count < 2) return default;
            //Cv2.FitLine(fitPList, DistanceTypes.L2, 0, 0.01, 0.01);
            var line = Cv2.FitLine(fitPList, DistanceTypes.L1, 0, 0, 0);       
            line.FitSize(srcImage.Width, srcImage.Height, out var p1, out var p2);
            return new M_LINE { sp = p1, ep = p2 };
        }

        /// <summary>
        /// 用RANSAC算法把异常点筛选出来
        /// </summary>
        /// <param name="points"></param>
        /// <param name="vpdExceptPoints"></param>
        /// <param name="sigma"></param>
        static public void RansacLineFiler(Point2f[] points, out List<Point2f> vpdExceptPoints, double sigma)
        {
            int n = points.Length;
            vpdExceptPoints = new List<Point2f>();

            if (n < 2)
            {
                return;
            }
            Random random = new Random();         
            double bestScore = -1;
            List<Point2f> vpdTemp = new List<Point2f>();
            int iterations = (int)(Math.Log(1 - 0.99) / (Math.Log(1 - (1.00 / n))) * 10);

            for (int k = 0; k < iterations; k++)
            {
                int i1 = 0, i2 = 0;
                while (i1 == i2)
                {
                    i1 = random.Next(n);
                    i2 = random.Next(n);
                }
                Point2f p1 = points[i1];
                Point2f p2 = points[i2];
                Point2f vectorP21 = p2 - p1;//向量
                //如果x是一个向量，那么norm(x)就等于x的模长。2范数
                vectorP21 *= 1.0 / Math.Sqrt(Math.Pow(vectorP21.X, 2) + Math.Pow(vectorP21.Y, 2));
                //vectorP21 *= 1.0 / norm(vectorP21);

                double score = 0;
                vpdTemp.Clear();
                for (int i = 0; i < n; i++)
                {
                    Point2f vectorPi1 = points[i] - p1;
                    double d = vectorPi1.X * vectorP21.X - vectorPi1.X * vectorP21.Y;//calculate the cos�� of the two vectors.
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


        static public void RansacLineFiler(List<Point2f> points, out List<Point2f> vpdExceptPoints,
            int iterations=1000, double sigma=1.0)

        {
            int n = points.Count;
            vpdExceptPoints = new List<Point2f>();

            if (n < 2)
            {
                return;
            }

            //RNG random = new RNG();
            Random random1 = new Random();
            double bestScore = -1;
            List<Point2f> vpdTemp = new List<Point2f>();
            //int iterations = (int)(Math.Log(1 - 0.99) / (Math.Log(1 - (1.00 / n))) * 10);

            for (int k = 0; k < iterations; k++)
            {
                int i1 = 0, i2 = 0;
                while (i1 == i2)
                {
                    i1 = random1.Next(n);
                    i2 = random1.Next(n);
                    //i1 = (int)random.Run((uint)n);
                    //i2 = (int)random.Run((uint)n);
                    

                }
                Point2f p1 = points[i1];
                Point2f p2 = points[i2];
                Point2f vectorP21 = p2 - p1;//向量
              
                //如果x是一个向量，那么norm(x)就等于x的模长。2范数
                //vectorP21 *= 1.0 / Math.Sqrt(Math.Pow(vectorP21.X, 2) + Math.Pow(vectorP21.Y, 2));
                vectorP21 *= 1.0 / (Math.Abs(vectorP21.X) + Math.Abs(vectorP21.Y));
                //vectorP21 *= 1.0 / norm(vectorP21);

                double score = 0;
                vpdTemp.Clear();
                for (int i = 0; i < n; i++)
                {
                    Point2f vectorPi1 = points[i] - p1;
                    double d = vectorPi1.Y * vectorP21.X - vectorPi1.X * vectorP21.Y;//calculate the cos�� of the two vectors.
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
        /// 随机抽样一致性法
        /// </summary>
        /// <param name="points"></param>
        /// <param name="iterations"></param>
        /// <param name="sigma"></param>
        /// <returns></returns>
        public static M_LINE FitLineRansac( Mat srcImage, Vector2[] fitPList, 
                                                    int iterations=1000, double sigma=1.0)
        {
            int bestScore = -1;
            Line2D result = null;
            Random random = new Random();
            for (int i = 0; i < iterations; i++)
            {
                if (bestScore > fitPList.Length * 0.5)
                    break;

                var indexes = GetRandomIndexes(random, 0, fitPList.Length);
                var p1 = fitPList[indexes.Item0];
                var p2 = fitPList[indexes.Item1];

                var dir = Vector2.Normalize(p2 - p1);
                var line = new Line2D(dir.X, dir.Y, p2.X, p2.Y);
                int score = 0;
                for (int j = 0; j < fitPList.Length; j++)
                {
                    if (line.Distance(fitPList[j].X, fitPList[j].Y) < sigma)
                        score += 1;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    result = line;
                }
            }

            result.FitSize(srcImage.Width, srcImage.Height, out var pa, out var pb);
            return new M_LINE { sp = pa, ep = pb };
        }

        private static Vec2i GetRandomIndexes(Random random, int min, int max)
        {
            var index1 = random.Next(min, max);
            var index2 = random.Next(min, max);
            if (index1 == index2)
                GetRandomIndexes(random, min, max);
            return new Vec2i(index1, index2);
        }

        /// <summary>
        /// 根据向量的夹角公式计算线段与X轴的夹角(反余弦)
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        double GetAngleVecWithX(Point2d p1, Point2d p2)
        {
            if (p1 == p2)
            {
                return -1;
            }
            Point2d vector = p2 - p1;
            if (vector.X == 0)
            {
                if (vector.Y > 0)
                {
                    return 90;
                }
                else
                {
                    return -90;
                }
            }
            double cosValue = Math.Pow(vector.X, 2) / (vector.X * Math.Sqrt(Math.Pow(vector.X, 2) + Math.Pow(vector.Y, 2)));
            double angle = Math.Acos(cosValue) * 180 / Math.PI;
            if (p1.Y > p2.Y)
            {
                angle = -angle;
            }

            return angle;
        }

        
    }
    /// <summary>
    /// 投影方向
    /// </summary>
    public enum EumProjection_direction
    {
        /// <summary>
        /// 垂直
        /// </summary>
        vertical,
        /// <summary>
        /// 水平
        /// </summary>
        Horizon
    }

    /// <summary>
    /// 直线
    /// </summary>
    public struct M_LINE
    {
        /// <summary>
        /// 起点
        /// </summary>
        public CVPoint sp;
        /// <summary>
        /// 终点
        /// </summary>
        public CVPoint ep;
    
    }
    /// <summary>
    ///  直线拟合结果
    /// </summary>
    public class LinearCaliperResult : Result
    {
        /// <summary>
        /// 起点
        /// </summary>
        public CVPoint sp;
        /// <summary>
        /// 终点
        /// </summary>
        public CVPoint ep;
        /// <summary>
        /// 边缘点集合
        /// </summary>
        public List<Point2f> edges = new List<Point2f>();
        /// <summary>
        /// 卡尺测量区域
        /// </summary>
        public List<RotatedCaliperRectF> rotatedRectFs = new List<RotatedCaliperRectF>();
    }
}
