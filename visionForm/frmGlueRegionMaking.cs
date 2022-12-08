using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisionShowLib;


using OpenCvSharp;
using OpenCvSharp.Extensions;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using Rect = System.Drawing.RectangleF;
using Point = System.Drawing.PointF;
using System.Drawing.Drawing2D;

namespace visionForm
{
    public partial class frmGlueRegionMaking : Form
    {
        public frmGlueRegionMaking()
        {
            InitializeComponent();
            visionShowControl1.Pic.MouseDown += pictureBox1_MouseDown;
            visionShowControl1.Pic.MouseUp += pictureBox1_MouseUp;
            visionShowControl1.Pic.MouseMove += pictureBox1_MouseMove;
            visionShowControl1.ClearOverlayHandle += ClearOverlay;
        } 
        public frmGlueRegionMaking(Mat src,List<Point> PolygonRegion) :this()
        {
            visionShowControl1.dispImage(src);
            visionShowControl1.DrawRegion(new  RegionEx(new  PolygonF(PolygonRegion),Color.Green,16));
            visionShowControl1.AddRegionBuffer(new RegionEx(new PolygonF(PolygonRegion), Color.Green, 16));
          
        }
        public frmGlueRegionMaking(Mat src) : this()
        {
            visionShowControl1.dispImage(src);         
        }

        /*-------------------------------------------------*/
        Region unionRegion;//掩膜区域绘制   
     
        /*-------------------------------------------------*/

        RectangleF rectangleF = new RectangleF(10, 10, 10, 10);//矩形
        GraphicsPath graph = new GraphicsPath();     
        private static bool drawing = false;//设置一个启动标志
        /*-------------------------------------------------*/
        public EventHandler SetModelMaskROIHandle = null;

        /*-------------------------------------------------*/

        public void ClearGraph()
        {
            graph.ClearMarkers();
            graph.Reset();
        
            if (unionRegion != null)
                unionRegion.Dispose();
          //  graph.SetMarkers();
        }

        void ClearOverlay(object sender, EventArgs e)
        {
            ClearGraph();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {             
                drawing = true;
                graph.SetMarkers();
                if (cobxPanType.Text == "矩形")
                {
                    using (Graphics g = visionShowControl1.Pic.CreateGraphics())
                    {
                        rectangleF.X = e.X - BarPanSize.Value / 2;
                        rectangleF.Y = e.Y - BarPanSize.Value / 2;
                        rectangleF.Width = rectangleF.Height = BarPanSize.Value;
                        //graph.AddRectangle(rectangleF);

                        unionRegion = new Region(rectangleF);
                        visionShowControl1.DrawRegion(new RegionEx(unionRegion, Color.Orange));
                        visionShowControl1.AddRegionBuffer(new RegionEx(unionRegion, Color.Orange));
                        //g.FillRectangle(Brushes.Orange, rectangleF);
                        // g.FillRectangle(Brushes.Green, rectangleF);
                    }
                }
                else if (cobxPanType.Text == "圆形")
                {
                    using (Graphics g = visionShowControl1.Pic.CreateGraphics())
                    {
                        rectangleF.X = e.X - BarPanSize.Value / 2;
                        rectangleF.Y = e.Y - BarPanSize.Value / 2;
                        rectangleF.Width = rectangleF.Height = BarPanSize.Value;
                        graph.AddEllipse(rectangleF);
                        unionRegion = new Region(graph);
                        //g.FillEllipse(Brushes.Orange, rectangleF);
                        //g.FillPath(Brushes.Green, graph);
                        visionShowControl1.DrawRegion(new RegionEx(unionRegion, Color.Orange));
                        visionShowControl1.AddRegionBuffer(new RegionEx(unionRegion, Color.Orange));
                    }
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {

                drawing = false;
             
             if (cobxPanType.Text == "矩形")
                {
                    //visionShowControl1.clearOverlay();
                    visionShowControl1.DrawRegion(new RegionEx(unionRegion, Color.Orange));
                    visionShowControl1.AddRegionBuffer(new RegionEx(unionRegion, Color.Orange));
        
                    //using (Graphics g = pictureBox1.CreateGraphics())
                    //{

                    //    g.FillRegion(Brushes.Orange, unionRegion);
                    //}
                }
                else if (cobxPanType.Text == "圆形")
                {
                    visionShowControl1.DrawRegion(new RegionEx(unionRegion, Color.Orange));
                    visionShowControl1.AddRegionBuffer(new RegionEx(unionRegion, Color.Orange));
                    //using (Graphics g = pictureBox1.CreateGraphics())
                    //{
                    //    g.FillRegion(Brushes.Orange, unionRegion);
                    //}
                }
       
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            using (Graphics g = visionShowControl1.Pic.CreateGraphics())
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (drawing)
                    {                    
                         if (cobxPanType.Text == "矩形")
                        {

                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;//消除锯齿
                            rectangleF.X = e.X - BarPanSize.Value / 2;
                            rectangleF.Y = e.Y - BarPanSize.Value / 2;
                            rectangleF.Width = rectangleF.Height = BarPanSize.Value;
                            //graph.AddRectangle(rectangleF);

                            unionRegion.Union(rectangleF);
                            //g.FillRectangle(Brushes.Orange, rectangleF);
                          
                          visionShowControl1.DrawRegion(new RegionEx(unionRegion, Color.Orange));
                        visionShowControl1.AddRegionBuffer(new RegionEx(unionRegion, Color.Orange));

                        }
                        else if (cobxPanType.Text == "圆形")
                        {
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;//消除锯齿
                            rectangleF.X = e.X - BarPanSize.Value / 2;
                            rectangleF.Y = e.Y - BarPanSize.Value / 2;
                            rectangleF.Width = rectangleF.Height = BarPanSize.Value;
                            graph.Reset();
                            graph.AddEllipse(rectangleF);
                            unionRegion.Union(graph);

                            //unionRegion = new Region(graph);
                            visionShowControl1.DrawRegion(new RegionEx(unionRegion, Color.Orange));
                            visionShowControl1.AddRegionBuffer(new RegionEx(unionRegion, Color.Orange));
                           
                            //unionRegion.Union(graph);
                            //g.FillEllipse(Brushes.Orange, rectangleF);

                        }

                    }

                }
            }
        }

        private void BarPanSize_Scroll(object sender, EventArgs e)
        {
            txbBarValue.Text = BarPanSize.Value.ToString();
        }
    }
}
