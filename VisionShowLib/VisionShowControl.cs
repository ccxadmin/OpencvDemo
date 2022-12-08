
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Point = System.Drawing.Point;
using OSLog;
using System.Diagnostics;

namespace VisionShowLib
{
    public delegate void OutPointGray(int x, int y);
    public partial class VisionShowControl : UserControl
    {
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }

        #region 变量
        Log v_log = new Log("视觉控件");
        private static object locker = new object();
        public EventHandler MouseMove;       
        /// <summary>
        /// 鼠标处像素坐标
        /// </summary>
        private float mouseX, mouseY;   
        /// <summary>
        /// 图像缩放比例
        /// </summary>
        public float sizeratio = 1;

        /// <summary>
        /// 形状集合（可再编辑）
        /// </summary>
        private List<object> shapelist = new List<object>();

         List<RotatedCaliperRectF> calipersRegions = new List<RotatedCaliperRectF>();
        /// <summary>
        /// 区域集合（不可再编辑）
        /// </summary>
        public List<RegionEx> regionExlist = new List<RegionEx>();

        /// <summary>
        /// 单独给十字光标分配区域
        /// </summary>
        public List<RegionEx> regionExlistOfCross = new List<RegionEx>();
        /// <summary>
        /// 文本集合
        /// </summary>
        public List<TextEx> textExlist = new List<TextEx>();

        /// <summary>
        /// 选中的图案
        /// </summary>
        private dynamic selectshape = null;

        public bool IsCamFreeModel { get; set; } = false;
        /// <summary>
        /// 创建图案是鼠标的实际位置
        /// </summary>
        // private float drawX, drawY;
        /// <summary>
        /// 正在创建中的图案
        /// </summary>
        // private dynamic drawshape;
        /// <summary>
        /// 画圆
        /// </summary>
        //private bool drawcircle = false;
        /// <summary>
        /// 画矩形
        /// </summary>
        //private bool drawrectangle1 = false;
     
        int mx = 0;
        int my = 0;
        /// <summary>
        /// 使用的画笔
        /// </summary>
        private Pen Penused;

        private float drawimgX, drawimgY, drawimgW, drawimgH;
        
        private Bitmap image;

        Mat savebuf=new Mat();

        int imageWidth,imageHeight;
       
        #endregion
        #region 缩放因子属性
        private double scalestep = 0.2;
        [Description("缩放步长"), Browsable(true)]
        public double FactorStep
        {
            get => scalestep;
            set => scalestep = value;
        }
        #endregion
        #region 显示图像
        /// <summary>
        /// 图像
        /// </summary>
        public Bitmap Image
        {
            get => image;
            set
            {
                image = value;
                updateImage();
            }
        }

        #endregion
        //picturebox
        public PictureBox Pic { get => this.PicBox; }

        int detectionTime = 0;
        /// <summary>
        /// 检测时间设定
        /// </summary>
        public int DetectionTime
        {
            get => this.detectionTime;
            set
            {
                this.detectionTime = value;

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => {
                        this.TimeLabel.Text = string.Format("{0}ms", value);
                    }));
                }
                else
                {
                    this.TimeLabel.Text = string.Format("{0}ms", value);
                }

            }
        }
        /// <summary>
        /// 标题
        /// </summary>
        [DefaultValue("cam1")]
        public string TitleName
        {
            get => this.lblTitleName.Text;
            set
            {
                if (this.toolStrip1.InvokeRequired)
                    this.Invoke(new Action(() =>
                    {
                        this.lblTitleName.Text = value;
                    }));
                else
                    this.lblTitleName.Text = value;
            }

        }

        //双击获取像素坐标
        public OutPointGray DoubleClickGetMousePosHandle2;
        public VisionShowControl()
        {
            InitializeComponent();
            statusStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;

            LocationLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;

            TimeLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;

            GrayLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;

            toolStripStatusLabel4.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;

            toolStripStatusLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;

            MyPens.Select.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            Penused = MyPens.Default;
            PicBox.Width = PanelBox.Width - 100;
            PicBox.Height = PanelBox.Height - 100;
            int centreX = (PanelBox.Location.X + PanelBox.Width) / 2;
            int centreY = (PanelBox.Location.Y + PanelBox.Height) / 2;
            int newX = centreX - PicBox.Width / 2;
            int newY = centreY - PicBox.Height / 2- toolStrip1.Height/2;
            PicBox.Location = new Point(newX, newY);
       
        }
        public Point mouseDownPoint;//存储鼠标焦点的全局变量
        public bool isSelected = false;//平移状态
        public bool isStartDrawPolygonF;//开始绘制多边形       
        List<PointF> polyPoints = new List<PointF>();//多边形点集合
        public int selectindex;// 选择的shape索引
        public EventHandler RoiChangedHandle;//ROI更新通知事件
        /*/////////////////////////////////////////////*/

        private void VisionShowControl_Load(object sender, EventArgs e)
        {
            PicBox.Dock = DockStyle.None;
            //Width = image.Width;
            //Height = image.Height;
            //computeWratio();
            //sizeratio = winratio;
            //这个事件是鼠标滑轮滚动的触发事件，可以在Designer.cs中注册。
            this.MouseWheel += new MouseEventHandler(this.PicBox_MouseWheel);
            this.PicBox.MouseWheel += new MouseEventHandler(this.PicBox_MouseWheel);
            this.PicBox.MouseDown += new MouseEventHandler(this.PicBox_MouseDown);
            this.PicBox.MouseMove += new MouseEventHandler(this.PicBox_MouseMove);
            this.PicBox.MouseUp += new MouseEventHandler(this.PicBox_MouseUp);
            this.PicBox.MouseHover += new EventHandler(this.PicBox_MouseHover);
        }
        /// <summary>
        /// 滚轮缩放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PicBox_MouseWheel(object sender, MouseEventArgs e)
        {
            double scale = FactorStep;
            int width = PicBox.Width;
            int height = PicBox.Height;

            int sign = Math.Sign(e.Delta);

            PicBox.Width += (int)(sign * scale * width);
            PicBox.Height += (int)(sign * scale * height);
            this.PicBox.Left -= (int)(sign * scale * e.X);
            this.PicBox.Top -= (int)(sign * scale * e.Y);           
            computePicratio();

        }
        //在MouseDown处获知鼠标是否按下，并记录下此时的鼠标坐标值；
        private void PicBox_MouseDown(object sender, MouseEventArgs e)
        {
            this.PicBox.Focus();
            if (e.Button == MouseButtons.Middle)
            {
                mouseDownPoint.X = Cursor.Position.X;  //注：全局变量mouseDownPoint前面已定义为Point类型  
                mouseDownPoint.Y = Cursor.Position.Y;
                isSelected = true;
            }   
            if(isStartDrawPolygonF)//开始多边形点位绘制
            {
                mouseX = (float)(e.X * sizeratio);
                mouseY = (float)(e.Y * sizeratio);
                if (e.Button==MouseButtons.Left)
                {
                    if (polyPoints == null)
                        polyPoints = new List<PointF>();
                                       
                    polyPoints.Add(new PointF(mouseX, mouseY));

                    updateImage();
                }
                else if(e.Button==MouseButtons.Right&&
                    polyPoints!=null)
                {
                    //Pen pen = new Pen(Color.Red,3);
                    using (var g= PicBox.CreateGraphics())
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.Clear(PicBox.BackColor);
                        //填充多边形
                        //using (SolidBrush br=new SolidBrush(Color.FromArgb(100,Color.Yellow)))
                        //{
                        //    g.FillPolygon(br,polyPoints.ToArray());
                        //}
                        if (polyPoints.ToArray().Length >= 3)
                        {
                            //绘制轮廓
                           // g.DrawPolygon(Pens.Red, polyPoints.ToArray());
                            //绘制顶点
                            //foreach(PointF p in polyPoints)
                            // {
                            //     g.FillEllipse(Brushes.Red,new Rectangle(p.X-2,p.Y-2,4,4));
                            // }


                            isStartDrawPolygonF = false;

                            shapelist.Add(new PolygonF(polyPoints));
                            updateImage();
                        }
                    }
                }
            }
        }
        //鼠标进入获取焦点
        private void PicBox_MouseHover(object sender,EventArgs e)
        {
            this.PicBox.Focus();
        }
        //在MouseUp处获知鼠标是否松开，终止拖动操作；
        private void PicBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                isSelected = false;
            }
           // updateImage();
        }
             
        //图片平移,在MouseMove处添加拖动函数操作
        private void PicBox_MouseMove(object sender, MouseEventArgs e)
        {
            //窗口移动
            if (isSelected && IsMouseInPanel())//确定已经激发MouseDown事件，和鼠标在picturebox的范围内         
            {

                this.PicBox.Left = this.PicBox.Left + (Cursor.Position.X - mouseDownPoint.X);
                this.PicBox.Top = this.PicBox.Top + (Cursor.Position.Y - mouseDownPoint.Y);
                mouseDownPoint.X = Cursor.Position.X;
                mouseDownPoint.Y = Cursor.Position.Y;
                computedrawsize();
            }

            mouseX = (float)(e.X * sizeratio);
            mouseY = (float)(e.Y * sizeratio);
            Color pixelcolor = Color.FromArgb(0, 0, 0);

            try
            {
                if (this.image != null && image.Size.Width > 0)
                {
                    Monitor.Enter(locker);
                    pixelcolor = this.image.GetPixel((int)mouseX, (int)mouseY);
                    Monitor.Exit(locker);
                }

            }
            catch (Exception er)
            {
                Console.WriteLine(er.Message);
            }
            if (MouseMove != null)
            {
                MouseMove(this, new MouseEventArgs(e.Button, e.Clicks, (int)mouseX, (int)mouseY, e.Delta));
            }
            //像素坐标
            this.LocationLabel.Text = string.Format("{0},{1}", (int)mouseX, (int)mouseY);
           //像素值
            this.GrayLabel.Text = string.Format("{0},{1},{2}",
               (int)pixelcolor.R, (int)pixelcolor.G, (int)pixelcolor.B);
               
      
            //选中
            if (e.Button != MouseButtons.Left || selectshape == null )
            {

                for (int i = 0; i < shapelist.Count; i++)
                {
                    bool select = true;
                    if (shapelist[i] is RectangleF)
                    {
                        RectangleF mouserect = (RectangleF)shapelist[i];
                        bool left = Math.Abs(mouseX - mouserect.X) < 10 * sizeratio;
                        bool top = Math.Abs(mouseY - mouserect.Y) < 10 * sizeratio;
                        bool right = Math.Abs(mouseX - mouserect.X - mouserect.Width) < 10 * sizeratio;
                        bool Bottom = Math.Abs(mouseY - mouserect.Y - mouserect.Height) < 10 * sizeratio;

                        RectangleF centerrect = new RectangleF(mouserect.X + mouserect.Width / 2 - 10,
                            mouserect.Y + mouserect.Height / 2 - 10, 20, 20);
                        RectangleF topleft = new RectangleF(mouserect.X - 20, mouserect.Y - 20, 40, 40);
                        RectangleF topright = new RectangleF(mouserect.X + mouserect.Width - 20, mouserect.Y - 20, 40, 40);
                        RectangleF bottomleft = new RectangleF(mouserect.X - 20, mouserect.Y + mouserect.Height - 20, 40, 40);
                        RectangleF bottomright = new RectangleF(mouserect.X + mouserect.Width - 20, mouserect.Y + mouserect.Height - 20, 40, 40);
                        if (centerrect.Contains(mouseX, mouseY))//中心点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.SizeAll;
                        }
                        else if (topleft.Contains(mouseX, mouseY))//左上点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanNW;
                        }
                        else if (topright.Contains(mouseX, mouseY))//右上点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanNE;
                        }
                        else if (bottomleft.Contains(mouseX, mouseY))//左下点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanSW;
                        }
                        else if (bottomright.Contains(mouseX, mouseY))//右下点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanSE;
                        }
                        else if (left && Math.Abs(mouseY - mouserect.Y - mouserect.Height / 2.0) < mouserect.Height / 2)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanWest;

                        }
                        else if (right && Math.Abs(mouseY - mouserect.Y - mouserect.Height / 2.0) < mouserect.Height / 2)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanEast;
                        }
                        else if (top && Math.Abs(mouseX - mouserect.X - mouserect.Width / 2) < mouserect.Width / 2)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanNorth;
                        }
                        else if (Bottom && Math.Abs(mouseX - mouserect.X - mouserect.Width / 2) < mouserect.Width / 2)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanSouth;
                        }
                        else
                        {
                            select = false;
                            selectshape = null;
                        }
                        ////
                        if (select)
                        {
                            selectshape = mouserect;
                            selectindex = i;
                            break;
                        }
                        else
                        {
                            this.Cursor = Cursors.Default;
                            Penused = MyPens.Default;
                        }
                    }
                    else if (shapelist[i] is RotatedRectF)
                    {
                        RotatedRectF mouserect = (RotatedRectF)shapelist[i];
                        RectangleF shapeTransRR = RotatedRectF.MinBoundRect(mouserect.getPointF());

                        bool left = Math.Abs(mouseX - (mouserect.getPointF()[0].X + mouserect.getPointF()[3].X) / 2) < 10 * sizeratio;
                        bool top = Math.Abs(mouseY - (mouserect.getPointF()[0].Y + mouserect.getPointF()[1].Y) / 2) < 10 * sizeratio;
                        bool right = Math.Abs(mouseX - (mouserect.getPointF()[1].X + mouserect.getPointF()[2].X) / 2) < 10 * sizeratio;
                        bool Bottom = Math.Abs(mouseY - (mouserect.getPointF()[2].Y + mouserect.getPointF()[3].Y) / 2) < 10 * sizeratio;

                        RectangleF centerrect = new RectangleF(mouserect.cx - 10, mouserect.cy - 10, 20, 20);

                        RectangleF topleft = new RectangleF(mouserect.getPointF()[0].X - 20, mouserect.getPointF()[0].Y - 20, 40, 40);
                        RectangleF topright = new RectangleF(mouserect.getPointF()[1].X - 20, mouserect.getPointF()[1].Y - 20, 40, 40);
                        RectangleF bottomleft = new RectangleF(mouserect.getPointF()[3].X - 20, mouserect.getPointF()[3].Y - 20, 40, 40);
                        RectangleF bottomright = new RectangleF(mouserect.getPointF()[2].X - 20, mouserect.getPointF()[2].Y - 20, 40, 40);

                        RectangleF angleSign = new RectangleF(mouserect.getPointF()[5].X - 20, mouserect.getPointF()[5].Y - 20, 40, 40);
                        
                        if (centerrect.Contains(mouseX, mouseY))//中心点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.SizeAll;
                        }
                       else if (angleSign.Contains(mouseX, mouseY))//角度标点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.Hand;
                        }
                        else if (topleft.Contains(mouseX, mouseY))//左上点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanNW;
                        }
                        else if (topright.Contains(mouseX, mouseY))//右上点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanNE;
                        }
                        else if (bottomleft.Contains(mouseX, mouseY))//左下点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanSW;
                        }
                        else if (bottomright.Contains(mouseX, mouseY))//右下点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanSE;
                        }
                        else if (left && Math.Abs(mouseY - (mouserect.getPointF()[0].Y + mouserect.getPointF()[3].Y) / 2) < mouserect.Height / 4)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanWest;

                        }
                        else if (right && Math.Abs(mouseY - (mouserect.getPointF()[1].Y + mouserect.getPointF()[2].Y) / 2) < mouserect.Height / 4)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanEast;
                        }
                        else if (top && Math.Abs(mouseX - (mouserect.getPointF()[0].X + mouserect.getPointF()[1].X) / 2) < mouserect.Width / 4)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanNorth;
                        }
                        else if (Bottom && Math.Abs(mouseX - (mouserect.getPointF()[2].X + mouserect.getPointF()[3].X) / 2) < mouserect.Width / 4)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanSouth;
                        }
                        else
                        {
                            select = false;
                            selectshape = null;
                       
                        }
                        ////
                        if (select)
                        {
                            selectshape = mouserect;
                            selectindex = i;
                            break;
                        }
                        else
                        {
                            this.Cursor = Cursors.Default;
                            Penused = MyPens.Default;
                        }
                    }                  
                    else if (shapelist[i] is RotatedCaliperRectF)
                    {
                        RotatedCaliperRectF mouserect = (RotatedCaliperRectF)shapelist[i];
                        bool left = Math.Abs(mouseX - (mouserect.getPointF()[0].X + mouserect.getPointF()[3].X) / 2) < 10 * sizeratio;
                        bool top = Math.Abs(mouseY - (mouserect.getPointF()[0].Y + mouserect.getPointF()[1].Y) / 2) < 10 * sizeratio;
                        bool right = Math.Abs(mouseX - (mouserect.getPointF()[1].X + mouserect.getPointF()[2].X) / 2) < 10 * sizeratio;
                        bool Bottom = Math.Abs(mouseY - (mouserect.getPointF()[2].Y + mouserect.getPointF()[3].Y) / 2) < 10 * sizeratio;

                        RectangleF centerrect = new RectangleF(mouserect.cx - 10, mouserect.cy - 10, 20, 20);
                        RectangleF topleft = new RectangleF(mouserect.getPointF()[0].X - 20, mouserect.getPointF()[0].Y - 20, 40, 40);
                        RectangleF topright = new RectangleF(mouserect.getPointF()[1].X - 20, mouserect.getPointF()[1].Y - 20, 40, 40);
                        RectangleF bottomleft = new RectangleF(mouserect.getPointF()[3].X - 20, mouserect.getPointF()[3].Y - 20, 40, 40);
                        RectangleF bottomright = new RectangleF(mouserect.getPointF()[2].X - 20, mouserect.getPointF()[2].Y - 20, 40, 40);
                        RectangleF angleSign = new RectangleF(mouserect.getPointF()[5].X - 20, mouserect.getPointF()[5].Y - 20, 40, 40);
                        if (centerrect.Contains(mouseX, mouseY))//中心点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.SizeAll;
                        }
                        else if (angleSign.Contains(mouseX, mouseY))//角度标点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.Hand;
                        }
                        else if (topleft.Contains(mouseX, mouseY))//左上点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanNW;
                        }
                        else if (topright.Contains(mouseX, mouseY))//右上点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanNE;
                        }
                        else if (bottomleft.Contains(mouseX, mouseY))//左下点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanSW;
                        }
                        else if (bottomright.Contains(mouseX, mouseY))//右下点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanSE;
                        }
                        else if (left && Math.Abs(mouseY - (mouserect.getPointF()[0].Y + mouserect.getPointF()[3].Y) / 2) < mouserect.Height / 4)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanWest;

                        }
                        else if (right && Math.Abs(mouseY - (mouserect.getPointF()[1].Y + mouserect.getPointF()[2].Y) / 2) < mouserect.Height / 4)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanEast;
                        }
                        else if (top && Math.Abs(mouseX - (mouserect.getPointF()[0].X + mouserect.getPointF()[1].X) / 2) < mouserect.Width / 4)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanNorth;
                        }
                        else if (Bottom && Math.Abs(mouseX - (mouserect.getPointF()[2].X + mouserect.getPointF()[3].X) / 2) < mouserect.Width / 4)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanSouth;
                        }
                        else
                        {
                            select = false;
                            selectshape = null;

                        }
                        ////
                        if (select)
                        {
                            selectshape = mouserect;
                            selectindex = i;
                            break;
                        }
                        else
                        {
                            this.Cursor = Cursors.Default;
                            Penused = MyPens.Default;
                        }
                    }
                    else if (shapelist[i] is CircleF)
                    {
                        CircleF circle = (CircleF)shapelist[i];
                        float radiu = (float)Math.Sqrt(Math.Pow(mouseX - circle.Centerx, 2) +
                                                        Math.Pow(mouseY - circle.Centery, 2));
                        if (Math.Abs(circle.Radius - radiu) < 10)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.Cross;

                        }
                        else if (radiu < 20)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.SizeAll;

                        }
                        else
                        {
                            select = false;
                            selectshape = null;
                        }
                        /////////////
                        if (select)
                        {
                            //(shapelist[i] as CircleF).isSelected = true;
                            selectshape = circle;
                            selectindex = i;
                            break;
                        }
                        else
                        {

                            this.Cursor = Cursors.Default;
                            Penused = MyPens.Default;
                            //(shapelist[i] as CircleF).isSelected = false;
                            selectshape = null;
                        }
                    }
                    else if (shapelist[i] is SectorF)
                    {
                        SectorF sectorF = (SectorF)shapelist[i];

                        RectangleF rect_startp = new RectangleF(sectorF.getEndpoint(sectorF.startAngle).X - 20,
                            sectorF.getEndpoint(sectorF.startAngle).Y - 20, 40, 40);

                        RectangleF rect_endp = new RectangleF(sectorF.getEndpoint(sectorF.getEndAngle).X - 20,
                          sectorF.getEndpoint(sectorF.getEndAngle).Y - 20, 40, 40);


                        float radiu = (float)Math.Sqrt(Math.Pow(mouseX - sectorF.centreP.X, 2) +
                                                        Math.Pow(mouseY - sectorF.centreP.Y, 2));

                        if (Math.Abs(sectorF.getRadius - radiu) < 10 &&
                           !rect_startp.Contains(mouseX, mouseY) && !rect_endp.Contains(mouseX, mouseY))
                        //半径缩放
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.Cross;

                        }
                        //圆心平移
                        else if (radiu < 20)
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.SizeAll;

                        }
                        else if (rect_startp.Contains(mouseX, mouseY))//起始点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanEast;
                        }
                        else if (rect_endp.Contains(mouseX, mouseY))//终止点位
                        {
                            Penused = MyPens.Select;
                            this.Cursor = Cursors.PanWest;
                        }

                        else
                        {
                            select = false;
                            selectshape = null;
                        }
                        /////////////
                        if (select)
                        {
                            //(shapelist[i] as CircleF).isSelected = true;
                            selectshape = sectorF;
                            selectindex = i;
                            break;
                        }
                        else
                        {

                            this.Cursor = Cursors.Default;
                            Penused = MyPens.Default;
                            //(shapelist[i] as CircleF).isSelected = false;
                            selectshape = null;
                        }
                    }
                }
            }
            //拖拽
            if (selectshape != null)
            {
                if (e.Button == MouseButtons.Left )
                {
                    if (selectshape is RectangleF)
                    {
                        float drawW = selectshape.Width;
                        float drawH = selectshape.Height;
                        float drawX = selectshape.X;
                        float drawY = selectshape.Y;
                        float x = drawX, y = drawY, width = drawW, height = drawH;
                        float x2 = drawX + drawW;
                        float y2 = drawY + drawH;
                        if (this.Cursor == Cursors.SizeAll)
                        {
                            float centerx = drawX + width / 2;
                            float centery = drawY + height / 2;

                            x += mouseX - centerx;
                            y += mouseY - centery;                            
                        }
                        else if (this.Cursor == Cursors.PanNW)
                        {
                            width += drawX - mouseX;
                            height += drawY - mouseY;
                            x = mouseX;
                            y = mouseY;
                          
                        }
                        else if (this.Cursor == Cursors.PanNE)
                        {
                            width = mouseX - drawX;
                            height += drawY - mouseY;
                            y = mouseY;
                           
                        }
                        else if (this.Cursor == Cursors.PanSW)
                        {
                            width -= mouseX - drawX;
                            height = mouseY - drawY;
                            x = mouseX;
                        }
                        else if (this.Cursor == Cursors.PanSE)
                        {
                            width = mouseX - drawX;
                            height = mouseY - drawY;

                        }
                        else if (this.Cursor == Cursors.PanWest)//左
                        {

                            width = (float)drawW + (drawX - mouseX);
                            x = mouseX;
                        }
                        else if (this.Cursor == Cursors.PanEast)
                        {
                            width = mouseX - drawX;
                        }
                        else if (this.Cursor == Cursors.PanNorth)
                        {
                            y = mouseY;
                            height = drawH + drawY - mouseY;
                        }
                        else if (this.Cursor == Cursors.PanSouth)
                        {
                            height = mouseY - drawY;
                        }

                      //边界防护
                        if (x < 0)
                            x = 0;
                        if (x + width >= imageWidth)
                            x = imageWidth - width;
                        if (y <= 0)
                            y = 0;
                        if (y + height >= imageHeight)
                            y = imageHeight - height;
                        //边界防护2
                        if (width < 50)
                        {
                            width = 50;
                            x = x2 - width;
                        }
                        if (height < 50)
                        {
                            height = 50;
                            y = y2 - height;
                        }
                        if (width > imageWidth)
                        {
                            width = imageWidth;
                            x = 0;
                        }
                        if (height > imageHeight)
                        {
                            height = imageHeight;
                            y = 0;
                        }
                        selectshape = new RectangleF(x, y, width, height);

                    }
                    else if (selectshape is RotatedRectF)
                    {
                        float angle = selectshape.angle;                     
                        float drawW = selectshape.Width;
                        float drawH = selectshape.Height;
                        float drawX = selectshape.cx;
                        float drawY = selectshape.cy;
                        float x = drawX, y = drawY, width = drawW, height = drawH;
                        if (this.Cursor == Cursors.SizeAll)//平移标志
                        {
                            float centerx = drawX;
                            float centery = drawY ;

                            x += mouseX - centerx;
                            y += mouseY - centery;
                        }
                        else if(this.Cursor == Cursors.Hand)//角度旋转标志
                        {
                            if (e.Button == MouseButtons.Left)
                            {
                                mx = e.X;
                                my = e.Y;
                                angle = (float)(Math.Atan2(mouseY - selectshape.cy, mouseX - selectshape.cx)*180/Math.PI);
                                selectshape.angle = angle;
                            }

                        }
                        else if (this.Cursor == Cursors.PanNW)
                        {
                            width = (drawX-mouseX ) * 2;
                            height = (drawY-mouseY) * 2;
                            if (width < 10)
                                width = 10;
                            if (height < 10)
                                height = 10;

                        }
                        else if (this.Cursor == Cursors.PanNE)
                        {
                            width = (mouseX - drawX) * 2;
                            height = (drawY - mouseY) * 2;
                            if (width < 10)
                                width = 10;
                            if (height < 10)
                                height = 10;
                        }
                        else if (this.Cursor == Cursors.PanSW)
                        {
                            width = (drawX - mouseX) * 2;
                            height = (mouseY - drawY) * 2;
                            if (width < 10)
                                width = 10;
                            if (height < 10)
                                height = 10;
                        }
                        else if (this.Cursor == Cursors.PanSE)
                        {
                            width =( mouseX - drawX)*2;
                            height =( mouseY - drawY)*2;
                            if (width < 10)
                                width = 10;
                            if (height < 10)
                                height = 10;

                        }
                        else if (this.Cursor == Cursors.PanWest)//左
                        {
                            double cosValue= Math.Cos(angle/180*Math.PI);

                            //     width = (float)drawW + (drawX - mouseX);
                            //     x = mouseX;

                            width = (drawX- mouseX) * 2;                         
                            if (width < 10)
                                width = 10;
                           

                        }
                        else if (this.Cursor == Cursors.PanEast)
                        {
                            width = (mouseX - drawX) * 2;
                            if (width < 10)
                                width = 10;
                        }
                        else if (this.Cursor == Cursors.PanNorth)
                        {
                            //    y = mouseY;
                            //       height = drawH + drawY - mouseY;
                            height = ( drawY - mouseY) * 2;
                            if (height < 10)
                                height = 10;

                        }
                        else if (this.Cursor == Cursors.PanSouth)
                        {
                            height = (mouseY - drawY)*2;
                            if (height < 10)
                                height = 10;
                        }

                        //构建边界矩形
                        RotatedRectF rotatedRectF = new RotatedRectF(x, y, width, height, angle);
                        RectangleF rectangleF = rotatedRectF.getrect();
                        float leftX = rectangleF.X;
                        float rightX = rectangleF.X + rectangleF.Width;
                        float topY = rectangleF.Y;
                        float bottomY = rectangleF.Y + rectangleF.Height;
                        float minSize = imageWidth > imageHeight ? imageHeight : imageWidth;
                        //边界防护
                        if (leftX < 0)
                            x = 0+ rectangleF.Width/2+10 ;
                        if (rightX >= imageWidth)
                            x = imageWidth - rectangleF.Width / 2-10;
                    
                        if (topY <= 0)
                            y = 0 + rectangleF.Height / 2+10;
                        if (bottomY >= imageHeight)
                            y = imageHeight - rectangleF.Height / 2-10;
                        //边界防护2
                        if (rectangleF.Width < 50)
                        {
                            width = 50;
                            //x = rightX - width / 2;
                            //if (x < 25)
                            //    x = 25;
                        }
                        if (rectangleF.Height < 50)
                        {
                            height = 50;
                            //y = bottomY - height / 2;
                            //if (y < 25)
                            //    y = 25;
                        }
                        //if (rectangleF.Width > minSize)
                        //{
                        //    width = minSize;                   
                        //    x = 0+ minSize / 2;
                        //    return;

                        //}
                        //if (rectangleF.Height > minSize)
                        //{
                        //    height = minSize;
                        //    y = 0+ minSize / 2;
                        //    return;
                        //}

                        selectshape = new RotatedRectF(x, y, width, height, angle);
                      
                    }                    
                    else if (selectshape is RotatedCaliperRectF)
                    {
                        float angle = selectshape.angle;
                        float drawW = selectshape.Width;
                        float drawH = selectshape.Height;
                        float drawX = selectshape.cx;
                        float drawY = selectshape.cy;
                        float x = drawX, y = drawY, width = drawW, height = drawH;
                        if (this.Cursor == Cursors.SizeAll)//平移标志
                        {
                            float centerx = drawX;
                            float centery = drawY;

                            x += mouseX - centerx;
                            y += mouseY - centery;
                        }
                        else if (this.Cursor == Cursors.Hand)//角度旋转标志
                        {
                            if (e.Button == MouseButtons.Left)
                            {
                                mx = e.X;
                                my = e.Y;
                                angle = (float)(Math.Atan2(mouseY - selectshape.cy, mouseX - selectshape.cx) * 180 / Math.PI);
                                selectshape.angle = angle;
                            }

                        }
                        else if (this.Cursor == Cursors.PanNW)
                        {
                            width = (drawX - mouseX) * 2;
                            height = (drawY - mouseY) * 2;
                            if (width < 10)
                                width = 10;
                            if (height < 5)
                                height = 5;

                        }
                        else if (this.Cursor == Cursors.PanNE)
                        {
                            width = (mouseX - drawX) * 2;
                            height = (drawY - mouseY) * 2;
                            if (width < 10)
                                width = 10;
                            if (height < 5)
                                height = 5;
                        }
                        else if (this.Cursor == Cursors.PanSW)
                        {
                            width = (drawX - mouseX) * 2;
                            height = (mouseY - drawY) * 2;
                            if (width < 10)
                                width = 10;
                            if (height < 5)
                                height = 5;
                        }
                        else if (this.Cursor == Cursors.PanSE)
                        {
                            width = (mouseX - drawX) * 2;
                            height = (mouseY - drawY) * 2;
                            if (width < 10)
                                width = 10;
                            if (height < 5)
                                height = 5;

                        }
                        else if (this.Cursor == Cursors.PanWest)//左
                        {
                            double cosValue = Math.Cos(angle / 180 * Math.PI);

                            //     width = (float)drawW + (drawX - mouseX);
                            //     x = mouseX;

                            width = (drawX - mouseX) * 2;
                            if (width < 10)
                                width = 10;


                        }
                        else if (this.Cursor == Cursors.PanEast)
                        {
                            width = (mouseX - drawX) * 2;
                            if (width < 10)
                                width = 10;
                        }
                        else if (this.Cursor == Cursors.PanNorth)
                        {
                            //    y = mouseY;
                            //       height = drawH + drawY - mouseY;
                            height = (drawY - mouseY) * 2;
                            if (height < 5)
                                height = 5;

                        }
                        else if (this.Cursor == Cursors.PanSouth)
                        {
                            height = (mouseY - drawY) * 2;
                            if (height < 5)
                                height = 5;
                        }

                        //构建边界矩形
                        RotatedRectF rotatedRectF = new RotatedRectF(x, y, width, height, angle);
                        RectangleF rectangleF = rotatedRectF.getrect();
                        float leftX = rectangleF.X;
                        float rightX = rectangleF.X + rectangleF.Width;
                        float topY = rectangleF.Y;
                        float bottomY = rectangleF.Y + rectangleF.Height;
                        float minSize = imageWidth > imageHeight ? imageHeight : imageWidth;
                        //边界防护
                        if (leftX < 0)
                            x = 0 + rectangleF.Width / 2 + 10;
                        if (rightX >= imageWidth)
                            x = imageWidth - rectangleF.Width / 2 - 10;

                        if (topY <= 0)
                            y = 0 + rectangleF.Height / 2 + 10;
                        if (bottomY >= imageHeight)
                            y = imageHeight - rectangleF.Height / 2 - 10;
                        //边界防护2
                        if (rectangleF.Width < 50)
                        {
                            width = 50;
                            //x = rightX - width / 2;
                            //if (x < 25)
                            //    x = 25;
                        }
                        if (rectangleF.Height < 50)
                        {
                            height = 50;
                            ////y = bottomY - height / 2;
                            ////if (y < 25)
                            ////    y = 25;
                        }
                        //if (rectangleF.Width > minSize)
                        //{
                        //    width = minSize;
                        //    //     height= RR_limitHeight;
                        //    x = 0 + minSize / 2;
                        //    return;

                        //}
                        //if (rectangleF.Height > minSize)
                        //{
                        //    height = minSize;
                        //    y = 0 + minSize / 2;
                        //    return;
                        //}

                        selectshape = new RotatedCaliperRectF(x, y, width, height, angle);

                    }
                    else if (selectshape is CircleF)
                    {
                        float cx = selectshape.x1;
                        float cy = selectshape.y1;
                        float x2 = selectshape.x2;
                        float y2 = selectshape.y2;
                        float radius = selectshape.Radius;
                        if (this.Cursor == Cursors.Cross)//缩放
                        {
                            x2 = mouseX;
                            y2 = mouseY;
                            radius = (float)Math.Sqrt(Math.Pow(cx - x2, 2)
                                                         + Math.Pow(cy - y2, 2));
                        }
                        else if (this.Cursor == Cursors.SizeAll)//平移
                        {
                            //x2 += mouseX - cx;
                            //y2 += mouseY - cy;
                            cx = mouseX;
                            cy = mouseY;
                          
                        }
                                                   
                        //构建边界矩形
                        CircleF circleF = new CircleF(cx, cy, radius);
                        RectangleF rectangleF = circleF.boundaryRect;
                        float leftX = rectangleF.X;
                        float rightX = rectangleF.X + rectangleF.Width;
                        float topY = rectangleF.Y;
                        float bottomY = rectangleF.Y + rectangleF.Height;
                        float limitSize = imageWidth > imageHeight ? imageHeight : imageWidth;
                        //边界防护
                        if (leftX < 0)
                            cx = 0 + rectangleF.Width / 2;
                        if (rightX > imageWidth)
                            cx = imageWidth - rectangleF.Width / 2;
                        if (topY < 0)
                            cy = 0 + rectangleF.Height / 2;
                        if (bottomY > imageHeight)
                            cy = imageHeight - rectangleF.Height / 2;
                        if (rectangleF.Height > limitSize)
                        {
                            radius = limitSize / 2;
                            cy = 0 + limitSize / 2;
                        }
                        if(  rectangleF.Width> limitSize)
                        {
                            radius = limitSize / 2;
                            cx = 0 + limitSize / 2;
                        }

                        selectshape = new CircleF(cx, cy, radius);
                    }
                    else if (selectshape is SectorF)
                    {
                        float centreX = ((SectorF)selectshape).centreP.X;
                        float centreY = ((SectorF)selectshape).centreP.Y;
                        double R = ((SectorF)selectshape).getRadius;
                        float width = ((SectorF)selectshape).width;
                        float height = ((SectorF)selectshape).height;
                        float startA = ((SectorF)selectshape).startAngle;
                        float sweepA = ((SectorF)selectshape).sweepAngle;
                        float endA= ((SectorF)selectshape).getEndAngle;


                        if (this.Cursor == Cursors.Cross)//缩放
                        {
                            R = Math.Sqrt(Math.Pow(mouseX - centreX, 2)
                                 + Math.Pow(mouseY - centreY, 2));

                            width = height= (float)R * 2;
                        
                        }
                        else if (this.Cursor == Cursors.SizeAll)//平移
                        {
                            centreX = mouseX;
                            centreY = mouseY;
                        }
                        else if (this.Cursor == Cursors.PanEast)
                        {

                            if (mouseX >= centreX && mouseX-20 <= centreX + R)
                                if (mouseY >= centreY)
                                {
                                    double offsetX = mouseX - centreX;
                                    double offsetY = mouseY - centreY;
                                    double angle = Math.Atan(offsetY / offsetX) * 180 / Math.PI;
                                    startA = (float)angle;
                                }
                                else {
                                    double offsetX = mouseX - centreX;
                                    double offsetY = centreY - mouseY;
                                    double angle = Math.Atan(offsetY / offsetX) * 180 / Math.PI;
                                    startA = 360-(float)angle;
                                }
                            else if (mouseX <= centreX && mouseX+20 >= centreX - R)
                                if (mouseY >= centreY)
                                {
                                    double offsetX = centreX - mouseX;
                                    double offsetY = mouseY - centreY;
                                    double angle = Math.Atan(offsetY / offsetX) * 180 / Math.PI;
                                    startA =180- (float)angle;
                                }
                                else
                                {
                                    double offsetX = centreX - mouseX;
                                    double offsetY = centreY - mouseY;
                                    double angle = Math.Atan(offsetY / offsetX) * 180 / Math.PI;
                                    startA = (float)angle+180;
                                }
                        }
                        else if (this.Cursor == Cursors.PanWest)
                        {

                            if (mouseX >= centreX && mouseX-20 <= centreX + R)
                                if (mouseY >= centreY)
                                {
                                    double offsetX = mouseX - centreX;
                                    double offsetY = mouseY - centreY;
                                    double angle = Math.Atan(offsetY / offsetX) * 180 / Math.PI;
                                    endA = (float)angle;
                                }
                                else
                                {
                                    double offsetX = mouseX - centreX;
                                    double offsetY = centreY - mouseY;
                                    double angle = Math.Atan(offsetY / offsetX) * 180 / Math.PI;
                                    endA =360 - (float)angle;
                                }
                            else if (mouseX <= centreX && mouseX+20 >= centreX - R)
                                if (mouseY >= centreY)
                                {
                                    double offsetX = centreX - mouseX;
                                    double offsetY = mouseY - centreY;
                                    double angle = Math.Atan(offsetY / offsetX) * 180 / Math.PI;
                                    endA = 180 - (float)angle;
                                }
                                else
                                {
                                    double offsetX = centreX - mouseX;
                                    double offsetY = centreY - mouseY;
                                    double angle = Math.Atan(offsetY / offsetX) * 180 / Math.PI;
                                    endA = (float)angle+180;
                                }
                            sweepA = endA - startA;
                        }

                        //构建边界矩形
                        SectorF sectorF = new SectorF(new PointF(centreX, centreY), (float)R, startA, sweepA);
                        RectangleF rectangleF = sectorF.boundaryRect;
                        float leftX = rectangleF.X;
                        float rightX = rectangleF.X + rectangleF.Width;
                        float topY = rectangleF.Y;
                        float bottomY = rectangleF.Y + rectangleF.Height;
                        float limitSize = imageWidth > imageHeight ? imageHeight : imageWidth;
                        //边界防护
                        if (leftX < 0)
                            centreX = 0 + rectangleF.Width / 2;
                        if (rightX > imageWidth)
                            centreX = imageWidth - rectangleF.Width / 2;
                        if (topY < 0)
                            centreY = 0 + rectangleF.Height / 2;
                        if (bottomY > imageHeight)
                            centreY = imageHeight - rectangleF.Height / 2;
                        if (rectangleF.Height > limitSize)
                        {
                            R = limitSize / 2;
                            centreY = 0 + limitSize / 2;
                        }
                        if (rectangleF.Width > limitSize)
                        {
                            R = limitSize / 2;
                            centreX = 0 + limitSize / 2;
                        }
              
                        selectshape = new SectorF(new PointF(centreX, centreY), (float)R, startA, sweepA);
                    }

                    shapelist.RemoveAt(selectindex);
                    shapelist.Insert(selectindex, selectshape);//更新
                    updateImage();
                }
            }
        }

        //图像重新绘制
        private void PicBox_Paint(object sender, PaintEventArgs e)
        {
           // if (IsCamFreeModel) return;
            if (image == null)
            {
                string funcName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                v_log.Info(funcName, "图像为空，无法进行重新绘制！");
                return;
            }
            //    e.Graphics.Clear(Color.White);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            Monitor.Enter(locker);
            e.Graphics.DrawImage(image, drawimgrect, drawsrcrect, GraphicsUnit.Pixel);
            Monitor.Exit(locker);
            //通过api添加的区域重新绘制
            foreach (var r in regionExlist)
                e.Graphics.drawRegion(r, sizeratio);
            //通过api添加的文本重新绘制
            foreach (var t in textExlist)
                e.Graphics.drawText(t, sizeratio);
            //十字光标区域重新绘制
            foreach (var r in regionExlistOfCross)
                e.Graphics.drawRegion(r, sizeratio);


            if (isStartDrawPolygonF)//开始多边形绘制
            {
                foreach (var s in polyPoints)
                {
                    //e.Graphics.DrawEllipse(MyPens.Select, s.X, s.Y, 10, 10);
                    e.Graphics.FillEllipse(Brushes.Blue, (s.X - 5) / sizeratio, (s.Y - 5) / sizeratio, 10 / sizeratio, 10 / sizeratio);
                }

            }

            //创建的区域集合绘制
            for (int i = 0; i < shapelist?.Count; i++)
            {
                if (shapelist[i] is RectangleF)
                {
                    RectangleF rect = (RectangleF)shapelist[i];
                    //if (!rect.isSelected)
                    e.Graphics.DrawRectangle(MyPens.Default, rect.X / sizeratio, rect.Y / sizeratio, rect.Width / sizeratio, rect.Height / sizeratio);

                }
                else if (shapelist[i] is RotatedRectF)
                {
                    RotatedRectF rrect = (RotatedRectF)shapelist[i];
                    using (var graph = new GraphicsPath())
                    {
                        PointF Center = new PointF(rrect.cx / sizeratio, rrect.cy / sizeratio);

                        graph.AddRectangle(new RectangleF(rrect.getrectofangleEqualZero().X / sizeratio,
                              rrect.getrectofangleEqualZero().Y / sizeratio,
                                rrect.getrectofangleEqualZero().Width / sizeratio,
                                   rrect.getrectofangleEqualZero().Height / sizeratio));

                        graph.AddLine(new PointF((rrect.cx - rrect.Width / 2 - 20) / sizeratio,
                            rrect.cy / sizeratio),
                         new PointF((rrect.cx + rrect.Width / 2 + 20) / sizeratio, rrect.cy / sizeratio));

                        /////
                        RotatedRectF rotatedRectF = new RotatedRectF((rrect.cx + rrect.Width / 2 + 20) / sizeratio,
                            rrect.cy / sizeratio, 20 / sizeratio, 10 / sizeratio, 0);
                        PointF[] point2Fs = rotatedRectF.getPointF();
                        graph.AddLine(new PointF((rrect.cx + rrect.Width / 2 + 20) / sizeratio,
                            rrect.cy / sizeratio), new PointF(point2Fs[0].X, point2Fs[0].Y));
                        graph.AddLine(new PointF((rrect.cx + rrect.Width / 2 + 20) / sizeratio,
                           rrect.cy / sizeratio), new PointF(point2Fs[3].X, point2Fs[3].Y));
                        /////

                        graph.AddString(rrect.angle.ToString("F3"),
                            new Font("宋体", 12f).FontFamily,
                            (int)FontStyle.Regular,
                            20,
                            new PointF((rrect.cx + rrect.Width / 2 + 120) / sizeratio,
                            rrect.cy / sizeratio),
                            StringFormat.GenericDefault);

                        var a = rrect.angle * (Math.PI / 180);
                        var n1 = (float)Math.Cos(a);
                        var n2 = (float)Math.Sin(a);
                        var n3 = -(float)Math.Sin(a);
                        var n4 = (float)Math.Cos(a);
                        var n5 = (float)(Center.X * (1 - Math.Cos(a)) + Center.Y * Math.Sin(a));
                        var n6 = (float)(Center.Y * (1 - Math.Cos(a)) - Center.X * Math.Sin(a));
                        graph.Transform(new Matrix(n1, n2, n3, n4, n5, n6));
                        e.Graphics.DrawPath(MyPens.Default, graph);

                    }
                    //clipersRegions = gen_calipers_region(rrect,20,4);


                }
                else if (shapelist[i] is RotatedCaliperRectF)
                {
                    RotatedCaliperRectF rrect = (RotatedCaliperRectF)shapelist[i];
                    using (var graph = new GraphicsPath())
                    {
                        PointF Center = new PointF(rrect.cx / sizeratio, rrect.cy / sizeratio);

                        graph.AddRectangle(new RectangleF(rrect.getrectofangleEqualZero().X / sizeratio,
                              rrect.getrectofangleEqualZero().Y / sizeratio,
                                rrect.getrectofangleEqualZero().Width / sizeratio,
                                   rrect.getrectofangleEqualZero().Height / sizeratio));

                        graph.AddLine(new PointF((rrect.cx - rrect.Width / 2 - 20) / sizeratio,
                            rrect.cy / sizeratio),
                         new PointF((rrect.cx + rrect.Width / 2 + 20) / sizeratio, rrect.cy / sizeratio));

                        /////
                        RotatedCaliperRectF rotatedRectF = new RotatedCaliperRectF((rrect.cx + rrect.Width / 2 + 20) / sizeratio,
                            rrect.cy / sizeratio, 20 / sizeratio, 10 / sizeratio, 0);
                        PointF[] point2Fs = rotatedRectF.getPointF();
                        graph.AddLine(new PointF((rrect.cx + rrect.Width / 2 + 20) / sizeratio,
                            rrect.cy / sizeratio), new PointF(point2Fs[0].X, point2Fs[0].Y));
                        graph.AddLine(new PointF((rrect.cx + rrect.Width / 2 + 20) / sizeratio,
                           rrect.cy / sizeratio), new PointF(point2Fs[3].X, point2Fs[3].Y));
                        /////

                        graph.AddString(rrect.angle.ToString("F3"),
                            new Font("宋体", 12f).FontFamily,
                            (int)FontStyle.Regular,
                            20,
                            new PointF((rrect.cx + rrect.Width / 2 + 120) / sizeratio,
                            rrect.cy / sizeratio),
                            StringFormat.GenericDefault);

                        var a = rrect.angle * (Math.PI / 180);
                        var n1 = (float)Math.Cos(a);
                        var n2 = (float)Math.Sin(a);
                        var n3 = -(float)Math.Sin(a);
                        var n4 = (float)Math.Cos(a);
                        var n5 = (float)(Center.X * (1 - Math.Cos(a)) + Center.Y * Math.Sin(a));
                        var n6 = (float)(Center.Y * (1 - Math.Cos(a)) - Center.X * Math.Sin(a));
                        graph.Transform(new Matrix(n1, n2, n3, n4, n5, n6));
                        e.Graphics.DrawPath(MyPens.Default, graph);

                    }
                    //clipersRegions = gen_calipers_region(rrect,20,4);


                }
                else if (shapelist[i] is CircleF)
                {
                    CircleF circle = (CircleF)shapelist[i];
                    //if (circle.isSelected)
                    e.Graphics.DrawEllipse(MyPens.Default, (circle.Centerx - circle.Radius) / sizeratio,
                        (circle.Centery - circle.Radius) / sizeratio, 2 * circle.Radius / sizeratio, 2 * circle.Radius / sizeratio);

                }
                else if (shapelist[i] is SectorF)
                {
                    SectorF sectorF = (SectorF)shapelist[i];
                    //if (circle.isSelected)
                    //e.Graphics.DrawEllipse(MyPens.assist, sectorF.x / sizeratio, sectorF.y / sizeratio,
                    //   sectorF.width / sizeratio, sectorF.height / sizeratio);

                    e.Graphics.DrawPie(MyPens.Default, sectorF.x / sizeratio, sectorF.y / sizeratio,
                        sectorF.width / sizeratio, sectorF.height / sizeratio,
                    sectorF.startAngle, sectorF.sweepAngle);

                    /*-----*/
                    double centreX = sectorF.centreP.X;
                    double centreY = sectorF.centreP.Y;
                    double r = sectorF.getRadius;
                    double startA = sectorF.startAngle;
                    double sweepA = sectorF.sweepAngle;

                    //外径环
                    e.Graphics.DrawPie(MyPens.Default, sectorF.getOuterSector().x / sizeratio, sectorF.getOuterSector().y / sizeratio,
                                     sectorF.getOuterSector().width / sizeratio, sectorF.getOuterSector().height / sizeratio,
                                sectorF.getOuterSector().startAngle, sectorF.getOuterSector().sweepAngle);
                    //内径环
                    e.Graphics.DrawPie(MyPens.Default, sectorF.getInnerSector().x / sizeratio, sectorF.getInnerSector().y / sizeratio,
                                     sectorF.getInnerSector().width / sizeratio, sectorF.getInnerSector().height / sizeratio,
                                sectorF.getInnerSector().startAngle, sectorF.getInnerSector().sweepAngle);

                    //clipersRegions = gen_calipers_region(sectorF, 20, 4);
                }
                else if (shapelist[i] is PolygonF)
                {
                    PolygonF polygonF = (PolygonF)shapelist[i];

                    int num = polygonF.Points.Count;
                    List<PointF> temP = new List<PointF>();
                    if (num >= 3)
                    {
                        foreach (var s in polygonF.Points)
                            temP.Add(new PointF(s.X / sizeratio, s.Y / sizeratio));
                        //绘制轮廓
                        e.Graphics.DrawPolygon(Pens.Red, temP.ToArray());
                        //绘制顶点
                        foreach (PointF p in polygonF.Points)
                        {
                            e.Graphics.FillEllipse(Brushes.Blue, (p.X - 5) / sizeratio, (p.Y - 5) / sizeratio, 10 / sizeratio, 10 / sizeratio);
                        }
                    }
                }

            }
            //被选择的区域绘制
            if (selectshape != null)
            {
                if (selectshape is RectangleF)
                {

                    float centerx = selectshape.X + selectshape.Width / 2;
                    float centerY = selectshape.Y + selectshape.Height / 2;
                    e.Graphics.DrawLine(Penused, centerx / sizeratio - 20, centerY / sizeratio, centerx / sizeratio + 20,
                        centerY / sizeratio);
                    e.Graphics.DrawLine(Penused, centerx / sizeratio, centerY / sizeratio - 20, centerx / sizeratio,
                        centerY / sizeratio + 20);
                    e.Graphics.DrawRectangle(MyPens.Select, selectshape.X / sizeratio, selectshape.Y / sizeratio, selectshape.Width / sizeratio, selectshape.Height / sizeratio);

                }
                else if (selectshape is RotatedRectF)
                {
                    RotatedRectF rrect = (RotatedRectF)selectshape;
                    PointF Center = new PointF(rrect.cx / sizeratio, rrect.cy / sizeratio);

                    using (var graph = new GraphicsPath())
                    {

                        //graph.AddRectangle(new RectangleF(rrect.getrectofangleEqualZero().X / sizeratio,
                        //    rrect.getrectofangleEqualZero().Y / sizeratio, rrect.getrectofangleEqualZero().Width / sizeratio,
                        //    rrect.getrectofangleEqualZero().Height / sizeratio));
                        graph.AddLine(new PointF(rrect.cx / sizeratio, rrect.cy / sizeratio),
                         new PointF((rrect.cx + rrect.Width / 2 + 110) / sizeratio, rrect.cy / sizeratio));
                        graph.AddEllipse(new RectangleF((rrect.cx + rrect.Width / 2 + 100) / sizeratio,
                            (rrect.cy - 10) / sizeratio, 20 / sizeratio, 20 / sizeratio));


                        graph.AddString(rrect.angle.ToString("F3"),
                      new Font("宋体", 12f).FontFamily,
                      (int)FontStyle.Regular,
                      20,
                      new PointF((rrect.cx + rrect.Width / 2 + 120) / sizeratio,
                      rrect.cy / sizeratio),
                      StringFormat.GenericDefault);

                        var a = rrect.angle * (Math.PI / 180);
                        var n1 = (float)Math.Cos(a);
                        var n2 = (float)Math.Sin(a);
                        var n3 = -(float)Math.Sin(a);
                        var n4 = (float)Math.Cos(a);
                        var n5 = (float)(Center.X * (1 - Math.Cos(a)) + Center.Y * Math.Sin(a));
                        var n6 = (float)(Center.Y * (1 - Math.Cos(a)) - Center.X * Math.Sin(a));
                        graph.Transform(new Matrix(n1, n2, n3, n4, n5, n6));
                        e.Graphics.DrawPath(MyPens.Select, graph);

                    }
                    e.Graphics.DrawLine(Penused, Center.X - 10, Center.Y, Center.X + 10,
                Center.Y);
                    e.Graphics.DrawLine(Penused, Center.X, Center.Y - 10, Center.X,
                        Center.Y + 10);
                }
                else if (selectshape is RotatedCaliperRectF)
                {
                    RotatedCaliperRectF rrect = (RotatedCaliperRectF)selectshape;
                    PointF Center = new PointF(rrect.cx / sizeratio, rrect.cy / sizeratio);

                    using (var graph = new GraphicsPath())
                    {

                        //graph.AddRectangle(new RectangleF(rrect.getrectofangleEqualZero().X / sizeratio,
                        //    rrect.getrectofangleEqualZero().Y / sizeratio, rrect.getrectofangleEqualZero().Width / sizeratio,
                        //    rrect.getrectofangleEqualZero().Height / sizeratio));
                        graph.AddLine(new PointF(rrect.cx / sizeratio, rrect.cy / sizeratio),
                         new PointF((rrect.cx + rrect.Width / 2 + 110) / sizeratio, rrect.cy / sizeratio));
                        graph.AddEllipse(new RectangleF((rrect.cx + rrect.Width / 2 + 100) / sizeratio,
                            (rrect.cy - 10) / sizeratio, 20 / sizeratio, 20 / sizeratio));


                        graph.AddString(rrect.angle.ToString("F3"),
                      new Font("宋体", 12f).FontFamily,
                      (int)FontStyle.Regular,
                      20,
                      new PointF((rrect.cx + rrect.Width / 2 + 120) / sizeratio,
                      rrect.cy / sizeratio),
                      StringFormat.GenericDefault);

                        var a = rrect.angle * (Math.PI / 180);
                        var n1 = (float)Math.Cos(a);
                        var n2 = (float)Math.Sin(a);
                        var n3 = -(float)Math.Sin(a);
                        var n4 = (float)Math.Cos(a);
                        var n5 = (float)(Center.X * (1 - Math.Cos(a)) + Center.Y * Math.Sin(a));
                        var n6 = (float)(Center.Y * (1 - Math.Cos(a)) - Center.X * Math.Sin(a));
                        graph.Transform(new Matrix(n1, n2, n3, n4, n5, n6));
                        e.Graphics.DrawPath(MyPens.Select, graph);

                    }
                    e.Graphics.DrawLine(Penused, Center.X - 10, Center.Y, Center.X + 10,
                Center.Y);
                    e.Graphics.DrawLine(Penused, Center.X, Center.Y - 10, Center.X,
                        Center.Y + 10);
                }
                else if (selectshape is CircleF)
                {

                    e.Graphics.DrawLine(Penused, selectshape.Centerx / sizeratio - 20, selectshape.Centery / sizeratio, selectshape.Centerx / sizeratio + 20,
                   selectshape.Centery / sizeratio);
                    e.Graphics.DrawLine(Penused, selectshape.Centerx / sizeratio, selectshape.Centery / sizeratio - 20, selectshape.Centerx / sizeratio,
                        selectshape.Centery / sizeratio + 20);
                    e.Graphics.DrawEllipse(MyPens.Select, (selectshape.Centerx - selectshape.Radius) / sizeratio,
                       (selectshape.Centery - selectshape.Radius) / sizeratio, 2 * selectshape.Radius / sizeratio, 2 * selectshape.Radius / sizeratio);

                }
                else if (selectshape is SectorF)
                {

                    e.Graphics.DrawLine(Penused, selectshape.centreP.X / sizeratio - 20, selectshape.centreP.Y / sizeratio, selectshape.centreP.X / sizeratio + 20,
                   selectshape.centreP.Y / sizeratio);
                    e.Graphics.DrawLine(Penused, selectshape.centreP.X / sizeratio, selectshape.centreP.Y / sizeratio - 20, selectshape.centreP.X / sizeratio,
                        selectshape.centreP.Y / sizeratio + 20);

                    //e.Graphics.DrawEllipse(MyPens.assist, selectshape.x / sizeratio, selectshape.y / sizeratio,
                    // selectshape.width / sizeratio, selectshape.height / sizeratio);

                    e.Graphics.DrawPie(MyPens.Default, selectshape.x / sizeratio, selectshape.y / sizeratio,
                        selectshape.width / sizeratio, selectshape.height / sizeratio,
                    selectshape.startAngle, selectshape.sweepAngle);

                    e.Graphics.DrawRectangle(MyPens.Default, (selectshape.getEndpoint(selectshape.startAngle).X - 5) / sizeratio,
                                  (selectshape.getEndpoint(selectshape.startAngle).Y - 5) / sizeratio,
                                               10 / sizeratio, 10 / sizeratio);
                    e.Graphics.DrawString("1", new Font("宋体", 12), new SolidBrush(Color.Red),
                        new PointF((selectshape.getEndpoint(selectshape.startAngle).X + 20) / sizeratio,
                        (selectshape.getEndpoint(selectshape.startAngle).Y) / sizeratio));

                    e.Graphics.DrawRectangle(MyPens.Default, (selectshape.getEndpoint(selectshape.getEndAngle).X - 5) / sizeratio,
                                 (selectshape.getEndpoint(selectshape.getEndAngle).Y - 5) / sizeratio,
                                              10 / sizeratio, 10 / sizeratio);
                    e.Graphics.DrawString("2", new Font("宋体", 12), new SolidBrush(Color.Red),
                     new PointF((selectshape.getEndpoint(selectshape.getEndAngle).X + 20) / sizeratio,
                     (selectshape.getEndpoint(selectshape.getEndAngle).Y) / sizeratio));


                    /*--------------*/
                    //外径环
                    e.Graphics.DrawPie(MyPens.Default, selectshape.getOuterSector().x / sizeratio, selectshape.getOuterSector().y / sizeratio,
                                     selectshape.getOuterSector().width / sizeratio, selectshape.getOuterSector().height / sizeratio,
                                selectshape.getOuterSector().startAngle, selectshape.getOuterSector().sweepAngle);
                    //内径环
                    e.Graphics.DrawPie(MyPens.Default, selectshape.getInnerSector().x / sizeratio, selectshape.getInnerSector().y / sizeratio,
                                     selectshape.getInnerSector().width / sizeratio, selectshape.getInnerSector().height / sizeratio,
                                selectshape.getInnerSector().startAngle, selectshape.getInnerSector().sweepAngle);

                }
                else if (selectshape is PolygonF)//多边形
                {
                    int num = selectshape.Points.Count;
                    List<PointF> temP = new List<PointF>();
                    if (num >= 3)
                    {
                        foreach (var s in selectshape.Points)
                            temP.Add(new PointF(s.X / sizeratio, s.Y / sizeratio));
                        //绘制轮廓
                        e.Graphics.DrawPolygon(MyPens.Select, temP.ToArray());
                        //绘制顶点
                        foreach (PointF p in selectshape.Points)
                        {
                            e.Graphics.FillEllipse(Brushes.Red, (p.X - 5) / sizeratio, (p.Y - 5) / sizeratio, 10 / sizeratio, 10 / sizeratio);
                        }
                    }

                    e.Graphics.DrawLine(Penused, selectshape.centerx / sizeratio - 20, selectshape.centery / sizeratio, selectshape.centerx / sizeratio + 20,
                   selectshape.centery / sizeratio);
                    e.Graphics.DrawLine(Penused, selectshape.centerx / sizeratio, selectshape.centery / sizeratio - 20, selectshape.centerx / sizeratio,
                        selectshape.centery / sizeratio + 20);

                }

            }
            RoiChangedHandle?.Invoke(selectshape, e);

            if (calipersRegions?.Count > 0)
            {
                foreach (RotatedCaliperRectF s in calipersRegions)
                    e.Graphics.drawRegion(new RegionEx(s, Color.LimeGreen, 2), sizeratio);
            }

        }
        private void PicBox_DoubleClick(object sender, EventArgs e)
        {

            DoubleClickGetMousePosHandle2?.Invoke((int)mouseX, (int)mouseY);
        }

        //控件尺寸变更
        private void VisionShowControl_SizeChanged(object sender, EventArgs e)
        {
            fitSize();
        }
        /// <summary>
        /// 窗口大小的固定比例
        /// </summary>
        private float winratio;

        private void computeWratio()
        {
            if (image == null) return;
            float windowWH = (float)PanelBox.Width / PanelBox.Height;
            float imgWH = (float)image.Width / image.Height;

            if (windowWH > imgWH)
            {
                PicBox.Height = PanelBox.Height;
                PicBox.Width = (int)(PanelBox.Height * imgWH);
                //Canvas.Height = PanelBox.Height;
                //Canvas.Width = (int)(PanelBox.Height * imgWH);
            }
            else
            {
                PicBox.Width = PanelBox.Width;
                PicBox.Height = (int)(PanelBox.Width / imgWH);
                //Canvas.Width = PanelBox.Width;
                //Canvas.Height = (int)(PanelBox.Width / imgWH);
            }

            this.PicBox.Left = (int)(PanelBox.Width - PicBox.Width) / 2;
            this.PicBox.Top = (int)(PanelBox.Height - PicBox.Height) / 2;

            //this.Canvas.X = (int)(PanelBox.Width - Canvas.Width) / 2;
            //this.Canvas.Y = (int)(PanelBox.Height - Canvas.Height) / 2;

            winratio = (float)image.Width / PanelBox.Width;
            sizeratio = (float)image.Width / PicBox.Width;
            //sizeratio = (float)image.Width / Canvas.Width;
        }
        /// <summary>
        /// 绘制图像的位置和大小
        /// </summary>
        private RectangleF drawimgrect;
        /// <summary>
        /// image对象中要绘制的部分
        /// </summary>
        private RectangleF drawsrcrect;
        /// <summary>
        /// 图像的真实缩放比例
        /// </summary>
        private void computePicratio()
        {
            if (image == null) return;
            sizeratio = (float)image.Width / PicBox.Width;
            //sizeratio = (float)image.Width / Canvas.Width;
            computedrawsize();
        }

        private void computedrawsize()
        {
            if (PicBox.Left < 0)
            {
                //drawimgX = -1 * PicBox.Left;
                drawimgX = -1 * PicBox.Left;
            }
            else
            {
                drawimgX = 0;
            }
            if (PicBox.Top < 0)
            {
                //drawimgY = -1 * PicBox.Top;
                drawimgY = -1 * PicBox.Top;
            }
            else
            {
                drawimgY = 0;
            }
            if (PicBox.Right > PanelBox.Width)
            {
                //float x2 = (PicBox.Right - PanelBox.Width);
                //drawimgW = PanelBox.Width - x2;
                drawimgW = PanelBox.Width;
            }
            else
            {
                drawimgW = PicBox.Right;
            }
            if (PicBox.Bottom > PanelBox.Height)
            {
                //float y2 = (PicBox.Bottom - PanelBox.Height);
                //drawimgH = PanelBox.Height - y2;
                drawimgH = PanelBox.Height;
            }
            else
            {
                drawimgH = PicBox.Bottom;
            }

            drawimgrect = new RectangleF(drawimgX, drawimgY, drawimgW, drawimgH);
            drawsrcrect = new RectangleF(drawimgX * sizeratio, drawimgY * sizeratio, drawimgW * sizeratio,
                drawimgH * sizeratio);
        }

        /// <summary>
        /// 判断鼠标是否在父窗口内移动
        /// </summary>
        /// <returns></returns>
        private bool IsMouseInPanel()
        {

            if (this.PanelBox.Left < PointToClient(Cursor.Position).X
                    && PointToClient(Cursor.Position).X < this.PanelBox.Left
                    + this.PanelBox.Width && this.PanelBox.Top
                    < PointToClient(Cursor.Position).Y && PointToClient(Cursor.Position).Y
                    < this.PanelBox.Top + this.PanelBox.Height)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 绘制圆形
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDrawCircle_Click(object sender, EventArgs e)
        {
            if (this.image == null) return;
            isStartDrawPolygonF = false;
            清除overlayToolStripMenuItem_Click(null, null);
            float scaleFactor = this.image.Width / 1000f;         
            shapelist.Add(new CircleF(110 * scaleFactor, 110 * scaleFactor, 100 * scaleFactor));
            updateImage();
        }
    
        private void 自适应toolStripButton_Click(object sender, EventArgs e)
        {
            computeWratio();
            computePicratio();
        }

        public EventHandler 显示中心十字坐标Handle;
        public bool IsShowCenterCross { get; set; }
       
        private void 十字光标toolStripButton_Click(object sender, EventArgs e)
        {
            if (this.image == null) return;
            if(!IsShowCenterCross)
            {
                //int cx = this.image.Width / 2;
                //int cy = this.image.Height / 2;
                //int width = this.image.Width;
                //int height = this.image.Height;
                //RegionEx regionEx = new RegionEx(new CrossF(cx, cy, width, height, 0), Color.Red, 2);
                //DrawRegion(regionEx);
               // AddRegionBufferOfCross(regionEx);
                IsShowCenterCross = true;
                显示中心十字坐标Handle?.Invoke(null, null);
            }
            else
            {
                this.regionExlistOfCross.Clear();              
                IsShowCenterCross = false;         
                updateImage();
            }
           
           
        }
        /// <summary>
        /// 绘制矩形
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDrawRectangle1_Click(object sender, EventArgs e)
        {
            if (this.image == null)
            {
                string funName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                v_log.Info(funName,"矩形区域创建失败，图像为空！");
                return;
            }
            isStartDrawPolygonF = false;
            清除overlayToolStripMenuItem_Click(null,null);
            float scaleFactor = this.image.Width/1000f;
            shapelist.Add(new RectangleF(10* scaleFactor, 10 * scaleFactor, 
                          110 * scaleFactor, 110 * scaleFactor));

            updateImage();
        }

        private void 旋转矩形toolStripButton_Click(object sender, EventArgs e)
        {
            if (this.image == null) return;
            isStartDrawPolygonF = false;
            清除overlayToolStripMenuItem_Click(null, null);
            float scaleFactor = this.image.Width / 1000f;

            RotatedRectF rotatedRectF = new RotatedRectF(100 * scaleFactor, 100 * scaleFactor,
                          100 * scaleFactor, 100 * scaleFactor, 0);
            shapelist.Add(rotatedRectF);

            //clipersRegions = gen_calipers_region(rotatedRectF);
            updateImage();
        }
        private void 旋转卡尺矩toolStripButton_Click(object sender, EventArgs e)
        {
            if (this.image == null) return;
            isStartDrawPolygonF = false;
            清除overlayToolStripMenuItem_Click(null, null);
            float scaleFactor = this.image.Width / 1000f;

            RotatedCaliperRectF rotatedRectF = new RotatedCaliperRectF(100 * scaleFactor, 100 * scaleFactor,
                          100 * scaleFactor, 100 * scaleFactor, 0);
            shapelist.Add(rotatedRectF);

            updateImage();
        }
        private void 多边形toolStripButton_Click(object sender, EventArgs e)
        {
            if (this.image == null) return;
            isStartDrawPolygonF = false;
            清除overlayToolStripMenuItem_Click(null, null);
            isStartDrawPolygonF = true;
            polyPoints.Clear();
        }

        private void 扇形toolStripButton_Click(object sender, EventArgs e)
        {
            if (this.image == null) return;
            isStartDrawPolygonF = false;
            清除overlayToolStripMenuItem_Click(null, null);
            float scaleFactor = this.image.Width / 1000f;
            shapelist.Add(new SectorF(new PointF(110 * scaleFactor, 110 * scaleFactor), 100 * scaleFactor, 0, 90));
            updateImage();
        }


        #region 右键菜单   
        public EventHandler LoadedImageNoticeHandle;
        private void 加载图片ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog m_OpenFileDialog = new OpenFileDialog();
            m_OpenFileDialog.Multiselect = true;
            m_OpenFileDialog.Filter = "JPEG文件,BMP文件|*.jpg*;*.bmp*|所有文件(*.*)|*.*";
            if (m_OpenFileDialog.ShowDialog() == DialogResult.OK)
            {
             clearAll();
     
                image = new Bitmap(m_OpenFileDialog.FileName);
                imageWidth = image.Width;
                imageHeight = image.Height;
                computeWratio();
                computePicratio();
                updateImage();
                //dispImage(image);
                LoadedImageNoticeHandle?.Invoke(null,null);
            }
        }
        private void 保存图片ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (image == null) return;

            SaveFileDialog m_SaveFileDialog = new SaveFileDialog();
            m_SaveFileDialog.Filter = "JPEG文件|*.jpg*|BMP文件|*.bmp*";
            m_SaveFileDialog.DereferenceLinks = true;

        
            if (m_SaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string tembuf = m_SaveFileDialog.FilterIndex == 1 ? ".jpg" : ".bmp";
                string name = m_SaveFileDialog.FileName;
                string tempath = string.Concat(name, tembuf);
                ThreadPool.QueueUserWorkItem((s) =>
                {
                    if (this.image != null)
                    {
                        Monitor.Enter(locker);
                        savebuf = BitmapToGrayMat(this.image);
                        Monitor.Exit(locker);
                        //this.image.Save(tempath, m_SaveFileDialog.FilterIndex == 1 ? ImageFormat.Jpeg: ImageFormat.Bmp);
                        savebuf.ImWrite(tempath);               
                    
                      
                    }                     
                });
            
            }        
        }
        public EventHandler ClearOverlayHandle;
        private void 清除overlayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.shapelist.Clear();
            this.regionExlist.Clear();
            this.textExlist.Clear();
            //this.regionExlistOfCross.Clear();
            this.calipersRegions.Clear();
            //IsShowCenterCross = false;
            selectshape = null;
            this.Cursor = Cursors.Default;
             updateImage();
            ClearOverlayHandle?.Invoke(null,null);
        }
        #endregion

        #region public method
    
        /// <summary>
        /// 获取相应的卡尺区域
        /// </summary>
        /// <param name="calipersWidth">卡尺宽度</param>
        /// <param name="calipersNum">卡尺数量</param>
        /// <param name="eumCalipersType">卡尺类型</param>
        /// <returns></returns>
        public List<RotatedCaliperRectF> GetCalipersRegions(float calipersWidth , int calipersNum,
            EumCalipersType  eumCalipersType= EumCalipersType.linear,
                     EumDirectionOfCircle dir= EumDirectionOfCircle.Outer)
        {
            calipersRegions.Clear();
            //创建的区域集合绘制
            for (int i = 0; i < shapelist?.Count; i++)
            {
                if(eumCalipersType== EumCalipersType.linear)//获取直线型卡尺区域
                {
                    if (shapelist[i] is RotatedCaliperRectF)
                    {
                        RotatedCaliperRectF rrect = (RotatedCaliperRectF)shapelist[i];
                        calipersRegions = gen_calipers_region(rrect, calipersWidth, calipersNum);
                        break;
                    }
                }
               else if (eumCalipersType == EumCalipersType.sector)//获取扇形卡尺区域
                {
                    if (shapelist[i] is SectorF)
                    {
                        SectorF sectorF = (SectorF)shapelist[i];
                        calipersRegions = gen_calipers_region(sectorF, calipersWidth, calipersNum, dir);
                        break;
                    }
                }
                else//获取圆形卡尺区域
                {
                    if (shapelist[i] is CircleF)
                    {
                        CircleF circleF = (CircleF)shapelist[i];
                        calipersRegions = gen_calipers_region(circleF, calipersWidth, calipersNum);
                        break;
                    }
                }          
            }
            return calipersRegions;
        }
       static  public List<RotatedCaliperRectF> GetLinearCalipersRegions(RotatedCaliperRectF rotatedRectF,
                                                               float calipersWidth, int calipersNum)
        {           
            return  gen_calipers_region(rotatedRectF, calipersWidth, calipersNum);
        }
        static public List<RotatedCaliperRectF> GetCircularCalipersRegions(SectorF  sectorF,
                                                              float calipersWidth, 
                                                              int calipersNum,
                                                               EumDirectionOfCircle dir )
        {
            return gen_calipers_region(sectorF, calipersWidth, calipersNum, dir);
        }

        static public List<RotatedCaliperRectF> GetCircularCalipersRegions(CircleF circleF,
                                                             float calipersWidth, int calipersNum)
        {
            return gen_calipers_region(circleF, calipersWidth, calipersNum);
        }
        /// <summary>
        /// bitmap转gray mat
        /// </summary>
        /// <param name="bitmap">输入bitmap</param>
        /// <returns></returns>
        public static Mat BitmapToGrayMat(System.Drawing.Bitmap bitmap)
        {
            Mat mat = BitmapConverter.ToMat(bitmap);
            if (mat.Channels() == 3)
            {
                Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2GRAY);
            }
            else if (mat.Channels() == 4)
            {
                Cv2.CvtColor(mat, mat, ColorConversionCodes.BGRA2GRAY);
            }
            return mat;
        }
        /// <summary>
        /// 获取绘制ROI集合
        /// </summary>
        /// <returns></returns>
        public List<object> getRoiList()
        {
            return this.shapelist;
        }
        /// <summary>
        /// 获取T类型区域集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> getRoiList<T>()
        {
            List<T> temlist = new List<T>();
            foreach (var s in shapelist)
                if (s is T)
                    temlist.Add((T)s);
            return temlist;        
        }
        /// <summary>
        /// 窗体自适应
        /// </summary>
        public void fitSize()
        {
            computeWratio();
            computePicratio();
        }
        /// <summary>
        /// 图像刷新
        /// </summary>
        public void updateImage()
        {
            if (PicBox.InvokeRequired)
                this.PicBox.Invoke(new Action(updateImage));
            else
                PicBox.Refresh(); 
        }
        /// <summary>
        /// 窗体清除
        /// </summary>
        public void clearAll()
        {        
            //if (this.image != null)
            //{
            //    this.image.Dispose();
            //    this.image = null;
            //}              
            this.shapelist.Clear();
            this.regionExlist.Clear();
            this.textExlist.Clear();
            calipersRegions.Clear();
            selectshape = null;
            //updateImage();
        }

        /// <summary>
        /// 清除图像覆盖
        /// </summary>
        public void clearOverlay()
        {
            this.shapelist.Clear();
            this.regionExlist.Clear();
            this.textExlist.Clear();
            //IsShowCenterCross = false;
            selectshape = null;
            updateImage();
        }

       /// <summary>
       /// 显示图像
       /// </summary>
       /// <param name="img"></param>
        public void dispImage(Mat img)
        {
      
            if (img == null || img.Empty() || img.Rows <= 0)
                return;
       
            IsCamFreeModel = false;
            img.CopyTo(savebuf);
            Monitor.Enter(locker);
            this.image = BitmapConverter.ToBitmap(savebuf);
            Monitor.Exit(locker);

            if (imageWidth!= img.Width|| imageHeight!= img.Height)
                fitSize();
            updateImage();
            imageWidth = img.Width;
            imageHeight = img.Height;
        }
    //public  static   Stopwatch sw = new Stopwatch();
        /// <summary>
        /// 显示图像
        /// </summary>
        /// <param name="img"></param>
        public void dispImage(Bitmap img)
        {
            //sw.Restart();
            if (img == null ||  img.Width <= 0)
                return;
            IsCamFreeModel = true;
            Monitor.Enter(locker);
            // savebuf = BitmapToGrayMat(img);
            //this.image = DeepClone(img);

       
            this.image = img.Clone(new Rectangle(0, 0, img.Width, img.Height), img.PixelFormat);
            //  this.image = BitmapConverter.ToBitmap(savebuf);
            Monitor.Exit(locker);
    

            if (imageWidth != this.image.Width || imageHeight != this.image.Height)
                fitSize();

              updateImage();
        
                //Bitmap bitmapOld = PicBox.Image as Bitmap;
                //// Provide the display control with the new bitmap. This action automatically updates the display.
                //PicBox.Image = this.image;
                //if (bitmapOld != null)
                //{
                //    // Dispose the bitmap.
                //    bitmapOld.Dispose();
                //}
    
            imageWidth = img.Width;
            imageHeight = img.Height;

            //sw.Stop();
            //Console.WriteLine(sw.ElapsedMilliseconds);

        }
        /// <summary>
        /// 图像深度复制
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        static public Bitmap DeepClone(Bitmap bitmap)
        {
            Bitmap dstBitmap = null;
            using (MemoryStream mStream = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(mStream, bitmap);
                mStream.Seek(0, SeekOrigin.Begin);//指定当前流的位置为流的开头。
                dstBitmap = (Bitmap)bf.Deserialize(mStream);
                mStream.Close();
            }
            return dstBitmap;
        }
          
    /// <summary>
    /// 绘制区域
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="regionEx"></param>
       public void DrawRegion(RegionEx regionEx)
        {
            if (this.image == null || this.image.Width <= 0 || this.image.Height <= 0)
                return;

            Graphics g = PicBox.CreateGraphics();
            g.drawRegion(regionEx, sizeratio);
            //regionExlist.Add(regionEx);
            //updateImage();
        }
        /// <summary>
        /// 添加到缓存集合
        /// </summary>
        /// <param name="regionEx"></param>
        public void AddRegionBuffer(RegionEx regionEx)
        {
            regionExlist.Add(regionEx);
        }
        /// <summary>
        /// 单独给十字光标使用
        /// </summary>
        /// <param name="regionEx"></param>
        public void AddRegionBufferOfCross(RegionEx regionEx)
        {
            //regionExlistOfCross.Clear();
            regionExlistOfCross.Add(regionEx);
        }

        private void PicBox_MouseLeave(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void toolStrip1_MouseEnter(object sender, EventArgs e)
        {
            this.toolStrip1.Focus();
        }

        /// <summary>
        /// 绘制文本
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="textEx"></param>
        public void DrawText(TextEx textEx)
        {
            if (this.image == null|| this.image.Width<=0|| this.image.Height<=0)
                return;
           
            Graphics g = PicBox.CreateGraphics();
            g.drawText(textEx, sizeratio);
            //textExlist.Add(textEx);
            //updateImage();
        }
        /// <summary>
        /// 添加到缓存集合
        /// </summary>
        /// <param name="textEx"></param>
        public void AddTextBuffer(TextEx textEx)
        {
            textExlist.Add(textEx);

        }

        /// <summary>
        /// 生成旋转矩形卡尺区域
        /// </summary>
        /// <param name="rotatedRectF"></param>
        /// <param name="calipersNum"></param>
       static    private  List<RotatedCaliperRectF> gen_calipers_region(RotatedCaliperRectF rotatedRectF, float calipersWidth = 30, int calipersNum = 2)
        {
            List<RotatedCaliperRectF> rotatedRectFs = new List<RotatedCaliperRectF>();
            float width = rotatedRectF.size.Width;
            float height = rotatedRectF.size.Height;
            float angle = rotatedRectF.angle;
            while (angle < 0) angle += 360;
            if (angle > 360) angle %= 360;
            float temang = angle + 90;
            while (temang < 0) temang += 360;
            if (temang > 360) temang %= 360;
            //旋转矩形顶点坐标
            PointF[] pointFs = rotatedRectF.getPointF();
            PointF A = pointFs[0];
            PointF B = pointFs[1];
            PointF C = pointFs[2];
            PointF D = pointFs[3];

            PointF p1 = new PointF((A.X + D.X) / 2, (A.Y + D.Y) / 2);
            PointF p2 = new PointF((B.X + C.X) / 2, (B.Y + C.Y) / 2);
            for (int i = 0; i < calipersNum; i++)
            {
                float x = p1.X + (p2.X - p1.X) / (calipersNum * 2) * (2 * i + 1);
                float y = p1.Y + (p2.Y - p1.Y) / (calipersNum * 2) * (2 * i + 1);
                RotatedCaliperRectF rotatedRectF1 = new RotatedCaliperRectF(x,y, height, calipersWidth, temang);
                rotatedRectFs.Add(rotatedRectF1);
            }
            return rotatedRectFs;
        }

        /// <summary>
        /// 生成扇形卡尺区域
        /// </summary>
        /// <param name="sectorF"></param>
        /// <param name="calipersWidth"></param>
        /// <param name="calipersNum"></param>
        /// <returns></returns>
        static private List<RotatedCaliperRectF> gen_calipers_region(SectorF sectorF, float calipersWidth = 30, 
            int calipersNum=4, EumDirectionOfCircle dir= EumDirectionOfCircle.Outer)
        {
            List<RotatedCaliperRectF> rotatedRectFs = new List<RotatedCaliperRectF>();
            float cenPx = sectorF.centreP.X;
            float cenPy = sectorF.centreP.Y;
            float radius = (float)sectorF.getRadius;
            SectorF inner = sectorF.getInnerSector();
            SectorF outer = sectorF.getOuterSector();
            double height = outer.getRadius - inner.getRadius;
            float startA=  sectorF.startAngle;
            float endA = sectorF.getEndAngle;
            float offsetA = 0;
            if ( Math.Abs(Math.Abs(sectorF.sweepAngle) - 360)<=1)//起点和终点重合,重合度允许1度的误差
                offsetA = 360 / calipersNum;
            else
                offsetA = sectorF.sweepAngle / (calipersNum - 1);//可正可负，依据起点与终点的位置来定

            //获取圆周上的左边点作为卡尺工具的中心
            //x = cx + r * cos(a)
            //y = cy + r * sin(a)
         
            for(int i=0;i< calipersNum;i++)
            {
                float angle = startA + offsetA * i;               
                double rad = angle / 180 * Math.PI;
                float cx = (float)(cenPx + radius * Math.Cos(-rad));
                float cy = (float)(cenPy - radius * Math.Sin(-rad));
                if (dir == EumDirectionOfCircle.Inner)
                {
                    angle =  angle-180;
                    rad = angle / 180 * Math.PI;
                     cx = (float)(cenPx - radius * Math.Cos(-rad));
                     cy = (float)(cenPy + radius * Math.Sin(-rad));
                }
                   
                RotatedCaliperRectF rotatedRectF1 = new RotatedCaliperRectF(cx, cy, (float)height,
                    calipersWidth, angle);
                rotatedRectFs.Add(rotatedRectF1);
            }
            return rotatedRectFs;
        }
        /// <summary>
        /// 圆形卡尺区域
        /// </summary>
        /// <param name="circleF"></param>
        /// <param name="calipersWidth"></param>
        /// <param name="calipersNum"></param>
        /// <returns></returns>
        static private List<RotatedCaliperRectF> gen_calipers_region(CircleF circleF, float calipersWidth = 30, int calipersNum = 4)
                                                                 
        {
            List<RotatedCaliperRectF> rotatedRectFs = new List<RotatedCaliperRectF>();
            float cenPx = circleF.Centerx;
            float cenPy = circleF.Centery;
            float radius = (float)circleF.Radius;
                        
            float offsetA = 360 / calipersNum;

            //获取圆周上的左边点作为卡尺工具的中心
            //x = cx + r * cos(a)
            //y = cy + r * sin(a)

            for (int i = 0; i < calipersNum; i++)
            {
                float angle = 0 + offsetA * i;
                double rad = angle / 180 * Math.PI;
                float cx = (float)(cenPx + radius * Math.Cos(-rad));
                float cy = (float)(cenPy - radius * Math.Sin(-rad));
                RotatedCaliperRectF rotatedRectF1 = new RotatedCaliperRectF(cx, cy, calipersWidth*3,
                    calipersWidth, angle);
                rotatedRectFs.Add(rotatedRectF1);
            }
            return rotatedRectFs;
        }

        
        #endregion


    }
    /// <summary>
    /// 画笔
    /// </summary>
    public static class MyPens
    {
        public static Pen Default = new Pen(Color.Red, 1);
        public static Pen Select = new Pen(Color.Blue, 1);

        public static Pen assist = new Pen(Color.Green, 1) { DashStyle= DashStyle.Dash};

    }

    /// <summary>
    /// 卡尺类型
    /// </summary>
    public enum EumCalipersType
    {
        /// <summary>
        ///线性
        /// </summary>
        linear,
        /// <summary>
        /// 扇形
        /// </summary>
        sector,
        /// <summary>
        /// 圆形
        /// </summary>
        circular
    }
    /// <summary>
    /// 找圆方向
    /// </summary>
    public enum EumDirectionOfCircle
    {
        Outer,
        Inner
    }
}
