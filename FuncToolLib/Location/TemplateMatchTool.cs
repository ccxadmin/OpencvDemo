using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// 模板匹配
    /// </summary>
    public class TemplateMatchTool : ToolBase
    {
        /// <summary>
        /// 模板匹配工具运行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputImg"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Result Run<T>(Mat inputImg, T obj)
        {
            if (inputImg == null || inputImg.Width <= 0)
                return new TemplateMatchResult { runStatus = false, exceptionInfo = "输入图像为空" };

            TemplateMatchData templateMatchData = obj as TemplateMatchData;
            Result templateMatchResult = new TemplateMatchResult();
        
            inputImg.CopyTo(templateMatchResult.resultToShow);
            //templateMatchData.mask = null;
            (Point2f loc, double angle, double res) tem = MatchTemplate(templateMatchResult.resultToShow, templateMatchData.tpl,
                templateMatchData.StartAngle, templateMatchData.EndAngle, templateMatchData.mask);
            (templateMatchResult as TemplateMatchResult).positionData.Add(tem.loc);
            (templateMatchResult as TemplateMatchResult).angles.Add(tem.angle);
            (templateMatchResult as TemplateMatchResult).scores.Add(tem.res);
       
            //Rect rect = new CVRect((int)tem.loc.X- templateMatchData.tpl.Width/2,
            //    (int)tem.loc.Y- templateMatchData.tpl.Height/2, templateMatchData.tpl.Width,
            //     templateMatchData.tpl.Height);
            Cv2.CvtColor(templateMatchResult.resultToShow, templateMatchResult.resultToShow, ColorConversionCodes.GRAY2BGR);
            templateMatchResult.resultToShow.DrawRotatedRect(
                new RotatedRect(new Point2f(tem.loc.X, tem.loc.Y), new Size2f(templateMatchData.tpl.Width,
                templateMatchData.tpl.Height), (float)tem.angle));
            templateMatchResult.resultToShow.drawCross(new CVPoint((int)tem.loc.X, (int)tem.loc.Y),
                new Scalar(0, 255, 0), 10, 2);

            return templateMatchResult;
        }
        /// <summary>
        /// 模板匹配
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="tpl">模板</param>
        /// <param name="mask">掩膜</param>
        /// <param name="StartAngle">起始角度</param>
        /// <param name="EndAngle">终止角度</param>
        /// <returns>坐标，角度，得分</returns>
        private static (Point2f loc, double angle, double res) MatchTemplate(Mat img, Mat tpl,  double StartAngle, double EndAngle, Mat mask=null)
        {
            Point2f Loc = new Point2f();
            double angle, res;
            double NormSize = 180 / Math.PI;
            //粗匹配
            (Loc, angle, res) =
                Match(img, tpl,  NormSize, StartAngle, EndAngle, 0.6, mask);

            Console.WriteLine("M1:{0:F3},{1:F3},{2:F3}", Loc, angle, res);

            var rotatedTpl = tpl.RotateAffine( angle);
            var rotatedMask = new Mat();
            if (mask != null)
                rotatedMask = mask.RotateAffine( angle);
            else
                rotatedMask = null;

            Rect rect = new CVRect((int)(Loc.X - rotatedTpl.Width / 2f), (int)(Loc.Y - rotatedTpl.Height / 2f), rotatedTpl.Width, rotatedTpl.Height);
            rect.Inflate(20, 20);
            rect = rect.Intersect(new CVRect(0, 0, img.Width, img.Height));
            var Roi = new Mat(img, rect);
            if (Roi.Width > rotatedTpl.Width && Roi.Height > rotatedTpl.Height)
            {
                double angle2;
                //精匹配
                (Loc, angle2, res) =
                    Match(Roi, rotatedTpl, 3 * 180 / Math.PI, -1.2, 1.2, 0.3, rotatedMask);
                if (res > 0)
                {
                    Loc.X += rect.Left;
                    Loc.Y += rect.Top;
                    Console.WriteLine("M2:{0:F3},{1:F3},{2:F3}", Loc, angle2, res);
                    angle += angle2;
                }
            }

            return (Loc, angle, res);
        }

        private static (Point2f loc, double angle, double res) Match(Mat img, Mat tpl, double Matchize, double StartAngle,
             double EndAngle, double StepAngle, Mat mask=null)
        {
            Mat query = new Mat(), train = new Mat(), trainMask = new Mat();
            double scale = Math.Min(1, Matchize / Math.Min(tpl.Width, tpl.Height));
            Cv2.Resize(img, query, new CVSize(), scale, scale);
            Cv2.Resize(tpl, train, new CVSize(), scale, scale);
            if (mask != null)
                Cv2.Resize(mask, trainMask, new CVSize(), scale, scale);
            else
                trainMask = null;

            Point2f center = new Point2f(train.Width / 2f, train.Height / 2f);
            Mat rotatedTrain = new Mat(), rotatedMask = new Mat();
            double MaxMatch = double.MinValue;
            double BestAngle = 0;
            Point2f MatchLoc = new Point2f();

            for (double angle = StartAngle; angle < EndAngle; angle += StepAngle)
            {
                rotatedTrain = train.RotateAffine( angle);
                if (trainMask != null)
                    rotatedMask = trainMask.RotateAffine(angle);
                else
                    rotatedMask = null;

                if (rotatedTrain.Width > query.Width || rotatedTrain.Height > query.Height)
                    continue;

                using (Mat result = new Mat())
                {
                    Cv2.MatchTemplate(query, rotatedTrain, result, TemplateMatchModes.CCoeffNormed, rotatedMask);
                    Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out CVPoint minLoc, out CVPoint maxLoc);
                    Point2f loc = new Point2f(maxLoc.X + rotatedTrain.Width / 2f, maxLoc.Y + rotatedTrain.Height / 2f);
                    if (maxVal >= MaxMatch)
                    {
                        MaxMatch = maxVal;
                        BestAngle = angle;
                        MatchLoc = loc;

                        //Console.WriteLine("angle:{0:F3},score:{1:F3}", angle, MaxMatch);
                        //Cv2.ImShow("rotated", rotatedTrain);
                    }
                }
            }

            MatchLoc.X /= (float)scale;
            MatchLoc.Y /= (float)scale;

            return (MatchLoc, BestAngle, MaxMatch);
        }

      
    }

    public class TemplateMatchResult : Result
    {
        public TemplateMatchResult()
        {
            positionData = new List<Point2f>();
            angles = new List<double>();
            scores = new List<double>();
        }

        public List<Point2f> positionData;

        public List<double> angles;

        public List<double> scores;
    }
    
}
