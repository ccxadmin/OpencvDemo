using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuncToolLib.Contour;
using FuncToolLib.Morphology;
using OpenCvSharp;
using ParamDataLib;
using ParamDataLib.Location;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using CVSize = OpenCvSharp.Size;
using Point = System.Windows.Point;

namespace FuncToolLib.Location
{
    /// <summary>
    /// 用来做canny模板匹配
    /// </summary>
   public  class CannyTemplateMatchTool : ToolBase
    {
        /*
         * 
         * 关于参数 method：
         * TM_SQDIFF平方差匹配法：该方法采用平方差来进行匹配；最好的匹配值为0；匹配越差，匹配值越大。
         * TM_CCORR相关匹配法：该方法采用乘法操作；数值越大表明匹配程度越好。
         * TM_CCOEFF相关系数匹配法：1表示完美的匹配；-1表示最差的匹配。
         * TM_SQDIFF_NORMED归一化平方差匹配法
         * TM_CCORR_NORMED归一化相关匹配法
         * TM_CCOEFF_NORMED归一化相关系数匹配法
         *        
         */

        public override Result Run<T>(Mat inputImg, T obj)
        {
            CannyTemplateMatchData parmasData = obj as CannyTemplateMatchData;
            CannyTemplateMatchResult result = new CannyTemplateMatchResult();
            if (parmasData == null)
                parmasData = new CannyTemplateMatchData();
            Mat dst = new Mat();
            Cv2.CvtColor(inputImg, dst, ColorConversionCodes.GRAY2BGR);

            if (parmasData.tpl.Empty() || parmasData.tpl.Width <= 0)
            {
                result.resultToShow = dst;
                result.exceptionInfo += "模板无效或不存在，请先创建模板！";
                result.runStatus = false;
                return result;
            }

            if (parmasData.MatchSearchMethod == EumMatchSearchMethod.AllRegion)
                result = TempalteMatchCanny(inputImg, parmasData.tpl, parmasData.StartAngle, parmasData.AngleRange,
                       parmasData.SegmentThresh, parmasData.NumLevels, parmasData.MatchScore);
            else
            {
                CVRect cVRect = new CVRect((int)parmasData.matchSearchRegion.X,
              (int)parmasData.matchSearchRegion.Y,
               (int)parmasData.matchSearchRegion.Width,
                (int)parmasData.matchSearchRegion.Height);
                //局部区域裁切
                Mat newMat = MatExtension.Crop_Mask_Mat(inputImg, cVRect);

                result = TempalteMatchCanny(newMat, parmasData.tpl, parmasData.StartAngle, parmasData.AngleRange,
                 parmasData.SegmentThresh, parmasData.NumLevels, parmasData.MatchScore);
            }

            if (result.Score > 0)
            {
                if (parmasData.MatchSearchMethod == EumMatchSearchMethod.AllRegion)
                {
                    dst.DrawRotatedRect(result.rotatedRect, Scalar.LimeGreen, 2);

                    dst.drawCross(new CVPoint(result.rotatedRect.Center.X, result.rotatedRect.Center.Y)
                                          , Scalar.Red, 20, 2);
                }
                else
                {
                    float startX = parmasData.matchSearchRegion.X;
                    float startY = parmasData.matchSearchRegion.Y;
                    result.X += startX;
                    result.Y += startY;
                    result.rotatedRect = new RotatedRect(new Point2f(result.rotatedRect.Center.X + startX,
                             result.rotatedRect.Center.Y + startY),
                                result.rotatedRect.Size, result.rotatedRect.Angle);
                    dst.drawCross(new CVPoint(result.rotatedRect.Center.X, result.rotatedRect.Center.Y)
                                       , Scalar.Red, 20, 2);
                }

                result.resultToShow = dst;
                result.runStatus = true;
                return result;
            }
            //匹配失败
            else
            {
                result.resultToShow = dst;
                result.exceptionInfo += "模板匹配失败！";
                result.runStatus = false;
                return result;
            }

        }


        /// <summary>
        /// 创建Canny模板图像
        /// </summary>
        /// <param name="src"></param>
        /// <param name="RegionaRect"></param>
        /// <param name="thresh"></param>
        /// <param name="temCannyMat"></param>
        /// <returns></returns>
        public Mat CreateTemplateCanny(Mat src,Rect RegionaRect,double thresh,ref Mat temCannyMat,
            ref double modelX, ref double modelY)
        {
            Mat ModelMat = MatExtension.Crop_Mask_Mat(src, RegionaRect);
            Mat Morphological = Morphological_Proces.MorphologyEx(ModelMat, MorphShapes.Rect,
                       new OpenCvSharp.Size(3, 3), MorphTypes.Open);
            Mat binMat = new Mat();
            Cv2.Threshold(Morphological, binMat, thresh, 255, ThresholdTypes.Binary);

            temCannyMat = EdgeTool.Canny(binMat, 50, 240);

            Mat dst = ModelMat.CvtColor(ColorConversionCodes.GRAY2BGR);
            CVPoint cenP = new CVPoint(RegionaRect.Width / 2,
             RegionaRect.Height / 2);
            modelX = RegionaRect.X + RegionaRect.Width / 2;
            modelY = RegionaRect.Y + RegionaRect.Height / 2;
            Console.WriteLine(string.Format("模板中心点位:x:{0},y:{1}", RegionaRect.X + RegionaRect.Width / 2,
                  RegionaRect.Y + RegionaRect.Height / 2));
            dst.drawCross(cenP, Scalar.Red, 20, 2);
            return dst;
        }


        /// <summary>
        /// 多角度模板匹配方法(NCC:归一化相关系数)
        /// </summary>
        /// <param name="srcImage">待匹配图像</param>
        /// <param name="modelImage">模板图像</param>
        /// <param name="angleStart">起始角度</param>
        /// <param name="angleRange">角度范围</param>
        /// <param name="thresh">分割阈值</param>
        /// <param name="numLevels">金字塔层级</param>
        /// <param name="thresScore">得分阈值</param>  
        /// <returns></returns>
        private CannyTemplateMatchResult TempalteMatchCanny(Mat srcImage, Mat modelImage,
                      double angleStart, double angleRange, double thresh,
                           int numLevels, double thresScore)
        {
            if (srcImage.Empty() || modelImage.Empty() ||
                 srcImage.Width < modelImage.Width || srcImage.Height < modelImage.Height)
                return new CannyTemplateMatchResult();

            double step = angleRange / ((angleRange / 1) / 10); //角度间隔
            double start = angleStart;                                      //起始角度
            double range = angleRange;                                        //角度范围

            //定义图片匹配所需要的参数
            //int resultCols = srcImage.Cols - modelImage.Cols + 1;
            //int resultRows = srcImage.Rows - modelImage.Cols + 1;
            //Mat result = new Mat(resultCols, resultRows, MatType.CV_8U);
            Mat result = new Mat();
            Mat src = new Mat();
            Mat model = new Mat();
            srcImage.CopyTo(src);
            modelImage.CopyTo(model);

            //Canny
            Mat Morphological = Morphological_Proces.MorphologyEx(src, MorphShapes.Rect,
             new OpenCvSharp.Size(3, 3), MorphTypes.Open);
            Mat binMat = new Mat();
            Cv2.Threshold(Morphological, binMat, thresh, 255, ThresholdTypes.Binary);
            src = EdgeTool.Canny(binMat, 50, 240);

            //对模板图像和待检测图像分别进行图像金字塔下采样
            for (int i = 1; i <=numLevels; i++)
            {
                Cv2.PyrDown(src, src, new Size(src.Cols / 2, src.Rows / 2));
                Cv2.PyrDown(model, model, new Size(model.Cols / 2, model.Rows / 2));
            }


            //在没有旋转的情况下进行第一次匹配
            Cv2.MatchTemplate(src, model, result,  TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out CVPoint minLoc, out CVPoint maxLoc, new Mat());

            CVPoint location = maxLoc;
            double temp = maxVal;
            double angle = 0;

            Mat newtemplate;
            Mat mask = new Mat();
            RotatedRect rotatedRect = new RotatedRect();
            RotatedRect modelrrect = new RotatedRect(new Point2f(model.Width / 2,
                              model.Height / 2), new Size2f(model.Width, model.Height), 0);


            //以最佳匹配点左右十倍角度步长进行循环匹配，直到角度步长小于参数角度步长


           //double step = Math.Min(Math.Pow(10, numLevels),10);   //金字塔最顶层角度间隔
            //do
            //{
                for (int i = 0; i <= (int)range / step; i++)
                {
                    //newtemplate = ImageRotate(start + step * i,model);
                    newtemplate = ImageRotate(model, start + step * i, ref rotatedRect, ref mask);
                if (newtemplate.Width > src.Width || newtemplate.Height > src.Height)
                    continue;
                Cv2.MatchTemplate(src, newtemplate, result, TemplateMatchModes.CCoeffNormed);
                    Cv2.MinMaxLoc(result, out double minval, out double maxval,
                                                out CVPoint minloc, out CVPoint maxloc, new Mat());

                    if (maxval > temp)
                    {
                        location = maxloc;
                        temp = maxval;
                        angle = start + step * i;
                        modelrrect = rotatedRect;
                    }
                }
            //    range = step * 2;
            //    start = angle - step;
            //    step = step / 10;
            //} while (step > 1);

            #region
            //开始上采样
            Rect cropRegion = new CVRect(0, 0, 0, 0);
            //for (int j = numLevels - 1; j >= 0; j--)
            {
                //为了提升速度，直接上采样到最底层
                for (int i = 0; i < numLevels; i++)
                {
                    Cv2.PyrUp(src, src, new Size(src.Cols * 2,
                                src.Rows * 2));//下一层，放大2倍
                    Cv2.PyrUp(model, model, new Size(model.Cols * 2,
                         model.Rows * 2));//下一层，放大2倍
                }

                location.X *= (int)Math.Pow(2, numLevels);
                location.Y *= (int)Math.Pow(2, numLevels);
                modelrrect = new RotatedRect(new Point2f((float)(modelrrect.Center.X * Math.Pow(2, numLevels)),//下一层，放大2倍
                                (float)(modelrrect.Center.Y * Math.Pow(2, numLevels))),
                                new Size2f(modelrrect.Size.Width * Math.Pow(2, numLevels),
                                modelrrect.Size.Height * Math.Pow(2, numLevels)), 0);

                CVPoint cenP = new CVPoint(location.X + modelrrect.Center.X,
                               location.Y + modelrrect.Center.Y);//投影到下一层的匹配点位中心

                int startX = cenP.X - model.Width;
                int startY = cenP.Y - model.Height;
                int endX = cenP.X + model.Width;
                int endY = cenP.Y + model.Height;
                cropRegion = new CVRect(startX, startY, endX - startX, endY - startY);
                cropRegion = cropRegion.Intersect(new CVRect(0, 0, src.Width, src.Height));
                Mat newSrc = MatExtension.Crop_Mask_Mat(src, cropRegion);
                //每下一层金字塔，角度间隔减少2倍
                step = 1;
                //角度开始和范围           
                range = 20;
                start = angle - 10;
                bool testFlag = false;
                for (int k = 0; k <= (int)range / step; k++)
                {

                    newtemplate = ImageRotate(model, start + step * k, ref rotatedRect, ref mask);
                    if (newtemplate.Width > newSrc.Width || newtemplate.Height > newSrc.Height)
                        continue;
                    Cv2.MatchTemplate(newSrc, newtemplate, result, TemplateMatchModes.CCoeffNormed);
                    Cv2.MinMaxLoc(result, out double minval, out double maxval,
                                                out CVPoint minloc, out CVPoint maxloc, new Mat());

                    if (maxval > temp)
                    {
                        //局部坐标
                        location.X = maxloc.X;
                        location.Y = maxloc.Y;
                        temp = maxval;
                        angle = start + step * k;
                        //局部坐标
                        modelrrect = rotatedRect;
                        testFlag = true;
                    }
                }
                if (testFlag)
                {
                    //局部坐标--》整体坐标
                    location.X += cropRegion.X;
                    location.Y += cropRegion.Y;
                }

            }
            #endregion

            Console.WriteLine(string.Format("x:{0},y:{1},angle:{2},score:{3}",
           (location.X + modelrrect.Center.X)*Math.Pow(2, 0),
           (location.Y + modelrrect.Center.Y) * Math.Pow(2, 0),
            angle,
            temp
            ));

            if (temp >thresScore)
            {
                return new CannyTemplateMatchResult(modelrrect.Center.X + location.X, modelrrect.Center.Y + location.Y, angle, temp)
                {
                    rotatedRect = new RotatedRect(new Point2f((float)((modelrrect.Center.X  + location.X ) * Math.Pow(2, 0)),
                  (float)((modelrrect.Center.Y  + location.Y ) * Math.Pow(2, 0))),
                  new Size2f(modelrrect.Size.Width * Math.Pow(2, 0),
                       modelrrect.Size.Height * Math.Pow(2, 0)),
                  (float)angle)
                };
            }

            return new CannyTemplateMatchResult();
        }

        /*----------------------------*/    
      
        /// <summary>
        /// 图像旋转，并获旋转后的图像边界旋转矩形
        /// </summary>
        /// <param name="image"></param>
        /// <param name="angle"></param>
        /// <param name="imgBounding"></param>
        /// <returns></returns>
       static Mat ImageRotate(Mat image, double angle,ref RotatedRect imgBounding,ref Mat maskMat)
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
            Rect rect = new CVRect(0, 0, image.Width, image.Height);
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

            for(int i=0;i<4;i++)
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

            double width1=Math.Sqrt(Math.Pow(boundaryPoints[0].X- boundaryPoints[1].X ,2)+
                Math.Pow(boundaryPoints[0].Y - boundaryPoints[1].Y,2));
            double width2 = Math.Sqrt(Math.Pow(boundaryPoints[0].X - boundaryPoints[3].X, 2) +
                Math.Pow(boundaryPoints[0].Y - boundaryPoints[3].Y, 2));

            //double width = width1 > width2 ? width1 : width2;

            //double height = width1 > width2 ? width2 : width1;

            imgBounding = new RotatedRect(cenP, new Size2f(width1, width2), (float)ang);

            Mat mask = new Mat(newImg.Size(), MatType.CV_8UC3, Scalar.Black);
            mask.DrawRotatedRect(imgBounding, Scalar.White, 1);
            Cv2.FloodFill(mask, new CVPoint(imgBounding.Center.X, imgBounding.Center.Y), Scalar.White);
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

    }

    public  class CannyTemplateMatchResult : Result
    {
        public CannyTemplateMatchResult()
        {
            X = 0;
            Y = 0;
            T = 0;
            Score = 0;
             rotatedRect = new RotatedRect();
        }
      
        public CannyTemplateMatchResult(double x, double y, double t, double score)
        {
            X = x;
            Y = y;
            T = t;
            Score = score;
        }
        public double X;
        public double Y;
        public double T;
        public double Score;
        public RotatedRect rotatedRect;
    }
}
