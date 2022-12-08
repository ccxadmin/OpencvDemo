using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace pictureboxTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Region unionRegion;//掩膜区域绘制   
        List<Point> temPoints = new List<Point>();//多边形轮廓
        Dictionary<int, List<Point>> pointdic = new Dictionary<int, List<Point>>();
        static int xldLable = 0;
        /*-------------------------------------------------*/

        RectangleF rectangleF = new RectangleF(10,10,10,10);//矩形
        GraphicsPath graph = new GraphicsPath();   
        private Point p1, p2;//定义两个点（启点，终点）
        private static bool drawing = false;//设置一个启动标志
        /*-------------------------------------------------*/

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                p1 = new Point(e.X, e.Y);
                p2 = new Point(e.X, e.Y);
                drawing = true;
                if (comboBox1.Text == "多边形")
                {
                    temPoints.Clear();
                    temPoints.Add(p1);
                    xldLable++;

                }
                else if (comboBox1.Text == "矩形")
                {
                    using (Graphics g = pictureBox1.CreateGraphics())
                    {
                        rectangleF.X = e.X - 10 / 2;
                        rectangleF.Y = e.Y - 10 / 2;
                        rectangleF.Width = rectangleF.Height = trackBar1.Value;
                        graph.AddRectangle(rectangleF);
                        unionRegion = new Region(rectangleF);
                        g.FillRectangle(Brushes.Orange, rectangleF);
                        // g.FillRectangle(Brushes.Green, rectangleF);
                    }
                }
                else if (comboBox1.Text == "圆形")
                {
                    using (Graphics g = pictureBox1.CreateGraphics())
                    {
                        rectangleF.X = e.X - 10 / 2;
                        rectangleF.Y = e.Y - 10 / 2;
                        rectangleF.Width = rectangleF.Height = trackBar1.Value;
                        graph.AddEllipse(rectangleF);
                        unionRegion = new Region(graph);
                        g.FillEllipse(Brushes.Orange, rectangleF);
                        //g.FillPath(Brushes.Green, graph);
                    }
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {


                drawing = false;
                if (comboBox1.Text == "多边形")
                {
                    Point[] buf=new Point[temPoints.Count];
                    temPoints.CopyTo( buf);
                
                    pointdic.Add(xldLable, buf.ToList<Point>());

                    return;
                }
                else if (comboBox1.Text == "矩形")
                {

                    using (Graphics g = pictureBox1.CreateGraphics())
                    {
                        g.FillRegion(Brushes.Orange, unionRegion);
                    }
                }
                else if (comboBox1.Text == "圆形")
                {
                    using (Graphics g = pictureBox1.CreateGraphics())
                    {
                        g.FillRegion(Brushes.Orange, unionRegion);
                    }
                }
                #region-----暂不使用----
                //Task.Run(() => {
                //    //      graph.Flatten();

                //    PointF[] points = graph.PathPoints;

                //    List<PointF> temPs = new List<PointF>();

                //    List<PointF> temPsSorted = new List<PointF>();

                //    ////////////////////////////////////////////////
                //    int num = points.Length/4;

                //    temPsSorted.Add(points[0]);
                //    for (int i=0;i<num-1;i++)
                //    {                 
                //        if (points[i * 4 ].Y <= points[(i + 1) * 4 ].Y)
                //        {
                //            temPsSorted.Add(points[(i + 1) * 4 + 1]);
                //        }
                //        else
                //        {
                //            temPsSorted.Add(points[(i + 1) * 4 + 0]);
                //        }
                //    }

                //    temPsSorted.Add(points[(num - 1) * 4 + 2]);

                //    for (int i = num-1; i >= 1; i--)
                //    {
                //        if (points[i * 4].Y <= points[(i - 1) * 4].Y)
                //        {
                //            temPsSorted.Add(points[(i - 1) * 4 + 2]);
                //        }
                //        else
                //        {
                //            temPsSorted.Add(points[(i- 1) * 4 + 3]);
                //        }
                //    }
                //    temPsSorted.Add(points [3]);
                //    temPsSorted.Add(points[0]);
                //    ////////////////////////////////////////////
                //    bool flag = false;

                //    foreach (var s in temPsSorted)
                //    {
                //        flag= graph.IsVisible(s.X, s.Y);
                //      //  flag = unionRegion.IsVisible(s.X, s.Y);

                //        if (!flag)
                //            temPs.Add(new PointF(s.X, s.Y));

                //    }



                //    using (Graphics g = pictureBox1.CreateGraphics())
                //    {
                //        g.DrawPolygon(new Pen(Color.Black,2), temPsSorted.ToArray<PointF>());
                //    }
                //});
                #endregion
            }
        }
        int compareOfX(PointF p1, PointF p2)
        {
            if (p1.X.Equals(p2.X))
            {
                return p1.Y.CompareTo(p2.Y);
            }
            else
            {
                return p1.X.CompareTo(p2.X);
            }
        }
        int compareOfY(PointF p1, PointF p2)
        {
            if (p1.Y.Equals(p2.Y))
            {
                return p1.X.CompareTo(p2.X);
            }
            else
            {
                return p1.Y.CompareTo(p2.Y);
            }
        }
        double distance(PointF p1)
        {
           return Math.Sqrt(Math.Pow(p1.X, 2) + Math.Pow(p1.Y, 2));
            //double d2 = Math.Sqrt(Math.Pow(p2.X, 2) + Math.Pow(p2.Y, 2));
            //return d1.CompareTo(d2);
        }
        int compareOfDis(PointF p1, PointF p2)
        {
            if (distance(p1).Equals(distance(p2)))
            {
                return p1.X.CompareTo(p2.X);
            }
            else
            {
                return distance(p1).CompareTo(distance(p2));
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            using (Graphics g = pictureBox1.CreateGraphics())
            {

                if (e.Button == MouseButtons.Left)
                {
                    if (drawing)
                    {
                        if (comboBox1.Text == "多边形")
                        {
                            //drawing = true;
                            Point currentPoint = new Point(e.X, e.Y);
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;//消除锯齿
                            g.DrawLine(new Pen(Color.Green, 1), p2, currentPoint);

                            p2.X = currentPoint.X;
                            p2.Y = currentPoint.Y;
                            temPoints.Add(p2);
                        }
                        else if (comboBox1.Text == "矩形")
                        {

                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;//消除锯齿
                            rectangleF.X = e.X - 10 / 2;
                            rectangleF.Y = e.Y - 10 / 2;
                            rectangleF.Width = rectangleF.Height = trackBar1.Value;
                            graph.AddRectangle(rectangleF);
                            unionRegion.Union(graph);
                            g.FillRectangle(Brushes.Orange, rectangleF);
                          
                        }
                        else if (comboBox1.Text == "圆形")
                        {
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;//消除锯齿
                            rectangleF.X = e.X - 10 / 2;
                            rectangleF.Y = e.Y - 10 / 2;
                            rectangleF.Width = rectangleF.Height = trackBar1.Value;                          
                            graph.AddEllipse(rectangleF);                                              
                            unionRegion.Union(graph);
                            g.FillEllipse(Brushes.Orange, rectangleF);

                        }

                    }
               
                }
            }
        }

        private void 结束绘制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
         //   temPoints.Add(p1);
            using (Graphics g = pictureBox1.CreateGraphics())
            {
                

                if (闭合.Checked)
                {
                    List<Point> pointBuf = new List<Point>();
                    foreach (var s in pointdic)
                        pointBuf.AddRange(s.Value);
                    Point oriP = pointBuf[0];
                    pointBuf.Add(oriP);

                    //绘制闭合曲线
                    g.DrawPolygon(new Pen(Color.Green, 1), pointBuf.ToArray<Point>());

                    //g.DrawClosedCurve(new Pen(Color.Green, 1), pointBuf.ToArray<Point>());
                }             
                else
                {
                    foreach (var s in pointdic)
                        //绘制非闭合曲线

                        //  g.DrawLines(new Pen(Color.Green, 1), s.Value.ToArray<Point>());

                        g.DrawCurve(new Pen(Color.Green, 1), s.Value.ToArray<Point>());
                }
                   
            }
            return;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Graphics g = pictureBox1.CreateGraphics();
            g.Clear(Color.White);

            Matrix matrix = new Matrix();
            matrix.Scale(0.5f,0.5f);
            unionRegion.Transform(matrix);
            g.FillRegion(Brushes.Orange, unionRegion);

        }


        //清屏操作
        private void button1_Click(object sender, EventArgs e)
        {
            pointdic.Clear();
            graph.Reset();
            temPoints.Clear();
            xldLable = 0;
            //unionRegion.Dispose();
            Graphics g = pictureBox1.CreateGraphics();
            g.Clear(Color.White);
        }
    }
}
