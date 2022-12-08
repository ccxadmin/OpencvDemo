
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FuncToolLib;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using Rect = System.Drawing.RectangleF;
using Point = System.Drawing.PointF;
using VisionShowLib;
using FilesRAW.Common;
using ParamDataLib;
using OpenCvSharp.XImgProc;
using System.IO;
using DeviceLib.Cam;
using FuncToolLib.Calibration;
using FuncToolLib.Contour;
using FuncToolLib.Location;
using ParamDataLib.Location;
using FuncToolLib.Filter;
using FuncToolLib.Morphology;
using FuncToolLib.Enhancement;
using System.Numerics;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            visionShowControl1.RoiChangedHandle += new EventHandler(RoiChangedEvent);
            visionShowControl1.DoubleClickGetMousePosHandle2 += new OutPointGray(OutPointGrayEvent);

            visionShowControl1.LoadedImageNoticeHandle += new EventHandler(LoadedImageNoticeEvent);


        }
        Icam icam = null;
        public void RoiChangedEvent(object sender, EventArgs e)
        {
            if (sender is RectangleF)
            {
                RegionaRect = new CVRect((int)((RectangleF)sender).X, (int)((RectangleF)sender).Y,
                    (int)((RectangleF)sender).Width, (int)((RectangleF)sender).Height);

            }
            
            else if(sender is RotatedRectF)
            {
                RegionRRect = new CVRRect(
                    new Point2f(((RotatedRectF)sender).centerP.X, ((RotatedRectF)sender).centerP.Y),
                               new Size2f(((RotatedRectF)sender).size.Width, ((RotatedRectF)sender).size.Height),
                             ((RotatedRectF)sender).angle);
              
   
            }
            else if (sender is RotatedCaliperRectF)
            {
                RegionRRect2 = (RotatedCaliperRectF)sender;
                rotatedRectFs = visionShowControl1.GetCalipersRegions(20, 8, EumCalipersType.linear);

            }
            else if(sender is CircleF)
            {
                RegionCircle = new CVCircle(new Point2f(((CircleF)sender).Centerx, ((CircleF)sender).Centery), ((CircleF)sender).Radius);
                rotatedRectFs2 = visionShowControl1.GetCalipersRegions(20, 6, EumCalipersType.circular);

            }
            else if (sender is SectorF)
            {
                 sectorF = (SectorF)sender;
            
                rotatedRectFs2= visionShowControl1.GetCalipersRegions(20, 6, EumCalipersType.sector);

            }
        }
        void   LoadedImageNoticeEvent(object sender,EventArgs e)
        {
            src = MatExtension.BitmapToGrayMat(visionShowControl1.Image);
        }
        void OutPointGrayEvent(int x,int y)
        {
            MessageBox.Show(string.Format("x:{0},y{1}",x,y)); 
        }
        // 保存ROI
        private CVRect RegionaRect = new CVRect(400, 600, 200, 200);

        private CVRRect RegionRRect = new CVRRect(new Point2f(100,100),new Size2f(100,100),0);
        private RotatedCaliperRectF RegionRRect2 = new RotatedCaliperRectF(100, 100, 100, 100, 0);
        private CVCircle RegionCircle = new CVCircle(new Point2f(100, 100), 100);

        private List<RotatedCaliperRectF> rotatedRectFs = new List<RotatedCaliperRectF>();//直线卡尺

        private List<RotatedCaliperRectF> rotatedRectFs2 = new List<RotatedCaliperRectF>();//圆弧卡尺

        SectorF sectorF;
        Mat src; Mat img2;
        private void button2_Click(object sender, EventArgs e)
        {
            Point p = new Point();
             src = MatExtension.BitmapToGrayMat(visionShowControl1.Image);
            if (src.Empty()) return;

          
            //获取检测掩膜图像
              Mat temimg = MatExtension.Reducue_Mask_Mat(src, RegionCircle);


          //  Mat ratateImage=   src.RotateAffine(30);
            //Cv2.Threshold(temimg, binary,60, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);


            //快速直线
            //FastLineDetector fastLineDetector = FastLineDetector.Create(lengthThreshold: 10, distanceThreshold: 100);
            //var lines = fastLineDetector.Detect(temimg);
            //foreach(var s in lines)
            //{
            //    Line2D line1 = new Line2D(lines[0].Item0, lines[0].Item1, lines[0].Item2, lines[0].Item3);
            //    line1.FitSize(Dst.Width, Dst.Height, out p1, out p2);
            //    Cv2.Line(Dst, p1, p2, Scalar.Green, 1);
            //}

            //var roi = MatExtension.GetRegional(src.Width, src.Height, _Regional);
            //img2 = new Mat(src, roi);
            //Mat binary = new Mat();
            //Cv2.Threshold(temimg, binary, 150, 255, ThresholdTypes.Binary);

            visionShowControl1.dispImage(src);
            //visionShowControl2.dispImage(ratateImage);
            //visionShowControl3.dispImage(temimg);

            switch (comboBox1.Text)
            {
                case "创建模板":
                    createModel();

                    break;
                case "模板匹配":
                    TemplateMatch();

                    break;

                case "金字塔下采样":
                    PyrDown();

                    break;

                case "创建轮廓模板":
                    createTemplate();

                    break;
                    
                case "轮廓匹配":
                    testShapeMatch();
                 
                    break;
                case "圆拟合":
                    fitCircle();
                    break;
                        
                case "霍夫直线":
                    HoughLine();
                    break;
                case "直线卡尺":
                    line_caliper();
                    break;
                case "圆卡尺":
                    circle_caliper();
                    break;
                case "图像旋转":
                    RotatedRect rotatedRect=new CVRRect ();
                    Mat mask = new Mat();
                     Mat dst= NccTemplateMatchTool.ImageRotate(modelGrayMat, 30, ref rotatedRect, ref mask);
                    //Mat result = new Mat();

                    //Cv2.MatchTemplate(src, dst, result, TemplateMatchModes.CCoeffNormed);
                    //Cv2.MinMaxLoc(result, out double minval, out double maxval,
                    //     out CVPoint minloc, out CVPoint maxloc, new Mat());
                    //Console.WriteLine(string.Format("得分:{0},点位:{1}", maxval, maxloc));

                    visionShowControl3.clearAll();
                    visionShowControl3.dispImage(dst);
                    visionShowControl4.clearAll();
                    visionShowControl4.dispImage(mask);

                    break;
                case "Blob":
                    Blob();
                    break;
                case "Blob2":
                    Blob2();
                    break;
                case "Blob3":
                    blob3();
                    break;

            }

        }

        /************测试*****************/
        Mat ModelMat;
        Mat temCannyMat=new Mat ();
        Mat modelGrayMat = new Mat();
        CVPoint[][] temContours;
        NccTemplateMatchTool t2 = new NccTemplateMatchTool();
        CannyTemplateMatchTool t3 = new CannyTemplateMatchTool();
        void createModel()
        {
            double modelx = 0, modely = 0;
            ModelMat= t2.CreateTemplateNcc(src, RegionaRect,ref modelx,ref modely,ref modelGrayMat);
            //visionShowControl4.clearAll();
            //visionShowControl4.dispImage(temCannyMat);
         //   Mat dst = ModelMat.CvtColor( ColorConversionCodes.GRAY2BGR);
            CVPoint cenP = new CVPoint(RegionaRect.Width / 2,
             RegionaRect.Height / 2);
            Console.WriteLine(string.Format("模板中心点位:x:{0},y:{1}", RegionaRect.X + RegionaRect.Width / 2,
                  RegionaRect.Y + RegionaRect.Height / 2));
            ModelMat.drawCross(cenP,Scalar.Red,20,2);
            visionShowControl2.clearAll();
            visionShowControl2.dispImage(ModelMat);
        }
        
        void TemplateMatch()
        {

            int tick = Environment.TickCount;
            NccTemplateMatchData data = new NccTemplateMatchData();
            data.MatchScore = 0.4;
            data.NumLevels = 2;      
            data.StartAngle = -90;
            data.AngleRange = 180;
            data.tpl = modelGrayMat;
            NccTemplateMatchResult result = t2.Run<NccTemplateMatchData>(src, data) as NccTemplateMatchResult;
            Console.WriteLine("时间:{0}",Environment.TickCount-tick);
            Mat dst = new Mat();
            Cv2.CvtColor(src, dst, ColorConversionCodes.GRAY2BGR);
            dst.DrawRotatedRect(result.rotatedRect, Scalar.LimeGreen, 2);

            dst.drawCross(new CVPoint(result.rotatedRect.Center.X, result.rotatedRect.Center.Y)
                              , Scalar.Red, 20, 2);


            visionShowControl3.clearAll();
            visionShowControl3.dispImage(dst);
        }

    
        void PyrDown()
        {
            Mat dst = new Mat();
            Cv2.CopyTo(modelGrayMat, dst);
            for (int i = 0; i < 2; i++)
            {
                Cv2.PyrDown(dst, dst, new OpenCvSharp.Size(dst.Cols / 2, dst.Rows / 2));

            }
            visionShowControl3.clearAll();
            visionShowControl3.dispImage(dst);
        }


        IRunTool runTool = null;
        ParmasDataBase parmaData = null;
        int matchindex = 0;
        float x = 0; float y = 0; double a = 0;
        float x2 = 0; float y2 = 0; double a2 = 0;
        Result stuResultOfToolRun;
             CVPoint[] templateContour = null;
        double modelx = 0, modely = 0, modelangle = 0;
        //CVRect templateRect;
        void createTemplate()
        {
            Mat cropMat = MatExtension.Crop_Mask_Mat(src, RegionaRect);
          
            runTool = new ShapeMatchTool();
            CVRect boundingRect;

           
            double coutourLen=0;
            double contourArea=0;

      
            Mat tp=(runTool as ShapeMatchTool).CreateTemplateContours(cropMat,120, RegionaRect,
                ref templateContour,ref coutourLen,ref contourArea,ref modelx,ref modely,ref modelangle);
          
            visionShowControl2.clearAll();
            visionShowControl2.dispImage(tp);
        }
        void  testShapeMatch()
        {
            int tick = Environment.TickCount;
            runTool = new ShapeMatchTool();
            parmaData = new ShapeMatchData();
            (parmaData as ShapeMatchData).baseAngle = 0;
            (parmaData as ShapeMatchData).MatchValue = 0.5;
            (parmaData as ShapeMatchData).Segthreshold = 120;
            (parmaData as ShapeMatchData).tpContour = templateContour;
            (parmaData as ShapeMatchData).MincoutourLen = 10;
            (parmaData as ShapeMatchData).MaxcoutourLen = 99999999;
            (parmaData as ShapeMatchData).MinContourArea = 10;
            (parmaData as ShapeMatchData).MaxContourArea = 999999999;

            Result result= runTool.Run<ShapeMatchData>(src, parmaData as ShapeMatchData);

           
            Console.WriteLine("时间:{0}", Environment.TickCount - tick);
          
            visionShowControl3.clearAll();
            visionShowControl3.dispImage(result.resultToShow);

        }

      
        void fitCircle()
        {
            runTool = new FitCircleTool();
            parmaData = new FitCircleData();
            (parmaData as FitCircleData).maxRadius = 500;
            (parmaData as FitCircleData).minRadius = 5;
            (parmaData as FitCircleData).EdgeThreshold =180;
  
            //parmaData.ROI = RegionSectorF;

            (parmaData as FitCircleData).sectorF = sectorF;

            Result stuResultOfToolRun = runTool.Run<FitCircleData>(src,
                           parmaData as FitCircleData);
            visionShowControl2.clearAll();
            visionShowControl2.dispImage(stuResultOfToolRun.resultToShow);



            //获取检测掩膜图像
            Mat temimg = MatExtension.Crop_Mask_Mat(src, (CVPoint[])(parmaData as FitCircleData).ROI, out CVRect BoundingRect);



            /*-------------*/
            Mat medianImage = new Mat();
            //中值滤波器平滑处理
            Cv2.MedianBlur(temimg, medianImage, 3);
            //二值化
            Mat binaryImage = new Mat();
            Cv2.Threshold(medianImage, binaryImage, 180, 255, ThresholdTypes.Binary);

            //提取轮廓
            Cv2.FindContours(binaryImage, out CVPoint[][] contours, out HierarchyIndex[] h, RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            Mat dst2 = new Mat();

            Cv2.CvtColor(temimg, dst2, ColorConversionCodes.GRAY2BGR);
            int num = contours.ToArray<CVPoint[]>().Length;
            for (int i = 0; i < num; i++)
                dst2.DrawContours(contours, i, Scalar.Red);
            visionShowControl3.dispImage(dst2);


            int left = BoundingRect.X;
            int top = BoundingRect.Y;

            CVPoint[] points = (CVPoint[])(parmaData as FitCircleData).ROI;

            List<CVPoint> points1 = new List<CVPoint>();
            foreach (var s in points)
            {
                points1.Add(new CVPoint(s.X - left, s.Y - top));
            }

            CVPoint[][] dstcontours = MatExtension.ExceptBoundOfSectorF(contours, sectorF, BoundingRect);


            Mat dst = new Mat();

            Cv2.CvtColor(temimg, dst, ColorConversionCodes.GRAY2BGR);

            int count = dstcontours.ToArray<CVPoint[]>().Length;
            for (int i = 0; i < count; i++)
                dst.DrawContours(dstcontours, i, Scalar.Red);

            visionShowControl4.dispImage(dst);


        }
     
    
        void HoughLine()
        {
            runTool = new HoughLinesPTool();
            parmaData = new HoughLinesPData();
            (parmaData as HoughLinesPData).canThddown = 150;
            (parmaData as HoughLinesPData).canThdup = 240;
            (parmaData as HoughLinesPData).ThresholdP = 20;
            (parmaData as HoughLinesPData).MinLineLenght = 10;
            (parmaData as HoughLinesPData).MaxLineGap = 20;
            parmaData.ROI = RegionRRect;
            Result stuResultOfToolRun = runTool.Run<HoughLinesPData>(src,
                          parmaData as HoughLinesPData);
            visionShowControl2.clearAll();
            visionShowControl2.dispImage(stuResultOfToolRun.resultToShow);

            CVRect BoundingRect = new CVRect();
            //获取检测掩膜图像
            Mat temimg = MatExtension.Crop_Mask_Mat(src, (CVRRect)(parmaData as HoughLinesPData).ROI, out BoundingRect);
           
            /*-------------*/
            Mat CannyImg = EdgeTool.Canny(temimg, 150, 240);
            LineSegmentPoint[] lineSegmentPoints= Cv2.HoughLinesP(CannyImg, 1, Cv2.PI / 180, 20, 10,
                          20);


            visionShowControl3.clearAll();
            visionShowControl3.dispImage(CannyImg);

            {
                //Point2f[] CVRRect_vertex = RegionRRect.Points();

                //CVRect cVRect = temimg.BoundingRect();
                //float offsetX = RegionRRect.Center.X - (cVRect.X + cVRect.Width / 2);
                //float offsetY = RegionRRect.Center.Y - (cVRect.Y + cVRect.Height / 2);
                //for (int i = 0; i < CVRRect_vertex.Length; i++)
                //{
                //    CVRRect_vertex[i].X -= offsetX;
                //    CVRRect_vertex[i].Y -= offsetY;
                //}

                //LineSegmentPoint[] boundarySeg = new LineSegmentPoint[4] {
                //new LineSegmentPoint( new CVPoint( CVRRect_vertex[0].X,CVRRect_vertex[0].Y),
                //          new CVPoint( CVRRect_vertex[1].X,CVRRect_vertex[1].Y)),
                // new LineSegmentPoint( new CVPoint( CVRRect_vertex[1].X,CVRRect_vertex[1].Y),
                //          new CVPoint( CVRRect_vertex[2].X,CVRRect_vertex[2].Y)),
                //  new LineSegmentPoint( new CVPoint( CVRRect_vertex[2].X,CVRRect_vertex[2].Y),
                //          new CVPoint( CVRRect_vertex[3].X,CVRRect_vertex[3].Y)),
                //   new LineSegmentPoint( new CVPoint( CVRRect_vertex[3].X,CVRRect_vertex[3].Y),
                //          new CVPoint( CVRRect_vertex[0].X,CVRRect_vertex[0].Y))
                //};

                //List<LineSegmentPoint> temList = lineSegmentPoints.ToList<LineSegmentPoint>();

                //for (int i = 0; i < lineSegmentPoints.Length; i++)
                //{
                //    for (int j = 0; j < 4; j++)
                //    {
                //        //如果有相交
                //        if (MatExtension.CoverSegments(lineSegmentPoints[i], boundarySeg[j]))
                //        {
                //            temList.Remove(lineSegmentPoints[i]); //移除与边界相交的线段
                //            break;
                //        }
                //    }
                //}
                //剔除边界重合点
                LineSegmentPoint[]  lineSegmentPoints1 = MatExtension.ExceptBoundOfRRect(lineSegmentPoints,
                   (CVRRect)(parmaData as HoughLinesPData).ROI,
                   BoundingRect);



                Mat dst = new Mat();
                Cv2.CvtColor(temimg,dst, ColorConversionCodes.GRAY2BGR);
                for (int i = 0; i < lineSegmentPoints1.Length; i++)
                    dst.Line(lineSegmentPoints1[i].P1, lineSegmentPoints1[i].P2, Scalar.White);
 

            visionShowControl4.clearAll();
            visionShowControl4.dispImage(dst);
            }
        }

    
        void line_caliper()
        {
            if (rotatedRectFs.Count <= 0) return;
            float roAngle = rotatedRectFs[0].angle;

            List<Point2f> point2Fs1 = new List<Point2f>();
            foreach (var s in rotatedRectFs)
                point2Fs1.Add(new Point2f (s.centerP.X,s.centerP.Y));

         Mat ErodeMat=   Morphological_Proces.ErodeImage(src, MorphShapes.Rect,new OpenCvSharp.Size (15,15));

         Mat DilateMat=   Morphological_Proces.DilateImage(ErodeMat, MorphShapes.Rect, new OpenCvSharp.Size(7, 7));

            Mat dst = new Mat();
            Cv2.CvtColor(DilateMat, dst, ColorConversionCodes.GRAY2BGR);
            Mat roMat=  dst.RotateAffine(roAngle);
          
            Mat roMat2 = new Mat();
            Cv2.CvtColor(roMat, roMat2, ColorConversionCodes.BGR2GRAY);
            List<Point2f> edges = new List<Point2f>();
            for (int i = 0; i < point2Fs1.Count; i++)
            {
                Point2f temP = point2Fs1[i];
                float tx = 0, ty = 0;
                dst.RotateAffine2(roAngle, ref temP, ref tx, ref ty);
                ///根据新中心点位左边重新生成新的检测卡尺区域
                CVRRect cVRRect = new CVRRect(new Point2f(temP.X, temP.Y),
                   new Size2f(rotatedRectFs[0].size.Width, rotatedRectFs[0].size.Height), 0);

                ///
                roMat.DrawRotatedRect(cVRRect);
                visionShowControl2.clearAll();
                visionShowControl2.dispImage(roMat);
                ///
                CVRect rect = cVRRect.BoundingRect();
               
                Mat tem = MatExtension.Crop_Mask_Mat(roMat2, rect);
                List<Point2f> point2Fs = new List<Point2f>();
       //        Mat solbeMat=    
                LinearCaliperTool.FindEdges(tem, 60, ref point2Fs);

                if (point2Fs.Count > 0)
                {
                    Mat temDst = new Mat();
                    Cv2.CvtColor(tem, temDst, ColorConversionCodes.GRAY2BGR);
                    temDst.drawCross(new CVPoint(point2Fs[0].X, point2Fs[0].Y), Scalar.LimeGreen, 20, 1);
                    visionShowControl3.clearAll();
                    visionShowControl3.dispImage(temDst);

                    CVPoint cVPoint = new CVPoint(point2Fs[0].X + rect.X, point2Fs[0].Y + rect.Y);

                    roMat.RotateAffineINV(-roAngle, -tx, -ty,ref cVPoint);
                
                    edges.Add(cVPoint);
                }
            }
            if(edges.Count>=2)
            {
                M_LINE line1 = LinearCaliperTool.fitLine(dst, edges);
                Vector2[] vector2s = new Vector2[edges.Count];
                for (int i = 0; i < edges.Count; i++)
                {
                    vector2s[i].X = edges[i].X;
                    vector2s[i].Y = edges[i].Y;
                }
           //     M_LINE line2 = LinearCaliperTool.FitLineRansac(dst, vector2s);

                //Cv2.FitLine( , DistanceTypes.L2,0,0.01,0.01);
            
                foreach (var s in edges)
                    dst.drawCross(s.ToPoint(), Scalar.LimeGreen, 10, 1);

                dst.Line(line1.sp, line1.ep, Scalar.LimeGreen);
             //   dst.Line(line2.sp, line2.ep, Scalar.Blue);
            }
            foreach (var s in rotatedRectFs)
                dst.DrawRotatedRect(new CVRRect(new Point2f(s.centerP.X, s.centerP.Y),
                    new Size2f(s.size.Width, s.size.Height), s.angle));
            visionShowControl4.clearAll();
            visionShowControl4.dispImage(dst);

        }
        void circle_caliper()
        {
            if (rotatedRectFs2.Count <= 0) return;
         
            List<Point2f> point2Fs1 = new List<Point2f>();
            foreach (var s in rotatedRectFs2)
                point2Fs1.Add(new Point2f(s.centerP.X, s.centerP.Y));//添加矩形中心点位

            Mat dst = new Mat();          
            Cv2.CvtColor(src, dst, ColorConversionCodes.GRAY2BGR);
                      
            List<Point2f> edges = new List<Point2f>();
            List<Point2f> vpdExceptPoints = new List<Point2f>();
   
            for (int i = 0; i < rotatedRectFs2.Count; i++)
            {
                float roAngle = rotatedRectFs2[i].angle;
                Point2f temP = point2Fs1[i];
                float tx = 0, ty = 0;
                Mat roMat = dst.RotateAffine(roAngle, ref temP, ref tx, ref ty);
                CVRRect cVRRect = new CVRRect(new Point2f(temP.X, temP.Y),
                    new Size2f(rotatedRectFs2[0].size.Width, rotatedRectFs2[0].size.Height), 0);
                Mat roMat2 = new Mat();
                Cv2.CvtColor(roMat, roMat2, ColorConversionCodes.BGR2GRAY);
                CVRect rect = cVRRect.BoundingRect();
                Mat tem = MatExtension.Crop_Mask_Mat(roMat2, rect);

              //  Mat roMat = cropRRect.RotateAffineOfSizeNoChange(roAngle, ref temP);
               // CVRRect cVRRect = new CVRRect(new Point2f(temP.X, temP.Y),
                //    new Size2f(rotatedRectFs2[0].size.Width, rotatedRectFs2[0].size.Height), 0);             
                //roMat.DrawRotatedRect(cVRRect);
            //    visionShowControl2.clearAll();
            //    visionShowControl2.dispImage(roMat);
                //visionShowControl4.clearAll();
                //visionShowControl4.dispImage(cropRRect);

                if (i==5)
                {
                    visionShowControl3.clearAll();
                    visionShowControl3.dispImage(roMat);
                    List<Point2f> point2Fs = new List<Point2f>();
                   CircularCaliperTool.FindEdges(tem, 40, ref point2Fs);
                    visionShowControl2.clearAll();
                    visionShowControl2.dispImage(tem);
                    if (point2Fs.Count > 0)
                    {


                        CVPoint cVPoint = new CVPoint(point2Fs[0].X + rect.X, point2Fs[0].Y + rect.Y);


                        roMat.RotateAffineINV(-roAngle, -tx, -ty, ref cVPoint);

                  
                        dst.drawCross(cVPoint,Scalar.LimeGreen,20,2);

                        //visionShowControl4.clearAll();
                        //visionShowControl4.dispImage(dst2);

                     
                        visionShowControl4.clearAll();
                        visionShowControl4.dispImage(dst);
                    }


                }
                /////////
                //Mat roMat2 = new Mat();
                //Cv2.CvtColor(roMat, roMat2, ColorConversionCodes.BGR2GRAY);
                //CVRect rect = cVRRect.BoundingRect();

                //Mat tem = MatExtension.Crop_Mask_Mat(roMat2, rect);
                //List<Point2f> point2Fs = new List<Point2f>();
                //CircularCaliperTool.FindEdges(roMat, 160, ref point2Fs);
                //if (point2Fs.Count > 0)
                //{
                   

                //    CVPoint cVPoint = new CVPoint(point2Fs[0].X , point2Fs[0].Y );

                //    roMat.RotateAffineINV(-roAngle, -tx, -ty, ref cVPoint);

                //    edges.Add(cVPoint);
                //}
            }
            //if (edges.Count >= 3)
            //{
            //    M_CIRCLE circle1 = CircularCaliperTool.fitCircle( edges);
               
            //    M_CIRCLE circle2 = CircularCaliperTool.RansacCircleFiler(edges, out vpdExceptPoints);      

            //    foreach (var s in edges)
            //        dst.drawCross(s.ToPoint(), Scalar.LimeGreen, 10, 1);

            //    dst.Circle(circle1.centreP.ToPoint(), (int)circle1.radius, Scalar.LimeGreen);
            //    dst.Circle(circle2.centreP.ToPoint(), (int)circle2.radius, Scalar.Blue);
            //}
            //foreach (var s in rotatedRectFs2)
            //    dst.DrawRotatedRect(new CVRRect(new Point2f(s.centerP.X, s.centerP.Y),
            //        new Size2f(s.size.Width, s.size.Height), s.angle));
            //visionShowControl4.clearAll();
            //visionShowControl4.dispImage(dst);

        }
        void Blob()
        {
            runTool = new BlobTool();
            parmaData = new BlobData();
            (parmaData as BlobData).MinThreshold = 80;
            (parmaData as BlobData).MaxThreshold = 150;
            (parmaData as BlobData).stuBlobFilter = new StuBlobFilter() {
                isFilterByArea = true, areaHigh = 99999, areaLow = 30
                       
            };
           
            parmaData.ROI = RegionaRect;
            Result stuResultOfToolRun = runTool.Run<BlobData>(src,
                          parmaData as BlobData);
            visionShowControl2.clearAll();
            visionShowControl2.dispImage(stuResultOfToolRun.resultToShow);
        }
        void Blob2()
        {
            runTool = new Blob2Tool();
            parmaData = new Blob2Data();
            (parmaData as Blob2Data).minthd = 80;
            (parmaData as Blob2Data).maxthd = 150;
            (parmaData as Blob2Data).stuBlobFilter = new StuBlobFilter2() { 
             areaLow=50, areaHigh=99999, widthLow=10, widthHigh=999, heightLow=10, heightHigh=999
            };
           
            parmaData.ROI = RegionaRect;
            Result stuResultOfToolRun = runTool.Run<Blob2Data>(src,
                          parmaData as Blob2Data);
            visionShowControl2.clearAll();
            visionShowControl2.dispImage(stuResultOfToolRun.resultToShow);

            Mat binary = new Mat();
            Cv2.Threshold(src, binary, 80, 150, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            visionShowControl3.clearAll();
            visionShowControl3.dispImage(binary);


        }


        void blob3()
        {
           
            
            runTool = new Blob3Tool();
            parmaData = new Blob3Data();
            (parmaData as Blob3Data).edgeThreshold = 110;
            (parmaData as Blob3Data).minArea = 1000;
            (parmaData as Blob3Data).maxArea =9999999;
            (parmaData as Blob3Data).eumWhiteOrBlack = EumWhiteOrBlack.Black;


            //Point2f[] pts = RegionRRect.Points();
            //CVRect temCVRect = Cv2.BoundingRect(pts);

            parmaData.ROI = RegionRRect;
            Result stuResultOfToolRun = runTool.Run<Blob3Data>(src, parmaData as Blob3Data);

            visionShowControl3.clearAll();
            visionShowControl3.dispImage(stuResultOfToolRun.resultToShow);


            Mat proMat = new Mat();
            src.CopyTo(proMat);

            ///////////////////////////////////////
              Mat binary2 = new Mat();
            Mat temimg = MatExtension.Crop_Mask_Mat(proMat, RegionRRect,
                out CVRect BoundingRect);
            Cv2.Threshold(temimg, binary2, 110, 255, ThresholdTypes.BinaryInv); //二值化   
            visionShowControl2.clearAll();
            visionShowControl2.dispImage(binary2);

            ////////////////////////////
            Mat cropMat = MatExtension.Crop_Mask_Mat(proMat, RegionRRect,
                out CVRect BoundingRect2, EumWhiteOrBlack.Black);

            Mat binary = new Mat();

            Cv2.Threshold(cropMat, binary, 110, 255, ThresholdTypes.BinaryInv); //二值化

            visionShowControl4.clearAll();
            visionShowControl4.dispImage(binary);

        }


        void matchTest(Mat src)
        {
            Mat thdMat = new Mat();
            Cv2.Threshold(src, thdMat,140,255,ThresholdTypes.Binary);
            Mat cannyMat = new Mat();
            Cv2.Canny(thdMat,cannyMat,100,255);

        }
        /*******************************/

        private void button1_Click(object sender, EventArgs e)
        {
            RegionEx regionEx = new RegionEx(new RectangleF(100, 100, 100, 100), Color.Green, 1);
            visionShowControl1.DrawRegion(regionEx);
            visionShowControl1.AddRegionBuffer(regionEx);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            TextEx textEx = new TextEx("hello world");
            visionShowControl1.DrawText(textEx);
            visionShowControl1.AddTextBuffer(textEx);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            icam = new CCD_BaslerGIGE("cam1");
            //注册相机图像采集事件
            icam.setImgGetHandle += new ImgGetHandle(getImageDelegate);
            string msg = string.Empty;
            bool initFlag = icam.OpenCam(0, ref msg);
            if (!initFlag)
                MessageBox.Show(msg);
            button4.Enabled = false;
            button8.Enabled = true;
        }
        //图像获取委托事件
        void getImageDelegate(Bitmap img)
        {
            this.Invoke(new Action(() =>
            {
               // Mat src = CommonTool.BitmapToGrayMat(img);
                visionShowControl1.dispImage(img);
              
            }));
        }

        private void button5_Click(object sender, EventArgs e)
        {
          
            icam.ContinueGrab();
            button5.Enabled = false;
            button6.Enabled = true;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            icam.StopGrab();
            button5.Enabled = true;
            button6.Enabled = false;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            icam.OneShot();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            icam.CloseCam();
            button4.Enabled = true;
            button8.Enabled = false;
        }

        private void button10_Click(object sender, EventArgs e)
        {
           Mat Hom_mat2d =  MatExtension.getMat(new Point2f(x,y),new Point2f(x2,y2), a2-a );
            Point2f robot_coordinate;
            var A = Hom_mat2d.Get<double>(0, 0);
            var B = Hom_mat2d.Get<double>(0, 1);
            var C = Hom_mat2d.Get<double>(0, 2);    //Tx
            var D = Hom_mat2d.Get<double>(1, 0);
            var E = Hom_mat2d.Get<double>(1, 1);
            var F = Hom_mat2d.Get<double>(1, 2);    //Ty

            robot_coordinate.X = (float)((A * RegionRRect.Center.X) + (B * RegionRRect.Center.Y) + C);
            robot_coordinate.Y = (float)((D * RegionRRect.Center.X) + (E * RegionRRect.Center.Y) + F);

            CVRRect rotatedRect = new CVRRect(new Point2f(   robot_coordinate.X, robot_coordinate.Y),
                RegionRRect.Size,(float)(a2 - a));

            stuResultOfToolRun.resultToShow.DrawRotatedRect(rotatedRect,Scalar.Blue);

            visionShowControl3.clearAll();
            visionShowControl3.dispImage(stuResultOfToolRun.resultToShow);

        }
        Mat Hom_mat2d;
        /// <summary>
        /// 9点标定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button11_Click(object sender, EventArgs e)
        {
            //List<Point2f> pixelList = new List<Point2f>();
            //List<Point2f> robotList = new List<Point2f>();
            //for (int i = 0; i < 9; i++)
            //{
            //    pixelList.Add(new Point2f(
            //                 float.Parse(listView1.Items[i].SubItems[0].Text),
            //               float.Parse(listView1.Items[i].SubItems[1].Text)
            //                 ));
            //}
            //for (int i = 0; i < 9; i++)
            //{
            //    robotList.Add(new Point2f(
            //                 float.Parse(listView1.Items[i].SubItems[2].Text),
            //               float.Parse(listView1.Items[i].SubItems[3].Text)
            //                 ));
            //}

            //Hom_mat2d = CalibrationTool.VectorToHomMat2d(pixelList, robotList);
            //double[] Coefficient=CalibrationTool.GetMatrixCoefficient(Hom_mat2d);
            //label1.Text = Coefficient[0].ToString("f3");
            //label2.Text = Coefficient[1].ToString("f3");
            //label3.Text = Coefficient[2].ToString("f3");
            //label4.Text = Coefficient[3].ToString("f3");
            //label5.Text = Coefficient[4].ToString("f3");
            //label6.Text = Coefficient[5].ToString("f3");
            //double[] rms=  CalibrationTool.calRMS(Hom_mat2d, pixelList, robotList);

            //label7.Text = rms[0].ToString("f3");
            //label8.Text = rms[1].ToString("f3");



        }

        void readdat(string filepath)
        {
            //FileStream fs = new FileStream(filepath, FileMode.Open);  
            //StreamReader s1 = new StreamReader(fs, Encoding.UTF8);

            //string sepatator = ",";  //以逗号分割字符串
            //char[] cgap = sepatator.ToCharArray();
            //listView1.Items.Clear();
            //string[][] data = new string[9][];
            //for (int i = 0; i <9; i++)
            //{
            //    string str1 = s1.ReadLine();   //读取一行字符
            //    if (str1 == null) break;
            //    string[] str2 = str1.Split(cgap, StringSplitOptions.None); //基于数组中的字符将字符串拆分为多个子字符串
            //    ListViewItem b = new ListViewItem(new string[] { str2[0], str2[1], str2[2],str2[3] });
            //    listView1.Items.Add(b);
            //}

        }
        void writeDat(string filepath)
        {
            //string s = "";
            //for (int m = 0; m < listView1.Items.Count; m++)  //所有项的集合数
            //{
            //    for (int n = 0; n < listView1.Items[m].SubItems.Count; n++)   // 一个项的所有子项集合数 
            //    {
            //        s += listView1.Items[m].SubItems[n].Text + "    ";
            //    }
            //    s += "\r\n";
            //}
            //using (StreamWriter sw = new StreamWriter(filepath))
            //{
            //    sw.WriteLine(s);   //在文件中添加文本
            //}
        }

        private void button12_Click(object sender, EventArgs e)
        {
            readdat("dat.txt");
        }

        private void button13_Click(object sender, EventArgs e)
        {
            Point2d point2F= CalibrationTool.AffineTransPoint2d(Hom_mat2d,new Point2d(0,0));
        }

        private void btnMorphologyTest_Click(object sender, EventArgs e)
        {
            
            string funcName = comboBox3.Text;
            Mat temMat = new Mat();
            OpenCvSharp.Size kernel = new OpenCvSharp.Size((int)numericUpDown1.Value, (int)numericUpDown2.Value);
            switch (funcName)
            {
                case "膨胀":
                    temMat = Morphological_Proces.DilateImage(src, MorphShapes.Rect, kernel);

                    break;
                case "腐蚀":
                    temMat = Morphological_Proces.ErodeImage(src, MorphShapes.Rect, kernel);
                    break;
                case "开运算":
                    temMat = Morphological_Proces.MorphologyEx(src, MorphShapes.Rect,
                        kernel, MorphTypes.Open);
                    break;
                case "闭运算":
                    temMat = Morphological_Proces.MorphologyEx(src, MorphShapes.Rect,
                          kernel, MorphTypes.Close);
                    break;
                case "形态学梯度":
                    temMat = Morphological_Proces.MorphologyEx(src, MorphShapes.Rect,
                         kernel, MorphTypes.Gradient);
                    break;
                case "顶帽":
                    temMat = Morphological_Proces.MorphologyEx(src, MorphShapes.Rect,
                       kernel, MorphTypes.TopHat);
                    break;
                case "黑帽":
                    temMat = Morphological_Proces.MorphologyEx(src, MorphShapes.Rect,
                        kernel, MorphTypes.BlackHat);
                    break;

            }
            visionShowControl3.clearAll();
            visionShowControl3.dispImage(temMat);

        }

        private void btnFilterTest_Click(object sender, EventArgs e)
        {
            string funcName = comboBox2.Text;
            Mat temMat = new Mat();
            int mediauvalue = ((int)numericUpDown1.Value) % 2 == 0 ? (int)numericUpDown1.Value + 1 :
                     (int)numericUpDown1.Value;
            OpenCvSharp.Size kernel = new OpenCvSharp.Size((int)numericUpDown1.Value, (int)numericUpDown2.Value);
            switch (funcName)
            {
                case "均值滤波":
                    temMat =   ImageFilter.MeanImage(src, kernel);

                    break;
                case "中值滤波":
                    temMat= ImageFilter.MedianImage(src, mediauvalue);
                    break;
                case "高斯滤波":
                    temMat = ImageFilter.GaussImage(src, kernel,10);
                    break;
                case "双边滤波":
                    temMat = ImageFilter.BilateralImage(src, 10, 10,10);
                    break;

            }
            visionShowControl2.clearAll();
            visionShowControl2.dispImage(temMat);
        }

        //直方图
        private void button14_Click(object sender, EventArgs e)
        {
            Mat temMat =  ImageEmphize.Histogram_equalization(src);
            visionShowControl4.clearAll();
            visionShowControl4.dispImage(temMat);
        }
        //拉普拉斯
        private void button15_Click(object sender, EventArgs e)
        {
            Mat kernel=new Mat(3, 3, MatType.CV_32FC1,
                new float[] { 0, -1, 0, 0, 3, 0, 0, -1, 0 });
            Mat temMat = ImageEmphize.Laplace_operator(src, kernel);
            visionShowControl4.clearAll();
            visionShowControl4.dispImage(temMat);
        }
        //伽马
        private void button16_Click(object sender, EventArgs e)
        {
          
            Mat temMat = ImageEmphize.Gamma(src, 3);
            visionShowControl4.clearAll();
            visionShowControl4.dispImage(temMat);
        }
        //图像增强
        private void button17_Click(object sender, EventArgs e)
        {
            Mat temMat = ImageEmphize.Emphasize(src, 15,15,1);
            visionShowControl4.clearAll();
            visionShowControl4.dispImage(temMat);
        }
        //像素缩放
        private void button18_Click(object sender, EventArgs e)
        {
            //  Mat temMat = ImageEmphize.scale_image(src, 1.5f, 10);
            Mat temMat = ImageEmphize.scale_image_range(src, 100, 200);
            visionShowControl4.clearAll();
            visionShowControl4.dispImage(temMat);
        }
    }
  
}
