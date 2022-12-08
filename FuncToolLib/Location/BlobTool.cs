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
    /// Blob检测
    /// </summary>
    public class BlobTool : ToolBase
    {
		public List<KeyPoint> blobp = new List<KeyPoint>();

		/// <summary>
		/// Blob工具运行
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="gray"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override Result Run<T>(Mat gray, T obj)
		{
			BlobData blobData = obj as BlobData;
			Result blobResult = new BlobResult();
		
			try
			{ //获取检测掩膜图像
				Mat temimg = MatExtension.Reducue_Mask_Mat(gray, (CVRect)blobData.ROI);
				KeyPoint[] keyPoints1 = Blob(temimg, blobData);
				if (keyPoints1 is null || keyPoints1.Length <= 0)
				{
					gray.CopyTo(blobResult.resultToShow);
					//检测区域
					blobResult.resultToShow.Rectangle((CVRect)blobData.ROI, new Scalar(255, 0, 0), 2);
					blobResult.exceptionInfo = "未找到Blob粒子";
					blobResult.runStatus = false;
					return blobResult;
				}
				//结果显示
				Mat Dst = new Mat();
				
				CVPoint[][] contours;
				Cv2.CvtColor(gray, Dst, ColorConversionCodes.GRAY2BGR);
				//检测区域
				Dst.Rectangle((CVRect)blobData.ROI, new Scalar(255, 0, 0), 2);

				///绘制粒子的外接圆形
				Cv2.DrawKeypoints(gray, keyPoints1, Dst, Scalar.Red, DrawMatchesFlags.DrawRichKeypoints);
				Mat binary = new Mat();
				//Cv2.Threshold(temimg, binary, blobData.MinThreshold, blobData.MaxThreshold, ThresholdTypes.Binary | ThresholdTypes.Otsu);
				Cv2.Threshold(temimg, binary,  blobData.MaxThreshold,255, ThresholdTypes.Binary);
				//获得轮廓
				Cv2.FindContours(binary, out contours, out _, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
				//绘制轮廓
				int index = 0;
				foreach (var contour in contours)
				{
					Cv2.DrawContours(Dst, contours, index, Scalar.Green, 2);
					index++;
				}

			

				foreach (KeyPoint s in keyPoints1)
				{
					
					///绘制十字中心
					Dst.drawCross(new CVPoint(s.Pt.X, s.Pt.Y), Scalar.Red, 10, 2);
					(blobResult as BlobResult).positionData.Add(new Point2f(
						s.Pt.X, s.Pt.Y));				
				}
				blobResult.resultToShow = Dst;
				blobResult.runStatus = true;

			}
			catch (Exception er)
			{
				Cv2.CvtColor(gray, blobResult.resultToShow, ColorConversionCodes.GRAY2BGR);
				//检测区域
				blobResult.resultToShow.Rectangle((CVRect)blobData.ROI, new Scalar(255, 0, 0), 2);

				blobResult.exceptionInfo = er.Message;
				blobResult.runStatus = false;
				return blobResult;
			}
			return blobResult;
		}

        private KeyPoint[] Blob(Mat gray, BlobData blobData)
        {
			SimpleBlobDetector.Params @params = new SimpleBlobDetector.Params();
			//二值化阈值区间
			@params.MinThreshold = blobData.MinThreshold;
			@params.MaxThreshold = blobData.MaxThreshold;			
			//根据灰度或颜色值过滤
			@params.FilterByColor = blobData.stuBlobFilter.isFilterByColor;
			@params.BlobColor = blobData.stuBlobFilter.BlobColor;
			//根据BLOB面积过滤
			@params.FilterByArea = blobData.stuBlobFilter.isFilterByArea;
			@params.MinArea = blobData.stuBlobFilter.areaLow;
			@params.MaxArea = blobData.stuBlobFilter.areaHigh;
			//根据圆度过滤；圆度表示斑点距圆的距离，例如圆的圆度为1，正方形的圆度为0.785
			@params.FilterByCircularity = blobData.stuBlobFilter.isFilterByCircularity;
			@params.MinCircularity = blobData.stuBlobFilter.MinCircularity;
			@params.MaxCircularity = blobData.stuBlobFilter.MaxCircularity;
			//根据惯性比过滤；衡量形状的伸长程度，相当于（短轴 / 长轴），例如对于一个圆，则惯性比是1，对于椭圆的惯性比在0和1之间，而对于线是0；  0≤  minInertiaRatio  ≤1  ，  maxInertiaRatio  ≤ 1 
			@params.FilterByInertia = blobData.stuBlobFilter.isFilterByInertia;
			@params.MinInertiaRatio = blobData.stuBlobFilter.MinInertiaRatio;
			@params.MaxInertiaRatio = blobData.stuBlobFilter.MaxInertiaRatio;
			//根据凹凸性进行过滤；凸度定义为：斑点的面积/该斑点的凸包面积；凸度小于等于1，越接近1表示该斑点凸性越强
			@params.FilterByConvexity = blobData.stuBlobFilter.isFilterByConvexity;
			@params.MinConvexity = blobData.stuBlobFilter.MinConvexity;
			@params.MaxConvexity = blobData.stuBlobFilter.MaxConvexity;
			SimpleBlobDetector bl = SimpleBlobDetector.Create(@params);
			return  bl.Detect(gray);
		
		}
    }
	/// <summary>
	/// Blob结果
	/// </summary>
	public class BlobResult : Result
	{
		public BlobResult()
		{
			positionData = new List<Point2f>();
		}
		public List<Point2f> positionData;
	}
}
