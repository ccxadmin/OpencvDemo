using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace visionForm
{
    /// <summary>
    /// 直线
    /// </summary>
    public struct StuLineResultData
    {
        public bool runFlag;
        //起点1
        public PointF P1;
        //终点2
        public PointF P2;
        public StuLineResultData(bool flag)
        {
            P1 = new PointF(0, 0);
            P2 = new PointF(0, 0);
            runFlag = flag;
        }
        public StuLineResultData(float x1,float y1,float x2,float y2)
        {
            P1 = new PointF(x1, y1);
            P2 = new PointF(x2, y2);
            runFlag = true;
        }
        public StuLineResultData(PointF p1, PointF p2)
        {
            P1 = p1;
            P2 = p2;
            runFlag = true;
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
                //double temangle2=Math.Atan2((Ry2 - Ry), (Rx2 - Rx)) * 180 / Math.PI;
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
        /// 获取直线与X轴的角度
        /// </summary>
        /// <returns></returns>
        public double GetAngle()
        {
           return calAngleOfLx(P1.X,P1.Y,P2.X,P2.Y);
        }
    }
    /// <summary>
    /// Blob
    /// </summary>
    public struct StuBlobResultData
    {
        public bool runFlag;
        //中点
        public PointF centreP;
     
        public StuBlobResultData(bool flag)
        {
            centreP = new PointF(0, 0);
          
            runFlag = flag;
        }
        public StuBlobResultData(float x1, float y1)
        {
            centreP = new PointF(x1, y1);
         
            runFlag = true;
        }
        public StuBlobResultData(PointF p1)
        {
            centreP = p1;
          
            runFlag = true;

        }
    }
    /// <summary>
    /// 圆
    /// </summary>
    public struct StuCircleResultData
    {
        public bool runFlag;
        //中点
        public PointF centreP;
        //半径
        public float Radius;
        public StuCircleResultData(bool flag)
        {
            centreP = new PointF(0, 0);
            Radius = 0;
            runFlag = flag;
        }
        public StuCircleResultData(float x1, float y1, float radius)
        {
            centreP = new PointF(x1, y1);
            Radius = radius;
            runFlag = true;
        }
        public StuCircleResultData(PointF p1, float radius)
        {
            centreP = p1;
            Radius = radius;
            runFlag = true;

        }     
    }
    /// <summary>
    /// 模板匹配
    /// </summary>
    public struct StuModelMatchData
    {
        public bool runFlag;
        public OpenCvSharp.Point2d matchPoint;
        public double matchOffsetAngle;
        public double matchScore;
        public StuModelMatchData(bool flag)
        {
            matchPoint = new OpenCvSharp.Point2d(0,0);
            matchOffsetAngle = 0;
            matchScore = 0;
            runFlag = flag;
        }
        public StuModelMatchData(OpenCvSharp.Point2d p1, float angle,float score)
        {
            matchPoint = p1;
            matchOffsetAngle = angle;
            matchScore = score;
            runFlag = true;
        }
        public StuModelMatchData(float x1,float y1, float angle, float score)
        {
            matchPoint = new OpenCvSharp.Point2d(x1, y1);
            matchOffsetAngle = angle;
            matchScore = score;
            runFlag = true;
        }
    }

    public  class OutPutDataOfCircleMatch
    {
        public OutPutDataOfCircleMatch()
        {
            stuCircleResultDatas = new List<StuCircleResultData>();
        }
        public  List< StuCircleResultData> stuCircleResultDatas=null;


    }
}
