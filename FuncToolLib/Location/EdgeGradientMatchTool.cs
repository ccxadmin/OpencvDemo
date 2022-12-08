using Numpy.Models;
using OpenCvSharp;
using ParamDataLib.Location;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CVSize = OpenCvSharp.Size;

using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using Rect = System.Drawing.RectangleF;

using System.Numerics;


namespace FuncToolLib.Location
{
    /// <summary>
    ///  基于边缘梯度模板匹配
    /// </summary>
    public class EdgeGradientMatchTool
    {
        struct ptin
        {
            public int DerivativeX;
            public int DerivativeY;
            public double Magnitude;
            public double MagnitudeN;
        }
        static float minScore = 0.8f;
        static float greediness = 0.8f;
        //static long contoursLength = 0;
        static double magnitudeTemp = 0;

        static Dictionary<int,long> contoursLengthDic = new Dictionary<int,long>();

        static Dictionary<int,  Point[][]> temContoursDic=new Dictionary<int, Point[][]> ();
    
        // 提取dx\dy\mag\log信息	
        static List<ptin[]> contoursInfo = new List<ptin[]>();
        static Dictionary<int, List<ptin[]>> contoursInfoDic = new Dictionary<int, List<ptin[]>>();
       // 提取相对坐标位置
       static List<Point[]> contoursRelative = new List<Point[]>();
        static Dictionary<int, List<Point[]>> contoursRelativeDic = new Dictionary<int, List<Point[]>>();

       
        static Dictionary<int, Mat> newgrayDic = new Dictionary<int, Mat>();
        static Mat mask = new Mat();//掩膜
        static RotatedRect rotatedRect = new RotatedRect();

        /// <summary>
        /// 创建模板
        /// </summary>
        /// <param name="temMat"></param>
        /// <param name="roi"></param>
        /// <param name="numLevels"></param>
        /// <returns></returns>
        static public Mat CreateTemplate(Mat temMat, CVRect roi, int numLevels = 3)
        {
            int tick = Environment.TickCount;


            newgrayDic.Clear();
            temContoursDic.Clear();
            contoursInfoDic.Clear();
            contoursRelativeDic.Clear();
            contoursLengthDic.Clear();

            Mat gray = new Mat();
            Cv2.CopyTo(temMat, gray);
            Mat dst = new Mat();

            //对模板图像和待检测图像分别进行图像金字塔下采样
            for (int i = 0; i < numLevels; i++)
            {
                Cv2.PyrDown(gray, gray, new Size(gray.Cols / 2, gray.Rows / 2));
            }
            Cv2.CvtColor(gray, dst, ColorConversionCodes.GRAY2BGR);


            //二值化
            Mat bw = new Mat();
            Cv2.Threshold(gray, bw, 110, 255, ThresholdTypes.Binary);
            //开运算
            InputArray kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5));
            Mat MorphologyMat = new Mat();
            Cv2.MorphologyEx(src: bw, dst: MorphologyMat, op: MorphTypes.Open, element: kernel);
            //canny边缘
            Mat edge = new Mat();
            Cv2.Canny(MorphologyMat, edge, 125, 350);

            //模板图进行旋转，然后计算梯度
            for (int i = 0; i < 180 / 10; i++)
            {
                int angle = -90 + 10 * i;
                Mat  newedge = ImageRotate(edge, angle, ref rotatedRect, ref mask);
                Mat newgray = ImageRotate(gray, angle);
                //梯度变化
                Mat gx = new Mat(), gy = new Mat();
                Cv2.Sobel(newgray, gx, MatType.CV_32F, 1, 0);
                Cv2.Sobel(newgray, gy, MatType.CV_32F, 0, 1);

                Mat magnitude = new Mat(), direction = new Mat();
                //笛卡尔坐标转换，计算二维向量的大小和角度。
                Cv2.CartToPolar(gx, gy, magnitude, direction);

                newgrayDic.Add(angle, newgray);

                Point[][] temContours;
                //找轮廓
                Cv2.FindContours(newedge, out temContours, out _, RetrievalModes.Tree,
                                         ContourApproximationModes.ApproxNone);

             
                temContoursDic.Add(angle, temContours);

                // 轮廓起点

                int originx = temContours[0][0].X;
                int originy = temContours[0][0].Y;
                //Console.WriteLine(string.Format("模板起始点 x:{0},y:{1}", originx * Math.Pow(2, numLevels) + roi.X,
                //                    originy * Math.Pow(2, numLevels) + roi.Y));


                contoursInfo.Clear();
                contoursRelative.Clear();
                int contoursLength = 0;
                //开始提取
                for (int j = 0; j < temContours.Length; j++)
                {
                    int n = temContours[j].Length;
                    contoursLength += n;
                    contoursInfo.Add(new ptin[n]);//记录每一个轮廓的信息
                    Point[] points = new Point[n];
                    for (int k = 0; k < n; k++)
                    {
                        int x = temContours[j][k].X;
                        int y = temContours[j][k].Y;
                        points[k].X = x - originx;
                        points[k].Y = y - originy;
                        ptin pointInfo = new ptin();
                        pointInfo.DerivativeX = (int)gx.At<float>(y, x);
                        pointInfo.DerivativeY = (int)gy.At<float>(y, x);
                        magnitudeTemp = magnitude.At<float>(y, x);
                        pointInfo.Magnitude = magnitudeTemp;
                        if (magnitudeTemp != 0)
                            pointInfo.MagnitudeN = 1 / magnitudeTemp;
                        contoursInfo[j][k] = pointInfo;
                    }
                    contoursRelative.Add(points);//记录每一个轮廓的相对点位坐标
                }

                contoursInfoDic.Add(angle, contoursInfo);
                contoursRelativeDic.Add(angle, contoursRelative);
                contoursLengthDic.Add(angle, contoursLength);
            }
            Cv2.CvtColor(newgrayDic[-90], dst, ColorConversionCodes.GRAY2BGR);

            for (int i = 0; i < temContoursDic[-90].Length; i++)
            {
                dst.DrawContours(temContoursDic[-90], i, Scalar.LimeGreen, 1);
            }
         
            Console.WriteLine("模板时间:{0}", Environment.TickCount - tick);
            return dst;
        }

        /// <summary>
        /// 梯度匹配
        /// </summary>
        /// <param name="src_search"></param>
        /// <param name="numLevels"></param>
        /// <returns></returns>
        static public Point MatchGradient(Mat src_search,int numLevels=3)
        {
            // 计算目标图像梯度

            Mat grayImage = new Mat();
            src_search.CopyTo(grayImage);
            
            //对模板图像和待检测图像分别进行图像金字塔下采样
            for (int i = 0; i < numLevels; i++)
            {
                Cv2.PyrDown(grayImage, grayImage, new Size(grayImage.Cols / 2, grayImage.Rows / 2));
            }
      
            Mat gradx = new Mat(), grady = new Mat();
            Cv2.Sobel(grayImage, gradx, MatType.CV_32F, 1, 0);
            Cv2.Sobel(grayImage, grady, MatType.CV_32F, 0, 1);
            Mat mag = new Mat(), angle = new Mat();
            Cv2.CartToPolar(gradx, grady, mag, angle);

            long totalLength = 0;
            double nMinScore = 0;
            double nGreediness = 0;


            double partialScore = 0;
            double resultAngle = 0;
            double resultScore = 0;
            int resultX = 0;//结果x
            int resultY = 0;//结果y
                        
            long c_count = 0;
            for (int row = 0; row < grayImage.Rows; row++)//从原图的起始点开始轮询
            {        
                //if (row > searchRect.Height - templateRect.Height)
                //    continue;
                for (int col = 0; col < grayImage.Cols; col++)//从原图的起始点开始轮询
                {
                    //if (col > searchRect.Width - templateRect.Width)
                    //    continue;
                    //目标边缘梯度
                    double _sdx = gradx.At<float>(row, col);
                 
                    double _sdy = grady.At<float>(row, col);

                    if (Math.Abs(_sdx) <10 && Math.Abs(_sdy) <10)
                        continue;

                    c_count++;


                 
                    //模板图进行旋转，然后计算梯度
                    for (int i = 0; i < 180 / 10; i++)
                    {
                        int currangle = -90 + 10 * i;

                        totalLength = contoursLengthDic[currangle];
                        nMinScore = minScore / totalLength;
                        nGreediness = (1 - greediness * minScore) / (1 - greediness) / totalLength;
                        double sum = 0;
                        long num = 0;
                        //所有轮廓(无旋转)
                        for (int m = 0; m < contoursRelativeDic[currangle].Count; m++)
                        {
                            //float sampleFrequency = 0.3f;
                            //int gap=1/ sampleFrequency;
                            //单个轮廓
                            for (int n = 0; n < contoursRelativeDic[currangle][m].Length; n = n + 2)//间隔采样频率0.3
                            {
                                num += 1;
                                /*先查找一个起始点看是否满足，然后根据起始点和相对坐标点的关系
                                 * 开始轮询，逐一比较
                                 */
                                int curX = col + contoursRelativeDic[currangle][m][n].X;
                                int curY = row + contoursRelativeDic[currangle][m][n].Y;
                                //不可超出图像边界
                                if (curX < 0 || curY < 0 || curX > grayImage.Cols - 1 || curY > grayImage.Rows - 1)
                                {
                                    continue;
                                }

                                // 目标边缘梯度
                                double sdx = gradx.At<float>(curY, curX);
                                double sdy = grady.At<float>(curY, curX);

                                // 模板边缘梯度
                                //double tdx = contoursInfo[m][n].DerivativeX;
                                //double tdy = contoursInfo[m][n].DerivativeY;
                                double tdx = contoursInfoDic[currangle][m][n].DerivativeX;
                                double tdy = contoursInfoDic[currangle][m][n].DerivativeY;
                                
                                // 计算匹配
                                if ((sdy != 0 || sdx != 0) && (tdx != 0 || tdy != 0))
                                {
                                    double nMagnitude = mag.At<float>(curY, curX);
                                    if (nMagnitude != 0)
                                        sum += (sdx * tdx + sdy * tdy) * contoursInfoDic[currangle][m][n].MagnitudeN / nMagnitude;
                                }

                                // 任意节点score之和必须大于最小阈值
                                partialScore = sum / num;

                                if (partialScore < Math.Min((minScore - 1) + (nGreediness * num), nMinScore * num))
                                    break;
                            }
                        }

                        // 保存匹配起始点
                        if (partialScore > resultScore)
                        {
                            resultScore = partialScore;
                             resultAngle = currangle;
                            resultX = col;
                            resultY = row;
                        }
                    }
                }
            }
            

            Point point = new Point();
            point.X = (int)(resultX * Math.Pow(2, numLevels));
            point.Y = (int)(resultY * Math.Pow(2, numLevels));
    
            Console.WriteLine(c_count);
            Console.WriteLine(resultScore);
            Console.WriteLine(resultAngle);
            Console.WriteLine(string.Format("匹配结果点 x:{0},y:{1}", point.X ,
               point.Y ));
            return point;
        }

        /// <summary>
        /// 图像旋转，并获旋转后的图像边界旋转矩形
        /// </summary>
        /// <param name="image"></param>
        /// <param name="angle"></param>
        /// <param name="imgBounding"></param>
        /// <returns></returns>
        public static Mat ImageRotate(Mat image, double angle, ref RotatedRect imgBounding, ref Mat maskMat)
        {
            Mat newImg = new Mat();
            Point2f pt = new Point2f((float)image.Cols / 2, (float)image.Rows / 2);
            Mat M = Cv2.GetRotationMatrix2D(pt, -angle, 1.0);
            var mIndex = M.GetGenericIndexer<double>();

            double cos = Math.Abs(mIndex[0, 0]);
            double sin = Math.Abs(mIndex[0, 1]);
            int nW = (int)((image.Height * sin) + (image.Width * cos));
            int nH = (int)((image.Height * cos) + (image.Width * sin));
            mIndex[0, 2] += (nW / 2) - pt.X;
            mIndex[1, 2] += (nH / 2) - pt.Y;

            Cv2.WarpAffine(image, newImg, M, new CVSize(nW, nH));
            //获取图像边界旋转矩形
            CVRect rect = new CVRect(0, 0, image.Width, image.Height);
            Point2f[] srcPoint2Fs = new Point2f[4]
                {
                    new Point2f(rect.Left,rect.Top),
                     new Point2f (rect.Right,rect.Top),
                       new Point2f (rect.Right,rect.Bottom),
                          new Point2f (rect.Left,rect.Bottom)
                };

            Point2f[] boundaryPoints = new Point2f[4];
            var A = M.Get<double>(0, 0);
            var B = M.Get<double>(0, 1);
            var C = M.Get<double>(0, 2);    //Tx
            var D = M.Get<double>(1, 0);
            var E = M.Get<double>(1, 1);
            var F = M.Get<double>(1, 2);    //Ty

            for (int i = 0; i < 4; i++)
            {
                boundaryPoints[i].X = (float)((A * srcPoint2Fs[i].X) + (B * srcPoint2Fs[i].Y) + C);
                boundaryPoints[i].Y = (float)((D * srcPoint2Fs[i].X) + (E * srcPoint2Fs[i].Y) + F);
                if (boundaryPoints[i].X < 0)
                    boundaryPoints[i].X = 0;
                else if (boundaryPoints[i].X > nW)
                    boundaryPoints[i].X = nW;
                if (boundaryPoints[i].Y < 0)
                    boundaryPoints[i].Y = 0;
                else if (boundaryPoints[i].Y > nH)
                    boundaryPoints[i].Y = nH;
            }

            Point2f cenP = new Point2f((boundaryPoints[0].X + boundaryPoints[2].X) / 2,
                                         (boundaryPoints[0].Y + boundaryPoints[2].Y) / 2);
            double ang = angle;

            double width1 = Math.Sqrt(Math.Pow(boundaryPoints[0].X - boundaryPoints[1].X, 2) +
                Math.Pow(boundaryPoints[0].Y - boundaryPoints[1].Y, 2));
            double width2 = Math.Sqrt(Math.Pow(boundaryPoints[0].X - boundaryPoints[3].X, 2) +
                Math.Pow(boundaryPoints[0].Y - boundaryPoints[3].Y, 2));

            //double width = width1 > width2 ? width1 : width2;

            //double height = width1 > width2 ? width2 : width1;

            imgBounding = new RotatedRect(cenP, new Size2f(width1, width2), (float)ang);

            Mat mask = new Mat(newImg.Size(), MatType.CV_8UC3, Scalar.Black);
            mask.DrawRotatedRect(imgBounding, Scalar.White, 1);
            Cv2.FloodFill(mask, new Point(imgBounding.Center.X, imgBounding.Center.Y), Scalar.White);
            //   mask.ConvertTo(mask, MatType.CV_8UC1);
            //mask.CopyTo(maskMat);
            //掩膜复制给maskMat       
            Cv2.CvtColor(mask, maskMat, ColorConversionCodes.BGR2GRAY);

            Mat _maskRoI = new Mat();
            Cv2.CvtColor(mask, _maskRoI, ColorConversionCodes.BGR2GRAY);
            Mat buf = new Mat();

            //# 黑白反转
            Cv2.BitwiseNot(_maskRoI, buf);
            Mat dst = new Mat();
            Cv2.BitwiseAnd(newImg, newImg, dst, _maskRoI);
            //Mat dst2 = new Mat();
            //Cv2.BitwiseOr(buf, dst, dst2);
            return dst;

        }
        /// <summary>
        /// 图像旋转
        /// </summary>
        /// <param name="image"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Mat ImageRotate(Mat image, double angle)
        {
            Mat newImg = new Mat();
            Point2f pt = new Point2f((float)image.Cols / 2, (float)image.Rows / 2);
            Mat M = Cv2.GetRotationMatrix2D(pt, -angle, 1.0);
            var mIndex = M.GetGenericIndexer<double>();

            double cos = Math.Abs(mIndex[0, 0]);
            double sin = Math.Abs(mIndex[0, 1]);
            int nW = (int)((image.Height * sin) + (image.Width * cos));
            int nH = (int)((image.Height * cos) + (image.Width * sin));
            mIndex[0, 2] += (nW / 2) - pt.X;
            mIndex[1, 2] += (nH / 2) - pt.Y;

            Cv2.WarpAffine(image, newImg, M, new CVSize(nW, nH));

            return newImg;

        }

    }
}
