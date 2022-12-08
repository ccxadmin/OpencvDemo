using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionShowLib
{
    static public class Shape
    {
        /// <summary>
        /// 区域绘制
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="regionEx"></param>
        /// <param name="sizeratio"></param>
       public  static void drawRegion(this Graphics graphics, RegionEx regionEx,float sizeratio=1)
        {       
            if (regionEx?.Region is RectangleF)
            {
                RectangleF rect = (RectangleF)regionEx.Region;
                graphics.DrawRectangle(new Pen(regionEx.Color, regionEx.Size), rect.X / sizeratio, rect.Y / sizeratio,
                                                    rect.Width / sizeratio, rect.Height / sizeratio);
            }
            else if(regionEx?.Region is RotatedRectF)
            {
                RotatedRectF rrect = (RotatedRectF)regionEx.Region;
               
                using (var graph = new GraphicsPath())
                {
                    PointF Center = new PointF(rrect.cx / sizeratio, rrect.cy / sizeratio);
                 
                    graph.AddRectangle(new RectangleF( rrect.getrectofangleEqualZero().X / sizeratio,
                        rrect.getrectofangleEqualZero().Y / sizeratio,
                        rrect.getrectofangleEqualZero().Width / sizeratio,
                        rrect.getrectofangleEqualZero().Height / sizeratio));
                    graph.AddLine(new PointF((rrect.cx - rrect.Width / 2) / sizeratio, rrect.cy / sizeratio),
                                 new PointF((rrect.cx + rrect.Width/2) / sizeratio, rrect.cy / sizeratio));
                    /////
                    RotatedRectF rotatedRectF = new RotatedRectF((rrect.cx + rrect.Width / 2) / sizeratio,
                        rrect.cy / sizeratio,20 / sizeratio, 10 / sizeratio, 0);
                    PointF[] point2Fs = rotatedRectF.getPointF();
                    graph.AddLine(new PointF((rrect.cx + rrect.Width / 2) / sizeratio,
                        rrect.cy / sizeratio), new PointF(point2Fs[0].X, point2Fs[0].Y));
                    graph.AddLine(new PointF((rrect.cx + rrect.Width / 2) / sizeratio,
                       rrect.cy / sizeratio), new PointF(point2Fs[3].X, point2Fs[3].Y));
                    /////
                    var a = rrect.angle * (Math.PI / 180);
                    var n1 = (float)Math.Cos(a);
                    var n2 = (float)Math.Sin(a);
                    var n3 = -(float)Math.Sin(a);
                    var n4 = (float)Math.Cos(a);
                    var n5 = (float)(Center.X * (1 - Math.Cos(a)) + Center.Y * Math.Sin(a));
                    var n6 = (float)(Center.Y * (1 - Math.Cos(a)) - Center.X * Math.Sin(a));
                    graph.Transform(new Matrix(n1, n2, n3, n4, n5, n6));
                    graphics.DrawPath(new Pen(regionEx.Color, regionEx.Size), graph);
                  
                }
            }           
            else if (regionEx?.Region is RotatedCaliperRectF)
            {
                RotatedCaliperRectF rrect = (RotatedCaliperRectF)regionEx.Region;

                using (var graph = new GraphicsPath())
                {
                    PointF Center = new PointF(rrect.cx / sizeratio, rrect.cy / sizeratio);

                    graph.AddRectangle(new RectangleF(rrect.getrectofangleEqualZero().X / sizeratio,
                        rrect.getrectofangleEqualZero().Y / sizeratio,
                        rrect.getrectofangleEqualZero().Width / sizeratio,
                        rrect.getrectofangleEqualZero().Height / sizeratio));
                    graph.AddLine(new PointF((rrect.cx - rrect.Width / 2) / sizeratio, rrect.cy / sizeratio),
                                 new PointF((rrect.cx + rrect.Width / 2) / sizeratio, rrect.cy / sizeratio));
                    /////
                    RotatedCaliperRectF rotatedRectF = new RotatedCaliperRectF((rrect.cx + rrect.Width / 2) / sizeratio,
                        rrect.cy / sizeratio, 20 / sizeratio, 10 / sizeratio, 0);
                    PointF[] point2Fs = rotatedRectF.getPointF();
                    graph.AddLine(new PointF((rrect.cx + rrect.Width / 2) / sizeratio,
                        rrect.cy / sizeratio), new PointF(point2Fs[0].X, point2Fs[0].Y));
                    graph.AddLine(new PointF((rrect.cx + rrect.Width / 2) / sizeratio,
                       rrect.cy / sizeratio), new PointF(point2Fs[3].X, point2Fs[3].Y));
                    /////
                    var a = rrect.angle * (Math.PI / 180);
                    var n1 = (float)Math.Cos(a);
                    var n2 = (float)Math.Sin(a);
                    var n3 = -(float)Math.Sin(a);
                    var n4 = (float)Math.Cos(a);
                    var n5 = (float)(Center.X * (1 - Math.Cos(a)) + Center.Y * Math.Sin(a));
                    var n6 = (float)(Center.Y * (1 - Math.Cos(a)) - Center.X * Math.Sin(a));
                    graph.Transform(new Matrix(n1, n2, n3, n4, n5, n6));
                    graphics.DrawPath(new Pen(regionEx.Color, regionEx.Size), graph);

                }
            }
            else if (regionEx?.Region is CircleF)
            {
                CircleF circle = (CircleF)regionEx.Region;
                graphics.DrawEllipse(new Pen(regionEx.Color, regionEx.Size), (circle.Centerx - circle.Radius) / sizeratio,
                      (circle.Centery - circle.Radius) / sizeratio, 2 * circle.Radius / sizeratio, 2 * circle.Radius / sizeratio);

            }
            else if (regionEx?.Region is PointF)
            {
                PointF point = (PointF)regionEx.Region;
                graphics.DrawPolygon(new Pen(regionEx.Color, regionEx.Size), new PointF[] { new PointF (
                    point.X/sizeratio,point.Y/sizeratio
                    )});
            }
            else if (regionEx?.Region is PolygonF)
            {
                PolygonF polygon = (PolygonF)regionEx.Region;
                List<PointF> temlist = new List<PointF>();
                foreach (var s in polygon.Points)
                    temlist.Add(new PointF(s.X / sizeratio, s.Y / sizeratio));
                graphics.DrawPolygon(new Pen(regionEx.Color, regionEx.Size), temlist.ToArray());
            }
            else if (regionEx?.Region is LineF)
            {
                LineF line = (LineF)regionEx.Region;            
                graphics.DrawLine(new Pen(regionEx.Color, regionEx.Size), line.x1/ sizeratio, line.y1/ sizeratio,
                   line.x2/ sizeratio, line.y2/ sizeratio);
            }
            else if (regionEx?.Region is CrossF)
            {
                CrossF cross = (CrossF)regionEx.Region;
                graphics.DrawLine(new Pen(regionEx.Color, regionEx.Size), (cross.x1- cross.width/2) / sizeratio, cross.y1 / sizeratio,
                  (cross.x1 + cross.width / 2) / sizeratio, cross.y1 / sizeratio);
                graphics.DrawLine(new Pen(regionEx.Color, regionEx.Size), cross.x1 / sizeratio, (cross.y1- cross.height/2) / sizeratio,
                  cross.x1 / sizeratio, (cross.y1 + cross.height / 2) / sizeratio);
                graphics.DrawEllipse(new Pen(regionEx.Color, regionEx.Size), (cross.x1 - cross.radius) / sizeratio,
                       (cross.y1 - cross.radius) / sizeratio, 2 * cross.radius / sizeratio, 2 * cross.radius / sizeratio);
            }
            else if(regionEx?.Region is SectorF)
            {
                SectorF sectorF=(SectorF)regionEx.Region;

                //graphics.DrawEllipse(MyPens.assist, sectorF.x / sizeratio, sectorF.y / sizeratio,
                //  sectorF.width / sizeratio, sectorF.height / sizeratio);

                graphics.DrawPie(new Pen(regionEx.Color, regionEx.Size),
                    sectorF.x / sizeratio, sectorF.y / sizeratio, 
                    sectorF.width / sizeratio, sectorF.height / sizeratio, 
                    sectorF.startAngle, sectorF.sweepAngle);
            }
            else if (regionEx?.Region is Region)
            {
                Region unionRegion = (Region)regionEx?.Region;
                //RectangleF rectangleF = unionRegion.GetBounds(graphics);
               
                //Matrix matrix = new Matrix();
                //matrix.Scale(1/sizeratio, 1/sizeratio);
                //unionRegion.Transform(matrix);

                //RectangleF rectangleF2= unionRegion.GetBounds(graphics);

                graphics.FillRegion(Brushes.Orange, unionRegion);
             
            }
            else
                ;
        }
       static    Region baseRegionBuf = new Region();
        /// <summary>
        /// 文本绘制
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="textEx"></param>
        /// <param name="sizeratio"></param>
        public static void drawText(this Graphics graphics, TextEx textEx, float sizeratio = 1)
        {
            graphics.DrawString(textEx.s, textEx.font, textEx.brush,(float)(textEx.x/ sizeratio), (float)(textEx.y/ sizeratio));
        }
    }

    /// <summary>
    /// 区域
    /// </summary>
    public class RegionEx
    {
        public RegionEx()
        {

        }
        public RegionEx(object region, Color color, int size)
        {
            Region = region;
            Color = color;
            Size = size;
        }
        public RegionEx(object region, Color color)
        {
            Region = region;
            Color = color;
            Size = 1;
        }
        public int Size { get; set; }
        public Color Color { get; set; }
        public object Region { get; set; }
    }
    /// <summary>
    /// 文本
    /// </summary>
    public class TextEx
    {
        public TextEx()
        {

        }
        public TextEx(string info)
        {
            s = info;
            font = new Font("宋体",16f, FontStyle.Bold);
            brush = new SolidBrush(Color.LimeGreen);
            x = 10;
            y = 10;
        }
        public TextEx(string info, Font _font, Brush _brush,float _x,float _y)
        {
            s = info;
            font = _font;
            brush = _brush;
            x = _x;
            y = _y;
        }
        public TextEx(string info, Font _font, Brush _brush, double _x, double _y)
        {
            s = info;
            font = _font;
            brush = _brush;
            x = _x;
            y = _y;
        }

        public string s;
        public Font font;
        public Brush brush;
        public double x;
        public double y;
    }
    /// <summary>
    /// 绘制的形状种类
    /// </summary>
    public enum shapeType
    {
        /// <summary>
        /// 矩形
        /// </summary>
        RectangleF,
        /// <summary>
        /// 旋转矩形
        /// </summary>
        RotatedRectF,
        /// <summary>
        /// 圆形
        /// </summary>
        CircleF,
        /// <summary>
        /// 点
        /// </summary>
        PointF,
        /// <summary>
        /// 多边形
        /// </summary>
        PolygonF,
        /// <summary>
        /// 直线
        /// </summary>
        LineF,
        /// <summary>
        /// 十字光标
        /// </summary>
        CrossF,
        /// <summary>
        /// 扇形
        /// </summary>
        SectorF

    }
    /// <summary>
    /// 旋转矩形
    /// </summary>
    [Serializable]
    public struct RotatedRectF
    {     
        public float cx;
        public float cy;
        public float Width;
        public float Height;
        public float angle;
        
       public PointF centerP
        {
            get
            {
                return new PointF(cx, cy);
            }
        }
        public SizeF size
        {
            get
            {
                return new SizeF(Width, Height);
            }
        }

     
        public RotatedRectF(float _cx, float _cy, float _width, float _height, float _angle)
        {          
            cx = _cx;
            cy = _cy;
            Width = _width;
            Height = _height;
            angle = _angle;
            lpfs = new PointF[4];
            lpfs = getPointF();
        }
        public PointF[] lpfs;
        /// <summary>
        /// 顶点坐标
        /// </summary>
        /// <returns></returns>
        public PointF[] getPointF()
        {
            PointF[] pointFs= Rect2Pointfs(new RectangleF(cx - Width / 2, cy - Height / 2, Width, Height), angle);
            return pointFs;
        }
        public RectangleF getrect()
        {
            return  MinBoundRect(lpfs);
        }
        public RectangleF getrectofangleEqualZero()
        {
            return new RectangleF(cx - Width / 2, cy - Height / 2, Width, Height);
        }
        /// <summary>
        /// 以旋转矩形的顶点坐标计算最小外接矩形
        /// </summary>
        /// <param name="lpfs"></param>
        /// <param name="Mbr"></param>
        public static RectangleF  MinBoundRect(PointF[] lpfs)
        {          
            float x, y, xw, yh;
            x = xw = lpfs[0].X;
            y = yh = lpfs[0].Y;
            for(int i=0;i<4;i++)
            {
                if (lpfs[i].X < x)
                    x = lpfs[i].X;
                if (lpfs[i].X > xw)
                    xw = lpfs[i].X;
                if (lpfs[i].Y < y)
                    y = lpfs[i].Y;
                if (lpfs[i].Y > yh)
                    yh = lpfs[i].Y;
            }
            //foreach (var item in lpfs)
            //{
            //    if (item.X < x)
            //        x = item.X;
            //    if (item.X > xw)
            //        xw = item.X;
            //    if (item.Y < y)
            //        y = item.Y;
            //    if (item.Y > yh)
            //        yh = item.Y;
            //}
           return new RectangleF((float)x, (float)y, (float)(xw - x), (float)(yh - y));
        }
        /// <summary>
        /// 将矩形绕中心旋转角度angle后的顶点坐标
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="angle"></param>
        /// <param name="lpfs"></param>
        public PointF[]  Rect2Pointfs(RectangleF rect, float angle)
        {
            PointF[] lpfs;
            using (var graph = new GraphicsPath())
            {
                PointF Center = new PointF(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
                graph.AddRectangle(rect);
                graph.AddLine(new PointF(Center.X , Center .Y),
                          new PointF(Center.X + rect.Width/2+110, Center.Y ));
                var a = angle * (Math.PI / 180);
                var n1 = (float)Math.Cos(a);
                var n2 = (float)Math.Sin(a);
                var n3 = -(float)Math.Sin(a);
                var n4 = (float)Math.Cos(a);
                var n5 = (float)(Center.X * (1 - Math.Cos(a)) + Center.Y * Math.Sin(a));
                var n6 = (float)(Center.Y * (1 - Math.Cos(a)) - Center.X * Math.Sin(a));
                try
                {
                    graph.Transform(new Matrix(n1, n2, n3, n4, n5, n6));
                    lpfs = graph.PathPoints;
                }
                catch
                {
                    lpfs = new PointF[6];
                }
            }
            return lpfs;
        }
      
        public static bool operator ==(RotatedRectF left, RotatedRectF right)
        {
            if ((double)left.cx == (double)right.cx && (double)left.cy == (double)right.cy && (double)left.Width == (double)right.Width
                && (double)left.Height == (double)right.Height)
                return (double)left.angle == (double)right.angle;
            return false;
        }

        public static bool operator !=(RotatedRectF left, RotatedRectF right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// 旋转卡尺矩
    /// </summary>
    [Serializable]
    public struct RotatedCaliperRectF
    {
        public float cx;
        public float cy;
        public float Width;
        public float Height;
        public float angle;

        public PointF centerP
        {
            get
            {
                return new PointF(cx, cy);
            }
        }
        public SizeF size
        {
            get
            {
                return new SizeF(Width, Height);
            }
        }


        public RotatedCaliperRectF(float _cx, float _cy, float _width, float _height, float _angle)
        {
            cx = _cx;
            cy = _cy;
            Width = _width;
            Height = _height;
            angle = _angle;
            lpfs = new PointF[4];
            lpfs = getPointF();
        }
        public PointF[] lpfs;
        /// <summary>
        /// 顶点坐标
        /// </summary>
        /// <returns></returns>
        public PointF[] getPointF()
        {
            PointF[] pointFs = Rect2Pointfs(new RectangleF(cx - Width / 2, cy - Height / 2, Width, Height), angle);
            return pointFs;
        }
        public RectangleF getrect()
        {
            return MinBoundRect(lpfs);
        }
        public RectangleF getrectofangleEqualZero()
        {
            return new RectangleF(cx - Width / 2, cy - Height / 2, Width, Height);
        }
        /// <summary>
        /// 以旋转矩形的顶点坐标计算最小外接矩形
        /// </summary>
        /// <param name="lpfs"></param>
        /// <param name="Mbr"></param>
        public static RectangleF MinBoundRect(PointF[] lpfs)
        {
            float x, y, xw, yh;
            x = xw = lpfs[0].X;
            y = yh = lpfs[0].Y;
            for (int i = 0; i < 4; i++)
            {
                if (lpfs[i].X < x)
                    x = lpfs[i].X;
                if (lpfs[i].X > xw)
                    xw = lpfs[i].X;
                if (lpfs[i].Y < y)
                    y = lpfs[i].Y;
                if (lpfs[i].Y > yh)
                    yh = lpfs[i].Y;
            }
            //foreach (var item in lpfs)
            //{
            //    if (item.X < x)
            //        x = item.X;
            //    if (item.X > xw)
            //        xw = item.X;
            //    if (item.Y < y)
            //        y = item.Y;
            //    if (item.Y > yh)
            //        yh = item.Y;
            //}
            return new RectangleF((float)x, (float)y, (float)(xw - x), (float)(yh - y));
        }
        /// <summary>
        /// 将矩形绕中心旋转角度angle后的顶点坐标
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="angle"></param>
        /// <param name="lpfs"></param>
        public PointF[] Rect2Pointfs(RectangleF rect, float angle)
        {
            PointF[] lpfs;
            using (var graph = new GraphicsPath())
            {
                PointF Center = new PointF(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
                graph.AddRectangle(rect);
                graph.AddLine(new PointF(Center.X, Center.Y),
                          new PointF(Center.X + rect.Width/2+110, Center.Y));
                var a = angle * (Math.PI / 180);
                var n1 = (float)Math.Cos(a);
                var n2 = (float)Math.Sin(a);
                var n3 = -(float)Math.Sin(a);
                var n4 = (float)Math.Cos(a);
                var n5 = (float)(Center.X * (1 - Math.Cos(a)) + Center.Y * Math.Sin(a));
                var n6 = (float)(Center.Y * (1 - Math.Cos(a)) - Center.X * Math.Sin(a));
                try
                {
                    graph.Transform(new Matrix(n1, n2, n3, n4, n5, n6));
                    lpfs = graph.PathPoints;
                }
                catch
                {
                    lpfs = new PointF[6];
                }
            }
            return lpfs;
        }

        public static bool operator ==(RotatedCaliperRectF left, RotatedCaliperRectF right)
        {
            if ((double)left.cx == (double)right.cx && (double)left.cy == (double)right.cy && (double)left.Width == (double)right.Width
                && (double)left.Height == (double)right.Height)
                return (double)left.angle == (double)right.angle;
            return false;
        }

        public static bool operator !=(RotatedCaliperRectF left, RotatedCaliperRectF right)
        {
            return !(left == right);
        }
    }


    /// <summary>
    /// 圆
    /// </summary>
    [Serializable]
    public struct CircleF
    {    
        public float x1;//中心x
        public float y1;//中心y
        public float x2;
        public float y2;
 
        public CircleF(float x11, float y11, float x22, float y22)
        {
            x1 = x11;
            y1 = y11;
            x2 = x22;
            y2 = y22;
            radius = (float)Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
          
        }
        public CircleF(float _centerx, float _centery, float _radius)
        {
            x1 = _centerx;
            y1 = _centery;
            x2 = 0;
            y2 = 0;
            radius = _radius;
           
        }
        private float radius;
        public float Radius
        {
            get => this.radius;
            set
            {
                this.radius = value;
            }
        }
        public float Centerx
        {
            get => x1;
            set => x1 = value;
        }
        public float Centery
        {
            get => y1;
            set => y1 = value;
        }
        public RectangleF boundaryRect
        {
          get => new RectangleF(Centerx - radius, Centery - radius, 2 * radius, 2 * radius);
        }
        
        public static bool operator ==(CircleF left, CircleF right)
        {
            if ((double)left.x1 == (double)right.x1 && (double)left.y1 == (double)right.y1 && (double)left.x2 == (double)right.x2)
                return (double)left.y2 == (double)right.y2;
            return false;
        }

        public static bool operator !=(CircleF left, CircleF right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// 直线
    /// </summary>
    [Serializable]
    public struct LineF
    {
        public float x1;
        public float y1;
        public float x2;
        public float y2;

        public LineF(float x11, float y11, float x22, float y22)
        {
            x1 = x11;
            y1 = y11;
            x2 = x22;
            y2 = y22;
        
        }
           
        public static bool operator ==(LineF left, LineF right)
        {
            if ((double)left.x1 == (double)right.x1 && (double)left.y1 == (double)right.y1 && (double)left.x2 == (double)right.x2)
                return (double)left.y2 == (double)right.y2;
            return false;
        }

        public static bool operator !=(LineF left, LineF right)
        {
            return !(left == right);
        }
    }
    /// <summary>
    /// 多边形
    /// </summary>
    [Serializable]
    public struct PolygonF
    {
        public List<PointF> Points;
     
       
        public PolygonF(PointF[] points)
        {
            Points = points.ToList<PointF>();
            float sumX = 0, sumY = 0;
            foreach (var s in points)
            {
                sumX += s.X;
                sumY += s.Y;
            }
            centerx = sumX / points.Length;
            centery = sumY / points.Length;
        }
        public PolygonF( List<PointF>  points)
        {
            Points = points;
            float sumX = 0, sumY = 0;
            foreach(var s in points)
            {
                sumX += s.X;
                sumY += s.Y;
            }
            centerx = sumX / points.Count;
            centery= sumY/ points.Count;

        }
        public float centerx { get; set; }
        public float centery { get; set; }

        public static bool operator ==(PolygonF left, PolygonF right)
        {
            if (left.Points.Count != right.Points.Count)
                return false;
            for(int i=0;i< left.Points.Count;i++)
            {
                if (left.Points[i].X != right.Points[i].X ||
                    left.Points[i].Y != right.Points[i].Y)
                    return false;
            }
            return true;
             
        }

        public static bool operator !=(PolygonF left, PolygonF right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// 十字光标
    /// </summary>
    [Serializable]
    public struct CrossF
    {
        public float x1;
        public float y1;
        public float width;
        public float height;
        public float radius;

        public CrossF(float x11, float y11, float _width, float _height, float _radius)
        {
            x1 = x11;
            y1 = y11;
            width = _width;
            height = _height;
            radius = _radius;
        }

        public static bool operator ==(CrossF left, CrossF right)
        {
            if ((double)left.x1 == (double)right.x1 && (double)left.y1 == (double)right.y1
                && (double)left.width == (double)right.width && (double)left.height == (double)right.height)
                return (double)left.radius == (double)right.radius;
            return false;
        }

        public static bool operator !=(CrossF left, CrossF right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// 扇形（圆形部分区域）0~360
    /// </summary>
    [Serializable]
    public struct SectorF
    {
        //边框的左上角的 x 坐标，该边框定义扇形所属的椭圆
        public float x;
        // 边框的左上角的 y 坐标，该边框定义扇形所属的椭圆
        public float y;
        //边框的宽度，该边框定义扇形所属的椭圆
        public float width;
        //边框的高度，该边框定义扇形所属的椭圆
        public float height;
        //从 x 轴到扇形的第一条边沿顺时针方向度量的角（以度为单位）
        public float startAngle;
        //从 startAngle 参数到扇形的第二条边沿顺时针方向度量的角（以度为单位）,夹角
        public float sweepAngle;
       


        public SectorF(float x1, float y1, float _width, float _height,float _startAngle,float _sweepAngle)
        {
            x = x1;
            y = y1;
            width = _width;
            height = _height;
            startAngle = _startAngle;
            sweepAngle = _sweepAngle;
        }


        public SectorF(PointF centreP,float radius, float _startAngle, float _sweepAngle)
        {
            width = height = radius * 2;
            x = centreP.X - width / 2;
            y = centreP.Y - height / 2;
            startAngle = _startAngle;
            sweepAngle = _sweepAngle;
        }
        /// <summary>
        /// 获取内环
        /// </summary>
        /// <returns></returns>
        public SectorF getInnerSector()
        {

            float x1 = centreP.X - (float)getRadius * 2 / 3;
            float y1 = centreP.Y - (float)getRadius * 2 / 3;
            float width1 = width * 2 / 3;
            float height1 = height * 2 / 3;
            float startAngle1 = startAngle;
            float sweepAngle1 = sweepAngle;
            return new SectorF(x1, y1, width1, height1, startAngle1, sweepAngle1);

        }
        /// <summary>
        /// 获取外环
        /// </summary>
        /// <returns></returns>
        public SectorF getOuterSector()
        {

            float x1 = centreP.X - (float)getRadius * 4 / 3;
            float y1 = centreP.Y - (float)getRadius * 4 / 3;
            float width1 = width * 4 / 3;
            float height1 = height * 4 / 3;
            float startAngle1 = startAngle;
            float sweepAngle1 = sweepAngle;
            return new SectorF(x1, y1, width1, height1, startAngle1, sweepAngle1);
        }
        /// <summary>
        /// 获得圆弧的端点坐标
        /// </summary>
        /// <returns></returns>
        public PointF getEndpoint(double angle)
        {
            double R = width / 2;
            double startP_x = 0, startP_y = 0;
            //第四象限
            if (angle >= 0 && angle <= 90)
            {
                double cosX = R * Math.Cos(angle / 180 * Math.PI);
                double sinY= R * Math.Sin(angle / 180 * Math.PI);
                startP_x = centreP.X + cosX;
                startP_y = centreP.Y + sinY;
            }
            //第三象限
           else if (angle > 90 && angle <= 180)
            {
                angle = 180 - angle;
                double cosX = R * Math.Cos(angle / 180 * Math.PI);
                double sinY = R * Math.Sin(angle / 180 * Math.PI);
                startP_x = centreP.X - cosX;
                startP_y = centreP.Y + sinY;
            }
            //第二象限
           else if (angle > 180 && angle < 270)
            {
                angle = angle -180;
                double cosX = R * Math.Cos(angle / 180 * Math.PI);
                double sinY = R * Math.Sin(angle / 180 * Math.PI);
                startP_x = centreP.X -cosX;
                startP_y = centreP.Y - sinY;
            }
            ////第一象限
            else 
            {
                angle = 360 - angle;
                double cosX = R * Math.Cos(angle / 180 * Math.PI);
                double sinY = R * Math.Sin(angle / 180 * Math.PI);
                startP_x = centreP.X + cosX;
                startP_y = centreP.Y - sinY;
            }
            return new PointF((float)startP_x, (float)startP_y);
        }

       /// <summary>
       /// 获取半径
       /// </summary>
       /// <returns></returns>
        public double  getRadius
        {
            get=>  width / 2;
        }


        //终止角
        public float getEndAngle
        {
            get => startAngle+ sweepAngle;
            set
            {
                sweepAngle = value - startAngle;
            }
        }

     
        /// <summary>
        /// 边界矩形
        /// </summary>
        public RectangleF boundaryRect
        {
            get => new RectangleF(x, y, width, height);
        }

        /// <summary>
        /// 中心点位
        /// </summary>
        public PointF  centreP
        {
            get => new PointF(x + width / 2, y + height / 2);

        }

        public static bool operator ==(SectorF left, SectorF right)
        {
            if ((double)left.x == (double)right.x && (double)left.y == (double)right.y && (double)left.width == (double)right.width
                && (double)left.height == (double)right.height )                                        
                return (double)left.sweepAngle == (double)right.sweepAngle;
            return false;
        }

        public static bool operator !=(SectorF left, SectorF right)
        {
            return !(left == right);
        }
    }

}
