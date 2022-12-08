using OpenCvSharp;
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
using System.Numerics;
using ParamDataLib;
using ParamDataLib.Location;

namespace FuncToolLib.Location
{
	/// <summary>
	/// 闭合轮廓，形状匹配
	/// </summary>
	public class ShapeMatchTool: ToolBase
	{
        public override Result Run<T>(Mat inputImg, T obj)
        {
			ShapeMatchData shapeMatchData = obj as ShapeMatchData;
			ShapeMatchResult shapeMatchResult = new ShapeMatchResult();
			if (shapeMatchData == null)
				shapeMatchData = new ShapeMatchData();
			Mat dst = new Mat();
			Cv2.CvtColor(inputImg, dst, ColorConversionCodes.GRAY2BGR);

			if (shapeMatchData.tpContour.Length <= 0)
            {
				shapeMatchResult.resultToShow = dst;
				shapeMatchResult.exceptionInfo += "模板轮廓不存在，请先创建模板轮廓！";
				shapeMatchResult.runStatus = false;
				return shapeMatchResult;
			}
            if(shapeMatchData.MatchSearchMethod== EumMatchSearchMethod.AllRegion)
                ShapeTemplateMatch(inputImg, shapeMatchData.tpContour, shapeMatchData.Segthreshold,
                    shapeMatchData.MatchValue, shapeMatchData.MincoutourLen, shapeMatchData.MaxcoutourLen,
                     shapeMatchData.MinContourArea, shapeMatchData.MaxContourArea, shapeMatchData.baseAngle,
                    ref shapeMatchResult, false);
            else
            {
                CVRect cVRect = new CVRect((int)shapeMatchData.matchSearchRegion.X,
                 (int)shapeMatchData.matchSearchRegion.Y,
                  (int)shapeMatchData.matchSearchRegion.Width,
                   (int)shapeMatchData.matchSearchRegion.Height);
                //局部区域裁剪
                Mat newMat=  MatExtension.Reducue_Mask_Mat(inputImg, cVRect);
                ShapeTemplateMatch(newMat, shapeMatchData.tpContour, shapeMatchData.Segthreshold,
                   shapeMatchData.MatchValue, shapeMatchData.MincoutourLen, shapeMatchData.MaxcoutourLen,
                    shapeMatchData.MinContourArea, shapeMatchData.MaxContourArea, shapeMatchData.baseAngle,
                   ref shapeMatchResult, false);

            }
            if (shapeMatchResult.positions.Count>0)
            {			
				shapeMatchResult.runStatus = true;
				return shapeMatchResult;
			}
			//匹配失败
            else
            {
				shapeMatchResult.exceptionInfo += "模板轮廓匹配失败！";
				shapeMatchResult.runStatus = false;
				return shapeMatchResult;
			}

		}

		/// <summary>
		/// 创建形状轮廓模板
		/// </summary>
		/// <param name="img_template">模板图像</param>
		///  <param name="Segthreshold">分割阈值</param>
		/// <param name="templateContour">模板轮廓</param>
		/// <param name="coutourLen">模板轮廓长度</param>
		/// <param name="contourArea">模板轮廓面积</param>
		///  <param name="modelx">模板轮廓X</param>
		///   <param name="modely">模板轮廓Y</param>
		///    <param name="modelangle">模板轮廓角度</param>
		/// <returns>返回绘制图</returns>
		public Mat CreateTemplateContours(Mat img_template,double Segthreshold, CVRect boundingRect,
			ref CVPoint[] templateContour, ref double coutourLen, ref double contourArea,
			ref double modelx,ref double modely,ref double modelangle)
		{
			//灰度化
			//Mat gray_img_template = new Mat();
			//Cv2.CvtColor(img_template, gray_img_template, ColorConversionCodes.BGR2GRAY);

			//阈值分割
			Mat thresh_img_template = new Mat();
			Cv2.Threshold(img_template, thresh_img_template, Segthreshold, 255, ThresholdTypes.Binary);
            //开运算处理，提出白色噪点
            Mat ellipse = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));   
            Cv2.MorphologyEx(thresh_img_template, thresh_img_template, MorphTypes.Open, ellipse);

			//Mat cannyMat = new Mat();
			//Cv2.Canny(thresh_img_template, cannyMat, Segthreshold, 255);

            //寻找边界
            CVPoint[][] contours_template;
			//Vector<Vector<CVPoint>> contours_template=new Vector<Vector<CVPoint>>();
			//Vector<Vec4i> hierarchy=new Vector<Vec4i>();
		//	HierarchyIndex[] hierarchy;
			Cv2.FindContours(thresh_img_template, out contours_template, out _, RetrievalModes.Tree,
				ContourApproximationModes.ApproxNone);

			CVPoint[][] ExceptContours = ContourOperate.ExceptBoundPoints(img_template.BoundingRect(), contours_template);
			
			int count = ExceptContours.ToList<CVPoint[]>().Count;
			List<CVPoint[]> ModelContours=new List<CVPoint[]>();
		
			for (int i=0;i< count; i++)
            {
				if(Cv2.ContourArea(ExceptContours[i])>= contourArea&&
					Cv2.ArcLength(ExceptContours[i],false)>= coutourLen)
				//if (ExceptContours[i].Length > 30)//至少30点有效
					ModelContours.Add(ExceptContours[i]);
			}
			ModelContours = ModelContours.OrderByDescending(s => s.Length).ToList();
			//绘制边界
			Mat dst = new Mat();
			Cv2.CvtColor(img_template, dst, ColorConversionCodes.GRAY2BGR);
			if(ModelContours.Count>0)
            {
				Cv2.DrawContours(dst, ModelContours, 0, new Scalar(0, 0, 255));
				//获取重心点
				Moments M;
				M = Cv2.Moments(ModelContours[0]);
				double cX = (M.M10 / M.M00);
				double cY = (M.M01 / M.M00);
				
				float a = (float)(M.M20 / M.M00 - cX * cX);
				float b = (float)(M.M11 / M.M00 - cX * cY);
				float c = (float)(M.M02 / M.M00 - cY * cY);
                //计算角度(0~180)
              //  double tanAngle = Cv2.FastAtan2(2 * b, (a - c)) / 2;

                //计算角度2(-90~90)
             //   double ang = (Math.Atan2(2 * b, (a - c)) * 180 / Math.PI) / 2;

		    	//double ang2=  Cv2.MinAreaRect(ModelContours[0]).Angle;

                //if (tanAngle > 90)
                //    tanAngle -= 180;
                //当前轮廓旋转矩
                RotatedRect currrect = Cv2.MinAreaRect(ModelContours[0]);
                //绘制旋转矩形
               	dst.DrawRotatedRect(currrect, Scalar.Lime);

                //绘制目标边界
                Cv2.DrawContours(dst, ModelContours, 0, new Scalar(0, 0, 255));
				//显示目标中心
				dst.drawCross(new CVPoint((int)cX, (int)cY),
					   new Scalar(0, 255, 0), 10, 2);
                //////////////////////////


                //CVPoint[] HullP = Cv2.ConvexHull(ModelContours[0], true);//顺时针方向

                //List<CVPoint[]> HullPList = new List<CVPoint[]>();

                //HullPList.Add(HullP);

                ////Cv2.Polylines(dst, HullPList, true, Scalar.Red);

                //Point2f cVPoint = CalBestDisP(new Point2d(cX, cY), HullP);

				//double ang3 = ang;

				//if(!(cVPoint.X==0&& cVPoint.Y == 0))
    //            {
				//	//计算角度2(-180~180)
				//	ang3 = calAngleOfLx(cX, cY, cVPoint.X, cVPoint.Y);
				//	Cv2.Line(dst, (int)cX, (int)cY, (int)cVPoint.X, (int)cVPoint.Y, Scalar.DarkOrange);
				//}
							
				//轮廓点位
				modelx = cX;
				modely = cY;
				modelangle = currrect.Angle;

				//轮廓长度
				coutourLen = Cv2.ArcLength(ModelContours[0],false);
				contourArea = Cv2.ContourArea(ModelContours[0]);
				templateContour = ModelContours[0];
			}	
			else
            {
				//轮廓点位
				modelx = 0;
				modely = 0;
				modelangle = 0;

				//轮廓长度
				coutourLen = 0;
				contourArea = 0;
				templateContour =null;
			}
            return dst;
		}

		/// <summary>
		/// 形状匹配
		/// </summary>
		/// <param name="image">输入图像</param>
		/// <param name="imgTemplatecontours">模板轮廓</param>
		///  <param name="Segthreshold">分割阈值</param>
		/// <param name="MatchValue">匹配值</param>
		/// <param name="MincoutourLen">轮廓最小长度</param>
		/// <param name="MaxcoutourLen">轮廓最大长度</param>
		/// <param name="MinContourArea">轮廓最小面积</param>
		/// <param name="MaxContourArea">轮廓最大面积</param>
		/// <param name="shapeMatchResult">匹配结果</param>
		/// <param name="isMultipleTemplates">是否使用多模板</param>
		/// <returns>返回绘制图</returns>
		bool ShapeTemplateMatch(Mat image, CVPoint[] imgTemplatecontours, double Segthreshold,
			double MatchValue, int MincoutourLen, int MaxcoutourLen,
			 double MinContourArea, double MaxContourArea,  double baseAngle,
			 ref ShapeMatchResult shapeMatchResult,
			 bool isMultipleTemplates=false,int offsetX=0,int offsetY=0)
		{
		
			//List<Point2d> image_coordinates = new List<Point2d>();
			//灰度化
			//Mat gray_img=new Mat();
			//Cv2.CvtColor(image, gray_img, ColorConversionCodes.BGR2GRAY);
			Mat dst = new Mat();
			Cv2.CvtColor(image, dst, ColorConversionCodes.GRAY2BGR);
			//阈值分割
			Mat thresh_img = new Mat();
			Cv2.Threshold(image, thresh_img, Segthreshold, 255, ThresholdTypes.Binary);


            #region------此处增加与模板创建时候同样的图像处理--------
            //开运算处理，提出白色噪点
            Mat ellipse = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));
	
			Cv2.MorphologyEx(thresh_img, thresh_img, MorphTypes.Open, ellipse);
			#endregion


			//Mat cannyMat = new Mat();
			//Cv2.Canny(thresh_img, cannyMat, Segthreshold, 255);

			//寻找边界
			CVPoint[][] contours_img;
			//HierarchyIndex[] hierarchy;
			Cv2.FindContours(thresh_img, out contours_img, out _, RetrievalModes.Tree,
				 ContourApproximationModes.ApproxNone);
			//根据形状模板进行匹配
			int min_pos = -1;
			double min_value = MatchValue;//匹配分值
			List<CVPoint[]> points = contours_img.ToList<CVPoint[]>();

			//List<double> angleList = new List<double>();
			for (int i = 0; i < points.Count; i++)
			{
				//计算轮廓面积，筛选掉一些没必要的小轮廓
				if (Cv2.ContourArea(contours_img[i]) < MinContourArea ||
							  Cv2.ContourArea(contours_img[i]) > MaxContourArea)
					continue;
				//轮廓长度不达标			
				if (Cv2.ArcLength(contours_img[i], false) < MincoutourLen ||
							  Cv2.ArcLength(contours_img[i], false) > MaxcoutourLen)
					continue;

				//得到匹配分值 ，值越小相似度越高
				double value = Cv2.MatchShapes(contours_img[i], imgTemplatecontours,
														   ShapeMatchModes.I3, 0.0);              
				value = 1 - value;

				//将匹配分值与设定分值进行比较 
				if (value >= min_value)
				{
					min_pos = i;

					//将目标的得分都存在数组中 
					shapeMatchResult.scores.Add(value);
					//匹配到的轮廓
					shapeMatchResult.contours.Add(contours_img[min_pos]);
					/*----------------*/
				}
								
			}
			/*----------------*/
			int count = shapeMatchResult.scores.Count;
			if(count<=0)
            {
				shapeMatchResult.resultToShow = dst;
				shapeMatchResult.exceptionInfo = "模板匹配失败！";
				return false;
			}

			if (isMultipleTemplates)
			{
				for (int j = 0; j < count; j++)
				{
					//绘制目标边界
					Cv2.DrawContours(dst, shapeMatchResult.contours, j, new Scalar(0, 0, 255));
					//得分绘制
					Cv2.PutText(dst,
						string.Format("Score:{0};Angle:{1}", shapeMatchResult.scores[j].ToString("F3"),
                        shapeMatchResult.rotations[j].ToString("F3")),
                             //anglebuf[j].ToString("F3")),
                             new CVPoint(shapeMatchResult.contours[j][0].X + 10, shapeMatchResult.contours[j][0].Y - 10),
										HersheyFonts.HersheyDuplex, 1, Scalar.Yellow);
					//显示目标中心并提取坐标点
					dst.drawCross(new CVPoint((int)shapeMatchResult.positions[j].X, (int)shapeMatchResult.positions[j].Y),
						   new Scalar(0, 255, 0), 10, 2);
					//当前轮廓旋转矩
					RotatedRect currrect = Cv2.MinAreaRect(shapeMatchResult.contours[j]);

					dst.DrawRotatedRect(currrect, Scalar.Lime);
				}
			}
			else
			{
			    double bestScore=  shapeMatchResult.scores.Max();	//最佳得分
				int index = shapeMatchResult.scores.FindIndex(s=>s== bestScore);
              //  double bestangle = shapeMatchResult.rotations[index]; //最佳角度			    
			//	Point2d bestpos = shapeMatchResult.positions[index]; //最佳点位
				CVPoint[] bestcontour= shapeMatchResult.contours[index]; //最佳轮廓			
			
				//绘制目标边界
				Cv2.DrawContours(dst, shapeMatchResult.contours, index, new Scalar(0, 0, 255));
			
				//获取重心点				
				Moments M = Cv2.Moments(bestcontour);
				double cX = (M.M10 / M.M00);
				double cY = (M.M01 / M.M00);

				float a = (float)(M.M20 / M.M00 - cX * cX);
				float b = (float)(M.M11 / M.M00 - cX * cY);
				float c = (float)(M.M02 / M.M00 - cY * cY);
                //计算角度(0~180)
               // double tanAngle = Cv2.FastAtan2(2 * b, (a - c)) / 2;
                //angleList.Add(tanAngle);

                //计算角度2(-90~90)
                //double ang = (Math.Atan2(2 * b, (a - c)) * 180 / Math.PI) / 2;

				#region----角度计算方式2---
				//-90~90度
				//由于先验目标最小包围矩形是长方形   
				//因此最小包围矩形的中心和重心的向量夹角为旋转
				RotatedRect rect_template = Cv2.MinAreaRect(imgTemplatecontours);
				RotatedRect rect_search = Cv2.MinAreaRect(bestcontour);
				//两个旋转矩阵是否同向
				float sign = (rect_template.Size.Width - rect_template.Size.Height) * 
					              (rect_search.Size.Width - rect_search.Size.Height);
				float angle=0;
				if (sign > 0)
					// 可以直接相减
					angle = rect_search.Angle - rect_template.Angle;
				else
					angle = (90 + rect_search.Angle) - rect_template.Angle;

				if (angle > 90)
					angle -= 180;
				#endregion


					//显示目标中心并提取坐标点
				dst.drawCross(new CVPoint((int)cX, (int)cY),
					        new Scalar(0, 255, 0), 10, 2);
                //当前轮廓旋转矩
                RotatedRect currrect = Cv2.MinAreaRect(bestcontour);
                //绘制旋转矩形
                dst.DrawRotatedRect(currrect, Scalar.Lime);
           
                //CVPoint[] HullP = Cv2.ConvexHull(bestcontour, true);//顺时针方向

                //List<CVPoint[]> HullPList = new List<CVPoint[]>();

                //HullPList.Add(HullP);

                //Cv2.Polylines(dst, HullPList, true, Scalar.Red);

                //Point2f cVPoint = CalBestDisP(new Point2d(cX, cY), HullP);

				//double ang3 = ang;

				//if (!(cVPoint.X == 0 && cVPoint.Y == 0))
				//{
				//	//计算角度2(-180~180)
				//	ang3 = calAngleOfLx(cX, cY, cVPoint.X, cVPoint.Y);
				//	Cv2.Line(dst, (int)cX, (int)cY, (int)cVPoint.X, (int)cVPoint.Y, Scalar.DarkOrange);
				//}
			
				//double offsetA = ang3 - baseAngle;//偏转角
				//if (offsetA < -180)
				//	offsetA += 360;
				//else if (offsetA > 180)
				//	offsetA -= 360;

					//得分绘制
				//Cv2.PutText(dst,
				//	string.Format("Score:{0};Angle:{1}", bestScore.ToString("F3"),
				//			  ang3.ToString("F3")),
				//		 new CVPoint(shapeMatchResult.contours[index][0].X + 10, shapeMatchResult.contours[index][0].Y - 10),
				//					HersheyFonts.HersheyDuplex, 1, Scalar.Yellow);

				
				shapeMatchResult.positions.Clear();
				shapeMatchResult.rotations.Clear();
				shapeMatchResult.scores.Clear();
				shapeMatchResult.contours.Clear();
				//将目标的重心坐标都存在数组中 
				shapeMatchResult.positions.Add(new Point2d(cX, cY));//向数组中存放点的坐标
																	
				shapeMatchResult.rotations.Add(angle);//将偏转角度都存在数组中 
														 
				shapeMatchResult.scores.Add(bestScore);//将目标的得分都存在数组中 
													  
				shapeMatchResult.contours.Add(bestcontour); //匹配到的轮廓
				/*----------------*/
			}

			shapeMatchResult.resultToShow = dst;
			return true;
		}

		/// <summary>
		/// 计算最优距离点
		/// 1.首先计算最大距离点（精度0.1），
		/// 2.如果存在2个或以上相同距离点则开始计算第二最大距离点（精度0.1）
		/// 3. 依次内推
		/// </summary>
		CVPoint CalBestDisP(Point2d baseP, CVPoint[] contourP)
			     
        {

			if (contourP.Length <= 1)
				return contourP[0];
			List<double> distanceList = new List<double>();
			List<double> distanceBuf = new List<double>();
			for (int i = 0; i < contourP.Length; i++)
			{
				double dis = Math.Sqrt(Math.Pow(contourP[i].X - baseP.X, 2) + Math.Pow(contourP[i].Y - baseP.Y, 2));
				distanceList.Add(dis);
				distanceBuf.Add(dis);
			}
			distanceList.Sort(compare);//排序：从大到小
			for (int j = 0;j < distanceList.Count;j++)
            {
				int count = distanceBuf.FindAll(s => Math.Abs(s - distanceList[j]) < 5).Count;
				if(count==1)
                {
					int index=distanceBuf.FindIndex(s =>Math.Abs( s - distanceList[j])<5);
					return contourP[index];
                }
			}
			return new CVPoint(0,0);
		}
		/// <summary>
		/// 计算最优距离点
		/// </summary>
		/// <param name="baseP"></param>
		/// <param name="contourP"></param>
		/// <returns></returns>
		Point2f CalBestDisP(Point2d baseP, Point2f[] contourP)
		{

			if (contourP.Length <= 1)
				return contourP[0];
			List<double> distanceList = new List<double>();
			List<double> distanceBuf = new List<double>();
			for (int i = 0; i < contourP.Length; i++)
			{
				double dis = Math.Sqrt(Math.Pow(contourP[i].X - baseP.X, 2) + Math.Pow(contourP[i].Y - baseP.Y, 2));
				distanceList.Add(dis);
				distanceBuf.Add(dis);
			}
			distanceList.Sort(compare);//排序：从大到小
			for (int j = 0; j < distanceList.Count; j++)
			{
				int count = distanceBuf.FindAll(s => Math.Abs(s - distanceList[j]) < 0.1).Count;
				if (count == 1)
				{
					int index = distanceBuf.FindIndex(s => Math.Abs(s - distanceList[j]) < 0.1);
					return contourP[index];
				}
			}
			return contourP[0];
		}
		int compare(double d1,double d2)
        {
			if (d1 > d2)
				return -1;
			else if (d1 == d2)
				return 0;
			else
				return 1;
        }
		/// <summary>
		/// 获取三角形最大边缘的中心点
		/// </summary>
		/// <param name="triangle"></param>
		/// <returns></returns>
		 Point2f getCentreOfMaxEdge_triangle(Point2f[] triangle)
        {
			List<double> distanceList = new List<double>();
			Point2f cp = new Point2f();
			double distance = Math.Sqrt(Math.Pow(triangle[1].X - triangle[0].X, 2) +
								 Math.Pow(triangle[1].Y - triangle[0].Y, 2));
			distanceList.Add(distance);
			double distance2 = Math.Sqrt(Math.Pow(triangle[2].X - triangle[1].X, 2) +
								 Math.Pow(triangle[2].Y - triangle[1].Y, 2));
			distanceList.Add(distance2);
			double distance3 = Math.Sqrt(Math.Pow(triangle[0].X - triangle[2].X, 2) +
								 Math.Pow(triangle[0].Y - triangle[2].Y, 2));
			distanceList.Add(distance3);

			double max = distanceList.Max();
			int maxIndex = distanceList.FindIndex(s => s == max);
			float cenX = 0, cenY = 0;
			if (maxIndex >= 0)
			{			
				if (maxIndex == 0)
				{
					cenX = (triangle[1].X + triangle[0].X) / 2;
					cenY = (triangle[1].Y + triangle[0].Y) / 2;
				
				}
				else if (maxIndex == 1)
				{
					cenX = (triangle[2].X + triangle[1].X) / 2;
					cenY = (triangle[2].Y + triangle[1].Y) / 2;
				
				}
				else if (maxIndex == 2)
				{
					cenX = (triangle[0].X + triangle[2].X) / 2;
					cenY = (triangle[0].Y + triangle[2].Y) / 2;
					
				}
			}
			cp.X = cenX;
			cp.Y = cenY;
			return cp;
		}

		/// <summary>
		/// 计算直线角度
		/// </summary>
		/// <param name="Rx">直线起点坐标x</param>
		/// <param name="Ry">直线起点坐标y</param>
		/// <param name="Rx2">直线终点坐标x</param>
		/// <param name="Ry2">直线终点坐标y</param>
		double calAngleOfLx(double Rx, double Ry, double Rx2, double Ry2)
		           
		{
			if ((Rx2 == Rx) && (Ry2 > Ry))
				return 90;
			else if ((Rx2 == Rx) && (Ry2 < Ry))
				return -90;
			else if ((Rx2 == Rx) && (Ry2 == Ry))
				return -999;
			else
			{
				double temangle = Math.Atan((Ry2 - Ry) / (Rx2 - Rx)) * 180 / Math.PI;
				if (Ry2 - Ry > 0 && Rx2 - Rx > 0)  //第一象限
					temangle = Math.Abs(temangle);
				else if (Ry2 - Ry > 0 && Rx2 - Rx < 0)//第二象限
					temangle += 180;
				else if (Ry2 - Ry < 0 && Rx2 - Rx < 0)//第三象限
					temangle -= 180;
				else if (Ry2 - Ry < 0 && Rx2 - Rx > 0)//第四象限
					temangle = -Math.Abs(temangle);
				return temangle;
			}
		}

		/// <summary>
		/// 判断三角形与四边形是否有边缘重合
		/// </summary>
		/// <param name="triangle">三角</param>
		/// <param name="rotatedRect">四边形</param>
		/// <returns></returns>
	    List<linef> linesCover(Point2f[] triangle,Point2f[] rotatedRect)
        {
			List<linef> linefs = new List<linef>();

			List<Point2f> trianglelist = triangle.ToList<Point2f>();
			//自动补充一个起点
			trianglelist.Add(triangle[0]);

			List<Point2f> rotatedRectlist = rotatedRect.ToList<Point2f>();
			//自动补充一个起点
			rotatedRectlist.Add(rotatedRect[0]);

			for (int i=0;i<3;i++)
				for(int j=0;j<4;j++)
                {
					if (MatExtension.CoverSegments(trianglelist[i], trianglelist[i + 1],
							rotatedRectlist[j], rotatedRectlist[j + 1]))
					{
						linefs.Add(new linef
						{
							startP = trianglelist[i],
							endP = trianglelist[i + 1]
						});
					}
				}

			return linefs;
		}

	}

	public struct linef
    {

		public Point2f startP;
		public Point2f endP;
    }
	/// <summary>
	/// 模板匹配结果
	/// </summary>
	public class ShapeMatchResult: Result
	{
		public ShapeMatchResult()
        {
			rotations = new List<double>();
			contours = new List<CVPoint[]>();
			scores = new List<double>();
			positions = new List<Point2d>();
		}
		public List<double> rotations;            // 旋转角度
		public List<CVPoint[]> contours;       // 匹配轮廓
		public List<double> scores;               // 匹配得分
		public List<Point2d> positions;         // 匹配位置
	}

}
