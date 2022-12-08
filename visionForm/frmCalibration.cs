using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FilesRAW.Common;
using System.Net.Sockets;
using DeviceLib.Cam;
using System.IO;
using System.Runtime.InteropServices;
using OSLog;
using System.Reflection;
using System.Threading;
using UIDesign;
using System.Diagnostics;
using VisionShowLib;
using DeviceLib;
using FuncToolLib;
using ParamDataLib;
using FuncToolLib.Calibration;
using FuncToolLib.Location;
using ParamDataLib.Location;
using FuncToolLib.GlueCheck;


using OpenCvSharp;
using OpenCvSharp.Extensions;
using CVPoint = OpenCvSharp.Point;
using CVRect = OpenCvSharp.Rect;
using CVRRect = OpenCvSharp.RotatedRect;
using CVCircle = OpenCvSharp.CircleSegment;
using Rect = System.Drawing.RectangleF;
using Point = System.Drawing.PointF;
using FuncToolLib.Morphology;
using FuncToolLib.Enhancement;

namespace visionForm
{
    /// <summary>
    /// 手自动标定模式
    /// </summary>
    public partial class frmCalibration : Form
    {
        /// <summary>
        /// 将改代码放置在父界面即可重载父界面及其子界面的双缓冲机制
        /// </summary>
        //protected override CreateParams CreateParams
        //{
        //    get
        //    {
        //        CreateParams cp = base.CreateParams;
        //        cp.ExStyle |= 0x02000000;
        //        return cp;
        //    }
        //}

        [DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();
        [DllImport("kernel32.dll")]
        public static extern Boolean FreeConsole();

        /*--------------------------------------------------------------------------------*/
        public EventHandler setCalCentreHandle;//参数保存返回事件,供外部订阅可获取旋转中心
        public EventHandler setUserPointHandle;//参数保存返回事件,供外部订阅可获取用户坐标
        public EventHandler setModelPointHandle;//模板匹配排序点位数据
        public EventHandler AutoFocusDataHandle;//自动对焦事件


        public EventHandler setProductAngleDataHandle;//产品倾斜弧度数据
        public EventHandler CamParmasChangeHandle;//参数保存返回事件,供外部订阅可获相机参数
        public EventHandler CamConnectStatusHandle;//相机链接状态时间
        public EventHandler RobotTeachDataHandle = null;//参数保存返回事件,供外部订阅可获工具示教相关参数
        public EventHandler SaveCaliParmaHandleOfNightPoint = null;//参数保存返回事件,供外部订阅可获取坐标系变换矩阵
        public EventHandler SaveMarkProductPointsHandle = null;//参数保存返回事件,供外部订阅可获取产品排序相关参数
        public EventHandler SaveTCPparmasHandle = null;//参数保存返回事件,供外部订阅可获取TCP连接相关参数
        public EventHandler SaveModelparmasHandle = null;//参数保存返回事件,供外部订阅可获取模板匹配相关参数

        //////////////////////////
        public delegate void CamContinueGrabHandle(bool isGrabing);
        public CamContinueGrabHandle camContinueGrabHandle;
        int i = 0, j = 0, k = 0, m = 0;
        int CheckBoxselectID = -1;

        // 双击获取像素坐标
        public OutPointGray DoubleClickGetMousePosHandle;
        /*------------------------------------标定数据--------------------------------*/
        pixelPointDataClass d_pixelPointDataClass = null;
        robotPointDataClass d_robotPointDataClass = null;
        converCoorditionDataClass d_converCoorditionDataClass = null;
        RotatePointDataClass d_RotatePointDataClass = null;
        RotateCentrePointDataClass d_RotateCentrePointDataClass = null;

        ////图像显示控件
        VisionShowControl currvisiontool = null;
        //当前采集图像
        Mat GrabImg = null;
        //日志
        Log log = new Log("视觉");
        Log glueLog = new Log("胶水检测结果");

        string modelOrigion = "0,0,0"; //产品模板基准点
        MatchBaseInfo matchBaseInfo = null;//基准轮廓信息
        static object locker1 = new object();
        string CurrRecipeName = string.Empty;//当前配方名称
        /*-----------------------------------相机---------------------------------------------*/
        Icam CurrCam = null;  //相机接口
        CamType currCamType = CamType.NONE;
      public  EunmcurrCamWorkStatus workstatus = EunmcurrCamWorkStatus.None;//当前相机工作状态
        //标定单点相机检测状态
        Dictionary<int, bool> NinePointStatusDic = new Dictionary<int, bool>();
        //标定单点相机检测状态
        Dictionary<int, bool> RotatoStatusDic = new Dictionary<int, bool>();
        /*-----------------------------------工具和参数--------------------------------------------*/
        PreToolDataClass d_preToolDataClass = null;
        IRunTool runTool = null;
        ParmasDataBase parmaData = null;
        Result ResultOfToolRun;
        Mat Hom_mat2d, Hom_mat2d_inv;
        List<Point2d> pixelList = new List<Point2d>();
        List<Point2d> robotList = new List<Point2d>();
        /*-----------------------------------工具结果--------------------------------------------*/
        //直线1
        StuLineResultData StuLineResultData = new StuLineResultData(false);
        double line1AngleLx = 0, line2AngleLx = 0;
        //直线2
        StuLineResultData StuLineResultData2 = new StuLineResultData(false);
        //模板
        StuModelMatchData stuModelMatchData = new StuModelMatchData(false);
        //圆
        StuCircleResultData stuCircleResultData = new StuCircleResultData(false);
        //Blob
        StuBlobResultData stuBlobResultData = new StuBlobResultData(false);
        //自动圆匹配
        OutPutDataOfCircleMatch outPutDataOfCircleMatch = new OutPutDataOfCircleMatch();

        /*-----------------------------------窗体---------------------------------------------*/
        Dictionary<string, TabPage> TabPages = new Dictionary<string, TabPage>();

        frmrecipe mp = null;
        static frmCalibration _frmCalibration2 = null;
        /*----------------------------------检测区域----------------------------------------------*/
        private RectangleF RegionaRect;  //矩形
        private RotatedCaliperRectF temBuffRegionRRect; //旋转卡尺矩
        private RotatedCaliperRectF RegionRRect;//找直线使用
        private RotatedCaliperRectF RegionRRect2;//找直线2使用
        private RotatedRectF temBuffRegionRRect2; //旋转矩形
        private RotatedRectF RegionRRect3;//Blob使用    

        private SectorF sectorF; //找圆使用
        private CircleF circleF; //找圆使用
        //定位工具搜索区域
        public List<object> SearchROI1 = new List<object>();//模板P1
        public List<object> SearchROI2 = new List<object>();//模板P2
        public List<object> SearchROI3 = new List<object>();//模板胶水
        /*--------------------------------------------------------------------------------*/
        //工具集合
        List<string> toolList = new List<string>();
        //当前模板类型
        EumModelType currModelType;
        //模板
        Mat modeltp = new Mat();
        Mat modelGrayMat = new Mat();
        Mat cannyMat = new Mat();
        CVPoint[] templateContour = default;
        double coutourLen = 0; double contourArea = 0; double modelangle = 0;
        //模板创建区域
        RectangleF setModelROIData;

        /*----以下文件需要随着模板文件保存，且需要一一对应---------*/
        string CirleMatchconfigPath = "圆匹配参数.ini";
        string Line1configPath = "直线1参数.ini";
        string Line2configPath = "直线2参数.ini";
        string CircleconfigPath = "圆参数.ini";
        string BlobconfigPath = "Blob参数.ini";
        string inspectToolPath = "附加检测工具.searchroi";
        /*----------------------------------------------------------------*/
        //表格数据
        public List<DatagridOfRegionInfo> RegionInfoCollection = new List<DatagridOfRegionInfo>();
        //表格数据
        public List<DatagridOfGlueCheckInfo> GlueCheckInfoCollection = new List<DatagridOfGlueCheckInfo>();
        //区域相关参数集合
        public List<RegionParamArray> regionParamArrayList = new List<RegionParamArray>();
        //当前胶水检测设定区域->多边形
        //当前胶水检测设定区域
        List<PointF> currGlueSetRegion = new List<Point>();
        int GlueStep = -1; int resultID = 0;
        //胶水检测参数
        public GlueCheckParam glueCheckParam = new GlueCheckParam();

        bool IsOpenAllFunctions = true;
        /*--------------------------------------------------------------------------------*/
        #region --------Construction-----------

        private frmCalibration()
        {

            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Config"))
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Config");

            InitializeComponent();

            listViewPixel.Columns[1].Width = (listViewPixel.Width - 30) / 2;
            listViewPixel.Columns[2].Width = (listViewPixel.Width - 30) / 2;
            listViewRobot.Columns[1].Width = (listViewRobot.Width - 30) / 2;
            listViewRobot.Columns[2].Width = (listViewRobot.Width - 30) / 2;
            currvisiontool = new VisionShowControl();
            currvisiontool.Dock = DockStyle.Fill;
            currvisiontool.Padding = new Padding(2);
            currvisiontool.LoadedImageNoticeHandle += new EventHandler(LoadedImageNoticeEvent);
            currvisiontool.显示中心十字坐标Handle += new EventHandler(显示中心十字坐标Event);
            currvisiontool.RoiChangedHandle += new EventHandler(RoiChangedEvent);
            currvisiontool.DoubleClickGetMousePosHandle2 += new OutPointGray(DoubleClickGetMousePosEvent2);
            this.uiPanel2.Controls.Clear();
            this.uiPanel2.Controls.Add(currvisiontool);

            listViewFlow.Columns[0].Width = 40;
            listViewFlow.Columns[1].Width = listViewFlow.Width - 40;

            foreach (var s in LinesIntersectPanel.Controls)
            {
                if (s is NumericUpDown)
                    (s as NumericUpDown).MouseWheel += new MouseEventHandler(Num_DiscountAmount_MouseWheel);
                if (s is ComboBox)
                    (s as ComboBox).MouseWheel += new MouseEventHandler(Num_DiscountAmount_MouseWheel);
            }
            foreach (var s in FindCirclePanel.Controls)
            {
                if (s is NumericUpDown)
                    (s as NumericUpDown).MouseWheel += new MouseEventHandler(Num_DiscountAmount_MouseWheel);
                if (s is ComboBox)
                    (s as ComboBox).MouseWheel += new MouseEventHandler(Num_DiscountAmount_MouseWheel);
            }
            foreach (var s in BlobCentrePanel.Controls)
            {
                if (s is NumericUpDown)
                    (s as NumericUpDown).MouseWheel += new MouseEventHandler(Num_DiscountAmount_MouseWheel);
            }
            foreach (var s in ModelMactPanel.Controls)
            {
                if (s is NumericUpDown)
                    (s as NumericUpDown).MouseWheel += new MouseEventHandler(Num_DiscountAmount_MouseWheel);
            }

            cobxModelType.MouseWheel += new MouseEventHandler(Num_DiscountAmount_MouseWheel);


            //-----------------DLL库
            virtualConnect = new VirtualConnect("虚拟连接A");
            BuiltConnect();
            setOperationAuthority();

            foreach (TabPage s in uiTabControl1.TabPages)
                TabPages.Add(s.Text, s);
            setStyle(IsOpenAllFunctions, false);
            uiTabControl1.TabBackColor = Color.FromArgb(255, 109, 60);
            uiTabControl1.TabSelectedColor = Color.White;
            uiTabControl1.FillColor = Color.FromArgb(255, 255, 255);
            uiTabControl1.TabSelectedForeColor = Color.FromArgb(255, 109, 60);
            uiTabControl1.TabSelectedHighColor = Color.White;

        }
        public static frmCalibration CreateInstance()
        {
            if (_frmCalibration2 == null)
                _frmCalibration2 = new frmCalibration();
            return _frmCalibration2;
        }

        #endregion

        #region ---------Property--------------
        /// <summary>
        /// 相机是否在线状态
        /// </summary>
        public bool IsCamAlive
        {
            get
            {
                if (CurrCam == null)
                    return false;
                return this.CurrCam.IsAlive;
            }

        }

        /// <summary>
        /// 窗体样式
        /// </summary>
        [DefaultValue(FormBorderStyle.None)]
        public FormBorderStyle CalibFormBorderStyle
        {
            get;
            set;
        }
        /// <summary>
        /// 是否启用窗体隐藏
        /// </summary>
        [DefaultValue(false)]
        public bool shouldHide
        {
            get;
            set;

        }


        #endregion

        #region---------Menu菜单项------------
        private void listViewFlow_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            listViewFlow.FullRowSelect = true;


            if (this.listViewFlow.SelectedItems.Count > 0)
            {

                //listViewFlow.SelectedItems[0].SubItems[0].ForeColor = Color.Blue;

                //先清除原有格式

                foreach (ListViewItem item in listViewFlow.Items)
                {
                    item.ForeColor = Color.Black;
                }
                foreach (ListViewItem item in listViewFlow.Items)
                {
                    //item.BackColor = System.Drawing.SystemColors.ControlLight; 
                    item.BackColor = Color.White;
                    Font f = new Font(Control.DefaultFont, FontStyle.Regular);
                    item.Font = f;
                }

                //加粗字体
                Font f2 = new Font(Control.DefaultFont, FontStyle.Bold);
                listViewFlow.SelectedItems[0].SubItems[0].Font = f2;
                //设置选中行背景颜色
                listViewFlow.SelectedItems[0].BackColor = Color.FromArgb(255, 109, 60);
                //listViewFlow.SelectedItems[0].Selected = false;
            }

        }

        //日志清除

        private void 清除ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            richTxb.Clear();
        }


        void 显示中心十字坐标Event(object sender, EventArgs e)
        {

           gen_tracking_cross();
        }
        /// <summary>
        /// 十字光标生成,间隔默认2mm
        /// </summary>
        public void gen_tracking_cross(int gap = 2)
        {

            if(GrabImg==null|| GrabImg.Empty()|| GrabImg.Width<=0)
            {
                currvisiontool.IsShowCenterCross = false;
                Appentxt("图像为空！无法生成十字光标");
                return;
            }
            int cx = this.GrabImg.Width/2;
            int cy = this.GrabImg.Height/2;
            int width = this.GrabImg.Width;
            int height = this.GrabImg.Height;
            if (Hom_mat2d == null || Hom_mat2d.Empty() || Hom_mat2d.Width <= 0)
            {
                currvisiontool.IsShowCenterCross = false;
                Appentxt("坐标转换矩阵为空，无法生成刻度尺！");
                return;
            }
            if (Hom_mat2d_inv == null || Hom_mat2d_inv.Empty() || Hom_mat2d_inv.Width <= 0)
            {
                currvisiontool.IsShowCenterCross = false;
                Appentxt("坐标转换矩阵为空，无法生成刻度尺！");
                return;
            }
            //图像中心对应的物理坐标
            Point2d centreRobotP=  CalibrationTool.AffineTransPoint2d(Hom_mat2d, new Point2d ( cx,cy));
            //偏移2mm
            Point2d centreRobotP_offset = new Point2d(centreRobotP.X+ gap, centreRobotP.Y);
            //偏移2mm对应的像素坐标
            Point2d centrePixelP_offset =CalibrationTool.AffineTransPoint2d(Hom_mat2d_inv, centreRobotP_offset);
            //2mm对应的实际像素距离
            double distance = Math.Sqrt(Math.Pow(centrePixelP_offset.X- cx, 2)+
                Math.Pow(centrePixelP_offset.Y- cy, 2));
            //绘制十字光标,半径2mm
            RegionEx regionEx = new RegionEx(new CrossF(cx, cy, width, height, (float)distance/2), Color.Red, 2);
            currvisiontool.DrawRegion(regionEx);
            currvisiontool.AddRegionBufferOfCross(regionEx);

            //横向刻度
            for(int i=1;cx+i* distance < width;i++)
            {
                //偏移2*i mm
                Point2d centreRobotP_offset_buf = new Point2d(centreRobotP.X + gap*i, centreRobotP.Y);
                //偏移2mm对应的像素坐标
                Point2d centrePixelP_offset_buf = CalibrationTool.AffineTransPoint2d(Hom_mat2d_inv, centreRobotP_offset_buf);
                //2*i mm对应的实际像素距离
                double distance_pixel = Math.Sqrt(Math.Pow(centrePixelP_offset_buf.X - cx, 2) +
                    Math.Pow(centrePixelP_offset_buf.Y - cy, 2));
                //绘制刻度
                RegionEx regionEx_buf = new RegionEx(new LineF((float)(cx+ distance_pixel), cy-20,
                               (float)(cx + distance_pixel), cy+20), Color.Red, 2);
                currvisiontool.DrawRegion(regionEx_buf);
                currvisiontool.AddRegionBufferOfCross(regionEx_buf);
            }
            for (int i = 1; cx - i * distance > 0; i++)
            {
                //偏移2*i mm
                Point2d centreRobotP_offset_buf = new Point2d(centreRobotP.X - gap * i, centreRobotP.Y);
                //偏移2mm对应的像素坐标
                Point2d centrePixelP_offset_buf = CalibrationTool.AffineTransPoint2d(Hom_mat2d_inv, centreRobotP_offset_buf);
                //2*i mm对应的实际像素距离
                double distance_pixel = Math.Sqrt(Math.Pow(centrePixelP_offset_buf.X - cx, 2) +
                    Math.Pow(centrePixelP_offset_buf.Y - cy, 2));
                //绘制刻度
                RegionEx regionEx_buf = new RegionEx(new LineF((float)(cx - distance_pixel), cy - 20,
                               (float)(cx - distance_pixel), cy + 20), Color.Red, 2);
                currvisiontool.DrawRegion(regionEx_buf);
                currvisiontool.AddRegionBufferOfCross(regionEx_buf);
            }
            //纵向刻度
            for (int i = 1; cy + i * distance < height; i++)
            {
                //偏移2*i mm
                Point2d centreRobotP_offset_buf = new Point2d(centreRobotP.X,centreRobotP.Y + gap * i);
                //偏移2mm对应的像素坐标
                Point2d centrePixelP_offset_buf = CalibrationTool.AffineTransPoint2d(Hom_mat2d_inv, centreRobotP_offset_buf);
                //2*i mm对应的实际像素距离
                double distance_pixel = Math.Sqrt(Math.Pow(centrePixelP_offset_buf.X - cx, 2) +
                    Math.Pow(centrePixelP_offset_buf.Y - cy, 2));
                //绘制刻度
                RegionEx regionEx_buf = new RegionEx(new LineF(cx -20, (float)(cy+distance_pixel),
                               cx +20, (float)(cy + distance_pixel)), Color.Red, 2);
                currvisiontool.DrawRegion(regionEx_buf);
                currvisiontool.AddRegionBufferOfCross(regionEx_buf);
            }
            //纵向刻度
            for (int i = 1; cy- i * distance >0; i++)
            {
                //偏移2*i mm
                Point2d centreRobotP_offset_buf = new Point2d(centreRobotP.X, centreRobotP.Y - gap * i);
                //偏移2mm对应的像素坐标
                Point2d centrePixelP_offset_buf = CalibrationTool.AffineTransPoint2d(Hom_mat2d_inv, centreRobotP_offset_buf);
                //2*i mm对应的实际像素距离
                double distance_pixel = Math.Sqrt(Math.Pow(centrePixelP_offset_buf.X - cx, 2) +
                    Math.Pow(centrePixelP_offset_buf.Y - cy, 2));
                //绘制刻度
                RegionEx regionEx_buf = new RegionEx(new LineF(cx - 20, (float)(cy- distance_pixel),
                               cx + 20, (float)(cy- distance_pixel)), Color.Red, 2);
                currvisiontool.DrawRegion(regionEx_buf);
                currvisiontool.AddRegionBufferOfCross(regionEx_buf);
            }
        }


        void LoadedImageNoticeEvent(object sender, EventArgs e)
        {
            if (GrabImg != null)
                GrabImg.Dispose();
            GrabImg = MatExtension.BitmapToGrayMat(currvisiontool.Image);
        
            RunToolStep = 0;
        }
        private void frmCalibration3_Load(object sender, EventArgs e)
        {
            uCPretreatmentParmas = new UCPretreatmentParmas();
            //uCPretreatmentParmas.Maskwidth_heightSaveHandle += Maskwidth_heightSaveEvent;
            uCPretreatmentParmas2 = new UCPretreatmentParmas2();
            //uCPretreatmentParmas2.NumMult_AddSaveHandle += NumMult_AddSaveEvent;
            //加载相机参数      
            loadCamParmas();
            //配方加载             
            RecipeSaveEvent(null, null);

            isAutoAutoCoorSys = bool.Parse(GeneralUse.ReadValue("坐标系", "同步", "config", "False"));
            chxbAutoCoorSys.Checked = isAutoAutoCoorSys;

            //isUsePixelCoordinate = bool.Parse(GeneralUse.ReadValue("圆心计算", "像素坐标", "config", "false"));

            SetParametersHide(false);
            //单例模式
            mp = frmrecipe.CreateInstance();
            mp.RecipeSaveHandle = new EventHandler(RecipeSaveEvent);
            //  mp.Owner = this;        
            mp.TopMost = true;
            mp.WindowState = FormWindowState.Normal;
            mp.StartPosition = FormStartPosition.CenterScreen;
            mp.Show();
            mp.Hide();

        }
        public void frmCalibration3_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (shouldHide)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
                Release();
        }

        /// <summary>
        /// 窗体关闭前资源释放
        /// </summary>
        public void Release()
        {
            FreeConsole();//释放控制台

            //currvisiontool.Dispose();

            if (CurrCam != null)
            {
                CurrCam.CloseCam();
                CurrCam.setImgGetHandle -= getImageDelegate;

            }
            if (GrabImg != null) GrabImg.Dispose();
            Disconnect();
            //this.Dispose();
            _frmCalibration2 = null;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selecttablename = (sender as TabControl).SelectedTab.Name;
            if (selecttablename == "tabPage1")
            {
                listViewPixel.Columns[1].Width = (listViewPixel.Width - 30) / 2;
                listViewPixel.Columns[2].Width = (listViewPixel.Width - 30) / 2;
            }
            else if (selecttablename == "tabPage2")
            {

                listViewRobot.Columns[1].Width = (listViewRobot.Width - 30) / 2;
                listViewRobot.Columns[2].Width = (listViewRobot.Width - 30) / 2;
            }
        }

        #endregion

        #region--------Common Method----------
        /// <summary>
        /// Point[]转CVPoint[]
        /// </summary>
        /// <param name="pointFs"></param>
        /// <returns></returns>
        static public List<CVPoint> pointList2cvpointList(List<Point> pointFs)
        {
            if (pointFs == null || pointFs.Count <= 0) return null;
            return pointFs.ConvertAll<CVPoint>(p => new CVPoint(p.X, p.Y));
        }

        /// <summary>
        ///CVPoint[]转 Point[]
        /// </summary>
        /// <param name="pointFs"></param>
        /// <returns></returns>
        static public List<Point> cvpointList2pointList(List<CVPoint> pointFs)
        {
            if (pointFs == null || pointFs.Count <= 0) return null;
            return pointFs.ConvertAll<Point>(delegate (CVPoint p) { return new Point(p.X, p.Y); });
        }

        /// <summary>
        /// OpenCv旋转矩形转矩形
        /// </summary>
        /// <param name="cVRRect"></param>
        /// <param name="cVRect"></param>
        public void shapeConvert(CVRRect cVRRect, out CVRect cVRect)
        {
            double cx = cVRRect.Center.X;
            double cy = cVRRect.Center.Y;
            double angle = cVRRect.Angle;
            double width = cVRRect.Size.Width;
            double height = cVRRect.Size.Height;

            Point2f[] vertexPs = cVRRect.Points();
            Point2f[] NewvertexPs = new Point2f[4];

            for (int i = 0; i < 4; i++)
            {
                NewvertexPs[i] = AxisCoorditionRotation.get_after_RotatePoint(vertexPs[i],
                new Point2f((float)cx, (float)cy), -angle);
            }

            double topx = 9999, topy = 9999;
            for (int j = 0; j < 4; j++)
            {
                if ((NewvertexPs[j].X <= topx - 10 && NewvertexPs[j].X > 0) ||
                    (NewvertexPs[j].Y <= topy - 10 && NewvertexPs[j].Y >= 0))
                {
                    topx = NewvertexPs[j].X;
                    topy = NewvertexPs[j].Y;
                }
            }

            cVRect = new CVRect((int)topx, (int)topy, (int)width, (int)height);
        }

        /// <summary>
        /// OpenCv矩形转旋转矩形
        /// </summary>
        /// <param name="cVRect"></param>
        /// <param name="cVRRect"></param>
        /// <param name="angle"></param>
        public void shapeConvert(CVRect cVRect, out CVRRect cVRRect, float angle = 0)
        {
            cVRRect = new CVRRect(new Point2f(cVRect.X + cVRect.Width / 2, cVRect.Y + cVRect.Height / 2),
           new Size2f(cVRect.Width, cVRect.Height), angle);
        }
        /// <summary>
        /// OpenCv旋转矩形转自定义旋转矩形
        /// </summary>
        /// <param name="cVRRect"></param>
        /// <param name="rotatedRectF"></param>
        public void shapeConvert(CVRRect cVRRect, out RotatedRectF rotatedRectF)
        {
            rotatedRectF = new RotatedRectF(cVRRect.Center.X, cVRRect.Center.Y,
                cVRRect.Size.Width, cVRRect.Size.Height, cVRRect.Angle);
        }
        /// <summary>
        /// 自定义旋转矩形转OpenCv旋转矩形
        /// </summary>
        /// <param name="rotatedRectF"></param>
        /// <param name="cVRRect"></param>
        public void shapeConvert(RotatedRectF rotatedRectF, out CVRRect cVRRect)
        {
            cVRRect = new CVRRect(new Point2f(rotatedRectF.cx, rotatedRectF.cy),
                new Size2f(rotatedRectF.Width, rotatedRectF.Height), rotatedRectF.angle);
        }
        /// <summary>
        /// OpenCv矩形转自定义矩形
        /// </summary>
        /// <param name="cVRect"></param>
        /// <param name="rectangleF"></param>
        public void shapeConvert(CVRect cVRect, out RectangleF rectangleF)
        {
            rectangleF = new RectangleF(cVRect.X, cVRect.Y, cVRect.Width, cVRect.Height);
        }
        /// <summary>
        /// 自定义矩形转OpenCv矩形
        /// </summary>
        /// <param name="rectangleF"></param>
        /// <param name="cVRect"></param>
        public void shapeConvert(RectangleF rectangleF, out CVRect cVRect)
        {
            cVRect = new CVRect((int)rectangleF.X, (int)rectangleF.Y, (int)rectangleF.Width, (int)rectangleF.Height);
        }


        /// <summary>
        /// 像素坐标转物理坐标
        /// </summary>
        /// <param name="pixelX"></param>
        /// <param name="pixelY"></param>
        /// <param name="convertPosX"></param>
        /// <param name="convertPosY"></param>
        public void PixelToPoint(double pixelX, double pixelY,
                             ref double convertPosX, ref double convertPosY)
        {
            Point2d robotP = CalibrationTool.AffineTransPoint2d(Hom_mat2d,
                new Point2d(pixelX, pixelY));

            convertPosX = Math.Round(robotP.X, 3);
            convertPosY = Math.Round(robotP.Y, 3);
        }
        /// <summary>
        /// 双击获取
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void DoubleClickGetMousePosEvent2(int x, int y)
        {
            DoubleClickGetMousePosHandle?.Invoke(x, y);

        }
        void checkedControl(string dirFileName)
        {

            if (currModelType == EumModelType.ProductModel_1)
            {
                //附加工具
                CheckBoxselectID = int.Parse(GeneralUse.ReadValue("附加工具", "工具编号",
                  "附加工具类型", "-1", dirFileName + "\\" +
                  "modelfile\\ProductModel_1"));
                ExchangeSelect(CheckBoxselectID);
            }
            else if (currModelType == EumModelType.ProductModel_2)
            {
                //附加工具
                CheckBoxselectID = int.Parse(GeneralUse.ReadValue("附加工具", "工具编号",
                 "附加工具类型", "-1", dirFileName + "\\" +
                 "modelfile\\ProductModel_2"));
                ExchangeSelect(CheckBoxselectID);
            }
            else if (currModelType == EumModelType.CaliBoardModel)
            {
                //附加工具
                CheckBoxselectID = -1;
                ExchangeSelect(CheckBoxselectID);
            }
            else
            {
                //附加工具
                CheckBoxselectID = int.Parse(GeneralUse.ReadValue("附加工具", "工具编号",
                  "附加工具类型", "-1", dirFileName + "\\" +
                  "modelfile\\GlueTapModel"));
                ExchangeSelect(CheckBoxselectID);
            }
        }

        /// <summary>
        /// 复制文件夹及文件
        /// </summary>
        /// <param name="sourceFolder">原文件路径</param>
        /// <param name="destFolder">目标文件路径</param>
        /// <returns></returns>
        bool CopyFolder(string sourceFolder, string destFolder)
        {
            try
            {
                //如果目标路径不存在,则创建目标路径
                if (!Directory.Exists(destFolder))
                    Directory.CreateDirectory(destFolder);
                //得到原文件根目录下的所有文件
                string[] files = Directory.GetFiles(sourceFolder);
                foreach (string file in files)
                {
                    string name = Path.GetFileName(file);
                    string dest = Path.Combine(destFolder, name);
                    File.Copy(file, dest, true);//复制文件
                }
                //得到原文件根目录下的所有文件夹
                string[] folders = Directory.GetDirectories(sourceFolder);
                foreach (string folder in folders)
                {
                    string name = Path.GetFileName(folder);
                    string dest = Path.Combine(destFolder, name);
                    CopyFolder(folder, dest);//构建目标路径,递归复制文件
                }
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }

        }
        /// <summary>
        /// 添加测试文本及日志
        /// </summary>
        /// <param name="txt"></param>
        void Appentxt(string info)
        {
            if (richTxb.InvokeRequired)
            {
                richTxb.Invoke(new Action<string>(Appentxt), info);
            }
            else
            {
                string dConvertString = "";
                if (!richTxb.Disposing)
                {
                    if (richTxb.TextLength > 2000)
                        richTxb.Clear();
                    dConvertString = string.Format("{0}  {1}\r",
                           DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), info);
                    richTxb.AppendText(dConvertString);
                    richTxb.ScrollToCaret();
                }
                log.Info("测试信息", info);
            }
        }
        /// <summary>
        /// 参数设置画面
        /// </summary>
        /// <param name="isOpenAllFunctions">是否启用功能简化</param>
        public void setStyle(bool isOpenAllFunctions, bool isContainGlue = false)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool, bool>(setStyle), isOpenAllFunctions, isContainGlue);
            }
            else
            {


                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
                txbMarkPixelX.RectColor = Color.FromArgb(255, 109, 60);
                txbMarkPixelY.RectColor = Color.FromArgb(255, 109, 60);
                txbMarkRobotX.RectColor = Color.FromArgb(255, 109, 60);
                txbMarkRobotY.RectColor = Color.FromArgb(255, 109, 60);
                CamExposureBar.RectColor = Color.FromArgb(255, 109, 60);
                CamGainBar.RectColor = Color.FromArgb(255, 109, 60);


                this.uiTabControl1.SuspendLayout();
                this.SuspendLayout();
                // 
                this.uiTabControl1.FillColor = Color.White;
                //this.uiTabControl1.TabPages.Clear();
                if (isOpenAllFunctions)
                {
                    TabPages["相机设置"].Parent = this.uiTabControl1;
                    TabPages["定位检测"].Parent = this.uiTabControl1;
                    if (isContainGlue)
                        TabPages["胶路检测"].Parent = this.uiTabControl1;
                    else
                        TabPages["胶路检测"].Parent = null;
                    TabPages["像素坐标"].Parent = this.uiTabControl1;
                    TabPages["物理坐标"].Parent = this.uiTabControl1;
                    TabPages["坐标变换"].Parent = this.uiTabControl1;
                    TabPages["旋转中心"].Parent = this.uiTabControl1;

                }
                else
                {
                    TabPages["相机设置"].Parent = this.uiTabControl1;
                    TabPages["定位检测"].Parent = this.uiTabControl1;
                    if (isContainGlue)
                        TabPages["胶路检测"].Parent = this.uiTabControl1;
                    else
                        TabPages["胶路检测"].Parent = null;
                    TabPages["像素坐标"].Parent = null;
                    TabPages["物理坐标"].Parent = null;
                    TabPages["坐标变换"].Parent = null;
                    TabPages["旋转中心"].Parent = null;

                }
                this.uiTabControl1.ResumeLayout(false);
                this.uiTabControl1.PerformLayout();
                this.ResumeLayout(false);
                this.PerformLayout();
            }
        }

        /// <summary>
        ///  相机曝光设置
        ///  默认值为5000
        /// </summary>
        /// <param name="dValue">设置曝光参数</param>
        /// <returns>返回设置是否成功标志</returns>
        public bool SetExposure(long dValue)
        {
            if (CurrCam == null)
            {
                Appentxt("相机未实例化！");
                return false;
            }
            if (!CurrCam.IsAlive)
            {
                Appentxt("相机未链接！");
                return false;
            }
            if (dValue < 1000 || dValue > 200000)
            {
                Appentxt("请设置1000~200000之间合适的整数！");
                return false;
            }
            Appentxt(string.Format("外部指令开启曝光参数设定，设定值：{0}", dValue));
            CamExposure = (int)dValue;
            lblCamExposure.Text = CamExposure.ToString();
            CamExposureBar.Value = CamExposure;
            return true;
        }
        /// <summary>
        /// 相机增益设置
        /// 默认值为0
        /// </summary>
        /// <param name="dValue">设置增益参数</param>
        /// <returns>返回设置是否成功标志</returns>
        public bool SetGain(long dValue)
        {
            if (CurrCam == null)
            {
                Appentxt("相机未实例化！");
                return false;
            }
            if (!CurrCam.IsAlive)
            {
                Appentxt("相机未链接！");
                return false;
            }
            if (dValue < 0 || dValue > 10)
            {
                Appentxt("请设置0~10之间合适的整数！");
                return false;
            }
            Appentxt(string.Format("外部指令开启增益参数设定，设定值：{0}", dValue));
            CamGain = (int)dValue;
            lblCamGain.Text = CamGain.ToString();
            CamGainBar.Value = CamGain;
            return true;
        }
        /// <summary>
        /// 设置图像采集自由模式
        /// </summary>
        public void SetCameraFreeStyle()
        {
            workstatus = EunmcurrCamWorkStatus.freestyle;
        }

        EumOperationAuthority currOperationAuthority = EumOperationAuthority.None;
        /// <summary>
        /// 设置操作权限
        /// </summary>
        /// <param name="eumOperationAuthority">操作人员类型</param>
        public void setOperationAuthority(EumOperationAuthority eumOperationAuthority =
              EumOperationAuthority.None)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<EumOperationAuthority>(setOperationAuthority), eumOperationAuthority);
            }
            else
            {
                currOperationAuthority = eumOperationAuthority;
                Appentxt(string.Format("当前操作人员类型：{0}", Enum.GetName(typeof(EumOperationAuthority),
                                currOperationAuthority)));
                switch (eumOperationAuthority)
                {
                    case EumOperationAuthority.Operator:
                        setOperator();
                        break;
                    case EumOperationAuthority.Programmer:
                        setProgrammer();
                        break;
                    case EumOperationAuthority.Administrators:
                        setAdministrators();
                        break;
                    default:
                        setNone();
                        break;
                }

            }
        }
        /// <summary>
        /// 无任何权限
        /// </summary>
        void setNone()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(setNone));
            }
            else
            {
                CamTypeSetBox.Enabled = false;
                CamParmasSetBox.Enabled = false;
                ImageGrabToolBox.Enabled = false;

                LogShowBox.Enabled = false;

                LocationDectionSetBox.Enabled = false;
                NinePointsOfPixelGetBox.Enabled = false;
                NinePointsOfPixelDatBox.Enabled = false;
                NinePointsOfRobotGetBox.Enabled = false;
                NinePointsOfRobotDatBox.Enabled = false;
                CoordinateTransBox.Enabled = false;
                RotatePixelGetBox.Enabled = false;
                RotateCalBox.Enabled = false;
                RotateOfPixelDatBox.Enabled = false;

            }
        }
        /// <summary>
        /// 设置为操作员
        /// </summary>
        void setOperator()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(setOperator));
            }
            else
            {
                CamTypeSetBox.Enabled = true;
                CamParmasSetBox.Enabled = false;
                ImageGrabToolBox.Enabled = false;

                LogShowBox.Enabled = false;

                LocationDectionSetBox.Enabled = false;
                NinePointsOfPixelGetBox.Enabled = false;
                NinePointsOfPixelDatBox.Enabled = false;
                NinePointsOfRobotGetBox.Enabled = false;
                NinePointsOfRobotDatBox.Enabled = false;
                CoordinateTransBox.Enabled = false;
                RotatePixelGetBox.Enabled = false;
                RotateCalBox.Enabled = false;
                RotateOfPixelDatBox.Enabled = false;

                if (CurrCam != null)
                    EnableCam(CurrCam.IsAlive);
            }
        }
        /// <summary>
        /// 设置为程序员
        /// </summary>
        void setProgrammer()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(setProgrammer));
            }
            else
            {
                CamTypeSetBox.Enabled = true;
                CamParmasSetBox.Enabled = true;
                ImageGrabToolBox.Enabled = true;

                LogShowBox.Enabled = true;

                LocationDectionSetBox.Enabled = true;
                NinePointsOfPixelGetBox.Enabled = true;
                NinePointsOfPixelDatBox.Enabled = true;
                NinePointsOfRobotGetBox.Enabled = true;
                NinePointsOfRobotDatBox.Enabled = true;
                CoordinateTransBox.Enabled = true;
                RotatePixelGetBox.Enabled = true;
                RotateCalBox.Enabled = true;
                RotateOfPixelDatBox.Enabled = true;

                if (CurrCam != null)
                    EnableCam(CurrCam.IsAlive);
                EnableDetectionControl();
            }
        }
        /// <summary>
        /// 设置为管理员
        /// </summary>
        void setAdministrators()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(setAdministrators));
            }
            else
            {
                CamTypeSetBox.Enabled = true;
                CamParmasSetBox.Enabled = true;
                ImageGrabToolBox.Enabled = true;

                LogShowBox.Enabled = true;

                LocationDectionSetBox.Enabled = true;
                NinePointsOfPixelGetBox.Enabled = true;
                NinePointsOfPixelDatBox.Enabled = true;
                NinePointsOfRobotGetBox.Enabled = true;
                NinePointsOfRobotDatBox.Enabled = true;
                CoordinateTransBox.Enabled = true;
                RotatePixelGetBox.Enabled = true;
                RotateCalBox.Enabled = true;
                RotateOfPixelDatBox.Enabled = true;

                if (CurrCam != null)
                    EnableCam(CurrCam.IsAlive);
                EnableDetectionControl();
            }
        }
        /// <summary>
        /// 创建配方
        /// ecipeName=配方名称
        /// </summary>
        /// <param name="recipeName">配方名</param>
        /// <param name="isReplace">是否同名替换</param>
        public bool createRecipe(string recipeName, bool isReplace = true)
        {
            bool flag = mp.CreateRecipe(recipeName, isReplace);
            if (!flag)
            {
                Appentxt("配方创建失败");
                return false;
            }
            string usedRecipeName = mp.SaveRecipe();
            if (usedRecipeName != "")
            {
                this.Invoke(new Action(() =>
                {
                    RecipeSaveEvent(usedRecipeName, null);
                }));

                return true;
            }
            else
            {
                Appentxt("配方创建失败");
                return false;
            }
        }

        /// <summary>
        /// 删除配方
        ///recipeName=配方名称
        /// </summary>
        /// <param name="recipeName"></param>
        public void DeleteRecipe(string recipeName)
        {
            mp.DeleteRecipe(recipeName);
            string usedRecipeName = mp.SaveRecipe();
            if (usedRecipeName != "")
            {
                this.Invoke(new Action(() =>
                {
                    RecipeSaveEvent(usedRecipeName, null);
                }));

                //return true;
            }
        }

        /// <summary>
        /// 保存配方
        /// return:true表示保存成功，false表示保存失败
        /// </summary>
        //public bool SaveRecipe()
        //{
        //    return mp.SaveRecipe();
        //}
        /// <summary>
        /// 配方切换
        /// recipeName=配方名称
        /// </summary>
        /// <param name="recipeName">配方名称</param>
        public void RecipeSwitching(string recipeName)
        {
            Appentxt(string.Format("外部指令开启配方切换,切换名称:{0}", recipeName));
            if (CurrRecipeName == recipeName)
            {
                Appentxt("当前使用配方与需要切换的同名！");
                return;
            }

            mp.SwitchRecipe(recipeName);
            //保存
            {
                string usedRecipeName = mp.SaveRecipe();
                if (usedRecipeName != "")
                {
                    this.Invoke(new Action(() =>
                    {
                        RecipeSaveEvent(usedRecipeName, null);
                    }));

                    //return true;
                }
                //else
                //    return false;
            }
            //string temvalue = GeneralUse.ReadValue("配方", "使用路径", "config");
            //RecipeSaveEvent(temvalue, null);
        }

        /// <summary>
        /// 获取当前使用配方名称
        /// return返回当前使用配方名称
        /// </summary>
        /// <returns>当前使用配方名称</returns>
        public string getCurrRecipeName()
        {
            return CurrRecipeName;
        }

        /// <summary>
        /// 获取配方名
        /// return:配方名集合
        /// </summary>
        /// <returns>配方名集合</returns>
        public List<string> GetRecipeNames()
        {
            if (mp == null)
            {
                Appentxt("配方集合获取失败：配方实例对象为空，请检查方法调用！");
                return null;
            }
            string errmsg = "";

            List<string> temListData = mp.GetRecipeName(ref errmsg);
            if (temListData == null)
                Appentxt(errmsg);
            return temListData;
        }

        /// <summary>
        /// 加载配方文件,附带文件自主校验
        /// path：需要加载得配方文件完整路径
        /// recipeName:重命名为recipeName;
        /// IsUse：加载后是否直接启用，默认启用
        /// </summary>
        /// <param name="path">需要加载得配方文件完整路径</param>
        /// <param name="recipeName">重命名为recipeName</param>
        /// <param name="replace">是否同名替换</param>
        /// <returns></returns>
        public bool AddRecipeFile(string path, string recipeName, bool replace = true)
        {
            if (mp.AddRecipeFile(path, recipeName, replace))
            {
                string usedRecipeName = mp.SaveRecipe();
                if (usedRecipeName != "")
                {
                    RecipeSaveEvent(usedRecipeName, null);
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        /// <summary>
        /// 将配方文件recipeName导出到特定文件路径path
        /// path:文件路径
        /// recipeName:配方文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="recipeName">配方文件</param>
        /// <returns></returns>
        public bool ExportRecipe(string path, string recipeName)
        {
            return mp.ExportRecipe(path, recipeName);
        }

        /// <summary>
        /// 折叠参数界面
        /// </summary>
        /// <param name="value">true:折叠/false:取消折叠</param>
        public void SetParametersHide(bool value)
        {
            uiPanel1.Visible = !value;

        }

        void ExchangeSelect(int selectvalue)
        {
            switch (selectvalue)
            {
                case -1:
                    chxbLinesIntersect.Checked = false;
                    chxbFindCircle.Checked = false;
                    chxbBlobCentre.Checked = false;

                    break;
                case 1:

                    chxbLinesIntersect.Checked = true;
                    chxbFindCircle.Checked = false;
                    chxbBlobCentre.Checked = false;

                    break;
                case 2:
                    chxbLinesIntersect.Checked = false;
                    chxbFindCircle.Checked = true;
                    chxbBlobCentre.Checked = false;

                    break;
                case 3:
                    chxbLinesIntersect.Checked = false;
                    chxbFindCircle.Checked = false;
                    chxbBlobCentre.Checked = true;

                    break;
            }


        }

        static string getDescription(Enum obj)
        {
            string objName = obj.ToString();
            Type t = obj.GetType();
            FieldInfo fi = t.GetField(objName);

            DescriptionAttribute[] arrDesc = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return arrDesc[0].Description;
        }
        //校验字符串是否为有效数值
        bool checkValueNumber(string txt)
        {
            float temvalue = 0f;
            return float.TryParse(txt, out temvalue);
        }
        //给控件Listview赋值
        void SetValueToListItem(ListView lv, string[] temvalueArray)
        {
            ListViewItem lvi = new ListViewItem(temvalueArray);
            lv.Items.Add(lvi);
        }

        static object camlock = new object();
        /// <summary>
        /// 连续采集
        /// </summary>
        /// <returns></returns>
        public void ContinueGrab()
        {
            lock (camlock)
            {
                Appentxt("外部指令开启了连续采集");
                workstatus = EunmcurrCamWorkStatus.freestyle;
                if (CurrCam == null) return;
                if (!CurrCam.IsAlive) return;

                Task.Run(() =>
                {
                    if (CurrCam.IsGrabing)
                    {
                        CurrCam.StopGrab();  //如果已在采集中则先停止采集
                        Thread.Sleep(20);
                    }

                    bool flag = CurrCam.ContinueGrab();
                    return flag;
                }).ContinueWith(t =>
                {
                    this.Invoke(new Action(() =>
                    {
                        if (t.Result)
                        {
                            btnContinueGrab.Enabled = false;
                            btnOneShot.Enabled = false;
                            btnStopGrab.Enabled = true;
                            btnGetPixelPoint.Enabled = false;
                            btnGetRotataPixel.Enabled = false;
                        }
                        else
                        {
                            btnContinueGrab.Enabled = true;
                            btnOneShot.Enabled = true;
                            btnStopGrab.Enabled = false;
                            btnGetPixelPoint.Enabled = true;
                            btnGetRotataPixel.Enabled = true;
                            Appentxt("重新开启连续采集失败，代码编号：1761");
                        }

                    }));

                });
            }
        }
        /// <summary>
        /// 停止采集
        /// </summary>
        /// <returns></returns>
        public void StopGrab()
        {
            Appentxt("外部指令开启了停止采集");
            if (CurrCam == null) return;
            if (!CurrCam.IsAlive) return;
            CurrCam.StopGrab();
            Thread.Sleep(20);
            this.Invoke(new Action(() =>
            {
                btnContinueGrab.Enabled = true;
                btnOneShot.Enabled = true;
                btnStopGrab.Enabled = false;
                btnGetPixelPoint.Enabled = true;
                btnGetRotataPixel.Enabled = true;

            }));
        }
        /// <summary>
        /// 单帧采集
        /// </summary>
        /// <returns></returns>
        public void OneShot()
        {
            Appentxt("外部指令开启了单帧采集");
            if (CurrCam == null) return;
            if (!CurrCam.IsAlive) return;
            Task.Run(() =>
            {
                if (CurrCam.IsGrabing)
                {
                    CurrCam.StopGrab();  //如果已在采集中则先停止采集
                    Thread.Sleep(20);
                }
            }).ContinueWith(t =>
            {
                CurrCam.OneShot();
            });
        }

        /// <summary>
        /// CurrCam.OneShot()==替换===>OneGrab();
        /// </summary>
        void OneGrab()
        {
            if (CurrCam == null)
            {
                Appentxt("相机对象为空");
                return;
            }
            if (!CurrCam.IsAlive)
            {
                Appentxt("相机未在线");
                return;
            }
            Task.Run(() =>
            {
                if (CurrCam.IsGrabing)
                {
                    CurrCam.StopGrab();  //如果已在采集中则先停止采集
                    Thread.Sleep(20);
                }

            }).ContinueWith(t =>
            {
                CurrCam.OneShot();
            });
        }

        #endregion

        #region------------CAM----------------

        public void Run()
        {
            if (CurrCam == null || !CurrCam.IsAlive) return;
            this.Invoke(new Action(() =>
            {
                cobxModelType.SelectedIndex = 0;
            }));
            workstatus = EunmcurrCamWorkStatus.NormalTest_T1;
            CurrCam.OneShot();    //单次采集
            Appentxt("开始自动检测,使用模板为产品1模板！");

        }

        delegate void SaveImgHandle(string path);

        void CamConnectEvent(object sender, EventArgs e)
        {
            CamConnectStatusHandle?.Invoke(sender, e);
        }

     
        //相机单帧采集
        private void btnOneShot_Click(object sender, EventArgs e)
        {
        
            workstatus = EunmcurrCamWorkStatus.freestyle;
            CurrCam.OneShot();
        }
        //相机连续采集
        private void btnContinueGrab_Click(object sender, EventArgs e)
        {
            workstatus = EunmcurrCamWorkStatus.freestyle;
            CurrCam.ContinueGrab();
            btnContinueGrab.Enabled = false;
            btnOneShot.Enabled = false;
            btnStopGrab.Enabled = true;
            btnGetPixelPoint.Enabled = false;
            btnGetRotataPixel.Enabled = false;
            camContinueGrabHandle?.Invoke(true);
        }
        //相机停止采集
        private void btnStopGrab_Click(object sender, EventArgs e)
        {
            CurrCam.StopGrab();
            btnContinueGrab.Enabled = true;
            btnOneShot.Enabled = true;
            btnStopGrab.Enabled = false;
            btnGetPixelPoint.Enabled = true;
            btnGetRotataPixel.Enabled = true;
            camContinueGrabHandle?.Invoke(false);
        }
        //相机参数保存事件
        private void btnSaveCamParma_Click(object sender, EventArgs e)
        {

            GeneralUse.WriteValue("相机", "型号", cobxCamType.SelectedItem.ToString(), "config");
            GeneralUse.WriteValue("相机", "索引", CamIndex.ToString(), "config");
            GeneralUse.WriteValue("相机", "曝光", lblCamExposure.Text, "config", "配方\\" + CurrRecipeName + "\\modelfile\\" + cobxModelType.Text);
            GeneralUse.WriteValue("相机", "增益", lblCamGain.Text.ToString(), "config", "配方\\" + CurrRecipeName + "\\modelfile\\" + cobxModelType.Text);
            // CamParmasChangeHandle?.Invoke(CurrCam, null);
            // MessageBox.Show("相机参数保存成功");
        }

        int CamExposure = 0, CamGain = 0, CamIndex = 0;
        private void loadCamParmas()
        {
            //---------------相机       
            currCamType = (CamType)Enum.Parse(typeof(CamType),
                     GeneralUse.ReadValue("相机", "型号", "config", "海康"));

            if (CurrCam != null)
            {
                if (CurrCam.IsAlive)
                    CurrCam.CloseCam();
                CurrCam.Dispose();
                CurrCam.setImgGetHandle -= getImageDelegate;//先关闭再注销掉             
                CurrCam.CamConnectHnadle -= CamConnectEvent;
                CurrCam.Dispose();
                CurrCam = null;
            }

            switch (currCamType)
            {
                case CamType.海康:
                    CurrCam = new HKCam("cam1");
                    this.currCamType = CamType.海康;
                    break;
                case CamType.大华:
                    CurrCam = new CCD_DaHua("cam1");
                    this.currCamType = CamType.大华;
                    break;
                case CamType.巴斯勒:
                    try
                    {
                        CurrCam = new CCD_BaslerGIGE("cam1");
                    }
                    catch (Exception er)
                    {
                        Appentxt(er.Message);
                    };
                    this.currCamType = CamType.巴斯勒;
                    break;
            }

            cobxCamType.SelectedItem = Enum.GetName(typeof(CamType), currCamType);

            CamIndex = int.Parse(GeneralUse.ReadValue("相机", "索引", "config", "0"));


            if (CurrCam == null)
            {
                Appentxt("相机实例化对象为空！");
                return;
            }
            for (int i = 0; i < CurrCam.CamNum; i++)
            {
                cobxCamIndex.Items.Add(i);
            }
            if (CamIndex < CurrCam.CamNum)
                cobxCamIndex.SelectedIndex = CamIndex;
            //注册相机图像采集事件
            CurrCam.setImgGetHandle += new ImgGetHandle(getImageDelegate);
            CurrCam.CamConnectHnadle += new EventHandler(CamConnectEvent);

            btnOpenCam_Click(null, null); //自动打开相机

        }

        //打开相机
        private void btnOpenCam_Click(object sender, EventArgs e)
        {
            if (CurrCam is CCD_BaslerGIGE)
            {
                CamGainBar.Minimum = 0;
                CamGainBar.Maximum = 3;
            }
            else if (CurrCam is HKCam)
            {
                CamGainBar.Minimum = 0;
                CamGainBar.Maximum = 10;
            }
            if (CurrCam == null)
            {
                MessageBox.Show("相机初始化失败，无法打开！", "Information",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            /////
            if (CurrCam.IsAlive)
                CurrCam.CloseCam();
            string msg = string.Empty;
            bool initFlag = CurrCam.OpenCam(CamIndex, ref msg);//先关闭再打开          
            /////////
            if (initFlag)
            {

                //相机曝光设置 
                if (CamExposureBar.Value != CamExposure)
                    CamExposureBar.Value = CamExposure;
                else
                {
                    bool flag = CurrCam.SetExposureTime(CamExposure);
                    if (!flag)
                    {
                        if (CurrCam.IsAlive)
                            CurrCam.CloseCam();
                        EnableCam(false);
                        Appentxt("相机曝光设置失败！");
                        MessageBox.Show("相机曝光设置失败！");
                        return;
                    }
                }
                //相机增益设置
                if (CamGainBar.Value != CamGain)
                    CamGainBar.Value = CamGain;
                else
                {
                    bool flag = CurrCam.SetGain(CamGain);
                    if (!flag)
                    {
                        if (CurrCam.IsAlive)
                            CurrCam.CloseCam();
                        EnableCam(false);
                        Appentxt("相机增益设置失败！");
                        MessageBox.Show("相机增益设置失败！");
                        return;
                    }
                }
                EnableCam(true);
                workstatus = EunmcurrCamWorkStatus.freestyle;
            }
            else
            {
                EnableCam(false);
                MessageBox.Show("相机打开失败：" + msg);
                Appentxt("相机打开失败：" + msg);
            }
        }

        void EnableCam(bool isEnable)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(EnableCam), isEnable);
            }
            else
            {
                if (isEnable)
                {
                    btnOpenCam.Enabled = false;
                    cobxCamType.Enabled = false;
                    cobxCamIndex.Enabled = false;
                    btnCloseCam.Enabled = true;

                    ImageGrabToolBox.Enabled = true;
                    btnOneShot.Enabled = true;
                    btnContinueGrab.Enabled = true;
                    btnStopGrab.Enabled = false;


                    CamParmasSetBox.Enabled = true;

                    btnGetPixelPoint.Enabled = true;
                    btnGetRotataPixel.Enabled = true;
                }
                else
                {
                    btnOpenCam.Enabled = true;
                    cobxCamType.Enabled = true;
                    cobxCamIndex.Enabled = true;
                    btnCloseCam.Enabled = false;

                    ImageGrabToolBox.Enabled = false;

                    CamParmasSetBox.Enabled = false;

                    btnGetPixelPoint.Enabled = false;
                    btnGetRotataPixel.Enabled = false;

                    cobxCamType.Enabled = true;
                    cobxCamIndex.Enabled = true;

                }
            }

        }
        //关闭相机
        private void btnCloseCam_Click(object sender, EventArgs e)
        {
            btnCloseCam.Enabled = false;
            Task.Run(() =>
            {
                CurrCam.CloseCam();
            }).
                ContinueWith(t =>
                {
                    EnableCam(false);
                });
        }
        static object ExposureLocker = new object();
        //相机曝光设置
        private void CamExposureBar_ValueChanged(object sender, EventArgs e)
        {
            int tem_CamExposure = this.CamExposureBar.Value;
            lock (ExposureLocker)
            {
                bool flag = CurrCam.SetExposureTime(tem_CamExposure);
                if (!flag)
                    Appentxt("相机曝光设置失败！");
                //MessageBox.Show("相机曝光设置失败！");
            }
            CamExposure = tem_CamExposure;
            lblCamExposure.Text = CamExposure.ToString();
        }
        static object GainLocker = new object();
        //相机增益设置
        private void CamGainBar_ValueChanged(object sender, EventArgs e)
        {
            int tem_CamGain = this.CamGainBar.Value;
            lock (GainLocker)
            {
                bool flag = CurrCam.SetGain(tem_CamGain);
                if (!flag)
                    Appentxt("相机增益设置失败！");
                // MessageBox.Show("相机增益设置失败！");
            }
            CamGain = tem_CamGain;
            lblCamGain.Text = CamGain.ToString();

        }
        //相机类型切换
        private void cobxCamType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Enum.GetName(typeof(CamType), this.currCamType) == cobxCamType.SelectedItem.ToString())
                return;
            if (CurrCam != null)
            {
                if (CurrCam.IsAlive)
                    CurrCam.CloseCam();
                CurrCam.setImgGetHandle -= getImageDelegate;//先关闭再注销掉             
                CurrCam.CamConnectHnadle -= CamConnectEvent;
                CurrCam.Dispose();
                CurrCam = null;

            }
            cobxCamIndex.Items.Clear();
            cobxCamIndex.Text = "";

            switch (cobxCamType.SelectedItem.ToString())
            {
                case "海康":
                    CurrCam = new HKCam("cam1");
                    this.currCamType = CamType.海康;
                    break;
                case "大华":
                    CurrCam = new CCD_DaHua("cam1");
                    this.currCamType = CamType.大华;
                    break;
                case "巴斯勒":
                    try
                    {
                        CurrCam = new CCD_BaslerGIGE("cam1");
                    }
                    catch (Exception er)
                    {
                        Appentxt(er.Message);
                    };
                    this.currCamType = CamType.巴斯勒;
                    break;
            }
            if (CurrCam == null)
            {
                Appentxt("相机实例化对象为空！");
                return;
            }

            //注册相机图像采集事件
            CurrCam.setImgGetHandle += getImageDelegate;

            for (int i = 0; i < CurrCam.CamNum; i++)
            {
                cobxCamIndex.Items.Add(i);
            }
            if (CamIndex < CurrCam.CamNum)
                cobxCamIndex.SelectedIndex = CamIndex;
            CamParmasChangeHandle?.Invoke(CurrCam, null);
        }
        //相机索引选择
        private void cobxCamIndex_SelectedIndexChanged(object sender, EventArgs e)
        {
            CamIndex = (int)cobxCamIndex.SelectedItem;
        }
    
        //图像获取委托事件
       public   void getImageDelegate(Bitmap img)
        {
            GC.Collect();
            //int ti = Environment.TickCount;
            Bitmap imgbuf = img.Clone(new Rectangle(0, 0, img.Width, img.Height), img.PixelFormat);
            //Console.WriteLine(Environment.TickCount - ti);
            this.BeginInvoke(new Action(() =>
            {
                currvisiontool.clearAll();
            
                currvisiontool.dispImage(img);

                //Thread.Sleep(5);
            }));
         
            GrabImg = MatExtension.BitmapToGrayMat(imgbuf);
            imgbuf.Dispose();

            RunToolStep = 0;
            if (workstatus != EunmcurrCamWorkStatus.freestyle)
                Appentxt(string.Format("相机当前工作状态：{0}",
                          Enum.GetName(typeof(EunmcurrCamWorkStatus), workstatus)));
            if (workstatus == EunmcurrCamWorkStatus.freestyle) //自由模式只采图不做检测
            {
                return;
            }
            else if (workstatus == EunmcurrCamWorkStatus.NinePointcLocation)  //9点标定定位模式
            {
                if (isUsingModelMatch)
                {
                    TestModelMatch();
                    this.Invoke(new Action(() =>
                    {
                        if (!stuModelMatchData.runFlag)
                        {
                            txbpixelX.Text = "NaN";
                            txbpixelY.Text = "NaN";
                            i++;
                            string[] temarray = new string[3] { i.ToString(), "0.000", "0.000" };
                            SetValueToListItem(listViewPixel, temarray);//像素点位保存到listView
                            NinePointStatusDic.Add(i, false);
                            //sendToRobCmdMsg(string.Format("{0},{1},{2}", "NP", i.ToString(), "NG"));//发送模板匹配NG
                            virtualConnect.WriteData(string.Format("{0},{1},{2}", "NP", i.ToString(), "NG"));//发送模板匹配NG
                            Appentxt(string.Format("模板匹配失败，当期模板类型：{0}，9点标定无法获取像素坐标点",
                                    Enum.GetName(typeof(EumModelType), currModelType)));
                            //MessageBox.Show("模板匹配失败，9点标定无法获取像素坐标点");
                            return;
                        }
                        else
                        {
                            txbpixelX.Text = stuModelMatchData.matchPoint.X.ToString("f3");
                            txbpixelY.Text = stuModelMatchData.matchPoint.Y.ToString("f3");
                            i++;
                            string[] temarray = new string[3] { i.ToString(),
                                stuModelMatchData.matchPoint.X.ToString("f3"),
                                 stuModelMatchData.matchPoint.Y.ToString("f3") };
                            SetValueToListItem(listViewPixel, temarray);//像素点位保存到listView
                            NinePointStatusDic.Add(i, true);
                            // sendToRobCmdMsg(string.Format("{0},{1},{2}", "NP", i.ToString(), "OK"));//发送模板匹配OK
                            virtualConnect.WriteData(string.Format("{0},{1},{2}", "NP", i.ToString(), "OK"));//发送模板匹配OK
                        }

                    }));
                }
                else
                {
                    MatchCircleRun();
                    Mat dst = new Mat();
                    Cv2.CvtColor(GrabImg, dst, ColorConversionCodes.GRAY2BGR);
                    int count = outPutDataOfCircleMatch.stuCircleResultDatas.Count;
                    for (int i = 0; i < count; i++)
                    {
                        StuCircleResultData sd = outPutDataOfCircleMatch.stuCircleResultDatas[i];
                        dst.Circle((int)(sd.centreP.X), (int)(sd.centreP.Y), (int)sd.Radius, Scalar.Green);
                    }
                    currvisiontool.dispImage(dst);

                    this.Invoke(new Action(() =>
                    {
                        StuCircleResultData sd = outPutDataOfCircleMatch.stuCircleResultDatas[0];
                        if (!sd.runFlag)
                        {
                            txbpixelX.Text = "NaN";
                            txbpixelY.Text = "NaN";
                            i++;
                            string[] temarray = new string[3] { i.ToString(), "0.000", "0.000" };
                            SetValueToListItem(listViewPixel, temarray);//像素点位保存到listView
                            NinePointStatusDic.Add(i, false);
                            //sendToRobCmdMsg(string.Format("{0},{1},{2}", "NP", i.ToString(), "NG"));//发送圆模板匹配NG
                            virtualConnect.WriteData(string.Format("{0},{1},{2}", "NP", i.ToString(), "NG"));//发送圆模板匹配NG
                            Appentxt("圆模板匹配失败,9点标定无法获取像素坐标点");

                            //MessageBox.Show("模板匹配失败，9点标定无法获取像素坐标点");
                            return;
                        }
                        else
                        {
                            txbpixelX.Text = sd.centreP.X.ToString("f3");
                            txbpixelY.Text = sd.centreP.Y.ToString("f3");
                            i++;
                            string[] temarray = new string[3] { i.ToString(), sd.centreP.X.ToString("f3"),
                                        sd.centreP.Y.ToString("f3") };
                            SetValueToListItem(listViewPixel, temarray);//像素点位保存到listView
                            NinePointStatusDic.Add(i, true);
                            // sendToRobCmdMsg(string.Format("{0},{1},{2}", "NP", i.ToString(), "OK"));//发送模板匹配OK
                            virtualConnect.WriteData(string.Format("{0},{1},{2}", "NP", i.ToString(), "OK"));//发送圆模板匹配OK
                        }

                    }));
                }

            }
            else if (workstatus == EunmcurrCamWorkStatus.RotatoLocation)  //旋转中心计定位模式
            {

                if (isUsingModelMatch)
                {
                    TestModelMatch();
                    this.Invoke(new Action(() =>
                    {
                        if (!stuModelMatchData.runFlag)
                        {

                            txbRotataPixelX.Text = "NaN";
                            txbRotataPixelY.Text = "NaN";
                            k++;
                            string[] temarray = new string[3] { k.ToString(), "0.000", "0.000" };
                            SetValueToListItem(RoratepointListview, temarray); ;//像素点位保存到listView
                            RotatoStatusDic.Add(k, false);
                            //sendToRobCmdMsg(string.Format("{0},{1},{2}", "C", k.ToString(), "NG"));//发送模板匹配NG
                            virtualConnect.WriteData(string.Format("{0},{1},{2}", "C", k.ToString(), "NG"));//发送模板匹配NG
                                                                                                            // MessageBox.Show("定位失败，无法获取像素坐标点");
                            Appentxt("定位失败，无法获取像素坐标点");
                            return;
                        }
                        else
                        {
                            txbRotataPixelX.Text = stuModelMatchData.matchPoint.X.ToString("f3");
                            txbRotataPixelY.Text = stuModelMatchData.matchPoint.Y.ToString("f3");
                            k++;
                            string[] temarray = new string[3] { k.ToString(),
                                 stuModelMatchData.matchPoint.X.ToString("f3"),
                                 stuModelMatchData.matchPoint.Y.ToString("f3") };
                            SetValueToListItem(RoratepointListview, temarray); ;//像素点位保存到listView
                            RotatoStatusDic.Add(k, true);
                            //sendToRobCmdMsg(string.Format("{0},{1},{2}", "C", k.ToString(), "OK"));//发送模板匹配OK
                            virtualConnect.WriteData(string.Format("{0},{1},{2}", "C", k.ToString(), "OK"));//发送模板匹配OK

                        }

                    }));
                }
                else
                {
                    MatchCircleRun();
                    Mat dst = new Mat();
                    Cv2.CvtColor(GrabImg, dst, ColorConversionCodes.GRAY2BGR);
                    int count = outPutDataOfCircleMatch.stuCircleResultDatas.Count;
                    for (int i = 0; i < count; i++)
                    {
                        StuCircleResultData sd = outPutDataOfCircleMatch.stuCircleResultDatas[i];
                        dst.Circle((int)(sd.centreP.X), (int)(sd.centreP.Y), (int)sd.Radius, Scalar.Green);
                    }
                    currvisiontool.dispImage(dst);

                    this.Invoke(new Action(() =>
                    {
                        StuCircleResultData sd = outPutDataOfCircleMatch.stuCircleResultDatas[0];
                        if (!sd.runFlag)
                        {

                            txbRotataPixelX.Text = "NaN";
                            txbRotataPixelY.Text = "NaN";
                            k++;
                            string[] temarray = new string[3] { k.ToString(), "0.000", "0.000" };
                            SetValueToListItem(RoratepointListview, temarray); ;//像素点位保存到listView
                            RotatoStatusDic.Add(k, false);
                            //sendToRobCmdMsg(string.Format("{0},{1},{2}", "C", k.ToString(), "NG"));//发送模板匹配NG
                            virtualConnect.WriteData(string.Format("{0},{1},{2}", "C", k.ToString(), "NG"));//发送圆模板匹配NG
                                                                                                            // MessageBox.Show("定位失败，无法获取像素坐标点");
                            Appentxt("定位失败，无法获取像素坐标点");
                            return;
                        }
                        else
                        {
                            txbRotataPixelX.Text = sd.centreP.X.ToString("f3");
                            txbRotataPixelY.Text = sd.centreP.Y.ToString("f3");
                            k++;
                            string[] temarray = new string[3] { k.ToString(), sd.centreP.X.ToString("f3"),
                                 sd.centreP.Y.ToString("f3") };
                            SetValueToListItem(RoratepointListview, temarray); ;//像素点位保存到listView
                            RotatoStatusDic.Add(k, true);
                            //sendToRobCmdMsg(string.Format("{0},{1},{2}", "C", k.ToString(), "OK"));//发送模板匹配OK
                            virtualConnect.WriteData(string.Format("{0},{1},{2}", "C", k.ToString(), "OK"));//发送圆模板匹配OK

                        }

                    }));
                }

            }
            else if (workstatus == EunmcurrCamWorkStatus.NormalTest_T1)  //正常定位测试(产品1)
            {
                PositionData dstPois;
                EumOutAngleMode eumOutAngleMode = EumOutAngleMode.Relative;
                if (IsUsingPretreatment)
                {
                    ToolTaskRun(toolParList.Count - 1);
                }
                if (isUsingModelMatch)
                {
                    TestModelMatch();
                    if (!stuModelMatchData.runFlag)
                    {
                        //用户坐标事件
                        setModelPointHandle?.Invoke("x:0,y:0,a:0;", null);
                        Appentxt("自动检测模板(产品1)匹配定位失败，无法获取坐标点");

                        return;
                    }
                    else
                    {

                        dstPois = new PositionData
                        {
                            pointXY = stuModelMatchData.matchPoint,
                            angle = stuModelMatchData.matchOffsetAngle
                        };
                        Appentxt($"模板匹配成功，点位：x:{dstPois.pointXY.X},y:{dstPois.pointXY.Y},a:{dstPois.angle}");
                    }
                }
                else if (isUsingAutoCircleMatch)
                {
                    MatchCircleRun();
                    dstPois = new PositionData
                    {
                        pointXY = stuModelMatchData.matchPoint,

                        angle = stuModelMatchData.matchOffsetAngle
                    };
                }
                else
                    dstPois = new PositionData { pointXY = new Point2d(0, 0), angle = 0 };
                /*-------------------------------------*/

                if (CheckBoxselectID == 1 || CheckBoxselectID == 2 || CheckBoxselectID == 3)

                {
                    Point2d crossP = new Point2d(0, 0);
                    float x0 = float.Parse(modelOrigion.Split(',')[0]);
                    float y0 = float.Parse(modelOrigion.Split(',')[1]);
                    float a0 = float.Parse(modelOrigion.Split(',')[2]);

                    float x1 = (float)stuModelMatchData.matchPoint.X;
                    float y1 = (float)stuModelMatchData.matchPoint.Y;
                    float a1 = (float)stuModelMatchData.matchOffsetAngle;
                    Mat mat2d = new Mat();
                    float offsetAngle = a1;
                    if (isUsingModelMatch)
                        mat2d = MatExtension.getMat(new Point2f(x0, y0), new Point2f(x1, y1), offsetAngle);

                    else
                        mat2d = MatExtension.getMat(new Point2f(0, 0), new Point2f(0, 0), 0);

                    switch (CheckBoxselectID)
                    {
                        case 1:
                            if (SearchROI1 == null || SearchROI1.Count < 2 ||
                                  SearchROI1[0] == null || SearchROI1[1] == null)
                            {
                                Appentxt("直线检测区域为空，请确认是否需要设置？");
                                break;
                            }
                            Linedetection(mat2d, offsetAngle, SearchROI1, ref crossP);
                            dstPois.angle = line1AngleLx;

                            eumOutAngleMode = EumOutAngleMode.Absolute;
                            break;
                        case 2:
                            if (SearchROI1 == null || SearchROI1.Count < 1 ||
                                SearchROI1[0] == null)
                            {
                                Appentxt("圆检测区域为空，请确认是否需要设置？");
                                break;
                            }

                            Circledetection(mat2d, offsetAngle, SearchROI1, ref crossP);
                            break;
                        case 3:
                            if (SearchROI1 == null || SearchROI1.Count < 1 ||
                               SearchROI1[0] == null)
                            {
                                Appentxt("Blob检测区域为空，请确认是否需要设置？");
                                break;
                            }

                            Blobdetection(mat2d, offsetAngle, SearchROI1, ref crossP);
                            break;
                    }
                    dstPois.pointXY = crossP;
                }


                /*----------------------------------*/
                Point2d robotP = new Point2d(0, 0);
                if (Hom_mat2d == null || Hom_mat2d.Width <= 0)
                    Appentxt("当前标定矩阵关系为空，请确认！");
                else
                {
                    if (!(dstPois.pointXY.X == 0 && dstPois.pointXY.Y == 0))
                        robotP = CalibrationTool.AffineTransPoint2d(Hom_mat2d, dstPois.pointXY);
                }

                string buff = "[发送特征点位数据]";
                buff += string.Format("x:{0:f3},y:{1:f3},a:{2:f3},m:{3};",
                                 robotP.X, robotP.Y, -dstPois.angle, Enum.GetName(typeof(EumOutAngleMode), eumOutAngleMode));

           
                //信息打印
                float x = (float)stuModelMatchData.matchPoint.X;
                float y = (float)stuModelMatchData.matchPoint.Y;
                float a = (float)stuModelMatchData.matchOffsetAngle;

                this.Invoke(new Action(() =>
                {
                    if (stuModelMatchData.runFlag)
                    {
                        currvisiontool.DrawRegion(new RegionEx(new CrossF(x, y, 20, 20, 5), Color.Red));
                        currvisiontool.AddRegionBuffer(new RegionEx(new CrossF(x, y, 20, 20, 5), Color.Red));
                    }
                    
                    //currvisiontool.DrawText(new TextEx(string.Format("{0},{1},{2}",
                    //        robotP.X.ToString("f3"),
                    //         robotP.Y.ToString("f3"),
                    //         dstPois.angle.ToString("f3")), new Font("宋体", 16), new SolidBrush(Color.Red),
                    //         dstPois.pointXY.X, dstPois.pointXY.Y));

                    //currvisiontool.AddTextBuffer(new TextEx(string.Format("{0},{1},{2}",
                    //        robotP.X.ToString("f3"),
                    //         robotP.Y.ToString("f3"),
                    //         dstPois.angle.ToString("f3")), new Font("宋体", 16), new SolidBrush(Color.Red),
                    //         dstPois.pointXY.X, dstPois.pointXY.Y));

                    currvisiontool.DrawText(new TextEx(string.Format("特征点位X:{0},Y:{1},A:{2}", robotP.X.ToString("f3"),
                     robotP.Y.ToString("f3"), dstPois.angle.ToString("f3")))
                    { x = 10, y = 300 });
                    currvisiontool.AddTextBuffer(new TextEx(string.Format("特征点位X:{0},Y:{1},A:{2}", robotP.X.ToString("f3"),
                     robotP.Y.ToString("f3"), dstPois.angle.ToString("f3")))
                    { x = 10, y = 300 });
                }));

                Appentxt(buff);
                //用户坐标事件
                setModelPointHandle?.Invoke(buff.Replace("[发送特征点位数据]", ""), null);
                stopwatch.Stop();
                int spend = (int)stopwatch.ElapsedMilliseconds;
                currvisiontool.DetectionTime = spend;

            }
            else if (workstatus == EunmcurrCamWorkStatus.NormalTest_T2)  //正常定位测试(产品2)
            {
                PositionData dstPois;
                EumOutAngleMode eumOutAngleMode = EumOutAngleMode.Relative;
                if (IsUsingPretreatment)
                {
                    ToolTaskRun(toolParList.Count - 1);
                }

                if (isUsingModelMatch)
                {
                    TestModelMatch();
                    if (!stuModelMatchData.runFlag)
                    {
                        //用户坐标事件
                        setModelPointHandle?.Invoke("x:0,y:0,a:0;", null);
                        Appentxt("自动检测模板(产品2)匹配定位失败，无法获取坐标点");
                        return;
                    }
                    else
                    {
                        dstPois = new PositionData
                        {
                            pointXY = stuModelMatchData.matchPoint,

                            angle = stuModelMatchData.matchOffsetAngle
                        };
                    }
                }
                else if (isUsingAutoCircleMatch)
                {
                    dstPois = new PositionData
                    {
                        pointXY = stuModelMatchData.matchPoint,

                        angle = stuModelMatchData.matchOffsetAngle
                    };
                }
                else
                    dstPois = new PositionData { pointXY = new Point2d(0, 0), angle = 0 };
                /*-------------------------------------*/

                if (CheckBoxselectID == 1 || CheckBoxselectID == 2 || CheckBoxselectID == 3)

                {
                    Point2d crossP = new Point2d(0, 0);
                    float x0 = float.Parse(modelOrigion.Split(',')[0]);
                    float y0 = float.Parse(modelOrigion.Split(',')[1]);
                    float a0 = float.Parse(modelOrigion.Split(',')[2]);

                    float x1 = (float)stuModelMatchData.matchPoint.X;
                    float y1 = (float)stuModelMatchData.matchPoint.Y;
                    float a1 = (float)stuModelMatchData.matchOffsetAngle;
                    Mat mat2d = new Mat();
                    float offsetAngle = a1;
                    if (isUsingModelMatch)
                        mat2d = MatExtension.getMat(new Point2f(x0, y0), new Point2f(x1, y1), offsetAngle);

                    else
                        mat2d = MatExtension.getMat(new Point2f(0, 0), new Point2f(0, 0), 0);

                    switch (CheckBoxselectID)
                    {
                        case 1:
                            if (SearchROI2 == null || SearchROI2.Count < 2 ||
                                    SearchROI2[0] == null || SearchROI2[1] == null)
                            {
                                Appentxt("直线检测区域为空，请确认是否需要设置？");
                                break;
                            }
                            Linedetection(mat2d, offsetAngle, SearchROI2, ref crossP);
                            dstPois.angle = line1AngleLx;
                            eumOutAngleMode = EumOutAngleMode.Absolute;
                            break;
                        case 2:
                            if (SearchROI2 == null || SearchROI2.Count < 1 ||
                                SearchROI2[0] == null)
                            {
                                Appentxt("圆检测区域为空，请确认是否需要设置？");
                                break;
                            }

                            Circledetection(mat2d, offsetAngle, SearchROI2, ref crossP);
                            break;
                        case 3:
                            if (SearchROI2 == null || SearchROI2.Count < 1 ||
                               SearchROI2[0] == null)
                            {
                                Appentxt("Blob检测区域为空，请确认是否需要设置？");
                                break;
                            }

                            Blobdetection(mat2d, offsetAngle, SearchROI2, ref crossP);
                            break;

                    }
                    dstPois.pointXY = crossP;
                }

                /*-------------------------------------*/
                Point2d robotP = new Point2d(0, 0);
                if (Hom_mat2d == null || Hom_mat2d.Width <= 0)
                    Appentxt("当前标定矩阵关系为空，请确认！");
                else
                {
                    if (!(dstPois.pointXY.X == 0 && dstPois.pointXY.Y == 0))
                        robotP = CalibrationTool.AffineTransPoint2d(Hom_mat2d, dstPois.pointXY);
                }

                //Point2d robotP = CalibrationTool.AffineTransPoint2d(Hom_mat2d, dstPois.pointXY);

                string buff = "[发送特征点位数据]";
                buff += string.Format("x:{0:f3},y:{1:f3},a:{2:f3},m:{3};",
                                 robotP.X, robotP.Y, -dstPois.angle, Enum.GetName(typeof(EumOutAngleMode), eumOutAngleMode));

                //信息打印
                float x = (float)stuModelMatchData.matchPoint.X;
                float y = (float)stuModelMatchData.matchPoint.Y;
                float a = (float)stuModelMatchData.matchOffsetAngle;

                this.Invoke(new Action(() =>
                {
                    if (stuModelMatchData.runFlag)
                    {
                        currvisiontool.DrawRegion(new RegionEx(new CrossF(x, y, 20, 20, 5), Color.Red));
                        currvisiontool.AddRegionBuffer(new RegionEx(new CrossF(x, y, 20, 20, 5), Color.Red));
                    }
                      
                    //currvisiontool.DrawText(new TextEx(string.Format("{0},{1},{2}",
                    //        robotP.X.ToString("f3"),
                    //         robotP.Y.ToString("f3"),
                    //         dstPois.angle.ToString("f3")), new Font("宋体", 16), new SolidBrush(Color.Red),
                    //         dstPois.pointXY.X, dstPois.pointXY.Y));

                    //currvisiontool.AddTextBuffer(new TextEx(string.Format("{0},{1},{2}",
                    //        robotP.X.ToString("f3"),
                    //         robotP.Y.ToString("f3"),
                    //         dstPois.angle.ToString("f3")), new Font("宋体", 16), new SolidBrush(Color.Red),
                    //         dstPois.pointXY.X, dstPois.pointXY.Y));

                    currvisiontool.DrawText(new TextEx(string.Format("特征点位X:{0},Y:{1},A:{2}", robotP.X.ToString("f3"),
                     robotP.Y.ToString("f3"), dstPois.angle.ToString("f3")))
                    { x = 10, y = 300 });
                    currvisiontool.AddTextBuffer(new TextEx(string.Format("特征点位X:{0},Y:{1},A:{2}", robotP.X.ToString("f3"),
                     robotP.Y.ToString("f3"), dstPois.angle.ToString("f3")))
                    { x = 10, y = 300 });
                }));
                Appentxt(buff);
                //用户坐标事件
                setModelPointHandle?.Invoke(buff.Replace("[发送特征点位数据]", ""), null);

                stopwatch.Stop();
                int spend = (int)stopwatch.ElapsedMilliseconds;
                currvisiontool.DetectionTime = spend;
            }
            else if (workstatus == EunmcurrCamWorkStatus.NormalTest_G)  //正常定位测试(点胶阀)
            {
                PositionData dstPois;
                EumOutAngleMode eumOutAngleMode = EumOutAngleMode.Relative;
                if (isUsingModelMatch)
                {
                    TestModelMatch();//不进行前处理
                    if (!stuModelMatchData.runFlag)
                    {
                        //用户坐标事件
                        setModelPointHandle?.Invoke("x:0,y:0,a:0;", null);
                        Appentxt("自动检测模板(点胶阀)匹配定位失败，无法获取坐标点");
                        return;
                    }
                    else
                    {
                        dstPois = new PositionData
                        {
                            pointXY = stuModelMatchData.matchPoint,

                            angle = stuModelMatchData.matchOffsetAngle
                        };
                    }
                }
                else if (isUsingAutoCircleMatch)
                {
                    dstPois = new PositionData
                    {
                        pointXY = stuModelMatchData.matchPoint,

                        angle = stuModelMatchData.matchOffsetAngle
                    };
                }
                else
                    dstPois = new PositionData { pointXY = new Point2d(0, 0), angle = 0 };
                //只给预留Blob检测工具
                if (CheckBoxselectID == 3)
                {
                    Point2d crossP = new Point2d(0, 0);
                    float x0 = float.Parse(modelOrigion.Split(',')[0]);
                    float y0 = float.Parse(modelOrigion.Split(',')[1]);
                    float a0 = float.Parse(modelOrigion.Split(',')[2]);

                    float x1 = (float)stuModelMatchData.matchPoint.X;
                    float y1 = (float)stuModelMatchData.matchPoint.Y;
                    float a1 = (float)stuModelMatchData.matchOffsetAngle;
                    Mat mat2d = new Mat();
                    float offsetAngle = a1;
                    if (isUsingModelMatch)
                        mat2d = MatExtension.getMat(new Point2f(x0, y0), new Point2f(x1, y1), offsetAngle);

                    else
                        mat2d = MatExtension.getMat(new Point2f(0, 0), new Point2f(0, 0), 0);

                    if (SearchROI3 == null || SearchROI3.Count < 1 ||
                                 SearchROI3[0] == null)
                        Appentxt("Blob检测区域为空，请确认是否需要设置？");
                    else
                        Blobdetection(mat2d, offsetAngle, SearchROI3, ref crossP);

                    dstPois.pointXY = crossP;
                }

                /*-------------------------------------*/
                Point2d robotP = new Point2d(0, 0);
                if (Hom_mat2d == null || Hom_mat2d.Width <= 0)
                    Appentxt("当前标定矩阵关系为空，请确认！");
                else
                {
                    if (!(dstPois.pointXY.X == 0 && dstPois.pointXY.Y == 0))
                        robotP = CalibrationTool.AffineTransPoint2d(Hom_mat2d, dstPois.pointXY);
                }

                //Point2d robotP = CalibrationTool.AffineTransPoint2d(Hom_mat2d, dstPois.pointXY);

                string buff = "[发送特征点位数据]";
                buff += string.Format("x:{0:f3},y:{1:f3},a:{2:f3},m:{3};",
                                 robotP.X, robotP.Y, -dstPois.angle, Enum.GetName(typeof(EumOutAngleMode), eumOutAngleMode));

              
                //信息打印
                float x = (float)stuModelMatchData.matchPoint.X;
                float y = (float)stuModelMatchData.matchPoint.Y;
                float a = (float)stuModelMatchData.matchOffsetAngle;

                this.Invoke(new Action(() =>
                {
                    if (stuModelMatchData.runFlag)
                    {
                        currvisiontool.DrawRegion(new RegionEx(new CrossF(x, y, 20, 20, 5), Color.Red));
                        currvisiontool.AddRegionBuffer(new RegionEx(new CrossF(x, y, 20, 20, 5), Color.Red));
                    }
                    
                    //currvisiontool.DrawText(new TextEx(string.Format("{0},{1},{2}",
                    //        robotP.X.ToString("f3"),
                    //         robotP.Y.ToString("f3"),
                    //         dstPois.angle.ToString("f3")), new Font("宋体", 16), new SolidBrush(Color.Red),
                    //         dstPois.pointXY.X, dstPois.pointXY.Y));

                    //currvisiontool.AddTextBuffer(new TextEx(string.Format("{0},{1},{2}",
                    //        robotP.X.ToString("f3"),
                    //         robotP.Y.ToString("f3"),
                    //         dstPois.angle.ToString("f3")), new Font("宋体", 16), new SolidBrush(Color.Red),
                    //         dstPois.pointXY.X, dstPois.pointXY.Y));

                    currvisiontool.DrawText(new TextEx(string.Format("特征点位X:{0},Y:{1},A:{2}", robotP.X.ToString("f3"),
                     robotP.Y.ToString("f3"), dstPois.angle.ToString("f3")))
                    { x = 10, y = 300 });
                    currvisiontool.AddTextBuffer(new TextEx(string.Format("特征点位X:{0},Y:{1},A:{2}", robotP.X.ToString("f3"),
                     robotP.Y.ToString("f3"), dstPois.angle.ToString("f3")))
                    { x = 10, y = 300 });
                }));

                Appentxt(buff);
                //用户坐标事件
                setModelPointHandle?.Invoke(buff.Replace("[发送特征点位数据]", ""), null);

                stopwatch.Stop();
                int spend = (int)stopwatch.ElapsedMilliseconds;
                currvisiontool.DetectionTime = spend;
            }

        }


        /// <summary>
        ///  直线检测
        /// </summary>
        /// <param name="homMat2d"></param>
        /// <param name="offsetangle"></param>
        /// <param name="SearchROI"></param>
        /// <param name="lineP"></param>
        /// <returns></returns>
        bool Linedetection(Mat homMat2d, float offsetangle, List<object> SearchROI, ref Point2d lineP)
        {
            RotatedCaliperRectF rotatedRectF = (RotatedCaliperRectF)SearchROI[0];
            RotatedCaliperRectF rotatedRectF2 = (RotatedCaliperRectF)SearchROI[1];
            //bool runFlag = false; bool runFlag2 = false;
            Point2f temP, temP2;
            Point2d p1 = new Point2d(0, 0);
            Point2d p2 = new Point2d(0, 0);
            Point2d p12 = new Point2d(0, 0);
            Point2d p22 = new Point2d(0, 0);
            var A = homMat2d.Get<double>(0, 0);
            var B = homMat2d.Get<double>(0, 1);
            var C = homMat2d.Get<double>(0, 2);    //Tx
            var D = homMat2d.Get<double>(1, 0);
            var E = homMat2d.Get<double>(1, 1);
            var F = homMat2d.Get<double>(1, 2);    //Ty

            temP.X = (float)((A * rotatedRectF.cx) + (B * rotatedRectF.cy) + C);
            temP.Y = (float)((D * rotatedRectF.cx) + (E * rotatedRectF.cy) + F);

            RotatedCaliperRectF rotatedRect = new RotatedCaliperRectF(temP.X, temP.Y,
              rotatedRectF.Width, rotatedRectF.Height, (float)(rotatedRectF.angle + offsetangle));
            ////////////////////////////////////////////////////////////////////////
            Mat dst = new Mat();
            Cv2.CvtColor(IsUsingPretreatment ? imageBuffer : GrabImg, dst, ColorConversionCodes.GRAY2BGR);
            ////////////////////////////
            temP2.X = (float)((A * rotatedRectF2.cx) + (B * rotatedRectF2.cy) + C);
            temP2.Y = (float)((D * rotatedRectF2.cx) + (E * rotatedRectF2.cy) + F);

            RotatedCaliperRectF rotatedRect2 = new RotatedCaliperRectF(temP2.X, temP2.Y,
              rotatedRectF2.Width, rotatedRectF2.Height, (float)(rotatedRectF2.angle + offsetangle));
            //直线1
            //霍夫直线参数
            this.Invoke(new Action(() =>
            {
                string tem = CobxfitMethod1.Text == "最小二乘法" ? "Least_square_method" : "Random_sampling_consistency";
                parmaData = new LinearCaliperData
                {
                    caliperWidth = (int)NumcaliperWidth1.Value,
                    caliperNum = (int)NumcaliperNum1.Value,
                    edgeThreshold = (byte)NumedgeThreshold1.Value,
                    fitMethod = (EumFittingMethod)Enum.Parse(typeof(EumFittingMethod), tem),
                    edgePolarity=(EumEdgePolarity)Enum.Parse(typeof(EumEdgePolarity), cobxPolarity1.Text)
                };
            }));
            runTool = new LinearCaliperTool();
            parmaData.ROI = rotatedRect;
            ResultOfToolRun = runTool.Run<LinearCaliperData>(IsUsingPretreatment ? imageBuffer : GrabImg,
               parmaData as LinearCaliperData);
          
            if (!ResultOfToolRun.runStatus)
            {
                StuLineResultData.runFlag = false;
                Appentxt("直线1拟合失败！");
            }
            else
            {
                p1 = (ResultOfToolRun as LinearCaliperResult).sp;
                p2 = (ResultOfToolRun as LinearCaliperResult).ep;

                StuLineResultData = new StuLineResultData((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
                if (Hom_mat2d != null && Hom_mat2d.Width > 0)
                {
                    Point2d r1 = CalibrationTool.AffineTransPoint2d(Hom_mat2d, p1);
                    Point2d r2 = CalibrationTool.AffineTransPoint2d(Hom_mat2d, p2);

                    line1AngleLx = new StuLineResultData((float)r1.X, (float)r1.Y, (float)r2.X, (float)r2.Y).GetAngle();

                    currvisiontool.DrawText(new TextEx(string.Format("直线1的角度angle：{0}", line1AngleLx.ToString("f3")),
                        new Font("宋体", 12f), new SolidBrush(Color.Green), 10, 10));

                    Appentxt(string.Format("直线1的角度angle：{0}", line1AngleLx.ToString("f3")));
                }
            }

            //绘制边缘点
            foreach (var s in (ResultOfToolRun as LinearCaliperResult).edges)
                dst.drawCross(s.ToPoint(), Scalar.LimeGreen, 10, 1);
            //绘制卡尺检测区域
            foreach (var s in (ResultOfToolRun as LinearCaliperResult).rotatedRectFs)
                dst.DrawRotatedRect(new CVRRect(new Point2f(s.centerP.X, s.centerP.Y),
                    new Size2f(s.size.Width, s.size.Height), s.angle));

            //temobj.Dispose();
            //直线2
            //霍夫直线参数
            this.Invoke(new Action(() =>
            {
                string tem = CobxfitMethod2.Text == "最小二乘法" ? "Least_square_method" : "Random_sampling_consistency";
                parmaData = new LinearCaliperData
                {
                    caliperWidth = (int)NumcaliperWidth2.Value,
                    caliperNum = (int)NumcaliperNum2.Value,
                    edgeThreshold = (byte)NumedgeThreshold2.Value,
                    fitMethod = (EumFittingMethod)Enum.Parse(typeof(EumFittingMethod), tem),
                    edgePolarity = (EumEdgePolarity)Enum.Parse(typeof(EumEdgePolarity), cobxPolarity2.Text)
                };
            }));
            runTool = new LinearCaliperTool();
            parmaData.ROI = rotatedRect2;
            ResultOfToolRun = runTool.Run<LinearCaliperData>(IsUsingPretreatment ? imageBuffer : GrabImg,
               parmaData as LinearCaliperData);
           
            if (!ResultOfToolRun.runStatus)
            {
                StuLineResultData2.runFlag = false;
                Appentxt("直线2拟合失败！");
            }
            else
            {
                p12 = (ResultOfToolRun as LinearCaliperResult).sp;
                p22 = (ResultOfToolRun as LinearCaliperResult).ep;
                StuLineResultData2 = new StuLineResultData((float)p12.X, (float)p12.Y, (float)p22.X, (float)p22.Y);
                if (Hom_mat2d != null && Hom_mat2d.Width > 0)
                {
                    Point2d r12 = CalibrationTool.AffineTransPoint2d(Hom_mat2d, p12);
                    Point2d r22 = CalibrationTool.AffineTransPoint2d(Hom_mat2d, p22);

                    line2AngleLx = new StuLineResultData((float)r12.X, (float)r12.Y, (float)r22.X, (float)r22.Y).GetAngle();

                    currvisiontool.DrawText(new TextEx(string.Format("直线2的角度angle：{0}", line2AngleLx.ToString("f3")),
                       new Font("宋体", 12f), new SolidBrush(Color.Green), 10, 20));

                    Appentxt(string.Format("直线2的角度angle：{0}", line2AngleLx.ToString("f3")));
                }
            }
           
            //绘制边缘点
            foreach (var s in (ResultOfToolRun as LinearCaliperResult).edges)
                dst.drawCross(s.ToPoint(), Scalar.LimeGreen, 10, 1);
            //绘制卡尺检测区域
            foreach (var s in (ResultOfToolRun as LinearCaliperResult).rotatedRectFs)
                dst.DrawRotatedRect(new CVRRect(new Point2f(s.centerP.X, s.centerP.Y),
                    new Size2f(s.size.Width, s.size.Height), s.angle));

            //计算交点
            Point2d crossP = new Point2d(0, 0);
            if (StuLineResultData.runFlag && StuLineResultData2.runFlag)
            {
                MatExtension.IntersectionPoint(
                 MatExtension.LineSegmentPoint2Line2D(new LineSegmentPoint(
                     new CVPoint(p1.X, p1.Y)
                     , new CVPoint(p2.X, p2.Y))
                 ),
                  MatExtension.LineSegmentPoint2Line2D(new LineSegmentPoint(
                     new CVPoint(p12.X, p12.Y)
                     , new CVPoint(p22.X, p22.Y)))
                , out crossP);

                dst.Line(new CVPoint(p1.X, p1.Y)
                     , new CVPoint(p2.X, p2.Y), Scalar.LimeGreen);
                dst.Line(new CVPoint(p12.X, p12.Y)
                     , new CVPoint(p22.X, p22.Y), Scalar.LimeGreen);
                dst.drawCross(new CVPoint(crossP.X, crossP.Y), Scalar.Red, 20, 2);
                currvisiontool.dispImage(dst);
            }
            else
            {
                Appentxt("直线未拟合，无法进行交点计算！");
                //   MessageBox.Show("直线未拟合，无法进行交点计算！");
                lineP = new Point2d(0, 0);

                currvisiontool.DrawText(new TextEx("直线未拟合，无法进行交点计算！") 
                { x = 800, y = 100, brush = new SolidBrush(Color.Red) });

                currvisiontool.AddTextBuffer(new TextEx("直线未拟合，无法进行交点计算！")
                { x = 800, y = 100, brush = new SolidBrush(Color.Red) });

                currvisiontool.dispImage(dst);
                return false;
            }
            lineP = new Point2d(crossP.X, crossP.Y);

            return true;

        }
        /// <summary>
        /// 圆检测
        /// </summary>
        /// <param name="homMat2d"></param>
        /// <param name="SearchROI"></param>
        /// <param name="centreP"></param>
        /// <returns></returns>
        bool Circledetection(Mat homMat2d, float offsetangle, List<object> SearchROI, ref Point2d centreP)
        {
            //if(SearchROI[0] is SectorF)
                sectorF = (SectorF)SearchROI[0];
            //else
            //    circleF = (CircleF)SearchROI[0];
            Point2f temP;
            var A = homMat2d.Get<double>(0, 0);
            var B = homMat2d.Get<double>(0, 1);
            var C = homMat2d.Get<double>(0, 2);    //Tx
            var D = homMat2d.Get<double>(1, 0);
            var E = homMat2d.Get<double>(1, 1);
            var F = homMat2d.Get<double>(1, 2);    //Ty
            //if (SearchROI[0] is SectorF)
            {
                temP.X = (float)((A * sectorF.centreP.X) + (B * sectorF.centreP.Y) + C);
                temP.Y = (float)((D * sectorF.centreP.X) + (E * sectorF.centreP.Y) + F);
            }
            //else
            //{
            //    temP.X = (float)((A * circleF.Centerx) + (B * circleF.Centery) + C);
            //    temP.Y = (float)((D * circleF.Centerx) + (E * circleF.Centery) + F);
            //}
              
            //变换后的扇形区域
            SectorF affineSectorF = new SectorF(new PointF(temP.X, temP.Y),
                                     (float)sectorF.getRadius,
                                         (float)(sectorF.startAngle + offsetangle),
                                                (float)(sectorF.sweepAngle ));
            //变换后的圆形区域
            CircleF affineCircleF = new CircleF(temP.X, temP.Y, (float)circleF.Radius);


            runTool = new CircularCaliperTool();
          
            parmaData = new CircularCaliperData 
            {
                caliperNum = (int)NumcaliperNum.Value,
                caliperWidth = (int)NumcaliperWidth.Value,
                edgeThreshold = (byte)NumedgeThreshold.Value,
                circleDir = (EumDirectionOfCircle)Enum.Parse(typeof(EumDirectionOfCircle), cobxCircleDir.Text),
                  edgePolarity = (EumEdgePolarity)Enum.Parse(typeof(EumEdgePolarity), cobxPolarity3.Text)
            };
            //if (SearchROI[0] is SectorF)
                parmaData.ROI = affineSectorF;
            //else
            //    parmaData.ROI = affineCircleF;

            ResultOfToolRun = runTool.Run<CircularCaliperData>(IsUsingPretreatment ? imageBuffer : GrabImg,
                           parmaData as CircularCaliperData);
            currvisiontool.dispImage(ResultOfToolRun.resultToShow);

            if (ResultOfToolRun.runStatus)
            {
                stuCircleResultData = new StuCircleResultData((float)(ResultOfToolRun as CircularCaliperResult).centreP.X,
                         (float)(ResultOfToolRun as CircularCaliperResult).centreP.Y,
                                (float)(ResultOfToolRun as CircularCaliperResult).radius);
                centreP =new Point2d ((ResultOfToolRun as CircularCaliperResult).centreP.X,
                                             (ResultOfToolRun as CircularCaliperResult).centreP.Y);
                return true;
            }

            else
            {
                stuCircleResultData.runFlag = false;
                Appentxt("圆查找失败！");
                currvisiontool.DrawText(new TextEx("圆查找失败！")
                { x = 1000, y = 100, brush = new SolidBrush(Color.Red) });
               
                currvisiontool.AddTextBuffer(new TextEx("圆查找失败！")
                { x = 1000, y = 100, brush = new SolidBrush(Color.Red) });
              
                centreP = new Point2d(0, 0);
                return false;
            }
        }

        /// <summary>
        ///  Blob检测
        /// </summary>
        /// <param name="homMat2d"></param>
        /// <param name="offsetangle"></param>
        /// <param name="SearchROI"></param>
        /// <param name="centreP"></param>
        /// <returns></returns>
        bool Blobdetection(Mat homMat2d, float offsetangle, List<object> SearchROI, ref Point2d centreP)
        {
            // RectangleF rectangleF = (RectangleF)SearchROI[0];
            RotatedRectF rotatedRectF = (RotatedRectF)SearchROI[0];
            //shapeConvert(new CVRect((int)rectangleF.X, (int)rectangleF.Y,
            //     (int)rectangleF.Width, (int)rectangleF.Height),out CVRRect cVRRect, 0);

            Point2f temP;
            var A = homMat2d.Get<double>(0, 0);
            var B = homMat2d.Get<double>(0, 1);
            var C = homMat2d.Get<double>(0, 2);    //Tx
            var D = homMat2d.Get<double>(1, 0);
            var E = homMat2d.Get<double>(1, 1);
            var F = homMat2d.Get<double>(1, 2);    //Ty


            temP.X = (float)((A * rotatedRectF.cx) + (B * rotatedRectF.cy) + C);
            temP.Y = (float)((D * rotatedRectF.cx) + (E * rotatedRectF.cy) + F);

            RotatedRectF newRotatedRectF = new RotatedRectF(temP.X, temP.Y, rotatedRectF.Width, rotatedRectF.Height,
                (float)(rotatedRectF.angle + offsetangle));

            //CVRRect rotatedRect = new CVRRect(new Point2f(temP.X, temP.Y),
            //  new Size2f(rotatedRectF.Width, rotatedRectF.Height),
            //            (float)(rotatedRectF.angle + offsetangle));

            //shapeConvert(rotatedRect, out CVRect temCVRect);


            runTool = new Blob3Tool();
            //Blob3检测参数
            parmaData = new Blob3Data
            {
                edgeThreshold = (double)Numminthd.Value,
                minArea = (int)NumareaLow.Value,
                maxArea = (int)NumareaHigh.Value,
                eumWhiteOrBlack = (EumWhiteOrBlack)Enum.Parse(typeof(EumWhiteOrBlack), cobxPolarity.Text)
            };
            parmaData.ROI = newRotatedRectF;
            ResultOfToolRun = runTool.Run<Blob3Data>(IsUsingPretreatment ? imageBuffer : GrabImg,
               parmaData as Blob3Data);

            currvisiontool.dispImage(ResultOfToolRun.resultToShow);
            if ((ResultOfToolRun as Blob3Result).positionData.Count > 0)
                stuBlobResultData = new StuBlobResultData((float)(ResultOfToolRun as Blob3Result).positionData[0].X,
                            (float)(ResultOfToolRun as Blob3Result).positionData[0].Y);
            else
            {
                stuBlobResultData.runFlag = false;
                Appentxt("Blob检测失败！");
            }

            //currvisiontool.clearOverlay();
            if (stuBlobResultData.runFlag)
            {
                centreP = (ResultOfToolRun as Blob3Result).positionData[0];
                return true;
            }
            else
            {
                currvisiontool.DrawText(new TextEx("Blob检测失败！",
                       new Font("宋体", 16), new SolidBrush(Color.Red),
                         1000, 100));

                currvisiontool.AddTextBuffer(new TextEx("Blob检测失败！")
                { x = 1000, y = 100, brush = new SolidBrush(Color.Red) });
                
                centreP = new Point2d(0, 0);
                return false;
            }

        }

        #endregion

        #region-----------DLL库连接-----------
        VirtualConnect virtualConnect = null;
        public delegate void GetDataHandle(string data);
        public event GetDataHandle GetDataOfCaliHandle = null;

        /// <summary>
        /// 外部数据写入DLL库
        /// </summary>
        /// <param name="data"></param>
        public void ExternWriteData(string data)
        {
            if (virtualConnect != null)
            {
                if (virtualConnect.IsRunning)
                {
                    virtualConnect.ReadData(data);
                }
            }
        }
        /// <summary>
        /// 建立虚拟连接
        /// </summary>
        /// <returns></returns>
        public bool BuiltConnect()
        {
            if (virtualConnect == null) return false;
            else if (virtualConnect.IsRunning) return true;
            virtualConnect.GetDataHandle += new EventHandler(GetDataEvent);
            virtualConnect.sendDataHandle += new VirtualConnect.SendDataHandle(sendDataEvent);
            return virtualConnect.StartConnect();

        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            if (virtualConnect == null) return false;
            else if (!virtualConnect.IsRunning) return true;
            virtualConnect.GetDataHandle -= GetDataEvent;
            return virtualConnect.Disconnnect();

        }
        /// <summary>
        /// 数据交互段
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void GetDataEvent(object sender, EventArgs e)
        {
            string strData = sender.ToString();
            Appentxt(string.Format("控制端接收信息：{0}", strData));
            if (strData.Contains("NP") || strData.Contains("C") || strData.Contains("T1")
                        || strData.Contains("T2") || strData.Contains("G"))   //视觉自动标定
            {
                // ModelMatchLoadParma(EumModelType.CalibModel);           
                if (strData.Contains("NP")) //9点标定流程
                {
                    this.Invoke(new Action(() =>
                    {
                        cobxModelType.SelectedIndex = 2; //标定模板
                    }));
                    string[] tempdataArray = strData.Split(',');
                    #region---//9点标定流程----------
                    switch (tempdataArray[1])
                    {
                        case "S":

                            //检测当前是否已经做好模板
                            //检查当前是否相机正常连接
                            //清除历史标记点位
                            //发送准备好信号，等待9点标记
                            this.Invoke(new Action(() =>
                            {
                                listViewPixel.Items.Clear();
                                i = 0;
                                listViewRobot.Items.Clear();
                                j = 0;
                            }));
                            NinePointStatusDic.Clear();
                            //if ((Hom_mat2d != null) &&(Hom_mat2d.Width>0) &&

                            if (CurrCam.IsAlive)

                            {
                                virtualConnect.WriteData("NP,S,OK");   //准备OK
                                Appentxt("9点标定准备好，开始标定");
                            }
                            else
                                virtualConnect.WriteData("NP,S,NG");  //未准备好
                            break;
                        case "E":
                            //校验9次模板匹配是否OK
                            //检测当前标定关系转换是否正常                     
                            //发送标定结果信号

                            bool flag = true;
                            foreach (var s in NinePointStatusDic)
                                flag &= s.Value;
                            Task.Factory.StartNew(new Action(() =>
                            {
                                coorditionConvert(); //9点标定关系转换
                                this.Invoke(new Action(() =>
                                {
                                    BtnSaveParmasOfNightPoints_click(null, null);//9点标定相关参数保存
                                }));
                            })).ContinueWith(t =>
                            {
                                virtualConnect.WriteData(string.Format("{0},{1}", "NP,E", flag ? "OK" : "NG"));
                                Appentxt("9点标定结束，标定结果" + (flag ? "OK" : "NG"));
                            });
                            break;
                        case "1":
                        case "2":
                        case "3":
                        case "4":
                        case "5":
                        case "6":
                        case "7":
                        case "8":
                        case "9":
                            //记录xy机械坐标点
                            //相机采集，模板匹配
                            //发送匹配结果信号
                            int key = int.Parse(tempdataArray[1]);
                            if (NinePointStatusDic.ContainsKey(key))
                            {
                                //TCPInfoAddText(string.Format("当前已经标记过第{0}点位", key));
                                virtualConnect.WriteData(string.Format("{0},{1},{2}", tempdataArray[0],
                                    tempdataArray[1], "NG"));
                            }
                            else
                            {
                                j++;
                                string[] temarray = new string[3] { tempdataArray[1], tempdataArray[2], tempdataArray[3] };
                                this.BeginInvoke(new Action(() =>
                                {
                                    SetValueToListItem(listViewRobot, temarray);//保存机器人点位到listview
                                }));
                                workstatus = EunmcurrCamWorkStatus.NinePointcLocation;
                                OneGrab();    //单次采集

                            }
                            break;

                    }
                    #endregion
                }
                else if (strData.Contains("C"))//旋转中心标定流程
                {
                    this.Invoke(new Action(() =>
                    {
                        cobxModelType.SelectedIndex = 2;  //标定模板
                    }));

                    string[] tempdataArray = strData.Split(',');
                    #region---//旋转中心标定流程---------
                    switch (tempdataArray[1])
                    {
                        case "S":

                            //检测当前是否已经做好模板
                            //检查当前是否相机正常连接
                            //清除历史标记点位
                            //发送准备好信号，等待旋转中心标定
                            this.Invoke(new Action(() =>
                            {
                                RoratepointListview.Items.Clear();
                                k = 0;
                            }));
                            RotatoStatusDic.Clear();
                            if ((Hom_mat2d != null) && (Hom_mat2d.Width > 0) &&
                                    (CurrCam.IsAlive))
                            {
                                virtualConnect.WriteData("C,S,OK");   //准备OK
                                Appentxt("旋转中心标定准备好,开始标定");
                            }

                            else
                                virtualConnect.WriteData("C,S,NG");  //未准备好

                            break;
                        case "E":
                            //校验5次模板匹配是否OK
                            //计算旋转中心                
                            //发送旋转中心标定结果信号

                            bool flag = true;
                            foreach (var s in RotatoStatusDic)
                                flag &= s.Value;

                            Task.Factory.StartNew(new Action(() =>
                            {
                                CaculateMultorRorateCenter(); //旋转中心计算
                                this.Invoke(new Action(() =>
                                {
                                    btnRatitoCaliDataSave_Click(null, null);  //旋转中心相关参数保存
                                }));

                            })).ContinueWith(t =>
                            {
                                virtualConnect.WriteData(string.Format("{0},{1}", "C,E", flag ? "OK" : "NG"));
                                Appentxt("旋转中心标定结束，标定结果" + (flag ? "OK" : "NG"));
                            });
                            break;
                        case "1":
                        case "2":
                        case "3":
                        case "4":
                        case "5":
                            //相机采集，模板匹配
                            //发送匹配结果信号
                            int key = int.Parse(tempdataArray[1]);
                            if (RotatoStatusDic.ContainsKey(key))
                            {
                                //CPInfoAddText(string.Format("当前旋转已经标记过第{0}点位", key));
                                virtualConnect.WriteData(string.Format("{0},{1},{2}", tempdataArray[0],
                                    tempdataArray[1], "NG"));
                            }
                            else
                            {
                                workstatus = EunmcurrCamWorkStatus.RotatoLocation; //旋转中心计算状态
                                OneGrab();    //单次采集

                            }
                            break;

                    }
                    #endregion
                }
                else if (strData.Contains("T1"))
                {
                    // ModelMatchLoadParma(EumModelType.ProductModel_1);
                    this.Invoke(new Action(() =>
                    {
                        cobxModelType.SelectedIndex = 0;
                    }));
                    stopwatch.Restart();
                    workstatus = EunmcurrCamWorkStatus.NormalTest_T1;
                    OneGrab();    //单次采集
                    Appentxt("开始自动检测,使用模板为产品1模板！");
                }
                else if (strData.Contains("T2"))
                {
                    //ModelMatchLoadParma(EumModelType.ProductModel_2);
                    this.Invoke(new Action(() =>
                    {
                        cobxModelType.SelectedIndex = 1;
                    }));
                    stopwatch.Restart();
                    workstatus = EunmcurrCamWorkStatus.NormalTest_T2;
                    OneGrab();    //单次采集
                    Appentxt("开始自动检测,使用模板为产品2模板！");
                }
                else if (strData.Contains("G"))
                {
                    // ModelMatchLoadParma(EumModelType.GluetapModel);
                    this.Invoke(new Action(() =>
                    {
                        cobxModelType.SelectedIndex = 3;
                    }));
                    stopwatch.Restart();
                    workstatus = EunmcurrCamWorkStatus.NormalTest_G;
                    OneGrab();    //单次采集
                    Appentxt("开始点胶阀示教检测");
                }
                else
                    return;
            }
        }

        void sendDataEvent(string data)
        {
            GetDataOfCaliHandle?.Invoke(data);
        }
        #endregion

        #region------------定位检测-----------
        /*-------------------------辅助工具---------------------------------------*/
        private void chxbImgBinarization_ValueChanged(object sender, bool value)
        {
            if (chxbImgBinarization.Checked)
                thresholdBar.Enabled = true;
            else
            {
                thresholdBar.Enabled = false;
                if (GrabImg == null || GrabImg.Empty() || GrabImg.Width <= 0)
                    return;
                currvisiontool.clearAll();
                currvisiontool.dispImage(GrabImg);
            }

        }

        private void thresholdBar_ValueChanged(object sender, EventArgs e)
        {
            int thresholdValue = thresholdBar.Value;
            lblThreshold.Text = thresholdValue.ToString();
            if (GrabImg == null || GrabImg.Empty() || GrabImg.Width <= 0)
                return;
            Mat dst = new Mat();
            Cv2.Threshold(GrabImg, dst, thresholdValue, 255, ThresholdTypes.Binary);
            currvisiontool.clearAll();
            currvisiontool.dispImage(dst);
        }
        /*-------------------------图像前处理---------------------------------------*/
        //图像前处理
        List<object> toolParList = new List<object>();
        int toolindex = -1;
        //工具新增
        private void toolStripLabel1_Click(object sender, EventArgs e)
        {
            switch ((sender as ToolStripLabel).Name)
            {
                case "膨胀toolStripLabel":
                    toolindex++;
                    listViewTools.Items.Add(new ListViewItem(
                        new string[2] { toolindex.ToString(), "膨胀" }));
                    gray_dilation_rect gray_Dilation_Rect = new gray_dilation_rect();
                    addControlToPanel(EumToolStatus.gray_dilation_rect, gray_Dilation_Rect);
                    toolParList.Add(gray_Dilation_Rect);
                    break;
                case "腐蚀toolStripLabel":
                    toolindex++;
                    listViewTools.Items.Add(new ListViewItem(
                        new string[2] { toolindex.ToString(), "腐蚀" }));
                    gray_erosion_rect gray_Erosion_Rect = new gray_erosion_rect();
                    addControlToPanel(EumToolStatus.gray_erosion_rect, gray_Erosion_Rect);
                    toolParList.Add(gray_Erosion_Rect);
                    break;
                case "开运算toolStripLabel":
                    toolindex++;
                    listViewTools.Items.Add(new ListViewItem(
                        new string[2] { toolindex.ToString(), "开运算" }));
                    gray_opening_rect gray_Opening_Rect = new gray_opening_rect();
                    addControlToPanel(EumToolStatus.gray_opening_rect, gray_Opening_Rect);
                    toolParList.Add(gray_Opening_Rect);
                    break;
                case "闭运算toolStripLabel":
                    toolindex++;
                    listViewTools.Items.Add(new ListViewItem(
                        new string[2] { toolindex.ToString(), "闭运算" }));
                    gray_closing_rect gray_Closing_Rect = new gray_closing_rect();
                    addControlToPanel(EumToolStatus.gray_closing_rect, gray_Closing_Rect);
                    toolParList.Add(gray_Closing_Rect);
                    break;
                case "常数toolStripLabel":
                    toolindex++;
                    listViewTools.Items.Add(new ListViewItem(
                        new string[2] { toolindex.ToString(), "常数" }));
                    scale_image scale_Image = new scale_image();
                    addControlToPanel(EumToolStatus.scale_image, scale_Image);
                    toolParList.Add(scale_Image);
                    break;
            }
            listViewTools.Update();
        }
        enum EumToolStatus
        {
            gray_dilation_rect,
            gray_erosion_rect,
            gray_opening_rect,
            gray_closing_rect,
            scale_image
        }
        UCPretreatmentParmas uCPretreatmentParmas;
        UCPretreatmentParmas2 uCPretreatmentParmas2;

        void addControlToPanel(EumToolStatus eumToolStatus, object dataobj)
        {
            if (eumToolStatus == EumToolStatus.gray_dilation_rect ||
                  eumToolStatus == EumToolStatus.gray_erosion_rect ||
                     eumToolStatus == EumToolStatus.gray_opening_rect ||
                          eumToolStatus == EumToolStatus.gray_closing_rect)
            {
                int _width = 3, _height = 3;
                switch (eumToolStatus)
                {
                    case EumToolStatus.gray_dilation_rect:
                        _width = (dataobj as gray_dilation_rect).MarkWidth;
                        _height = (dataobj as gray_dilation_rect).MarkHeight;
                        break;
                    case EumToolStatus.gray_erosion_rect:
                        _width = (dataobj as gray_erosion_rect).MarkWidth;
                        _height = (dataobj as gray_erosion_rect).MarkHeight;
                        break;
                    case EumToolStatus.gray_opening_rect:
                        _width = (dataobj as gray_opening_rect).MarkWidth;
                        _height = (dataobj as gray_opening_rect).MarkHeight;
                        break;
                    case EumToolStatus.gray_closing_rect:
                        _width = (dataobj as gray_closing_rect).MarkWidth;
                        _height = (dataobj as gray_closing_rect).MarkHeight;
                        break;
                }
                uCPretreatmentParmas.SetData(_width, _height);
                uCPretreatmentParmas.Dock = DockStyle.Fill;
                uCPretreatmentParmas.Margin = new Padding(3);
                panel14.Padding = new Padding(5);
                panel14.Controls.Clear();
                panel14.Controls.Add(uCPretreatmentParmas);

            }
            else
            {
                float numMult = 0.01f; int numAdd = 0;
                numMult = (dataobj as scale_image).Mult;
                numAdd = (dataobj as scale_image).Add;

                uCPretreatmentParmas2.SetData(numMult, numAdd);
                uCPretreatmentParmas2.Dock = DockStyle.Fill;
                uCPretreatmentParmas2.Margin = new Padding(3);
                panel14.Padding = new Padding(5);
                panel14.Controls.Clear();
                panel14.Controls.Add(uCPretreatmentParmas2);

            }
        }
        private void listViewTools_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewTools.SelectedItems.Count <= 0)
                return;
            int index = listViewTools.SelectedIndices[0];
            object obj = toolParList[index];
            int _width = 3, _height = 3;
            if (obj is gray_dilation_rect)
            {
                _width = (obj as gray_dilation_rect).MarkWidth;
                _height = (obj as gray_dilation_rect).MarkHeight;
                uCPretreatmentParmas.SetData(_width, _height);
                uCPretreatmentParmas.Margin = new Padding(3);
                uCPretreatmentParmas.Dock = DockStyle.Fill;
                panel14.Padding = new Padding(5);
                panel14.Controls.Clear();
                panel14.Controls.Add(uCPretreatmentParmas);
            }
            else if (obj is gray_erosion_rect)
            {
                _width = (obj as gray_erosion_rect).MarkWidth;
                _height = (obj as gray_erosion_rect).MarkHeight;
                uCPretreatmentParmas.SetData(_width, _height);
                uCPretreatmentParmas.Margin = new Padding(3);
                uCPretreatmentParmas.Dock = DockStyle.Fill;
                panel14.Padding = new Padding(5);
                panel14.Controls.Clear();
                panel14.Controls.Add(uCPretreatmentParmas);
            }
            else if (obj is gray_opening_rect)
            {
                _width = (obj as gray_opening_rect).MarkWidth;
                _height = (obj as gray_opening_rect).MarkHeight;
                uCPretreatmentParmas.SetData(_width, _height);
                uCPretreatmentParmas.Margin = new Padding(3);
                uCPretreatmentParmas.Dock = DockStyle.Fill;
                panel14.Padding = new Padding(5);
                panel14.Controls.Clear();
                panel14.Controls.Add(uCPretreatmentParmas);
            }
            else if (obj is gray_closing_rect)
            {
                _width = (obj as gray_closing_rect).MarkWidth;
                _height = (obj as gray_closing_rect).MarkHeight;
                uCPretreatmentParmas.SetData(_width, _height);
                uCPretreatmentParmas.Margin = new Padding(3);
                uCPretreatmentParmas.Dock = DockStyle.Fill;
                panel14.Padding = new Padding(5);
                panel14.Controls.Clear();
                panel14.Controls.Add(uCPretreatmentParmas);

            }
            else
            {
                float numMult = 0.01f; int numAdd = 0;
                numMult = (obj as scale_image).Mult;
                numAdd = (obj as scale_image).Add;
                uCPretreatmentParmas2.SetData(numMult, numAdd);
                uCPretreatmentParmas2.Margin = new Padding(3);
                uCPretreatmentParmas2.Dock = DockStyle.Fill;
                panel14.Padding = new Padding(5);
                panel14.Controls.Clear();
                panel14.Controls.Add(uCPretreatmentParmas2);

            }

        }
        private void listViewTools_MouseDown(object sender, MouseEventArgs e)
        {
            //if (e.Button != MouseButtons.Right)
            //    return;

            //判断是否在节点单击
            ListViewHitTestInfo test = listViewTools.HitTest(e.X, e.Y);


            if (test.Item == null || test.Location != ListViewHitTestLocations.Label)       //单击空白
            {
                listViewTools.ContextMenuStrip = null;
            }
            else
            {
                int index = test.Item.Index;
                listViewTools.Items[index].Selected = true;


                if (index == 0)
                    setRange(EumMenuPos.first);
                else if (index == listViewTools.Items.Count - 1)
                    setRange(EumMenuPos.last);
                else
                    setRange(EumMenuPos.normal);
                listViewTools.ContextMenuStrip = this.contextMenuStrip7;
                // rightClickMenu.Show(e.X, e.Y);
            }
        }
        void setRange(EumMenuPos eumMenuPos)
        {
            int itemNum = listViewTools.Items.Count;
            if (eumMenuPos == EumMenuPos.first)
            {
                if (itemNum >= 2)
                {
                    this.contextMenuStrip7.Items[0].Enabled = false;
                    this.contextMenuStrip7.Items[1].Enabled = true;
                }
                else
                {
                    this.contextMenuStrip7.Items[0].Enabled = false;
                    this.contextMenuStrip7.Items[1].Enabled = false;
                }
            }
            else if (eumMenuPos == EumMenuPos.last)
            {
                if (itemNum >= 2)
                {
                    this.contextMenuStrip7.Items[0].Enabled = true;
                    this.contextMenuStrip7.Items[1].Enabled = false;
                }
                else
                {
                    this.contextMenuStrip7.Items[0].Enabled = false;
                    this.contextMenuStrip7.Items[1].Enabled = false;
                }

            }
            else
            {
                this.contextMenuStrip7.Items[0].Enabled = true;
                this.contextMenuStrip7.Items[1].Enabled = true;
            }
        }

        enum EumMenuPos
        {
            first,
            last,
            normal

        }

        bool IsUsingPretreatment = false;  //是否启用前处理工具

        private void checkBox4_CheckedChanged(object sender, bool value)
        {
            IsUsingPretreatment = chxbPretreatmentTool.Checked;
            enbalePreControl(IsUsingPretreatment);

        }
        void enbalePreControl(bool flag)
        {
            PretreatmentToolPanel.Enabled = flag;

            //if (flag)
            //    listViewTools.BorderColor = Color.FromArgb(255, 109, 60);
            //else
            //    listViewTools.BorderColor = Color.Gray;

        }

        //工具参数保存
        private void 参数保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewTools.Items.Count > 0
            && listViewTools.SelectedItems.Count > 0)

            {
                int index = listViewTools.SelectedIndices[0];

                object obj = toolParList[index];
                int _width = 3, _height = 3;
                float numMult = 0.01f; int numAdd = 0;
                if (obj is gray_dilation_rect)
                {
                    uCPretreatmentParmas.GetData(ref _width, ref _height);
                    (obj as gray_dilation_rect).MarkWidth = _width;
                    (obj as gray_dilation_rect).MarkHeight = _height;
                }
                else if (obj is gray_erosion_rect)
                {
                    uCPretreatmentParmas.GetData(ref _width, ref _height);
                    (obj as gray_erosion_rect).MarkWidth = _width;
                    (obj as gray_erosion_rect).MarkHeight = _height;

                }
                else if (obj is gray_opening_rect)
                {
                    uCPretreatmentParmas.GetData(ref _width, ref _height);
                    (obj as gray_opening_rect).MarkWidth = _width;
                    (obj as gray_opening_rect).MarkHeight = _height;

                }
                else if (obj is gray_closing_rect)
                {
                    uCPretreatmentParmas.GetData(ref _width, ref _height);
                    (obj as gray_closing_rect).MarkWidth = _width;
                    (obj as gray_closing_rect).MarkHeight = _height;
                }
                else
                {
                    uCPretreatmentParmas2.GetData(ref numMult, ref numAdd);
                    (obj as scale_image).Mult = numMult;
                    (obj as scale_image).Add = numAdd;
                }
            }
        }

        private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewTools.SelectedItems.Count <= 0)
                return;

            int index = listViewTools.SelectedIndices[0];
            toolParList.RemoveAt(index);
            listViewTools.Items.Remove(listViewTools.SelectedItems[0]);
        }
        private void 上移ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewTools.SelectedItems.Count <= 0)
                return;
            int index = listViewTools.SelectedIndices[0];
            toolParList.Insert(index - 1, toolParList[index]);
            toolParList.RemoveAt(index + 1);
            ListViewItem listViewItem = listViewTools.SelectedItems[0].Clone() as ListViewItem;
            listViewTools.Items.Insert(index - 1, listViewItem);
            listViewTools.Items.RemoveAt(index + 1);
            listViewTools.Update();
        }

        private void 下移ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewTools.SelectedItems.Count <= 0)
                return;
            int index = listViewTools.SelectedIndices[0];
            toolParList.Insert(index + 2, toolParList[index]);
            toolParList.RemoveAt(index);
            ListViewItem listViewItem = listViewTools.SelectedItems[0].Clone() as ListViewItem;
            listViewTools.Items.Insert(index + 2, listViewItem);
            listViewTools.Items.RemoveAt(index);
        }

        private void 运行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = listViewTools.SelectedIndices[0];
            if (index < 0) return;
            if (CurrCam != null)
                if (CurrCam.IsGrabing)
                    btnStopGrab_Click(null, null);  //如果已在采集中则先停止采集
            Task.Run(delegate () { ToolTaskRun(index); });
        }

        Mat imageBuffer = new Mat();
        //工具运行
        void ToolTaskRun(int index)
        {
            if (GrabImg == null)
            {
                Appentxt("image is null");
                // MessageBox.Show("image is null");
                return;
            }

            Cv2.CopyTo(GrabImg, imageBuffer);
       
            for (int i = 0; i < index + 1; i++)  //工具循环执行
            {
                object obj = toolParList[i];
                Mat temimage ;         
                if (obj is gray_dilation_rect)//膨胀
                {
                    int _width = (obj as gray_dilation_rect).MarkWidth;
                    int _height = (obj as gray_dilation_rect).MarkHeight;            
                    temimage= Morphological_Proces.DilateImage(imageBuffer,
                                   MorphShapes.Rect,new OpenCvSharp.Size (_width, _height));
                    Cv2.CopyTo(temimage, imageBuffer);                   
                    temimage.Dispose();
                    temimage = null;
                }
                else if (obj is gray_erosion_rect)//腐蚀
                {
                    int _width = (obj as gray_erosion_rect).MarkWidth;
                    int _height = (obj as gray_erosion_rect).MarkHeight;
                    temimage = Morphological_Proces.ErodeImage(imageBuffer,
                                  MorphShapes.Rect, new OpenCvSharp.Size(_width, _height));

                    Cv2.CopyTo(temimage, imageBuffer);
                    temimage.Dispose();
                    temimage = null;

                }
                else if (obj is gray_opening_rect)//开运算
                {
                    int _width = (obj as gray_opening_rect).MarkWidth;
                    int _height = (obj as gray_opening_rect).MarkHeight;
                    temimage = Morphological_Proces.MorphologyEx(imageBuffer, MorphShapes.Rect,
                                  new OpenCvSharp.Size(_width, _height), MorphTypes.Open);

                    Cv2.CopyTo(temimage, imageBuffer);
                    temimage.Dispose();
                    temimage = null;

                }
                else if (obj is gray_closing_rect)//闭运算
                {
                    int _width = (obj as gray_closing_rect).MarkWidth;
                    int _height = (obj as gray_closing_rect).MarkHeight;
                    temimage = Morphological_Proces.MorphologyEx(imageBuffer, MorphShapes.Rect,
                                   new OpenCvSharp.Size(_width, _height), MorphTypes.Close);

                    Cv2.CopyTo(temimage, imageBuffer);
                    temimage.Dispose();
                    temimage = null;

                }
                else//常数计算
                {
                    float numMult = (obj as scale_image).Mult;
                    int numAdd = (obj as scale_image).Add;

                    temimage= ImageEmphize.scale_image(imageBuffer, numMult, numAdd);
                    Cv2.CopyTo(temimage, imageBuffer);
                    temimage.Dispose();
                    temimage = null;
                }
            }
            //结果显示
        
            currvisiontool.dispImage(imageBuffer);
           
        }

        /*----------------------------------------------------------------*/
      
        //模板搜索方式
        EumMatchSearchMethod eumMatchSearchMethod = EumMatchSearchMethod.AllRegion;
        //模板搜索区域
        RectangleF matchSearchRegion =new Rect(0,0,1000,1000);
        /// <summary>
        /// 模板匹配范围设定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cobxMatchRegion_SelectedIndexChanged(object sender, EventArgs e)
        {
            string serachName = cobxMatchSearchRegion.Text;
            if (serachName == "全图搜索")
            {
                eumMatchSearchMethod = EumMatchSearchMethod.AllRegion;
                btnModelSearchRegion.Enabled = false;
            }    
            else
            {
                eumMatchSearchMethod = EumMatchSearchMethod.PartRegion;
                btnModelSearchRegion.Enabled = true;
            }              
        }

        enum EumMatchMethod
        { 
            ShapeMatch,
            NccMatch,
            CannyMatch
        
        }
        EumMatchMethod currMatchMethod = EumMatchMethod.ShapeMatch;//当时模板匹配使用方法
       bool NoChangeFlag = false;
        /// <summary>
        /// 模板匹配方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cobxTemplateMatchMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (NoChangeFlag)
            {
                NoChangeFlag = false;
                return;
            }

            currMatchMethod = (EumMatchMethod)Enum.Parse(typeof(EumMatchMethod), cobxTemplateMatchMethod.Text);
      
            reloadDataOfMatchMethod(currMatchMethod);


        }
        /// <summary>
        /// 模板匹配方法切换，参数重载
        /// </summary>
        void reloadDataOfMatchMethod(EumMatchMethod method)
        {
            if(this.InvokeRequired)
            {
                this.Invoke(new Action<EumMatchMethod>(reloadDataOfMatchMethod), method);
            }
            else
            {
                this.modeltp = new Mat();
                if (method == EumMatchMethod.ShapeMatch)
                {
                    label25.Text = "分割阈值:";
                    label28.Text = "匹配得分:";
                    label22.Text = "最小长度:";
                    label23.Text = "最大长度:";
                    label24.Text = "最小面积:";
                    label26.Visible = true;
                    NumMaxContourArea.Visible = true;
                    //分割阈值
                    NumSegthreshold.Maximum = 255;
                    NumSegthreshold.Minimum = 0;
                    NumSegthreshold.DecimalPlaces = 0;
                    NumSegthreshold.Increment = 1;
                    //匹配得分
                    NumMatchValue.Maximum = 1.0m;
                    NumMatchValue.Minimum = 0.3m;
                    NumMatchValue.DecimalPlaces = 2;
                    NumMatchValue.Increment = 0.1m;
                    //轮廓最小长度
                    NumMincoutourLen.Maximum = 99999999;
                    NumMincoutourLen.Minimum = 0;
                    NumMincoutourLen.DecimalPlaces = 0;
                    NumMincoutourLen.Increment = 100;
                    //轮廓最大长度
                    NumMaxcoutourLen.Maximum = 99999999;
                    NumMaxcoutourLen.Minimum = 0;
                    NumMaxcoutourLen.DecimalPlaces = 0;
                    NumMaxcoutourLen.Increment = 100;
                    //轮廓最小面积
                    NumMinContourArea.Maximum = 99999999;
                    NumMinContourArea.Minimum = 0;
                    NumMinContourArea.DecimalPlaces = 0;
                    NumMinContourArea.Increment = 100;

                    if (currModelType == EumModelType.ProductModel_1)
                    {
                        //模板加载
                        string templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\ShapeMatch" + "\\模板.png";
                        if (File.Exists(templatePath))
                            this.modeltp = MatDataWriteRead.ReadImage(templatePath);
                        parmaData = GeneralUse.ReadSerializationFile<ShapeMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1" + "\\形状匹配.ini");

                    }
                    else if (currModelType == EumModelType.ProductModel_2)
                    {
                        //模板加载
                        string templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\ShapeMatch" + "\\模板.png";
                        if (File.Exists(templatePath))
                            this.modeltp = MatDataWriteRead.ReadImage(templatePath);
                        parmaData = GeneralUse.ReadSerializationFile<ShapeMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2" + "\\形状匹配.ini");

                    }
                    else if (currModelType == EumModelType.CaliBoardModel)
                    {
                        //模板加载
                        string templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\ShapeMatch" + "\\模板.png";
                        if (File.Exists(templatePath))
                            this.modeltp = MatDataWriteRead.ReadImage(templatePath);
                        parmaData = GeneralUse.ReadSerializationFile<ShapeMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel" + "\\形状匹配.ini");

                    }
                    else if (currModelType == EumModelType.GluetapModel)
                    {
                        //模板加载
                        string templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\ShapeMatch" + "\\模板.png";
                        if (File.Exists(templatePath))
                            this.modeltp = MatDataWriteRead.ReadImage(templatePath);
                        parmaData = GeneralUse.ReadSerializationFile<ShapeMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel" + "\\形状匹配.ini");

                    }
                    else
                    {
                        this.modeltp = null;
                        parmaData = new ShapeMatchData();
                    }
                    picTemplate.ContextMenuStrip = null;
                    modelangle = (parmaData as ShapeMatchData).baseAngle;
                    templateContour = (parmaData as ShapeMatchData).tpContour;
                    NumSegthreshold.Value = (decimal)(parmaData as ShapeMatchData).Segthreshold;
                    NumMatchValue.Value = (decimal)(parmaData as ShapeMatchData).MatchValue;
                    NumMincoutourLen.Value = (decimal)(parmaData as ShapeMatchData).MincoutourLen;
                    NumMaxcoutourLen.Value = (decimal)(parmaData as ShapeMatchData).MaxcoutourLen;
                    NumMinContourArea.Value = (decimal)(parmaData as ShapeMatchData).MinContourArea;
                    NumMaxContourArea.Value = (decimal)(parmaData as ShapeMatchData).MaxContourArea;

                    if (matchBaseInfo == null)
                        matchBaseInfo = new MatchBaseInfo();
                    lIstModelInfo.Items.Clear();
                    lIstModelInfo.Items.Add(new ListViewItem(
                        new string[] { "BaseX", matchBaseInfo.BaseX.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                      new string[] { "BaseY", matchBaseInfo.BaseY.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                      new string[] { "BaseAngle", matchBaseInfo.BaseAngle.ToString("f3") }));

                    lIstModelInfo.Items.Add(new ListViewItem(
                         new string[] { "ContourLength", matchBaseInfo.ContourLength.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                         new string[] { "ContourArea", matchBaseInfo.ContourArea.ToString("f3") }));

                }
                else if (method == EumMatchMethod.NccMatch)
                {
                    label25.Text = "起始角度:";
                    label28.Text = "角度范围:";
                    label22.Text = "角度步长:";
                    label23.Text = "金字塔数:";
                    label24.Text = "匹配得分:";
                    label26.Visible = false;
                    NumMaxContourArea.Visible = false;
                    //起始角度
                    NumSegthreshold.Maximum = -10;
                    NumSegthreshold.Minimum = -180;
                    NumSegthreshold.DecimalPlaces = 0;
                    NumSegthreshold.Increment = 10;
                    //角度范围
                    NumMatchValue.Maximum = 360;
                    NumMatchValue.Minimum = 0;
                    NumMatchValue.DecimalPlaces = 0;
                    NumMatchValue.Increment = 10;
                    //角度步长
                    NumMincoutourLen.Maximum = 10;
                    NumMincoutourLen.Minimum = 1;
                    NumMincoutourLen.DecimalPlaces = 0;
                    NumMincoutourLen.Increment = 1;
                    //金字塔数
                    NumMaxcoutourLen.Maximum = 5;
                    NumMaxcoutourLen.Minimum = 0;
                    NumMaxcoutourLen.DecimalPlaces = 0;
                    NumMaxcoutourLen.Increment = 1;
                    //匹配得分
                    NumMinContourArea.Maximum = 1.0m;
                    NumMinContourArea.Minimum = 0.3m;
                    NumMinContourArea.DecimalPlaces = 2;
                    NumMinContourArea.Increment = 0.1m;

                    if (currModelType == EumModelType.ProductModel_1)
                    {
                        //模板加载
                        string templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\NccMatch" + "\\显示模板.png";
                        if (File.Exists(templatePath))
                            this.modeltp = MatDataWriteRead.ReadImage(templatePath);
                        //模板加载
                        templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\NccMatch" + "\\测试模板.png";
                        if (File.Exists(templatePath))
                            this.modelGrayMat = MatDataWriteRead.ReadImage(templatePath);
                        parmaData = GeneralUse.ReadSerializationFile<NccTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1" + "\\Ncc匹配.ini");

                    }

                    else if (currModelType == EumModelType.ProductModel_2)
                    {
                        //模板加载
                        string templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\NccMatch" + "\\显示模板.png";
                        if (File.Exists(templatePath))
                            this.modeltp = MatDataWriteRead.ReadImage(templatePath);
                        //模板加载
                        templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\NccMatch" + "\\测试模板.png";
                        if (File.Exists(templatePath))
                            this.modelGrayMat = MatDataWriteRead.ReadImage(templatePath);
                        parmaData = GeneralUse.ReadSerializationFile<NccTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2" + "\\Ncc匹配.ini");

                    }

                    else if (currModelType == EumModelType.CaliBoardModel)
                    {
                        //模板加载
                        string templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\NccMatch" + "\\显示模板.png";
                        if (File.Exists(templatePath))
                            this.modeltp = MatDataWriteRead.ReadImage(templatePath);
                        //模板加载
                        templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\NccMatch" + "\\测试模板.png";
                        if (File.Exists(templatePath))
                            this.modelGrayMat = MatDataWriteRead.ReadImage(templatePath);
                        parmaData = GeneralUse.ReadSerializationFile<NccTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel" + "\\Ncc匹配.ini");

                    }

                    else if (currModelType == EumModelType.GluetapModel)
                    {
                        //模板加载
                        string templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\NccMatch" + "\\显示模板.png";
                        if (File.Exists(templatePath))
                            this.modeltp = MatDataWriteRead.ReadImage(templatePath);
                        //模板加载
                        templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\NccMatch" + "\\测试模板.png";
                        if (File.Exists(templatePath))
                            this.modelGrayMat = MatDataWriteRead.ReadImage(templatePath);
                        parmaData = GeneralUse.ReadSerializationFile<NccTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel" + "\\Ncc匹配.ini");

                    }

                    else
                    {
                        this.modeltp = null;
                        parmaData = new NccTemplateMatchData();
                    }

                    picTemplate.ContextMenuStrip = null;
                    //modeltp = (parmaData as NccTemplateMatchData).tpl;
                    NumSegthreshold.Value = (decimal)(parmaData as NccTemplateMatchData).StartAngle;
                    NumMatchValue.Value = (decimal)(parmaData as NccTemplateMatchData).AngleRange;
                    NumMincoutourLen.Value = (decimal)(parmaData as NccTemplateMatchData).AngleStep;
                    NumMaxcoutourLen.Value = (decimal)(parmaData as NccTemplateMatchData).NumLevels;
                    NumMinContourArea.Value = (decimal)(parmaData as NccTemplateMatchData).MatchScore;

                    if (matchBaseInfo == null)
                        matchBaseInfo = new MatchBaseInfo();
                    lIstModelInfo.Items.Clear();
                    lIstModelInfo.Items.Add(new ListViewItem(
                        new string[] { "BaseX", matchBaseInfo.BaseX.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                      new string[] { "BaseY", matchBaseInfo.BaseY.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                      new string[] { "BaseAngle", matchBaseInfo.BaseAngle.ToString("f3") }));
                }
                else
                {
                    label25.Text = "起始角度:";
                    label28.Text = "角度范围:";
                    label22.Text = "分割阈值:";
                    label23.Text = "金字塔数:";
                    label24.Text = "匹配得分:";
                    label26.Visible = false;
                    NumMaxContourArea.Visible = false;

                    //起始角度
                    NumSegthreshold.Maximum = -10;
                    NumSegthreshold.Minimum = -180;
                    NumSegthreshold.DecimalPlaces = 0;
                    NumSegthreshold.Increment = 10;
                    //角度范围
                    NumMatchValue.Maximum = 360;
                    NumMatchValue.Minimum = 0;
                    NumMatchValue.DecimalPlaces = 0;
                    NumMatchValue.Increment = 10;
                    //分割阈值
                    NumMincoutourLen.Maximum = 255;
                    NumMincoutourLen.Minimum = 0;
                    NumMincoutourLen.DecimalPlaces = 0;
                    NumMincoutourLen.Increment = 1;
                    //金字塔数
                    NumMaxcoutourLen.Maximum = 5;
                    NumMaxcoutourLen.Minimum = 0;
                    NumMaxcoutourLen.DecimalPlaces = 0;
                    NumMaxcoutourLen.Increment = 1;
                    //匹配得分
                    NumMinContourArea.Maximum = 1.0m;
                    NumMinContourArea.Minimum = 0.3m;
                    NumMinContourArea.DecimalPlaces = 2;
                    NumMinContourArea.Increment = 0.1m;

                    if (currModelType == EumModelType.ProductModel_1)
                    {
                        //模板加载
                        string templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\CannyMatch" + "\\原图模板.png";
                        if (File.Exists(templatePath))
                            this.modeltp = MatDataWriteRead.ReadImage(templatePath);

                        //模板加载
                        templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\CannyMatch" + "\\Canny模板.png";
                        if (File.Exists(templatePath))
                            this.cannyMat = MatDataWriteRead.ReadImage(templatePath);

                        parmaData = GeneralUse.ReadSerializationFile<CannyTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1" + "\\Canny匹配.ini");

                    }
                    else if (currModelType == EumModelType.ProductModel_2)
                    {
                        //模板加载
                        string templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\CannyMatch" + "\\原图模板.png";
                        if (File.Exists(templatePath))
                            this.modeltp = MatDataWriteRead.ReadImage(templatePath);
                        //模板加载
                        templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\CannyMatch" + "\\Canny模板.png";
                        if (File.Exists(templatePath))
                            this.cannyMat = MatDataWriteRead.ReadImage(templatePath);

                        parmaData = GeneralUse.ReadSerializationFile<CannyTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2" + "\\Canny匹配.ini");

                    }
                    else if (currModelType == EumModelType.CaliBoardModel)
                    {
                        //模板加载
                        string templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\CannyMatch" + "\\原图模板.png";
                        if (File.Exists(templatePath))
                            this.modeltp = MatDataWriteRead.ReadImage(templatePath);
                        //模板加载
                        templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\CannyMatch" + "\\Canny模板.png";
                        if (File.Exists(templatePath))
                            this.cannyMat = MatDataWriteRead.ReadImage(templatePath);

                        parmaData = GeneralUse.ReadSerializationFile<CannyTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel" + "\\Canny匹配.ini");

                    }
                    else if (currModelType == EumModelType.GluetapModel)
                    {
                        //模板加载
                        string templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\CannyMatch" + "\\原图模板.png";
                        if (File.Exists(templatePath))
                            this.modeltp = MatDataWriteRead.ReadImage(templatePath);
                        //模板加载
                        templatePath = "配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\CannyMatch" + "\\Canny模板.png";
                        if (File.Exists(templatePath))
                            this.cannyMat = MatDataWriteRead.ReadImage(templatePath);

                        parmaData = GeneralUse.ReadSerializationFile<CannyTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel" + "\\Canny匹配.ini");

                    }
                    else
                    {
                        this.modeltp = new Mat();
                        this.cannyMat = new Mat();
                        parmaData = new CannyTemplateMatchData();
                    }
                    picTemplate.ContextMenuStrip = this.contextMenuStrip6;
                    (contextMenuStrip6.Items[0] as ToolStripMenuItem).Checked = true;
                    (contextMenuStrip6.Items[1] as ToolStripMenuItem).Checked = false;
                    //modeltp = (parmaData as CannyTemplateMatchData).tpl;
                    NumSegthreshold.Value = (decimal)(parmaData as CannyTemplateMatchData).StartAngle;
                    NumMatchValue.Value = (decimal)(parmaData as CannyTemplateMatchData).AngleRange;
                    NumMincoutourLen.Value = (decimal)(parmaData as CannyTemplateMatchData).SegmentThresh;
                    NumMaxcoutourLen.Value = (decimal)(parmaData as CannyTemplateMatchData).NumLevels;
                    NumMinContourArea.Value = (decimal)(parmaData as CannyTemplateMatchData).MatchScore;

                    if (matchBaseInfo == null)
                        matchBaseInfo = new MatchBaseInfo();
                    lIstModelInfo.Items.Clear();
                    lIstModelInfo.Items.Add(new ListViewItem(
                        new string[] { "BaseX", matchBaseInfo.BaseX.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                      new string[] { "BaseY", matchBaseInfo.BaseY.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                      new string[] { "BaseAngle", matchBaseInfo.BaseAngle.ToString("f3") }));
                }

           
                if (!this.modeltp.Empty() && this.modeltp.Width > 0)
                    picTemplate.Image = BitmapConverter.ToBitmap(this.modeltp);
                else
                    picTemplate.Image = null;
            }
          
        }
        private void 原图ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            原图ToolStripMenuItem.Checked = true;
            canny图ToolStripMenuItem.Checked = false;
            picTemplate.Image = BitmapConverter.ToBitmap(modeltp);

        }

        private void canny图ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            原图ToolStripMenuItem.Checked = false;
            canny图ToolStripMenuItem.Checked = true;
            picTemplate.Image = BitmapConverter.ToBitmap(cannyMat);
        }

        //创建模板
        private void btncreateModel_Click(object sender, EventArgs e)
        {
            if (!IsUsingPretreatment)  //不进行前处理
            {
                if (GrabImg == null || GrabImg.Width <= 0)
                {
                    Appentxt("未获取图像");
                    stuModelMatchData.runFlag = false;
                    return;
                }
            }
            else
            {
                if (imageBuffer.Empty() || imageBuffer.Width <= 0)
                {
                    Appentxt("未获取预处理图像");
                    stuModelMatchData.runFlag = false;
                    return;
                }
            }
            List<RectangleF> roiList = currvisiontool.getRoiList<RectangleF>();
            if (roiList.Count <= 0)
            {
                MessageBox.Show("请设置模板创建区域{矩形}");
                return;

            }
            if (MessageBox.Show("确认创建新模板？", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                                  == DialogResult.Yes)
            {
                CVRect cVRect = new CVRect((int)roiList[0].X, (int)roiList[0].Y, (int)roiList[0].Width, (int)roiList[0].Height);
                Mat tp = MatExtension.Crop_Mask_Mat(IsUsingPretreatment ? imageBuffer : GrabImg, cVRect);

                if (currMatchMethod == EumMatchMethod.ShapeMatch)//形状匹配
                {
                    picTemplate.ContextMenuStrip = null;
                    templateContour = null;
                    coutourLen = 100;
                    NumMincoutourLen.Value = 100;
                    contourArea = 100;
                    NumMinContourArea.Value = 100;
                    double modelx = 0, modely = 0;


                    runTool = new ShapeMatchTool();
                    parmaData = new ShapeMatchData();
                    (parmaData as ShapeMatchData).Segthreshold = (double)NumSegthreshold.Value;

                    modeltp = (runTool as ShapeMatchTool).CreateTemplateContours(tp,
                         (parmaData as ShapeMatchData).Segthreshold, cVRect,
                        ref templateContour,
                        ref coutourLen, ref contourArea, ref modelx, ref modely, ref modelangle);

                    picTemplate.Image = BitmapConverter.ToBitmap(modeltp);
                    if (templateContour == null)
                    {
                        MessageBox.Show("模板创建失败！");
                        return;
                    }
                    modelx += cVRect.X;
                    modely += cVRect.Y;
                    lIstModelInfo.Items.Clear();
                    lIstModelInfo.Items.Add(new ListViewItem(
                        new string[] { "BaseX", modelx.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                      new string[] { "BaseY", modely.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                      new string[] { "BaseAngle", modelangle.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                     new string[] { "ContourLength", coutourLen.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                     new string[] { "ContourArea", contourArea.ToString("f3") }));

                    modelOrigion = string.Format("{0},{1},{2}",
                          modelx.ToString("f3"),
                              modely.ToString("f3"),
                                  modelangle.ToString("f3"));

                    if (coutourLen * 0.8 > (double)NumMincoutourLen.Maximum ||
                          contourArea * 0.8 > (double)NumMinContourArea.Maximum)
                    {
                        MessageBox.Show("模板创建完成失败，模板区域过大！");
                        return;
                    }
                    NumMincoutourLen.Value = (decimal)(coutourLen * 0.8);
                    NumMaxcoutourLen.Value = (decimal)(coutourLen * 1.2);

                    NumMinContourArea.Value = (decimal)(contourArea * 0.8);
                    NumMaxContourArea.Value = (decimal)(contourArea * 1.2);

                    NumMatchValue.Value = (decimal)0.5;
                    MessageBox.Show("模板创建完成！");
                }
                else if(currMatchMethod == EumMatchMethod.NccMatch)//NCC匹配
                {
                    picTemplate.ContextMenuStrip =null;
                    double modelx = 0, modely = 0;
                    modelangle = 0;
                    runTool = new NccTemplateMatchTool();

                    modeltp = (runTool as NccTemplateMatchTool).CreateTemplateNcc(IsUsingPretreatment ? imageBuffer : GrabImg, cVRect,
                        ref modelx, ref modely,ref modelGrayMat);
                    
                    picTemplate.Image = BitmapConverter.ToBitmap(modeltp);

                    lIstModelInfo.Items.Clear();
                    lIstModelInfo.Items.Add(new ListViewItem(
                        new string[] { "BaseX", modelx.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                      new string[] { "BaseY", modely.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                      new string[] { "BaseAngle", modelangle.ToString("f3") }));

                    modelOrigion = string.Format("{0},{1},{2}",
                          modelx.ToString("f3"),
                              modely.ToString("f3"),
                                  modelangle.ToString("f3"));
                    MessageBox.Show("模板创建完成！");
                }
                else//Canny匹配
                {
                    picTemplate.ContextMenuStrip = this.contextMenuStrip6;
                    double modelx = 0, modely = 0;
                    modelangle = 0;
                    runTool = new CannyTemplateMatchTool();
                    double thresh = (double)NumMincoutourLen.Value;
                    modeltp = (runTool as CannyTemplateMatchTool).CreateTemplateCanny(IsUsingPretreatment ? imageBuffer : GrabImg, cVRect,
                     thresh, ref cannyMat,  ref modelx, ref modely);

                    picTemplate.Image = BitmapConverter.ToBitmap(modeltp);
                    (contextMenuStrip6.Items[0] as ToolStripMenuItem).Checked = true;
                    (contextMenuStrip6.Items[1] as ToolStripMenuItem).Checked = false;

                    lIstModelInfo.Items.Clear();
                    lIstModelInfo.Items.Add(new ListViewItem(
                        new string[] { "BaseX", modelx.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                      new string[] { "BaseY", modely.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                      new string[] { "BaseAngle", modelangle.ToString("f3") }));

                    modelOrigion = string.Format("{0},{1},{2}",
                          modelx.ToString("f3"),
                              modely.ToString("f3"),
                                  modelangle.ToString("f3"));
                    MessageBox.Show("模板创建完成！");
                }
            }
        }
        //模板搜索区域设定
        private void btnModelSearchRegion_Click(object sender, EventArgs e)
        {
            List<RectangleF> roiList = currvisiontool.getRoiList<RectangleF>();
            if (roiList.Count <= 0)
            {
                MessageBox.Show("请设置模板搜索区域{矩形}");
                return;

            }
            if (MessageBox.Show("确认创建模板搜索区域？", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                                  == DialogResult.Yes)
            {
                matchSearchRegion = roiList[0];               
            }
        }
        //模板匹配测试
        private void btnTest_modelMatch_Click(object sender, EventArgs e)
        {
            stopwatch.Restart();
            currvisiontool.clearAll();
            TestModelMatch();
            stopwatch.Stop();
            int spend = (int)stopwatch.ElapsedMilliseconds;
            currvisiontool.DetectionTime = spend;
        }
        private void picTemplate_MouseHover(object sender, EventArgs e)
        {
            if (this.modeltp.Empty() || this.modeltp.Width <= 0) return;
            frmTemplateShow f = frmTemplateShow.createInstance();
            if (currMatchMethod != EumMatchMethod.CannyMatch)
                f.ImageShow = BitmapConverter.ToBitmap(this.modeltp);
            else
            {
                if (原图ToolStripMenuItem.Checked)
                    f.ImageShow = BitmapConverter.ToBitmap(this.modeltp);
                else
                    f.ImageShow = BitmapConverter.ToBitmap(this.cannyMat);
            }
            f.Owner = this;
            f.StartPosition = FormStartPosition.CenterParent;
            f.Show();
            f.UpdateShow();
        }
        private void picTemplate_MouseLeave(object sender, EventArgs e)
        {
            if (this.modeltp.Empty() || this.modeltp.Width <= 0) return;
            frmTemplateShow f = frmTemplateShow.createInstance(BitmapConverter.ToBitmap(this.modeltp));
            f.Hide();
        }


        //模板参数保存
        private void btnSaveModel_Click(object sender, EventArgs e)
        {
            SaveModelMatchParma();
        }
        /*---------------------------------------------------------------------*/
        //自动圆匹配
        private void btnTestAutoCircleMatch_Click(object sender, EventArgs e)
        {
         
            if (GrabImg == null || GrabImg.Width <= 0)
            {
                MessageBox.Show("未获取图像");

            }
            List<RectangleF> roiList = currvisiontool.getRoiList<RectangleF>();
            if (roiList.Count <= 0)
            {
                MessageBox.Show("请设置自动圆匹配区域{矩形}");
                return;

            }
            stopwatch.Restart();
            MatchCircleRun();
            int count = outPutDataOfCircleMatch.stuCircleResultDatas.Count;
            //Mat dst = new Mat();
            //Cv2.CvtColor(GrabImg,dst, ColorConversionCodes.GRAY2BGR);
            currvisiontool.textExlist.Clear();
            for (int i = 0; i < count; i++)
            {
                StuCircleResultData ss = outPutDataOfCircleMatch.stuCircleResultDatas[i];
                currvisiontool.DrawText(new TextEx(
                    string.Format("X：{0}\nY：{1}\nR：{2}",
                       ss.centreP.X.ToString("f3"), ss.centreP.Y.ToString("f3"), ss.Radius.ToString("f3")),
                     new Font("宋体", 12f), new SolidBrush(Color.Green), ss.centreP.X, ss.centreP.Y));

                currvisiontool.AddTextBuffer(new TextEx(
                    string.Format("X：{0}\nY：{1}\nR：{2}",
                       ss.centreP.X.ToString("f3"), ss.centreP.Y.ToString("f3"), ss.Radius.ToString("f3")),
                     new Font("宋体", 12f), new SolidBrush(Color.Green), ss.centreP.X, ss.centreP.Y));
            }

            stopwatch.Stop();
            int spend = (int)stopwatch.ElapsedMilliseconds;
            currvisiontool.DetectionTime = spend;
        }
        /*---------------------------------------------------------------------*/
        //获取直线1
        private void btnGetLine1_Click(object sender, EventArgs e)
        {

            #region-----直线1------
            if (!IsUsingPretreatment)  //不进行前处理
            {
                if (GrabImg == null || GrabImg.Width <= 0)
                {
                    Appentxt("未获取图像");
                    stuModelMatchData.runFlag = false;
                    return;
                }
            }
            else
            {
                if (imageBuffer.Empty() || imageBuffer.Width <= 0)
                {
                    Appentxt("未获取预处理图像");
                    stuModelMatchData.runFlag = false;
                    return;
                }
            }
            List<RotatedCaliperRectF> roiList = currvisiontool.getRoiList<RotatedCaliperRectF>();
            if (roiList.Count <= 0)
            {
                MessageBox.Show("请设置直线查找区域{旋转卡尺矩}");
                return;

            }
            stopwatch.Restart();
            currvisiontool.dispImage(IsUsingPretreatment ? imageBuffer : GrabImg);
            //卡尺直线1参数
            string tem = CobxfitMethod1.Text == "最小二乘法" ? "Least_square_method" : "Random_sampling_consistency";
            parmaData = new LinearCaliperData
            {
                caliperWidth = (int)NumcaliperWidth1.Value,
                caliperNum = (int)NumcaliperNum1.Value,
                edgeThreshold = (byte)NumedgeThreshold1.Value,
                fitMethod = (EumFittingMethod)Enum.Parse(typeof(EumFittingMethod), tem),
                edgePolarity=(EumEdgePolarity)Enum.Parse(typeof(EumEdgePolarity), cobxPolarity1.Text)
            };
            parmaData.ROI = RegionRRect = temBuffRegionRRect;
            runTool = new LinearCaliperTool();
         
            ResultOfToolRun = runTool.Run<LinearCaliperData>(IsUsingPretreatment ? imageBuffer : GrabImg,
               parmaData as LinearCaliperData);
         
            currvisiontool.dispImage(ResultOfToolRun.resultToShow);

            if (!ResultOfToolRun.runStatus)
                Appentxt("直线1拟合失败！");
            else
            {
                Point2d p1 = (ResultOfToolRun as LinearCaliperResult).sp;
                Point2d p2 = (ResultOfToolRun as LinearCaliperResult).ep;
                StuLineResultData = new StuLineResultData((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
                if (Hom_mat2d != null && Hom_mat2d.Width > 0)
                {
                    Point2d r1 = CalibrationTool.AffineTransPoint2d(Hom_mat2d, p1);
                    Point2d r2 = CalibrationTool.AffineTransPoint2d(Hom_mat2d, p2);

                    line1AngleLx = new StuLineResultData((float)r1.X, (float)r1.Y, (float)r2.X, (float)r2.Y).GetAngle();

                    currvisiontool.DrawText(new TextEx(string.Format("直线1的角度angle：{0}", line1AngleLx.ToString("f3")),
                        new Font("宋体", 12f), new SolidBrush(Color.Green), 100, 100));

                    Appentxt(string.Format("直线1的角度angle：{0}", line1AngleLx.ToString("f3")));
                }
            }

            EnableDetectionControl();
            stopwatch.Stop();
            int spend = (int)stopwatch.ElapsedMilliseconds;
            currvisiontool.DetectionTime = spend;
            #endregion

        }
        //获取直线2
        private void btnGetLine2_Click(object sender, EventArgs e)
        {

            #region---直线2-----
            if (!IsUsingPretreatment)  //不进行前处理
            {
                if (GrabImg == null || GrabImg.Width <= 0)
                {
                    Appentxt("未获取图像");
                    stuModelMatchData.runFlag = false;
                    return;
                }
            }
            else
            {
                if (imageBuffer.Empty() || imageBuffer.Width <= 0)
                {
                    Appentxt("未获取预处理图像");
                    stuModelMatchData.runFlag = false;
                    return;
                }
            }
            List<RotatedCaliperRectF> roiList2 = currvisiontool.getRoiList<RotatedCaliperRectF>();

            if (roiList2.Count <= 0)
            {
                MessageBox.Show("请设置直线2查找区域{旋转卡尺矩}");
                return;

            }
            stopwatch.Restart();
            currvisiontool.dispImage(IsUsingPretreatment ? imageBuffer : GrabImg);
            //卡尺直线2参数
            string tem = CobxfitMethod2.Text == "最小二乘法" ? "Least_square_method" : "Random_sampling_consistency";
            parmaData = new LinearCaliperData
            {
                caliperWidth = (int)NumcaliperWidth2.Value,
                caliperNum = (int)NumcaliperNum2.Value,
                edgeThreshold = (byte)NumedgeThreshold2.Value,
                fitMethod = (EumFittingMethod)Enum.Parse(typeof(EumFittingMethod), tem),
                edgePolarity = (EumEdgePolarity)Enum.Parse(typeof(EumEdgePolarity), cobxPolarity2.Text)
            };

            parmaData.ROI = RegionRRect2 = temBuffRegionRRect;
            runTool = new LinearCaliperTool();
            ResultOfToolRun = runTool.Run<LinearCaliperData>(IsUsingPretreatment ? imageBuffer : GrabImg,
               parmaData as LinearCaliperData);

            currvisiontool.dispImage(ResultOfToolRun.resultToShow);


            if (!ResultOfToolRun.runStatus)
                Appentxt("直线2拟合失败！");
            else
            {
                Point2d p12 = (ResultOfToolRun as LinearCaliperResult).sp;
                Point2d p22 = (ResultOfToolRun as LinearCaliperResult).ep;
                StuLineResultData2 = new StuLineResultData((float)p12.X, (float)p12.Y, (float)p22.X, (float)p22.Y);

                if (Hom_mat2d != null && Hom_mat2d.Width > 0)
                {
                    Point2d r12 = CalibrationTool.AffineTransPoint2d(Hom_mat2d, p12);
                    Point2d r22 = CalibrationTool.AffineTransPoint2d(Hom_mat2d, p22);

                    line2AngleLx = new StuLineResultData((float)r12.X, (float)r12.Y, (float)r22.X, (float)r22.Y).GetAngle();

                    currvisiontool.DrawText(new TextEx(string.Format("直线2的角度angle：{0}", line2AngleLx.ToString("f3")),
                       new Font("宋体", 12f), new SolidBrush(Color.Green), 100, 100));

                    Appentxt(string.Format("直线2的角度angle：{0}", line2AngleLx.ToString("f3")));
                }

            }
            EnableDetectionControl();
            stopwatch.Stop();
            int spend = (int)stopwatch.ElapsedMilliseconds;
            currvisiontool.DetectionTime = spend;
            #endregion

        }
        //直线交点
        private void btnIntersectLines_Click(object sender, EventArgs e)
        {
            Point2d crossP = new Point2d(0, 0);
            if (StuLineResultData.runFlag && StuLineResultData2.runFlag)
            {
                MatExtension.IntersectionPoint(
                 MatExtension.LineSegmentPoint2Line2D(new LineSegmentPoint(
                     new CVPoint(StuLineResultData.P1.X, StuLineResultData.P1.Y)
                     , new CVPoint(StuLineResultData.P2.X, StuLineResultData.P2.Y))
                 ),
                  MatExtension.LineSegmentPoint2Line2D(new LineSegmentPoint(
                     new CVPoint(StuLineResultData2.P1.X, StuLineResultData2.P1.Y)
                     , new CVPoint(StuLineResultData2.P2.X, StuLineResultData2.P2.Y)))
                , out crossP);

                Mat dst = new Mat();
                Cv2.CvtColor(IsUsingPretreatment ? imageBuffer : GrabImg, dst, ColorConversionCodes.GRAY2BGR);
                dst.Line(new CVPoint(StuLineResultData.P1.X, StuLineResultData.P1.Y)
                     , new CVPoint(StuLineResultData.P2.X, StuLineResultData.P2.Y), Scalar.LimeGreen);
                dst.Line(new CVPoint(StuLineResultData2.P1.X, StuLineResultData2.P1.Y)
                     , new CVPoint(StuLineResultData2.P2.X, StuLineResultData2.P2.Y), Scalar.LimeGreen);
                dst.drawCross(new CVPoint(crossP.X, crossP.Y), Scalar.Red, 20, 2);

                currvisiontool.dispImage(dst);

                currvisiontool.textExlist.Clear();
                currvisiontool.DrawText(new TextEx(string.Format("点位:x:{0:f3} y:{1:f3}",
                           crossP.X, crossP.Y),
                           new Font("宋体", 16f), new SolidBrush(Color.LimeGreen), 10, 60));
                currvisiontool.AddTextBuffer(new TextEx(string.Format("点位:x:{0:f3} y:{1:f3}",
                           crossP.X, crossP.Y),
                    new Font("宋体", 16f), new SolidBrush(Color.LimeGreen), 10, 60));
            }
            else
            {
                MessageBox.Show("直线未拟合，无法进行交点计算！");
                return;
            }

            //if (Hom_mat2d == null || Hom_mat2d.Width <= 0)
            //{
            //    Appentxt("标定矩阵为空，当前不可转换坐标！");
            //    return;
            //}

            //Point2d robotP = CalibrationTool.AffineTransPoint2d(Hom_mat2d, new Point2d(crossP.X, crossP.Y));

            ////用户坐标事件
            //setUserPointHandle?.Invoke(new string[] { robotP.X.ToString("f3"),
            //       robotP.Y.ToString("f3")}, null);
            //currvisiontool.DrawText(new TextEx(
            //    string.Format("直线交点X：{0},Y：{1}", robotP.X.ToString("f3"),
            //               robotP.Y.ToString("f3")), new Font("宋体", 12f),
            //    new SolidBrush(Color.Green), 100, 100));
        }

        /*---------------------------------------------------------------------*/

        //获取圆
        private void btnGetCircle_Click(object sender, EventArgs e)
        {


            if (!IsUsingPretreatment)  //不进行前处理
            {
                if (GrabImg == null || GrabImg.Width <= 0)
                {
                    Appentxt("未获取图像");
                    stuModelMatchData.runFlag = false;
                    return;
                }
            }
            else
            {
                if (imageBuffer.Empty() || imageBuffer.Width <= 0)
                {
                    Appentxt("未获取预处理图像");
                    stuModelMatchData.runFlag = false;
                    return;
                }
            }

            List<SectorF> roiList = currvisiontool.getRoiList<SectorF>();
     //       List<CircleF> roiList2= currvisiontool.getRoiList<CircleF>();
            if (roiList.Count <= 0)
            {
                MessageBox.Show("请设置圆检测区域{扇形}");

                return;
            }

            stopwatch.Restart();
            //控件使能
            {
                LinesIntersectPanel.Enabled = false;
                BlobCentrePanel.Enabled = false;
                ModelMactPanel.Enabled = false;
                btnGetCircle.Enabled = false;
            }
   
            currvisiontool.dispImage(IsUsingPretreatment ? imageBuffer : GrabImg);
            string tem = cobxPolarity3.Text ;
            parmaData = new CircularCaliperData
            {
                caliperNum = (int)NumcaliperNum.Value,
                caliperWidth = (int)NumcaliperWidth.Value,
                edgeThreshold = (byte)NumedgeThreshold.Value,
                circleDir = (EumDirectionOfCircle)Enum.Parse(typeof(EumDirectionOfCircle), cobxCircleDir.Text),
                edgePolarity = (EumEdgePolarity)Enum.Parse(typeof(EumEdgePolarity), tem)

            };    
            if(roiList.Count>0)
                parmaData.ROI = sectorF;
            //else
            //    parmaData.ROI = circleF;
            runTool = new CircularCaliperTool();
            Result stuResultOfToolRun = runTool.Run<CircularCaliperData>(IsUsingPretreatment ? imageBuffer : GrabImg,
                           parmaData as CircularCaliperData);
         
            currvisiontool.dispImage(stuResultOfToolRun.resultToShow);
            CircularCaliperResult RT = stuResultOfToolRun as CircularCaliperResult;
            currvisiontool.textExlist.Clear();
            currvisiontool.DrawText(new TextEx(string.Format("点位:x:{0:f3} y:{1:f3},半径:{2:f3}",
                       RT.centreP.X, RT.centreP.Y, RT.radius),
                       new Font("宋体", 16f), new SolidBrush(Color.LimeGreen), 10, 60));
            currvisiontool.AddTextBuffer(new TextEx(string.Format("点位:x:{0:f3} y:{1:f3},半径:{2:f3}",
                RT.centreP.X, RT.centreP.Y, RT.radius),
                new Font("宋体", 16f), new SolidBrush(Color.LimeGreen), 10, 60));
            //控件使能
            {
                LinesIntersectPanel.Enabled = true;
                BlobCentrePanel.Enabled = true;
                ModelMactPanel.Enabled = true;

                btnGetCircle.Enabled = true;
            }

            EnableDetectionControl();
            stopwatch.Stop();
            int spend = (int)stopwatch.ElapsedMilliseconds;
            currvisiontool.DetectionTime = spend;
        }
        /*---------------------------------------------------------------------*/
        //Blob检测
        private void btnGetBlobArea_Click(object sender, EventArgs e)
        {
            if (!IsUsingPretreatment)  //不进行前处理
            {
                if (GrabImg == null || GrabImg.Width <= 0)
                {
                    Appentxt("未获取图像");
                    stuModelMatchData.runFlag = false;
                    return;
                }
            }
            else
            {
                if (imageBuffer.Empty() || imageBuffer.Width <= 0)
                {
                    Appentxt("未获取预处理图像");
                    stuModelMatchData.runFlag = false;
                    return;
                }
            }
            List<RotatedRectF> roiList = currvisiontool.getRoiList<RotatedRectF>();
            if (roiList.Count <= 0)
            {
                MessageBox.Show("请设置Blob检测区域{旋转矩形}");
                return;

            }
            stopwatch.Restart();
            currvisiontool.dispImage(IsUsingPretreatment ? imageBuffer : GrabImg);
            //Blob3检测参数
            parmaData = new Blob3Data
            {
                edgeThreshold = (double)Numminthd.Value,
                minArea = (int)NumareaLow.Value,
                maxArea = (int)NumareaHigh.Value,
                eumWhiteOrBlack = (EumWhiteOrBlack)Enum.Parse(typeof(EumWhiteOrBlack), cobxPolarity.Text)
            };
            //使用旋转矩形
            parmaData.ROI = RegionRRect3 = temBuffRegionRRect2;
       
            runTool = new Blob3Tool();
            ResultOfToolRun = runTool.Run<Blob3Data>(IsUsingPretreatment ? imageBuffer : GrabImg,
               parmaData as Blob3Data);
        
            currvisiontool.dispImage(ResultOfToolRun.resultToShow);
            Blob3Result blob3Result = ResultOfToolRun as Blob3Result;
            int i = 0;
            currvisiontool.textExlist.Clear();
            foreach (var s in blob3Result.areas)
            {
                i++;
                currvisiontool.DrawText(new TextEx (string.Format("编号:{0},面积:{1},点位:x:{2:f3} y:{3:f3}",
                    i,s, blob3Result.positionData[i-1].X, blob3Result.positionData[i - 1].Y),
                    new Font("宋体",16f),new SolidBrush(Color.LimeGreen),10,10+50*i));
                currvisiontool.AddTextBuffer(new TextEx(string.Format("编号:{0},面积:{1},点位:x:{2:f3} y:{3:f3}", 
                    i, s, blob3Result.positionData[i - 1].X, blob3Result.positionData[i - 1].Y),
                    new Font("宋体", 16f), new SolidBrush(Color.LimeGreen), 10, 10 + 50 * i));
            }

            EnableDetectionControl();
            stopwatch.Stop();
            int spend = (int)stopwatch.ElapsedMilliseconds;
            currvisiontool.DetectionTime = spend;
        }

        /*---------------------------------------------------------------------*/

        /// <summary>
        /// ROI绘制完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RoiChangedEvent(object sender, EventArgs e)
        {
            if (sender is RectangleF)
            {
                RegionaRect = (RectangleF)sender;

            }
            else if (sender is RotatedRectF)
            {
                temBuffRegionRRect2 = (RotatedRectF)sender;            
                
            }
           
           else if (sender is RotatedCaliperRectF)
            {
                temBuffRegionRRect = (RotatedCaliperRectF)sender;
                this.Invoke(new Action(() => {
                    if(cobxLine1or2.Text=="直线1")
                    {
                        int caliperWidth = (int)NumcaliperWidth1.Value;
                        int caliperNum = (int)NumcaliperNum1.Value;
                        currvisiontool.GetCalipersRegions(caliperWidth, caliperNum, EumCalipersType.linear);
                    }
                    else
                    {
                        int caliperWidth = (int)NumcaliperWidth2.Value;
                        int caliperNum = (int)NumcaliperNum2.Value;
                        currvisiontool.GetCalipersRegions(caliperWidth, caliperNum, EumCalipersType.linear);
                    }
                   
                }));

            }
            else if (sender is SectorF)
            {
                sectorF = (SectorF)sender;
                this.Invoke(new Action(() => {

                    int caliperWidth = (int)NumcaliperWidth.Value;
                    int caliperNum = (int)NumcaliperNum.Value;
                    EumDirectionOfCircle circleDir = (EumDirectionOfCircle)Enum.Parse(typeof(EumDirectionOfCircle), cobxCircleDir.Text);
                    currvisiontool.GetCalipersRegions(caliperWidth, caliperNum, EumCalipersType.sector, circleDir);
                }));             
            }
            else if (sender is CircleF)
            {
                circleF = (CircleF)sender;
                this.Invoke(new Action(() => {

                    int caliperWidth = (int)NumcaliperWidth.Value;
                    int caliperNum = (int)NumcaliperNum.Value;
                    currvisiontool.GetCalipersRegions(caliperWidth, caliperNum, EumCalipersType.circular);
                }));
            }
        }
        private void checkBox1_CheckedChanged(object sender, bool value)
        {
            if (!chxbLinesIntersect.Checked && !chxbFindCircle.Checked && !chxbBlobCentre.Checked)
            {
                ExchangeSelect(-1);
                CheckBoxselectID = -1;
            }

            if ((sender as UICheckBox).Checked)
                switch ((sender as UICheckBox).Name)
                {
                    case "chxbLinesIntersect":
                        ExchangeSelect(1);
                        CheckBoxselectID = 1;

                        break;
                    case "chxbFindCircle":
                        ExchangeSelect(2);
                        CheckBoxselectID = 2;
                        break;
                    case "chxbBlobCentre":
                        ExchangeSelect(3);
                        CheckBoxselectID = 3;
                        break;

                }
        }

        /// <summary>
        /// 是否启用模板匹配
        /// </summary>
        bool isUsingModelMatch = true;
        private void chxbModelMatch_ValueChanged(object sender, bool value)
        {
            isUsingModelMatch = chxbModelMatch.Checked;
            if (isUsingModelMatch)
            {
                chxbAutoCircleMatch.Checked = false;
                chxbAutoCircleMatch.Enabled = false;
                CircleMatchPanel.Enabled = false;

                checkedControl("配方\\" + CurrRecipeName);
                EnableDetectionControl();
            }
            else
            {
                chxbAutoCircleMatch.Enabled = true;
                CircleMatchPanel.Enabled = true;

                checkedControl("配方\\" + CurrRecipeName);
                EnableDetectionControl();

            }
        }

        /// <summary>
        /// 自动圆匹配工具
        /// </summary>
        bool isUsingAutoCircleMatch = false;
        private void chxbAutoCircleMatch_ValueChanged(object sender, bool value)
        {
            isUsingAutoCircleMatch = chxbAutoCircleMatch.Checked;

            if (isUsingAutoCircleMatch)
            {
                chxbModelMatch.Checked = false;
                chxbModelMatch.Enabled = false;
                ModelMactPanel.Enabled = false;

                checkedControl("配方\\" + CurrRecipeName);
                EnableDetectionControl();
            }
            else
            {
                chxbModelMatch.Enabled = true;
                ModelMactPanel.Enabled = true;
                checkedControl("配方\\" + CurrRecipeName);
                EnableDetectionControl();

            }
        }

        private void btnTest_modelMatch_MouseHover(object sender, EventArgs e)
        {
            switch ((sender as UIButton).Name)
            {
                case "btnTest_modelMatch":
                    this.toolTip1.SetToolTip(this.btnTest_modelMatch, "计算后的坐标为像素坐标");
                    break;
                case "btnLntersectLine":
                    this.toolTip1.SetToolTip(this.btnLntersectLine, "计算后的坐标为物理坐标");
                    break;
                case "btnfircircle":
                    this.toolTip1.SetToolTip(this.btnGetCircle, "计算后的坐标为物理坐标");
                    break;
                case "btnBlobCentre":
                    this.toolTip1.SetToolTip(this.btnGetBlobArea, "计算后的坐标为物理坐标");
                    break;

            }
        }
        /*---------------------------------------------------------------------*/
        static int RunToolStep = 0;

        //运行工具
        private void btnRunTool_Click(object sender, EventArgs e)
        {
            if (CurrCam != null)
                if (CurrCam.IsGrabing)
                    btnStopGrab_Click(null, null);  //如果已在采集中则先停止采集
            if (listViewFlow.SelectedItems.Count <= 0) return;
            int index = listViewFlow.SelectedIndices[0];
            if (index < 0) return;
            if (!toolList.Contains("前处理工具") && RunToolStep == 0) RunToolStep = 1;

            if (toolList[index] == "前处理工具")
                if (RunToolStep == 1)
                { 
                    Appentxt("前处理工具已运行，无需重复！");
                    return;
                }
                else
                    Task.Run(delegate () { ToolTaskRun(listViewTools.Items.Count - 1); }).ContinueWith
                        (t => { RunToolStep = 1; });
            else  if (toolList[index] == "模板匹配")
            {
                if (toolList.Contains("前处理工具") && RunToolStep != 1)
                {

                    Appentxt("请先运行前处理工具!");
                    //  MessageBox.Show("请先运行前处理工具!");
                    return;
                }
                if (RunToolStep == 2)
                {
                    Appentxt("模板匹配工具已运行，无需重复！");
                    // MessageBox.Show("模板匹配工具已运行，无需重复！");
                    return;
                }
                else
                {
                    btnTest_modelMatch_Click(null, null);
                    RunToolStep = 2;
                }

            }
            else if (toolList[index] == "自动圆匹配")
            {
                if (toolList.Contains("前处理工具") && RunToolStep != 1)
                {
                    // MessageBox.Show("请先运行前处理工具!");
                    Appentxt("请先运行前处理工具!");
                    return;
                }
                if (RunToolStep == 2)
                {
                    // currvisiontool.DispMessage("自动圆匹配工具已运行，无需重复！", 300, 500, "red", 50);
                    Appentxt("自动圆匹配工具已运行，无需重复！");
                    // MessageBox.Show("自动圆匹配工具已运行，无需重复！");
                    return;
                }
                else
                {
                    btnTestAutoCircleMatch_Click(null, null);
                    RunToolStep = 2;
                }

            }
            else if (toolList[index] == "直线定位")
            {
                if (toolList.Contains("前处理工具") && RunToolStep != 1)
                {
                    // MessageBox.Show("请先运行前处理工具!");
                    Appentxt("请先运行前处理工具!");
                    return;
                }
                if ((toolList.Contains("模板匹配")|| toolList.Contains("自动圆匹配"))&& RunToolStep !=2)
                {
                    Appentxt("请先运行模板匹配工具!");
                    //MessageBox.Show("请先运行模板匹配工具!");
                    return;
                }
                if (RunToolStep == 3)
                {
                    Appentxt("直线相交工具已运行，无需重复！");
                    //MessageBox.Show("直线相交工具已运行，无需重复！");
                    return;
                }
                else
                {
                    btnintersectlines();
                    RunToolStep = 3;
                }

            }
            else if (toolList[index] == "找圆定位")
            {

                if (toolList.Contains("前处理工具") && RunToolStep != 1)
                {
                    // MessageBox.Show("请先运行前处理工具!");
                    Appentxt("请先运行前处理工具!");
                    return;
                }
                if ((toolList.Contains("模板匹配") || toolList.Contains("自动圆匹配")) && RunToolStep != 2)
                {
                    Appentxt("请先运行模板匹配工具!");
                    //MessageBox.Show("请先运行模板匹配工具!");
                    return;
                }
                if (RunToolStep == 3)
                {
                    Appentxt("查找圆心工具已运行，无需重复！");
                    // MessageBox.Show("查找圆心工具已运行，无需重复！");
                    return;
                }
                else
                {
                    btnfitcircle();
                    RunToolStep = 3;
                }

            }
            else if (toolList[index] == "Blob定位")
            {

                if (toolList.Contains("前处理工具") && RunToolStep != 1)
                {
                    // MessageBox.Show("请先运行前处理工具!");
                    Appentxt("请先运行前处理工具!");
                    return;
                }
                if ((toolList.Contains("模板匹配") || toolList.Contains("自动圆匹配")) && RunToolStep != 2)
                {
                    Appentxt("请先运行模板匹配工具!");
                    //MessageBox.Show("请先运行模板匹配工具!");
                    return;
                }
                if (RunToolStep == 3)
                {
                    Appentxt("Blob中心工具已运行，无需重复！");
                    // MessageBox.Show("Blob中心工具已运行，无需重复！");
                    return;
                }
                else
                {
                    btncalblobcentre();
                    RunToolStep = 3;
                }

            }
        }

        //运行流程
        static object locker = new object();
        Stopwatch stopwatch = new Stopwatch();
        private void btnRunFlow_Click(object sender, EventArgs e)
        {
            if (CurrCam != null)
                if (CurrCam.IsGrabing)
                    btnStopGrab_Click(null, null);  //如果已在采集中则先停止采集
            stopwatch.Restart();

            lock (locker)
            {
                #region
                PositionData dstPois;

                EumOutAngleMode eumOutAngleMode = EumOutAngleMode.Relative;

                if (GrabImg == null || GrabImg.Width <= 0)
                {
                    MessageBox.Show("未获取图像");
                    return;
                }
                if (IsUsingPretreatment)
                {
                    ToolTaskRun(toolParList.Count - 1);
                }
                if (isUsingModelMatch)
                {
                    TestModelMatch();
                    dstPois = new PositionData
                    {
                        pointXY = stuModelMatchData.matchPoint,

                        angle = stuModelMatchData.matchOffsetAngle
                    };
                }

                else if (isUsingAutoCircleMatch)
                {
                    MatchCircleRun();
                    dstPois = new PositionData
                    {
                        pointXY = stuModelMatchData.matchPoint,

                        angle = stuModelMatchData.matchOffsetAngle
                    };
                }
                else
                    dstPois = new PositionData { pointXY = new Point2d(0, 0), angle = 0 };
                /*-----------------------------*/
                if (CheckBoxselectID == 1 || CheckBoxselectID == 2 || CheckBoxselectID == 3)

                {

                    float x0 = float.Parse(modelOrigion.Split(',')[0]);
                    float y0 = float.Parse(modelOrigion.Split(',')[1]);
                    float a0 = float.Parse(modelOrigion.Split(',')[2]);

                    float x1 = (float)stuModelMatchData.matchPoint.X;
                    float y1 = (float)stuModelMatchData.matchPoint.Y;
                    float a1 = (float)stuModelMatchData.matchOffsetAngle;
                    Mat mat2d = new Mat();

                    float offsetAngle = a1;
                    if (isUsingModelMatch)
                        mat2d = MatExtension.getMat(new Point2f(x0, y0), new Point2f(x1, y1), offsetAngle);
                    else
                        mat2d = MatExtension.getMat(new Point2f(0, 0), new Point2f(0, 0), 0);

                    switch (CheckBoxselectID)
                    {
                        case 1:
                            if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                == EumModelType.ProductModel_1)
                            {
                                if (SearchROI1 == null || SearchROI1.Count < 2 ||
                                   SearchROI1[0] == null && SearchROI1[1] == null)
                                {
                                    Appentxt("直线检测区域为空，请确认是否需要设置？");
                                    break;
                                }
                                Linedetection(mat2d, offsetAngle, SearchROI1, ref dstPois.pointXY);
                                dstPois.angle = line1AngleLx;
                                eumOutAngleMode = EumOutAngleMode.Absolute;
                            }
                            else
                            {
                                if (SearchROI2 == null || SearchROI2.Count < 2 ||
                                  SearchROI2[0] == null || SearchROI2[1] == null)
                                {
                                    Appentxt("直线检测区域为空，请确认是否需要设置？");
                                    break;
                                }
                                Linedetection(mat2d, offsetAngle, SearchROI2, ref dstPois.pointXY);
                                dstPois.angle = line1AngleLx;
                                eumOutAngleMode = EumOutAngleMode.Absolute;
                            }

                            break;
                        case 2:
                            if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
              == EumModelType.ProductModel_1)
                            {
                                if (SearchROI1 == null || SearchROI1.Count < 1 ||
                                                                   SearchROI1[0] == null)
                                {
                                    Appentxt("圆检测区域为空，请确认是否需要设置？");
                                    break;
                                }
                                Circledetection(mat2d, offsetAngle, SearchROI1, ref dstPois.pointXY);
                            }
                            else
                            {
                                if (SearchROI2 == null || SearchROI2.Count < 1 ||
                                                                   SearchROI2[0] == null)
                                {
                                    Appentxt("圆检测区域为空，请确认是否需要设置？");
                                    break;
                                }
                                Circledetection(mat2d, offsetAngle, SearchROI2, ref dstPois.pointXY);
                            }
                            break;
                        case 3:
                            if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
            == EumModelType.ProductModel_1)
                            {
                                if (SearchROI1 == null || SearchROI1.Count < 1 ||
                                                                   SearchROI1[0] == null)
                                {
                                    Appentxt("Blob检测区域为空，请确认是否需要设置？");
                                    break;
                                }
                                Blobdetection(mat2d, offsetAngle, SearchROI1, ref dstPois.pointXY);
                            }
                            else if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
           == EumModelType.ProductModel_2)
                            {
                                if (SearchROI2 == null || SearchROI2.Count < 1 ||
                                                             SearchROI2[0] == null)

                                {
                                    Appentxt("Blob检测区域为空，请确认是否需要设置？");
                                    break;
                                }
                                Blobdetection(mat2d, offsetAngle, SearchROI2, ref dstPois.pointXY);
                            }
                            else if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
           == EumModelType.GluetapModel)
                            {
                                if (SearchROI3 == null || SearchROI3.Count < 1 ||
                                                             SearchROI3[0] == null)

                                {
                                    Appentxt("Blob检测区域为空，请确认是否需要设置？");
                                    break;
                                }
                                Blobdetection(mat2d, offsetAngle, SearchROI3, ref dstPois.pointXY);
                            }
                            break;

                    }

                }

                /*------------------机械坐标点位转换----------------*/
                Point2d robotP = new Point2d(0, 0);
                if (Hom_mat2d == null || Hom_mat2d.Width <= 0)
                    Appentxt("当前标定矩阵关系为空，请确认！");
                else
                {
                    if (!(dstPois.pointXY.X == 0 && dstPois.pointXY.Y == 0))
                        robotP = CalibrationTool.AffineTransPoint2d(Hom_mat2d, dstPois.pointXY);
                }

                string buff = "[检测点位数据]";
                buff += string.Format("x:{0:f3},y:{1:f3},a:{2:f3},m:{3};",
                                 robotP.X, robotP.Y, dstPois.angle, Enum.GetName(typeof(EumOutAngleMode), eumOutAngleMode));

                Appentxt(buff);

                float x = (float)stuModelMatchData.matchPoint.X;
                float y = (float)stuModelMatchData.matchPoint.Y;
                float a = (float)stuModelMatchData.matchOffsetAngle;
                if(stuModelMatchData.runFlag)
                {
                    currvisiontool.DrawRegion(new RegionEx(new CrossF(x, y, 20, 20, 5), Color.Red));
                    currvisiontool.AddRegionBuffer(new RegionEx(new CrossF(x, y, 20, 20, 5), Color.Red));
                }
          
                //currvisiontool.DrawText(new TextEx(string.Format("{0},{1},{2}",
                //        robotP.X.ToString("f3"),
                //         robotP.Y.ToString("f3"),
                //         dstPois.angle.ToString("f3")), new Font("宋体", 16), new SolidBrush(Color.LimeGreen),
                //         dstPois.pointXY.X, dstPois.pointXY.Y));

                //currvisiontool.AddTextBuffer(new TextEx(string.Format("{0},{1},{2}",
                //        robotP.X.ToString("f3"),
                //         robotP.Y.ToString("f3"),
                //         dstPois.angle.ToString("f3")), new Font("宋体", 16), new SolidBrush(Color.LimeGreen),
                //         dstPois.pointXY.X, dstPois.pointXY.Y));

                currvisiontool.DrawText(new TextEx(string.Format("特征点位X:{0},Y:{1},A:{2}", robotP.X.ToString("f3"),
                 robotP.Y.ToString("f3"),dstPois.angle.ToString("f3")))
                { x = 10, y = 300 });
                currvisiontool.AddTextBuffer(new TextEx(string.Format("特征点位X:{0},Y:{1},A:{2}", robotP.X.ToString("f3"),
                 robotP.Y.ToString("f3"), dstPois.angle.ToString("f3")))
                { x = 10, y = 300 });
                #endregion
            }
            stopwatch.Stop();
            int spend = (int)stopwatch.ElapsedMilliseconds;
            currvisiontool.DetectionTime = spend;
        }

        /*---------------------------------------------------------------------*/
        //模板匹配
        void TestModelMatch()
        {
        
            if (!IsUsingPretreatment)  //不进行前处理
            {
                if (GrabImg == null || GrabImg.Width <= 0)
                {
                    Appentxt("未获取图像");
                    stuModelMatchData.runFlag = false;
                    return;
                }
            }
            else
            {
                if (imageBuffer.Empty()|| imageBuffer.Width <=0)
                {
                    Appentxt("未获取预处理图像");
                    stuModelMatchData.runFlag = false;
                    return;
                }
            }

            if (currMatchMethod== EumMatchMethod.ShapeMatch)//形状匹配
            {

                if (templateContour == null)
                {
                    stuModelMatchData.runFlag = false;
                    MessageBox.Show("模板不存在，请先创建模板！");
                    return;
                }
                runTool = new ShapeMatchTool();
                parmaData = new ShapeMatchData();
                (parmaData as ShapeMatchData).tpContour = templateContour;
                (parmaData as ShapeMatchData).Segthreshold = (double)NumSegthreshold.Value;
                (parmaData as ShapeMatchData).MatchValue = (double)NumMatchValue.Value;
                (parmaData as ShapeMatchData).MincoutourLen = (int)NumMincoutourLen.Value;
                (parmaData as ShapeMatchData).MaxcoutourLen = (int)NumMaxcoutourLen.Value;
                (parmaData as ShapeMatchData).MinContourArea = (int)NumMinContourArea.Value;
                (parmaData as ShapeMatchData).MaxContourArea = (int)NumMaxContourArea.Value;
                (parmaData as ShapeMatchData).baseAngle = modelangle;
                parmaData.MatchSearchMethod = eumMatchSearchMethod;
                parmaData.matchSearchRegion = matchSearchRegion;

                ResultOfToolRun = runTool.Run<ShapeMatchData>(IsUsingPretreatment ? imageBuffer : GrabImg, parmaData as ShapeMatchData);

                currvisiontool.clearAll();
              

                ShapeMatchResult shapeMatchResult = ResultOfToolRun as ShapeMatchResult;

                if (shapeMatchResult.scores.Count <= 0)
                {
                    currvisiontool.DrawText(new TextEx("模板匹配失败！") { x = 1000, y = 10, brush = new SolidBrush(Color.Red) });

                    currvisiontool.AddTextBuffer(new TextEx("模板匹配失败！") { x = 1000, y = 10, brush = new SolidBrush(Color.Red) });

                    stuModelMatchData.runFlag = false;
                    if (eumMatchSearchMethod == EumMatchSearchMethod.PartRegion)
                        currvisiontool.AddRegionBuffer(new RegionEx(matchSearchRegion, Color.DarkOrange));
                    currvisiontool.dispImage(ResultOfToolRun.resultToShow);
                    return;
                }
              //  currvisiontool.DrawText(new TextEx("得分:" + shapeMatchResult.scores[0].ToString("f3")));
                currvisiontool.AddTextBuffer(new TextEx("得分:" + shapeMatchResult.scores[0].ToString("f3")));

              //  currvisiontool.DrawText(new TextEx("偏转角度:" + shapeMatchResult.rotations[0].ToString("f3")) { x = 10, y = 100 });
                currvisiontool.AddTextBuffer(new TextEx("偏转角度:" + shapeMatchResult.rotations[0].ToString("f3")) { x = 10, y = 100 });

                //currvisiontool.DrawText(new TextEx(string.Format("匹配点位X:{0},Y:{1}", shapeMatchResult.positions[0].X.ToString("f3"),
                //    shapeMatchResult.positions[0].Y.ToString("f3")))
                //{ x = 10, y = 200 });
                currvisiontool.AddTextBuffer(new TextEx(string.Format("匹配点位X:{0},Y:{1}", shapeMatchResult.positions[0].X.ToString("f3"),
                    shapeMatchResult.positions[0].Y.ToString("f3")))
                { x = 10, y = 200 });

                stuModelMatchData.matchPoint = shapeMatchResult.positions[0];
                stuModelMatchData.matchOffsetAngle = shapeMatchResult.rotations[0];
                stuModelMatchData.matchScore = shapeMatchResult.scores[0];
                stuModelMatchData.runFlag = true;
                if (eumMatchSearchMethod == EumMatchSearchMethod.PartRegion)
                    currvisiontool.AddRegionBuffer(new RegionEx(matchSearchRegion, Color.DarkOrange));
                currvisiontool.dispImage(ResultOfToolRun.resultToShow);
            }
            else if (currMatchMethod == EumMatchMethod.NccMatch)//Ncc匹配
            {
                if (this.modelGrayMat.Empty()||this.modelGrayMat.Width<=0)
                {
                    stuModelMatchData.runFlag = false;
                    MessageBox.Show("模板不存在，请先创建模板！");
                    return;
                }
                runTool = new NccTemplateMatchTool();
                parmaData = new NccTemplateMatchData();
                (parmaData as NccTemplateMatchData).tpl = this.modelGrayMat;
                (parmaData as NccTemplateMatchData).StartAngle = (double)NumSegthreshold.Value;
                (parmaData as NccTemplateMatchData).AngleRange = (double)NumMatchValue.Value;
                (parmaData as NccTemplateMatchData).AngleStep = (int)NumMincoutourLen.Value;
                (parmaData as NccTemplateMatchData).NumLevels = (int)NumMaxcoutourLen.Value;
                (parmaData as NccTemplateMatchData).MatchScore = (double)NumMinContourArea.Value;
                parmaData.MatchSearchMethod = eumMatchSearchMethod;
                parmaData.matchSearchRegion = matchSearchRegion;

                ResultOfToolRun = runTool.Run<NccTemplateMatchData>(IsUsingPretreatment ? imageBuffer : GrabImg, parmaData as NccTemplateMatchData);

               currvisiontool.clearAll();
            
                NccTemplateMatchResult result = ResultOfToolRun as NccTemplateMatchResult;

                if (result.Score<= 0)
                {
                    currvisiontool.DrawText(new TextEx("模板匹配失败！") { x = 1000, y = 10, brush = new SolidBrush(Color.Red) });

                    currvisiontool.AddTextBuffer(new TextEx("模板匹配失败！") { x = 1000, y = 10, brush = new SolidBrush(Color.Red) });

                    stuModelMatchData.runFlag = false;
                    if (eumMatchSearchMethod == EumMatchSearchMethod.PartRegion)
                        currvisiontool.AddRegionBuffer(new RegionEx(matchSearchRegion, Color.DarkOrange));
                    currvisiontool.dispImage(ResultOfToolRun.resultToShow);
                    return;
                }
             //   currvisiontool.DrawText(new TextEx("得分:" + result.Score.ToString("f3")));
                currvisiontool.AddTextBuffer(new TextEx("得分:" + result.Score.ToString("f3")));

             //   currvisiontool.DrawText(new TextEx("偏转角度:" + result.T.ToString("f3")) { x = 10, y = 100 });
                currvisiontool.AddTextBuffer(new TextEx("偏转角度:" + result.T.ToString("f3")) { x = 10, y = 100 });

                //currvisiontool.DrawText(new TextEx(string.Format("匹配点位X:{0},Y:{1}", result.X.ToString("f3"),
                //   result.Y.ToString("f3")))
                //{ x = 10, y = 200 });
                currvisiontool.AddTextBuffer(new TextEx(string.Format("匹配点位X:{0},Y:{1}", result.X.ToString("f3"),
                   result.Y.ToString("f3")))
                { x = 10, y = 200 });
                if (eumMatchSearchMethod == EumMatchSearchMethod.PartRegion)
                    currvisiontool.AddRegionBuffer(new RegionEx(matchSearchRegion, Color.DarkOrange));
                stuModelMatchData.matchPoint =new Point2d (result.X, result.Y);
                stuModelMatchData.matchOffsetAngle = result.T;
                stuModelMatchData.matchScore = result.Score;
                stuModelMatchData.runFlag = true;
                currvisiontool.dispImage(ResultOfToolRun.resultToShow);
            }
            else//Canny匹配
            {
                if (this.cannyMat.Empty() || this.cannyMat.Width <= 0)
                {
                    stuModelMatchData.runFlag = false;
                    MessageBox.Show("模板不存在，请先创建模板！");
                    return;
                }
                runTool = new CannyTemplateMatchTool();
                parmaData = new CannyTemplateMatchData();
                (parmaData as CannyTemplateMatchData).tpl = this.cannyMat;
                (parmaData as CannyTemplateMatchData).StartAngle = (double)NumSegthreshold.Value;
                (parmaData as CannyTemplateMatchData).AngleRange = (double)NumMatchValue.Value;
                (parmaData as CannyTemplateMatchData).SegmentThresh = (int)NumMincoutourLen.Value;
                (parmaData as CannyTemplateMatchData).NumLevels = (int)NumMaxcoutourLen.Value;
                (parmaData as CannyTemplateMatchData).MatchScore = (double)NumMinContourArea.Value;
                parmaData.MatchSearchMethod = eumMatchSearchMethod;
                parmaData.matchSearchRegion = matchSearchRegion;

                ResultOfToolRun = runTool.Run<CannyTemplateMatchData>(IsUsingPretreatment ? imageBuffer : GrabImg, parmaData as CannyTemplateMatchData);

                currvisiontool.clearAll();
               
                CannyTemplateMatchResult result = ResultOfToolRun as CannyTemplateMatchResult;

                if (result.Score <= 0)
                {
                    currvisiontool.DrawText(new TextEx("模板匹配失败！") { x = 1000, y = 10, brush = new SolidBrush(Color.Red) });

                    currvisiontool.AddTextBuffer(new TextEx("模板匹配失败！") { x = 1000, y = 10, brush = new SolidBrush(Color.Red) });

                    stuModelMatchData.runFlag = false;
                    if (eumMatchSearchMethod == EumMatchSearchMethod.PartRegion)
                        currvisiontool.AddRegionBuffer(new RegionEx(matchSearchRegion, Color.DarkOrange));
                    currvisiontool.dispImage(ResultOfToolRun.resultToShow);
                    return;
                }
              //  currvisiontool.DrawText(new TextEx("得分:" + result.Score.ToString("f3")));
                currvisiontool.AddTextBuffer(new TextEx("得分:" + result.Score.ToString("f3")));

              //  currvisiontool.DrawText(new TextEx("偏转角度:" + result.T.ToString("f3")) { x = 10, y = 100 });
                currvisiontool.AddTextBuffer(new TextEx("偏转角度:" + result.T.ToString("f3")) { x = 10, y = 100 });

                //currvisiontool.DrawText(new TextEx(string.Format("匹配点位X:{0},Y:{1}", result.X.ToString("f3"),
                //   result.Y.ToString("f3")))
                //{ x = 10, y = 200 });
                currvisiontool.AddTextBuffer(new TextEx(string.Format("匹配点位X:{0},Y:{1}", result.X.ToString("f3"),
                   result.Y.ToString("f3")))
                { x = 10, y = 200 });
                if (eumMatchSearchMethod == EumMatchSearchMethod.PartRegion)
                    currvisiontool.AddRegionBuffer(new RegionEx(matchSearchRegion, Color.DarkOrange));
                stuModelMatchData.matchPoint = new Point2d(result.X, result.Y);
                stuModelMatchData.matchOffsetAngle = result.T;
                stuModelMatchData.matchScore = result.Score;
                stuModelMatchData.runFlag = true;
                currvisiontool.dispImage(ResultOfToolRun.resultToShow);

              
            }
        }

        //自动圆匹配,圆拟合
        void MatchCircleRun()
        {
            outPutDataOfCircleMatch.stuCircleResultDatas.Clear();
            if (!IsUsingPretreatment)  //不进行前处理
            {
                if (GrabImg.Empty()|| GrabImg.Width<=0)
                {         
                    Appentxt("未获取图像");
                    outPutDataOfCircleMatch.stuCircleResultDatas.Add(new StuCircleResultData(false));
                    // MessageBox.Show("未获取图像");
                }
             
            }
            else
            {
                if (imageBuffer.Empty()|| imageBuffer.Width<=0)
                {                 
                    Appentxt("未获取预处理图像");
                    outPutDataOfCircleMatch.stuCircleResultDatas.Add(new StuCircleResultData(false));
                }
            }

            runTool = new HoughCircleTool();
            parmaData = new HoughCircleData();
            (parmaData as HoughCircleData).MinDist = (double)NumMinDist.Value;
            (parmaData as HoughCircleData).Param1 = (double)NumParam1.Value;
            (parmaData as HoughCircleData).Param2 = (double)NumParam2.Value;
            (parmaData as HoughCircleData).MinRadius = (int)numberMinRadius.Value;
            (parmaData as HoughCircleData).MaxRadius = (int)numberMaxRadius.Value;
            Result stuResultOfToolRun = runTool.Run<HoughCircleData>(IsUsingPretreatment ? imageBuffer : GrabImg,
                          parmaData as HoughCircleData);
          //  currvisiontool.clearAll();
            currvisiontool.dispImage(stuResultOfToolRun.resultToShow);

            int cn = (stuResultOfToolRun as HoughCircleResult).positionData.Count;
            for (int i = 0; i < cn; i++)
                outPutDataOfCircleMatch.stuCircleResultDatas.Add(
                    new StuCircleResultData((stuResultOfToolRun as HoughCircleResult).positionData[i].X,
                  (stuResultOfToolRun as HoughCircleResult).positionData[i].Y,
                        (float)(stuResultOfToolRun as HoughCircleResult).radiusArray[i]));

        }

        //直线相交
        void btnintersectlines()
        {

            Point2d crossP = new Point2d(0, 0);

            float x0 = float.Parse(modelOrigion.Split(',')[0]);
            float y0 = float.Parse(modelOrigion.Split(',')[1]);
            float a0 = float.Parse(modelOrigion.Split(',')[2]);

            float x1 = (float)stuModelMatchData.matchPoint.X;
            float y1 = (float)stuModelMatchData.matchPoint.Y;
            float a1 = (float)stuModelMatchData.matchOffsetAngle;
            Mat mat2d = new Mat();
            float offsetAngle = a1;
            if (isUsingModelMatch)
                mat2d = MatExtension.getMat(new Point2f(x0, y0), new Point2f(x1, y1), offsetAngle);

            else
                mat2d = MatExtension.getMat(new Point2f(0, 0), new Point2f(0, 0), 0);

            if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                 == EumModelType.ProductModel_1)
            {
                if (SearchROI1 == null || SearchROI1.Count < 2 ||
                                  SearchROI1[0] == null || SearchROI1[1] == null)
                {
                    Appentxt("直线检测区域为空，请确认是否需要设置？");

                }
                Linedetection(mat2d, offsetAngle, SearchROI1, ref crossP);
            }
            else
            {
                if (SearchROI2 == null || SearchROI2.Count < 2 ||
                                                  SearchROI2[0] == null || SearchROI2[1] == null)
                {
                    Appentxt("直线检测区域为空，请确认是否需要设置？");

                }
                Linedetection(mat2d, offsetAngle, SearchROI2, ref crossP);
            }


        }

        void btnfitcircle()
        {
            Point2d crossP = new Point2d(0, 0);
            float x0 = float.Parse(modelOrigion.Split(',')[0]);
            float y0 = float.Parse(modelOrigion.Split(',')[1]);
            float a0 = float.Parse(modelOrigion.Split(',')[2]);

            float x1 = (float)stuModelMatchData.matchPoint.X;
            float y1 = (float)stuModelMatchData.matchPoint.Y;
            float a1 = (float)stuModelMatchData.matchOffsetAngle;

            Mat Hom_mat2d = MatExtension.getMat(new Point2f(x0, y0), new Point2f(x1, y1), a1);


            if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                    == EumModelType.ProductModel_1)
            {
                if (SearchROI1 == null || SearchROI1.Count < 1 ||
                                SearchROI1[0] == null)
                {
                    Appentxt("圆检测区域为空，请确认是否需要设置？");

                }
                Circledetection(Hom_mat2d, a1, SearchROI1, ref crossP);
            }
            else
            {
                if (SearchROI2 == null || SearchROI2.Count < 1 ||
                            SearchROI2[0] == null)
                {
                    Appentxt("圆检测区域为空，请确认是否需要设置？");

                }
                Circledetection(Hom_mat2d, a1, SearchROI2, ref crossP);
            }

        }

        void btncalblobcentre()
        {
            Point2d crossP = new Point2d(0, 0);
            float x0 = float.Parse(modelOrigion.Split(',')[0]);
            float y0 = float.Parse(modelOrigion.Split(',')[1]);
            float a0 = float.Parse(modelOrigion.Split(',')[2]);

            float x1 = (float)stuModelMatchData.matchPoint.X;
            float y1 = (float)stuModelMatchData.matchPoint.Y;
            float a1 = (float)stuModelMatchData.matchOffsetAngle;

            float offsetAngle = a1;

            Mat Hom_mat2d = MatExtension.getMat(new Point2f(x0, y0), new Point2f(x1, y1), a1);


            if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                      == EumModelType.ProductModel_1)
            {
                if (SearchROI1 == null || SearchROI1.Count < 1 ||
                        SearchROI1[0] == null)
                {
                    Appentxt("Blob检测区域为空，请确认是否需要设置？");

                }
                Blobdetection(Hom_mat2d, offsetAngle, SearchROI1, ref crossP);
            }
            else if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                  == EumModelType.ProductModel_2)
            {
                if (SearchROI2 == null || SearchROI2.Count < 1 ||
                       SearchROI2[0] == null)
                {
                    Appentxt("Blob检测区域为空，请确认是否需要设置？");

                }
                Blobdetection(Hom_mat2d, offsetAngle, SearchROI2, ref crossP);
            }
            else if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                  == EumModelType.GluetapModel)
            {
                if (SearchROI3 == null || SearchROI3.Count < 1 ||
                       SearchROI3[0] == null)
                {
                    Appentxt("Blob检测区域为空，请确认是否需要设置？");

                }
                Blobdetection(Hom_mat2d, offsetAngle, SearchROI3, ref crossP);
            }


        }

        /*----------------------------------------------------------------*/
        /// <summary>
        ///  模板切换
        /// </summary>
        /// <param name="eumModelType">模板切换类型参数</param>
        /// <returns>模板切换是否成功标志</returns>
        public bool SwitchModelType(EumModelType eumModelType)
        {
            Appentxt(string.Format("外部指令进行模板类型切换，切换类型:{0}"
                   ,Enum.GetName(typeof(EumModelType), eumModelType)));
            if (eumModelType == EumModelType.None) return false;
            if (currModelType == eumModelType)//如果无切换则不重载
            {
                Appentxt(string.Format("当前模板类型为:{0}，无需切换！",
                    Enum.GetName(typeof(EumModelType), currModelType)));
                return true;
            }
               
            if (cobxModelType.InvokeRequired)
            {
                this.cobxModelType.Invoke(new Action(() =>
                {
                    cobxModelType.SelectedIndex = (int)eumModelType;
                }));
            }
            else
                cobxModelType.SelectedIndex = (int)eumModelType;

            return true;
        }
        
        private void cobxModelType_SelectedIndexChanged(object sender, EventArgs e)
        {
            EumModelType _currModelType = (EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text);
            if (currModelType == _currModelType)//如果无切换则不重载
            {
                Appentxt(string.Format("当前模板类型：{0}与需要选择{1}相同，无需重载！", cobxModelType.Text,
                   Enum.GetName(typeof(EumModelType), currModelType) ));
                return;
            }
               
            RunToolStep = 0;
            toolList.Clear();
            LoadPreTools("配方\\" + CurrRecipeName, _currModelType);
            ModelMatchLoadParma("配方\\" + CurrRecipeName, _currModelType);
            LoadTestingTools("配方\\" + CurrRecipeName, _currModelType);

            ////相机曝光增益跟随模板调整
            CamExposure = (int)float.Parse(GeneralUse.ReadValue("相机", "曝光", "config", "10000", "配方\\" + CurrRecipeName + "\\modelfile\\" + cobxModelType.Text));
            CamExposureBar.Value = CamExposure;
            lblCamExposure.Text = CamExposure.ToString();
            CamGain = (int)float.Parse(GeneralUse.ReadValue("相机", "增益", "config", "0", "配方\\" + CurrRecipeName + "\\modelfile\\" + cobxModelType.Text));
            CamGainBar.Value = CamGain;
            lblCamGain.Text = CamGain.ToString();

            CurrCam.SetExposureTime(CamExposure);
            CurrCam.SetGain(CamGain);


            listViewFlow.Items.Clear();
            for (int i = 0; i < toolList.Count; i++)
                listViewFlow.Items.Add(new ListViewItem(new string[] { i.ToString(), toolList[i] }));

            EnableDetectionControl();

            currvisiontool.clearAll();
            currvisiontool.dispImage(GrabImg);
            currModelType = _currModelType;
        }

        private void btnSendDataToControl_Click(object sender, EventArgs e)
        {
            string buff = "[发送产品角度数据]";
            buff += string.Format("R1:{0},R2:{1}", line1AngleLx.ToString("f3"), line2AngleLx.ToString("f3"));
            Appentxt(buff);
            //产品倾斜弧度数据
            setProductAngleDataHandle?.Invoke(buff.Replace("[发送产品角度数据]", ""), null);
        }

        //禁用numericUpDown控件鼠标中键滚轮消息响应
        private void Num_DiscountAmount_MouseWheel(object sender, MouseEventArgs e)
        {

            HandledMouseEventArgs h = e as HandledMouseEventArgs;
            if (h != null)
            {
                h.Handled = true;
            }
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            frmAgreement fa = new frmAgreement();
            //fa.Owner = this;
            fa.ShowDialog();
        }

        //定位检测相关参数保存，包含模板参数
        private void btnSaveRunParmas_Click(object sender, EventArgs e)
        {
            SaveInspectionParma();

        }
        /// <summary>
        /// 检测相关参数保存，包含模板相关参数
        /// </summary>
        void SaveInspectionParma()
        {

            try
            {
                toolList.Clear();
                SavePreTools();
                SaveModelMatchParma();
                SaveTestingTools();
                listViewFlow.Items.Clear();
                for (int i = 0; i < toolList.Count; i++)
                    listViewFlow.Items.Add(new ListViewItem(new string[] { i.ToString(), toolList[i] }));
                MessageBox.Show("检测相关参数保存成功！");

            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message);
            }

        }

        /*----------------------------------------------------------------*/
        //前处理工具保存
        void SavePreTools()
        {
            if (listViewTools.Items.Count <= 0)
            {
                chxbPretreatmentTool.Checked = false;
                IsUsingPretreatment = false;
            }

            if (!IsUsingPretreatment)
            {
                listViewTools.Items.Clear();
                panel14.Controls.Clear();
                toolParList.Clear();
            }

            d_preToolDataClass.ListToolName.Clear();
            foreach (ListViewItem s in listViewTools.Items)
                d_preToolDataClass.ListToolName.Add(s.SubItems[1].Text);
            if (d_preToolDataClass.ListToolName.Count > 0 && IsUsingPretreatment)
                toolList.Add("前处理工具");


            if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                        == EumModelType.ProductModel_1)
            {

                GeneralUse.WriteSerializationFile<PreToolDataClass>(saveToUsePath +
                      "\\" + "modelfile\\ProductModel_1\\ToolsName", d_preToolDataClass);

                GeneralUse.WriteSerializationFile<List<object>>(saveToUsePath +
                      "\\" + "modelfile\\ProductModel_1\\ToolsData", toolParList);
                GeneralUse.WriteValue("参数", "是否启用", IsUsingPretreatment.ToString(), "前处理工具", saveToUsePath +
                      "\\" + "modelfile\\ProductModel_1");
            }
            else if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                        == EumModelType.ProductModel_2)
            {
                GeneralUse.WriteSerializationFile<PreToolDataClass>(saveToUsePath +
                      "\\" + "modelfile\\ProductModel_2\\ToolsName", d_preToolDataClass);

                GeneralUse.WriteSerializationFile<List<object>>(saveToUsePath +
                      "\\" + "modelfile\\ProductModel_2\\ToolsData", toolParList);
                GeneralUse.WriteValue("参数", "是否启用", IsUsingPretreatment.ToString(), "前处理工具", saveToUsePath +
                      "\\" + "modelfile\\ProductModel_2");
            }
            else if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                          == EumModelType.GluetapModel)
            {
                GeneralUse.WriteSerializationFile<List<object>>(saveToUsePath +
                     "\\" + "modelfile\\GlueTapModel\\ToolsData", toolParList);
                GeneralUse.WriteSerializationFile<PreToolDataClass>(saveToUsePath +
                      "\\" + "modelfile\\GlueTapModel\\ToolsName", d_preToolDataClass);
                GeneralUse.WriteValue("参数", "是否启用", IsUsingPretreatment.ToString(), "前处理工具", saveToUsePath +
                       "\\" + "modelfile\\GlueTapModel");
            }
        }

        //保存检测工具
        void SaveTestingTools()
        {
            if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                  == EumModelType.ProductModel_1)
            {
                SaveLineParmas("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_1\\");
                SaveBlobParms("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_1\\");
                SaveCircleParmas("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_1\\");

                GeneralUse.WriteValue("附加工具", "工具编号",
                      CheckBoxselectID.ToString(), "附加工具类型", "配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_1");

                switch (CheckBoxselectID)
                {
                    case 1:
                        if ((RegionRRect == null || RegionRRect2 == null ||
                            RegionRRect.Width <= 0 || RegionRRect2.Width <= 0)
                            && SearchROI1.Count <= 0)
                            MessageBox.Show("直线检测区域未设置，请确认！");
                        else
                        {
                            if (RegionRRect != null && RegionRRect != null &&
                                RegionRRect.Width > 0 && RegionRRect2.Width > 0)
                            {
                                SearchROI1.Clear();                             
                                SearchROI1.Add(RegionRRect);
                                SearchROI1.Add(RegionRRect2);

                            }
                            GeneralUse.WriteSerializationFile<List<Object>>("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_1\\" + inspectToolPath, SearchROI1);
                        }
                        toolList.Add("直线定位");
                        break;
                    case 3:
                        if ((RegionRRect3 == null ||
                            RegionRRect3.Width <= 0) && SearchROI1.Count <= 0)
                            MessageBox.Show("Blob检测区域未设置，请确认！");
                        else
                        {
                            if (RegionRRect3 != null && RegionRRect3.Width > 0)
                            {
                                SearchROI1.Clear();                        
                                SearchROI1.Add(RegionRRect3);
                            }
                            GeneralUse.WriteSerializationFile<List<Object>>("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_1\\" + inspectToolPath, SearchROI1);

                        }
                        toolList.Add("Blob定位");
                        break;
                    case 2:
                        if ((sectorF == null || sectorF.getRadius <= 0) && SearchROI1.Count <= 0)
                            MessageBox.Show("圆检测区域未设置，请确认！");
                        else
                        {
                            if (sectorF != null && sectorF.getRadius > 0)
                            {
                                SearchROI1.Clear();
                                SearchROI1.Add(sectorF);
                            }

                            GeneralUse.WriteSerializationFile<List<Object>>("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_1\\" + inspectToolPath, SearchROI1);

                        }
                        toolList.Add("找圆定位");
                        break;
                }
            }
            else if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                    == EumModelType.ProductModel_2)
            {
                SaveLineParmas("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_2\\");
                SaveBlobParms("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_2\\");
                SaveCircleParmas("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_2\\");

                GeneralUse.WriteValue("附加工具", "工具编号",
                    CheckBoxselectID.ToString(), "附加工具类型", "配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_2");

                switch (CheckBoxselectID)
                {
                    case 1:
                        if ((RegionRRect == null || RegionRRect2 == null ||
                            RegionRRect.Width <= 0 || RegionRRect2.Width <= 0)
                            && SearchROI2.Count <= 0)
                            MessageBox.Show("直线检测区域未设置，请确认！");
                        else
                        {
                            if (RegionRRect != null && RegionRRect2 != null &&
                                 RegionRRect.Width > 0 && RegionRRect2.Width > 0)

                            {
                                SearchROI2.Clear();                           
                                SearchROI2.Add(RegionRRect);
                                SearchROI2.Add(RegionRRect2);
                            }

                            GeneralUse.WriteSerializationFile<List<Object>>("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_2\\" + inspectToolPath, SearchROI2);
                        }
                        toolList.Add("直线定位");
                        break;
                    case 3:
                        if ((RegionRRect3 == null ||
                            RegionRRect3.Width <= 0) && SearchROI1.Count <= 0)
                            MessageBox.Show("Blob检测区域未设置，请确认！");
                        else
                        {
                            if (RegionRRect3 != null && RegionRRect3.Width > 0)
                            {
                                SearchROI2.Clear();                         
                                SearchROI2.Add(RegionRRect3);
                            }
                            GeneralUse.WriteSerializationFile<List<Object>>("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_2\\" + inspectToolPath, SearchROI2);
                        }
                        toolList.Add("Blob定位");
                        break;
                    case 2:
                        if ((sectorF == null || sectorF.getRadius <= 0) && SearchROI1.Count <= 0)
                            MessageBox.Show("圆检测区域未设置，请确认！");
                        else
                        {
                            if (sectorF != null && sectorF.getRadius > 0)
                            {
                                SearchROI2.Clear();
                                SearchROI2.Add(sectorF);
                            }
                            GeneralUse.WriteSerializationFile<List<Object>>("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_2\\" + inspectToolPath, SearchROI2);
                        }
                        toolList.Add("找圆定位");
                        break;

                }

            }
            else if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                     == EumModelType.CaliBoardModel)
            {

                SaveLineParmas("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\CaliBoardModel\\");
                SaveBlobParms("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\CaliBoardModel\\");
                SaveCircleParmas("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\CaliBoardModel\\");
            }
            else
            {
                SaveLineParmas("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\GlueTapModel\\");
                SaveBlobParms("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\GlueTapModel\\");
                SaveCircleParmas("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\GlueTapModel\\");

                GeneralUse.WriteValue("附加工具", "工具编号",
               CheckBoxselectID.ToString(), "附加工具类型", "配方\\" + CurrRecipeName +
             "\\" + "modelfile\\GlueTapModel");

                if (CheckBoxselectID == 3)
                {
                    ///只给开放Blob中心
                    if ((RegionRRect3 == null ||
                              RegionRRect3.Width <= 0) && SearchROI3.Count <= 0)
                        MessageBox.Show("Blob检测区域未设置，请确认！");
                    else
                    {
                        if (RegionRRect3 != null && RegionRRect3.Width > 0)
                        {
                            SearchROI3.Clear();                      
                            SearchROI3.Add(RegionRRect3);
                        }
                        GeneralUse.WriteSerializationFile<List<Object>>("配方\\" + CurrRecipeName +
              "\\" + "modelfile\\GlueTapModel\\" + inspectToolPath, SearchROI3);
                    }
                    toolList.Add("Blob定位");
                }
            }
        }

        //模板相关参数保存
        void SaveModelMatchParma()
        {
            try
            {
                if(currMatchMethod== EumMatchMethod.ShapeMatch)
                {
                    parmaData = new ShapeMatchData();
                    (parmaData as ShapeMatchData).baseAngle = modelangle;
                    (parmaData as ShapeMatchData).tpContour = templateContour;
                    (parmaData as ShapeMatchData).Segthreshold = (double)NumSegthreshold.Value;
                    (parmaData as ShapeMatchData).MatchValue = (double)NumMatchValue.Value;
                    (parmaData as ShapeMatchData).MincoutourLen = (int)NumMincoutourLen.Value;
                    (parmaData as ShapeMatchData).MaxcoutourLen = (int)NumMaxcoutourLen.Value;
                    (parmaData as ShapeMatchData).MinContourArea = (int)NumMinContourArea.Value;
                    (parmaData as ShapeMatchData).MaxContourArea = (int)NumMaxContourArea.Value;
                    if (matchBaseInfo == null)
                        matchBaseInfo = new MatchBaseInfo();
                    matchBaseInfo.BaseX = double.Parse(lIstModelInfo.Items[0].SubItems[1].Text);
                    matchBaseInfo.BaseY = double.Parse(lIstModelInfo.Items[1].SubItems[1].Text);
                    matchBaseInfo.BaseAngle = double.Parse(lIstModelInfo.Items[2].SubItems[1].Text);
                    matchBaseInfo.ContourLength = double.Parse(lIstModelInfo.Items[3].SubItems[1].Text);
                    matchBaseInfo.ContourArea = double.Parse(lIstModelInfo.Items[4].SubItems[1].Text);
                }
                else if(currMatchMethod== EumMatchMethod.NccMatch)
                {
                    parmaData = new NccTemplateMatchData();                     
                    (parmaData as NccTemplateMatchData).StartAngle = (double)NumSegthreshold.Value;
                    (parmaData as NccTemplateMatchData).AngleRange = (double)NumMatchValue.Value;
                   (parmaData as NccTemplateMatchData).AngleStep = (double)NumMincoutourLen.Value;
                   (parmaData as NccTemplateMatchData).NumLevels= (int)NumMaxcoutourLen.Value;
                   (parmaData as NccTemplateMatchData).MatchScore = (double)NumMinContourArea.Value;
                    if (matchBaseInfo == null)
                        matchBaseInfo = new MatchBaseInfo();
                    matchBaseInfo.BaseX = double.Parse(lIstModelInfo.Items[0].SubItems[1].Text);
                    matchBaseInfo.BaseY = double.Parse(lIstModelInfo.Items[1].SubItems[1].Text);
                    matchBaseInfo.BaseAngle = double.Parse(lIstModelInfo.Items[2].SubItems[1].Text);
              
                }
                else
                {
                    parmaData = new CannyTemplateMatchData();
                    (parmaData as CannyTemplateMatchData).StartAngle = (double)NumSegthreshold.Value;
                    (parmaData as CannyTemplateMatchData).AngleRange = (double)NumMatchValue.Value;
                    (parmaData as CannyTemplateMatchData).SegmentThresh = (double)NumMincoutourLen.Value;
                    (parmaData as CannyTemplateMatchData).NumLevels = (int)NumMaxcoutourLen.Value;
                    (parmaData as CannyTemplateMatchData).MatchScore = (double)NumMinContourArea.Value;
                    if (matchBaseInfo == null)
                        matchBaseInfo = new MatchBaseInfo();
                    matchBaseInfo.BaseX = double.Parse(lIstModelInfo.Items[0].SubItems[1].Text);
                    matchBaseInfo.BaseY = double.Parse(lIstModelInfo.Items[1].SubItems[1].Text);
                    matchBaseInfo.BaseAngle = double.Parse(lIstModelInfo.Items[2].SubItems[1].Text);
                }

       
                if (!Directory.Exists("配方\\" + CurrRecipeName +
                      "\\Config"))
                    Directory.CreateDirectory("配方\\" + CurrRecipeName +
                      "\\Config");
                GeneralUse.WriteValue("当前模板", "类型",
                       cobxModelType.Text, "模板匹配类型", "配方\\" + CurrRecipeName + "\\Config");


                if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                        == EumModelType.ProductModel_1)
                {
                   
                    GeneralUse.WriteValue("模板", "模板匹配方法", cobxTemplateMatchMethod.Text, "config",
                          "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\Config");
                    GeneralUse.WriteValue("模板", "模板区域搜索方法", cobxMatchSearchRegion.Text, "config",
                        "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\Config");
                    GeneralUse.WriteValue("模板", "模板匹配", isUsingModelMatch.ToString(), "config",
                      "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\Config");
                    GeneralUse.WriteValue("模板", "自动圆匹配", isUsingAutoCircleMatch.ToString(), "config",
                             "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\Config");
                    GeneralUse.WriteValue("模板", "模板基准点", modelOrigion.ToString(), "config",
                             "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\Config");

                    GeneralUse.WriteSerializationFile<MatchBaseInfo>("配方\\" + CurrRecipeName +
                        "\\modelfile\\ProductModel_1\\Config\\基准轮廓信息.ini"
                                          , matchBaseInfo);


                    if (isUsingModelMatch)
                        toolList.Add("模板匹配");
                    else if (isUsingAutoCircleMatch)
                        toolList.Add("自动圆匹配");

                 

                    if (currMatchMethod == EumMatchMethod.ShapeMatch)
                    {
                        GeneralUse.WriteSerializationFile<ShapeMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1"
                                   + "\\形状匹配.ini", parmaData as ShapeMatchData);
                        if (!Directory.Exists("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\ShapeMatch"))
                            Directory.CreateDirectory("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\ShapeMatch");
                        //模板保存
                        if (!this.modeltp.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\ShapeMatch" +
                                     "\\模板.png", modeltp);

                    }
                      
                    else if(currMatchMethod== EumMatchMethod.NccMatch)
                    {
                        GeneralUse.WriteSerializationFile<NccTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1"
                                                 + "\\Ncc匹配.ini", parmaData as NccTemplateMatchData);

                        if (!Directory.Exists("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\NccMatch"))
                            Directory.CreateDirectory("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\NccMatch");
                        //模板保存
                        if (!this.modeltp.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\NccMatch" +
                                     "\\显示模板.png", modeltp);
                        //模板保存
                        if (!this.modelGrayMat.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\NccMatch" +
                                     "\\测试模板.png", modelGrayMat);
                    }
                             
                    else
                    {
                        GeneralUse.WriteSerializationFile<CannyTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1"
                                     + "\\Canny匹配.ini", parmaData as CannyTemplateMatchData);

                        if (!Directory.Exists("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\CannyMatch"))
                            Directory.CreateDirectory("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\CannyMatch");
                        //模板保存
                        if (!this.modeltp.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\CannyMatch" +
                                     "\\原图模板.png", modeltp);
                        //模板保存
                        if (!this.cannyMat.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\CannyMatch" +
                                     "\\Canny模板.png", cannyMat);

                    }
                    

                    SaveCircleMatchParma("配方\\" + CurrRecipeName +
                                                  "\\" + "modelfile\\ProductModel_1\\");
                    SaveModelparmasHandle?.Invoke("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1", null);
                    GeneralUse.WriteSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\ProductModel_1\\模板创建区域.rectf", setModelROIData);
                    GeneralUse.WriteSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
              "\\" + "modelfile\\ProductModel_1\\模板搜索区域.rectf", matchSearchRegion);
                    if ((eumMatchSearchMethod == EumMatchSearchMethod.PartRegion) &&
                            (matchSearchRegion.Width <= 0 || matchSearchRegion.Height <= 0))
                        MessageBox.Show("模板搜索区域未设置，请确认！");
                    Appentxt(string.Format("当前模板：{0}",
                       Enum.GetName(typeof(EumModelType), currModelType)) +

                       ((currModelType == EumModelType.ProductModel_1) ? "，模板基准点:" + modelOrigion : "")
                       );
                }
                else if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                        == EumModelType.ProductModel_2)
                {
                    GeneralUse.WriteValue("模板", "模板匹配方法", cobxTemplateMatchMethod.Text, "config",
                            "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\Config");
                    GeneralUse.WriteValue("模板", "模板区域搜索方法", cobxMatchSearchRegion.Text, "config",
                    "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\Config");
                    GeneralUse.WriteValue("模板", "模板匹配", isUsingModelMatch.ToString(), "config",
                       "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\Config");
                    GeneralUse.WriteValue("模板", "自动圆匹配", isUsingAutoCircleMatch.ToString(), "config",
                             "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\Config");

                    GeneralUse.WriteValue("模板", "模板基准点", modelOrigion.ToString(), "config",
                           "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\Config");

                    GeneralUse.WriteSerializationFile<MatchBaseInfo>("配方\\" + CurrRecipeName +
                        "\\modelfile\\ProductModel_2\\Config\\基准轮廓信息.ini"
                                          , matchBaseInfo);
                    if (isUsingModelMatch)
                        toolList.Add("模板匹配");
                    else if (isUsingAutoCircleMatch)
                        toolList.Add("自动圆匹配");

                  
                 
                    if (currMatchMethod == EumMatchMethod.ShapeMatch)
                    {
                        if (!Directory.Exists("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\ShapeMatch"))
                            Directory.CreateDirectory("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\ShapeMatch");
                        //模板保存
                        if (!this.modeltp.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\ShapeMatch" +
                                     "\\模板.png", modeltp);

                        GeneralUse.WriteSerializationFile<ShapeMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2"
                           + "\\形状匹配.ini", parmaData as ShapeMatchData);
                    }
                      
                    else if (currMatchMethod == EumMatchMethod.NccMatch)
                    {
                        if (!Directory.Exists("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\NccMatch"))
                            Directory.CreateDirectory("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\NccMatch");
                        //模板保存
                        if (!this.modeltp.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\NccMatch" +
                                     "\\显示模板.png", modeltp);
                        //模板保存
                        if (!this.modelGrayMat.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\NccMatch" +
                                     "\\测试模板.png", modelGrayMat);
                        GeneralUse.WriteSerializationFile<NccTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2"
                                         + "\\Ncc匹配.ini", parmaData as NccTemplateMatchData);
                    }
                     
                    else
                    {
                        if (!Directory.Exists("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\CannyMatch"))
                            Directory.CreateDirectory("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\CannyMatch");
                        //模板保存
                        if (!this.modeltp.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\CannyMatch" +
                                     "\\原图模板.png", modeltp);
                        //模板保存
                        if (!this.cannyMat.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\CannyMatch" +
                                     "\\Canny模板.png", cannyMat);

                        GeneralUse.WriteSerializationFile<CannyTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2"
                                 + "\\Canny匹配.ini", parmaData as CannyTemplateMatchData);

                    }
                    
                    SaveCircleMatchParma("配方\\" + CurrRecipeName +
                "\\" + "modelfile\\ProductModel_2\\");

                    SaveModelparmasHandle?.Invoke("配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2", null);
                    GeneralUse.WriteSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                           "\\" + "modelfile\\ProductModel_2\\模板创建区域.rectf", setModelROIData);
                    GeneralUse.WriteSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                             "\\" + "modelfile\\ProductModel_2\\模板搜索区域.rectf", matchSearchRegion);
                    if ((eumMatchSearchMethod== EumMatchSearchMethod.PartRegion)&&
                            (matchSearchRegion.Width <= 0 || matchSearchRegion.Height <= 0))
                        MessageBox.Show("模板搜索区域未设置，请确认！");
                    Appentxt(string.Format("当前模板：{0}",
                       Enum.GetName(typeof(EumModelType), currModelType)) +

                       ((currModelType == EumModelType.ProductModel_2) ? "，模板基准点:" + modelOrigion : "")
                       );

                }
                else if ((EumModelType)Enum.Parse(typeof(EumModelType), cobxModelType.Text)
                         == EumModelType.CaliBoardModel)
                {
                    GeneralUse.WriteValue("模板", "模板匹配方法", cobxTemplateMatchMethod.Text, "config",
                         "配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\Config");
                    GeneralUse.WriteValue("模板", "模板区域搜索方法", cobxMatchSearchRegion.Text, "config",
                  "配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\Config");
                    GeneralUse.WriteValue("模板", "模板匹配", isUsingModelMatch.ToString(), "config",
                    "配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\Config");
                    GeneralUse.WriteValue("模板", "自动圆匹配", isUsingAutoCircleMatch.ToString(), "config",
                             "配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\Config");

                    GeneralUse.WriteSerializationFile<MatchBaseInfo>("配方\\" + CurrRecipeName +
                     "\\modelfile\\CaliBoardModel\\Config\\基准轮廓信息.ini"
                                       , matchBaseInfo);
                
                    if (isUsingModelMatch)
                        toolList.Add("模板匹配");
                    else if (isUsingAutoCircleMatch)
                        toolList.Add("自动圆匹配");

                 
                    if (currMatchMethod == EumMatchMethod.ShapeMatch)
                    {
                        if (!Directory.Exists("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\ShapeMatch"))
                            Directory.CreateDirectory("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\ShapeMatch");
                        //模板保存
                        if (!this.modeltp.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\ShapeMatch" +
                                     "\\模板.png", modeltp);
                        GeneralUse.WriteSerializationFile<ShapeMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel"
                          + "\\形状匹配.ini", parmaData as ShapeMatchData);
                    }
                      
                    else if (currMatchMethod == EumMatchMethod.NccMatch)
                    {
                        if (!Directory.Exists("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\NccMatch"))
                            Directory.CreateDirectory("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\NccMatch");
                        //模板保存
                        if (!this.modeltp.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\NccMatch" +
                                     "\\显示模板.png", modeltp);
                        //模板保存
                        if (!this.modelGrayMat.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\NccMatch" +
                                     "\\测试模板.png", modelGrayMat);

                        GeneralUse.WriteSerializationFile<NccTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel"
                                          + "\\Ncc匹配.ini", parmaData as NccTemplateMatchData);
                    }
                      
                    else
                    {
                        if (!Directory.Exists("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\CannyMatch"))
                            Directory.CreateDirectory("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\CannyMatch");
                        //模板保存
                        if (!this.modeltp.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\CannyMatch" +
                                     "\\原图模板.png", modeltp);
                        //模板保存
                        if (!this.cannyMat.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\CannyMatch" +
                                     "\\Canny模板.png", cannyMat);

                        GeneralUse.WriteSerializationFile<CannyTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel"
                             + "\\Canny匹配.ini", parmaData as CannyTemplateMatchData);
                    }
                

                    SaveCircleMatchParma("配方\\" + CurrRecipeName +
              "\\" + "modelfile\\CaliBoardModel\\");

                    // SaveModelparmasHandle?.Invoke(CaliconfigPath, null);   自动检测不需要知道标定板更新
                    GeneralUse.WriteSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\CaliBoardModel\\模板创建区域.rectf", setModelROIData);
                    GeneralUse.WriteSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                    "\\" + "modelfile\\CaliBoardModel\\模板搜索区域.rectf", matchSearchRegion);
                    if ((eumMatchSearchMethod == EumMatchSearchMethod.PartRegion) &&
                            (matchSearchRegion.Width <= 0 || matchSearchRegion.Height <= 0))
                        MessageBox.Show("模板搜索区域未设置，请确认！");
                }
                else
                {
                    GeneralUse.WriteValue("模板", "模板匹配方法", cobxTemplateMatchMethod.Text, "config",
                      "配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\Config");
                    GeneralUse.WriteValue("模板", "模板区域搜索方法", cobxMatchSearchRegion.Text, "config",
                 "配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\Config");
                    GeneralUse.WriteValue("模板", "模板匹配", isUsingModelMatch.ToString(), "config",
                  "配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\Config");
                    GeneralUse.WriteValue("模板", "自动圆匹配", isUsingAutoCircleMatch.ToString(), "config",
                             "配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\Config");
                    GeneralUse.WriteValue("模板", "模板基准点", modelOrigion.ToString(), "config",
                           "配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\Config");
                    GeneralUse.WriteSerializationFile<MatchBaseInfo>("配方\\" + CurrRecipeName +
                 "\\modelfile\\GlueTapModel\\Config\\基准轮廓信息.ini"
                                   , matchBaseInfo);

                    if (isUsingModelMatch)
                        toolList.Add("模板匹配");
                    else if (isUsingAutoCircleMatch)
                        toolList.Add("自动圆匹配");

               
                    if (currMatchMethod == EumMatchMethod.ShapeMatch)
                    {
                        if (!Directory.Exists("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\ShapeMatch"))
                            Directory.CreateDirectory("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\ShapeMatch");
                        //模板保存
                        if (!this.modeltp.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\ShapeMatch" +
                                     "\\模板.png", modeltp);
                        GeneralUse.WriteSerializationFile<ShapeMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel"
                                 + "\\形状匹配.ini", parmaData as ShapeMatchData);
                    }
                      
                    else if (currMatchMethod == EumMatchMethod.NccMatch)
                    {
                        if (!Directory.Exists("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\NccMatch"))
                            Directory.CreateDirectory("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\NccMatch");
                        //模板保存
                        if (!this.modeltp.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\NccMatch" +
                                     "\\显示模板.png", modeltp);
                        //模板保存
                        if (!this.modelGrayMat.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\NccMatch" +
                                     "\\测试模板.png", modelGrayMat);
                        GeneralUse.WriteSerializationFile<NccTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel"
                                          + "\\Ncc匹配.ini", parmaData as NccTemplateMatchData);
                    }
                      
                    else
                    {
                        if (!Directory.Exists("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\CannyMatch"))
                            Directory.CreateDirectory("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\CannyMatch");
                        //模板保存
                        if (!this.modeltp.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\CannyMatch" +
                                     "\\原图模板.png", modeltp);
                        //模板保存
                        if (!this.cannyMat.Empty())
                            MatDataWriteRead.WriteImage("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\CannyMatch" +
                                     "\\Canny模板.png", cannyMat);

                        GeneralUse.WriteSerializationFile<CannyTemplateMatchData>("配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel"
                                  + "\\Canny匹配.ini", parmaData as CannyTemplateMatchData);
                    }
                     
                    SaveCircleMatchParma("配方\\" + CurrRecipeName +
                  "\\" + "modelfile\\GlueTapModel\\");

                    GeneralUse.WriteSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                "\\" + "modelfile\\GlueTapModel\\模板创建区域.rectf", setModelROIData);

                    GeneralUse.WriteSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
               "\\" + "modelfile\\GlueTapModel\\模板搜索区域.rectf", matchSearchRegion);
                    if ((eumMatchSearchMethod == EumMatchSearchMethod.PartRegion) &&
                          (matchSearchRegion.Width <= 0 || matchSearchRegion.Height <= 0))
                        MessageBox.Show("模板搜索区域未设置，请确认！");
                    Appentxt(string.Format("当前模板：{0}",
                       Enum.GetName(typeof(EumModelType), currModelType)) +

                       ((currModelType == EumModelType.GluetapModel) ? "，模板基准点:" + modelOrigion : "")
                       );
                }
                // MessageBox.Show("模板相关参数保存成功！");


            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message);
            }

        }
        //保存圆匹配参数
        void SaveCircleMatchParma(string direFilePath)
        {
            parmaData = new HoughCircleData();
            (parmaData as HoughCircleData).MinDist = (double)NumMinDist.Value;
            (parmaData as HoughCircleData).Param1 = (double)NumParam1.Value;
            (parmaData as HoughCircleData).Param2 = (double)NumParam2.Value;
            (parmaData as HoughCircleData).MinRadius = (int)numberMinRadius.Value;
            (parmaData as HoughCircleData).MaxRadius = (int)numberMaxRadius.Value;
            GeneralUse.WriteSerializationFile<HoughCircleData>(direFilePath + CirleMatchconfigPath,
                      parmaData as HoughCircleData);

        }
        //保存直线检测参数
        void SaveLineParmas(string direFilePath)
        {
            string tem = CobxfitMethod1.Text == "最小二乘法" ? "Least_square_method" : "Random_sampling_consistency";
            //霍夫直线参数
            parmaData = new LinearCaliperData
            {
                caliperWidth = (int)NumcaliperWidth1.Value,
                caliperNum = (int)NumcaliperNum1.Value,
                edgeThreshold = (byte)NumedgeThreshold1.Value,
                fitMethod = (EumFittingMethod)Enum.Parse(typeof(EumFittingMethod), tem),
                edgePolarity = (EumEdgePolarity)Enum.Parse(typeof(EumEdgePolarity), cobxPolarity1.Text)
            };
            //parmaData.ROI = new RotatedRectF( RegionRRect.Center.X, RegionRRect.Center.Y,
            //    RegionRRect.Size.Width, RegionRRect.Size.Height, RegionRRect.Angle);


            GeneralUse.WriteSerializationFile<LinearCaliperData>(direFilePath + Line1configPath,
                                                parmaData as LinearCaliperData);

            string tem2 = CobxfitMethod2.Text == "最小二乘法" ? "Least_square_method" : "Random_sampling_consistency";
            //霍夫直线2参数
            parmaData = new LinearCaliperData
            {
                caliperWidth = (int)NumcaliperWidth2.Value,
                caliperNum = (int)NumcaliperNum2.Value,
                edgeThreshold = (byte)NumedgeThreshold2.Value,
                fitMethod = (EumFittingMethod)Enum.Parse(typeof(EumFittingMethod), tem2),
                edgePolarity = (EumEdgePolarity)Enum.Parse(typeof(EumEdgePolarity), cobxPolarity2.Text)
            };
            //parmaData.ROI = new RotatedRectF(RegionRRect.Center.X, RegionRRect.Center.Y,
            //   RegionRRect.Size.Width, RegionRRect.Size.Height, RegionRRect.Angle);

            GeneralUse.WriteSerializationFile<LinearCaliperData>(direFilePath + Line2configPath,
                                        parmaData as LinearCaliperData);

        }
        //保存圆检测参数
        void SaveCircleParmas(string direFilePath)
        {
            string tem = cobxPolarity3.Text;
            parmaData = new CircularCaliperData
            {
                caliperNum = (int)NumcaliperNum.Value,
                caliperWidth = (int)NumcaliperWidth.Value,
                edgeThreshold = (byte)NumedgeThreshold.Value,
                circleDir = (EumDirectionOfCircle)Enum.Parse(typeof(EumDirectionOfCircle), cobxCircleDir.Text),
                edgePolarity = (EumEdgePolarity)Enum.Parse(typeof(EumEdgePolarity), tem)
            };
          
            GeneralUse.WriteSerializationFile<CircularCaliperData>(direFilePath + CircleconfigPath,
                      parmaData as CircularCaliperData);

        }
        //保存Blob检测参数
        void SaveBlobParms(string direFilePath)
        {
            //Blob3检测参数
            parmaData = new Blob3Data
            {
                edgeThreshold = (double)Numminthd.Value,
                minArea = (int)NumareaLow.Value,
                maxArea = (int)NumareaHigh.Value,
                eumWhiteOrBlack = (EumWhiteOrBlack)Enum.Parse(typeof(EumWhiteOrBlack), cobxPolarity.Text)

            };
           
            GeneralUse.WriteSerializationFile<Blob3Data>(direFilePath + BlobconfigPath, parmaData as Blob3Data);
        }

        /*----------------------------------------------------------------*/
        //前处理工具加载
        void LoadPreTools(string dirFileName, EumModelType _currModelType = EumModelType.None)
        {
            if (_currModelType == EumModelType.None)
            {
                string buf = GeneralUse.ReadValue("当前模板", "类型", "模板匹配类型", "ProductModel_1", dirFileName + "\\Config");
                if (buf == "CaliModel" || buf == "CalibModel")
                    buf = "CaliBoardModel";
                currModelType = (EumModelType)Enum.Parse(typeof(EumModelType), buf);
            }
            else
                currModelType = _currModelType;


            if (!Directory.Exists(dirFileName))
                Directory.CreateDirectory(dirFileName);
            if (currModelType == EumModelType.ProductModel_1)
            {
                toolParList = GeneralUse.ReadSerializationFile<List<object>>(dirFileName +
                      "\\" + "modelfile\\ProductModel_1\\ToolsData");
                d_preToolDataClass = GeneralUse.ReadSerializationFile<PreToolDataClass>(dirFileName +
                      "\\" + "modelfile\\ProductModel_1\\ToolsName");
                IsUsingPretreatment = bool.Parse(GeneralUse.ReadValue("参数", "是否启用", "前处理工具", "false", dirFileName +
                      "\\" + "modelfile\\ProductModel_1\\"));
            }
            else if (currModelType == EumModelType.ProductModel_2)
            {
                toolParList = GeneralUse.ReadSerializationFile<List<object>>(dirFileName +
                      "\\" + "modelfile\\ProductModel_2\\ToolsData");
                d_preToolDataClass = GeneralUse.ReadSerializationFile<PreToolDataClass>(dirFileName +
                      "\\" + "modelfile\\ProductModel_2\\ToolsName");
                IsUsingPretreatment = bool.Parse(GeneralUse.ReadValue("参数", "是否启用", "前处理工具", "false", dirFileName +
                      "\\" + "modelfile\\ProductModel_2\\"));
            }
            else if (currModelType == EumModelType.GluetapModel)
            {
                toolParList = GeneralUse.ReadSerializationFile<List<object>>(dirFileName +
                      "\\" + "modelfile\\GlueTapModel\\ToolsData");
                d_preToolDataClass = GeneralUse.ReadSerializationFile<PreToolDataClass>(dirFileName +
                      "\\" + "modelfile\\GlueTapModel\\ToolsName");
                IsUsingPretreatment = bool.Parse(GeneralUse.ReadValue("参数", "是否启用", "前处理工具", "false", dirFileName +
                      "\\" + "modelfile\\GlueTapModel\\"));
            }
            else
            {
                IsUsingPretreatment = false;
                toolParList = null;
                d_preToolDataClass = null;

            }

            if (d_preToolDataClass == null)
                d_preToolDataClass = new PreToolDataClass();

            if (toolParList == null)
                toolParList = new List<object>();

            listViewTools.Items.Clear();
            panel14.Controls.Clear();
            toolindex = -1;

            foreach (var s in d_preToolDataClass.ListToolName)
            {
                toolindex++;
                ListViewItem tem = new ListViewItem(new string[2] { toolindex.ToString(), s });
                listViewTools.Items.Add(tem);
            }
            if (listViewTools.Items.Count > 0)
                listViewTools.Items[0].Selected = true;
            chxbPretreatmentTool.Checked = IsUsingPretreatment;
            enbalePreControl(IsUsingPretreatment);
            if (IsUsingPretreatment)
                toolList.Add("前处理工具");
        }
        //加载模板参数
        void ModelMatchLoadParma(string dirFileName, EumModelType _currModelType = EumModelType.None)
        {
            if (!Directory.Exists(dirFileName))
                Directory.CreateDirectory(dirFileName);

            if (_currModelType == EumModelType.None)
            {
                string buf = GeneralUse.ReadValue("当前模板", "类型", "模板匹配类型", "ProductModel_1", dirFileName + "\\Config");
                if (buf == "CaliModel" || buf == "CalibModel")
                    buf = "CaliBoardModel";
                currModelType = (EumModelType)Enum.Parse(typeof(EumModelType), buf);
            }
            else
                currModelType = _currModelType;

        
            try
            {
                picTemplate.Image = null;
                this.modeltp = new Mat();         
                string errmsg = string.Empty;
                if (currModelType == EumModelType.ProductModel_1)
                {
                    string method = GeneralUse.ReadValue("模板", "模板匹配方法", "config", "ShapeMatch",
                                        "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\Config");
                    currMatchMethod = (EumMatchMethod)Enum.Parse(typeof(EumMatchMethod), method);

                    string search = GeneralUse.ReadValue("模板", "模板区域搜索方法", "config", "全局搜索",
                                        "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\Config");
                    search = search == "全局搜索" ? "AllRegion" : "PartRegion";
                    eumMatchSearchMethod = (EumMatchSearchMethod)Enum.Parse(typeof(EumMatchSearchMethod), search);
                    cobxMatchSearchRegion.SelectedIndex = (int)eumMatchSearchMethod;
                    btnModelSearchRegion.Enabled = eumMatchSearchMethod == EumMatchSearchMethod.PartRegion;

                    setModelROIData = GeneralUse.ReadSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                                   "\\" + "modelfile\\ProductModel_1\\模板创建区域.rectf");

                    matchSearchRegion = GeneralUse.ReadSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                                "\\" + "modelfile\\ProductModel_1\\模板搜索区域.rectf");
                  

                    LoadCircleMatchParmas(dirFileName + "\\" + "modelfile\\ProductModel_1\\");

                    chxbModelMatch.Checked = isUsingModelMatch = bool.Parse(GeneralUse.ReadValue("模板", "模板匹配", "config",
                                   "true", dirFileName + "\\modelfile\\ProductModel_1\\Config"));
                    chxbAutoCircleMatch.Checked = isUsingAutoCircleMatch = bool.Parse(GeneralUse.ReadValue("模板", "自动圆匹配", "config",
                                    "false", dirFileName + "\\modelfile\\ProductModel_1\\Config"));

                    modelOrigion = GeneralUse.ReadValue("模板", "模板基准点", "config", "0,0,0",
                         "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_1\\Config");

                    matchBaseInfo = GeneralUse.ReadSerializationFile<MatchBaseInfo>("配方\\" + CurrRecipeName +
             "\\modelfile\\ProductModel_1\\Config\\基准轮廓信息.ini");

                    if (isUsingModelMatch)
                        toolList.Add("模板匹配");
                    else if (isUsingAutoCircleMatch)
                        toolList.Add("自动圆匹配");

                }
                else if (currModelType == EumModelType.ProductModel_2)
                {
                    string method = GeneralUse.ReadValue("模板", "模板匹配方法", "config", "ShapeMatch",
                                  "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\Config");
                    currMatchMethod = (EumMatchMethod)Enum.Parse(typeof(EumMatchMethod), method);

                    string search = GeneralUse.ReadValue("模板", "模板区域搜索方法", "config", "全局搜索",
                                   "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\Config");
                    search = search == "全局搜索" ? "AllRegion" : "PartRegion";
                    eumMatchSearchMethod = (EumMatchSearchMethod)Enum.Parse(typeof(EumMatchSearchMethod), search);
                    cobxMatchSearchRegion.SelectedIndex = (int)eumMatchSearchMethod;
                    btnModelSearchRegion.Enabled = eumMatchSearchMethod == EumMatchSearchMethod.PartRegion;

                    setModelROIData = GeneralUse.ReadSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                                     "\\" + "modelfile\\ProductModel_2\\模板创建区域.rectf");
                    matchSearchRegion = GeneralUse.ReadSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                            "\\" + "modelfile\\ProductModel_2\\模板搜索区域.rectf");

                    LoadCircleMatchParmas(dirFileName + "\\" + "modelfile\\ProductModel_2\\");

                    chxbModelMatch.Checked = isUsingModelMatch = bool.Parse(GeneralUse.ReadValue("模板", "模板匹配", "config",
                                        "true", dirFileName + "\\modelfile\\ProductModel_2\\Config"));
                    chxbAutoCircleMatch.Checked = isUsingAutoCircleMatch = bool.Parse(GeneralUse.ReadValue("模板", "自动圆匹配", "config",
                                      "false", dirFileName + "\\modelfile\\ProductModel_2\\Config"));
                    modelOrigion = GeneralUse.ReadValue("模板", "模板基准点", "config", "0,0,0",
                        "配方\\" + CurrRecipeName + "\\modelfile\\ProductModel_2\\Config");

                    matchBaseInfo = GeneralUse.ReadSerializationFile<MatchBaseInfo>("配方\\" + CurrRecipeName +
            "\\modelfile\\ProductModel_2\\Config\\基准轮廓信息.ini");

                    if (isUsingModelMatch)
                        toolList.Add("模板匹配");
                    else if (isUsingAutoCircleMatch)
                        toolList.Add("自动圆匹配");

                }
                else if (currModelType == EumModelType.CaliBoardModel)
                {
                    string method = GeneralUse.ReadValue("模板", "模板匹配方法", "config", "ShapeMatch",
                            "配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\Config");
                    currMatchMethod = (EumMatchMethod)Enum.Parse(typeof(EumMatchMethod), method);


                    string search = GeneralUse.ReadValue("模板", "模板区域搜索方法", "config", "全局搜索",
                                   "配方\\" + CurrRecipeName + "\\modelfile\\CaliBoardModel\\Config");
                    search = search == "全局搜索" ? "AllRegion" : "PartRegion";
                    eumMatchSearchMethod = (EumMatchSearchMethod)Enum.Parse(typeof(EumMatchSearchMethod), search);
                    cobxMatchSearchRegion.SelectedIndex = (int)eumMatchSearchMethod;
                    btnModelSearchRegion.Enabled = eumMatchSearchMethod == EumMatchSearchMethod.PartRegion;
                    setModelROIData = GeneralUse.ReadSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                                        "\\" + "modelfile\\CaliBoardModel\\模板创建区域.rectf");
                    matchSearchRegion = GeneralUse.ReadSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                         "\\" + "modelfile\\CaliBoardModel\\模板搜索区域.rectf");

                    LoadCircleMatchParmas(dirFileName + "\\" + "modelfile\\CaliBoardModel\\");

                    chxbModelMatch.Checked = isUsingModelMatch = bool.Parse(GeneralUse.ReadValue("模板", "模板匹配", "config",
                                           "true", dirFileName + "\\modelfile\\CaliBoardModel\\Config"));
                    chxbAutoCircleMatch.Checked = isUsingAutoCircleMatch = bool.Parse(GeneralUse.ReadValue("模板", "自动圆匹配", "config",
                                             "false", dirFileName + "\\modelfile\\CaliBoardModel\\Config"));
                    matchBaseInfo = GeneralUse.ReadSerializationFile<MatchBaseInfo>("配方\\" + CurrRecipeName +
             "\\modelfile\\CaliBoardModel\\Config\\基准轮廓信息.ini");

                    if (isUsingModelMatch)
                        toolList.Add("模板匹配");
                    else if (isUsingAutoCircleMatch)
                        toolList.Add("自动圆匹配");
                }
                else
                {
                    string method = GeneralUse.ReadValue("模板", "模板匹配方法", "config", "ShapeMatch",
                                    "配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\Config");
                    currMatchMethod = (EumMatchMethod)Enum.Parse(typeof(EumMatchMethod), method);

                    string search = GeneralUse.ReadValue("模板", "模板区域搜索方法", "config", "全局搜索",
                              "配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\Config");
                    search = search == "全局搜索" ? "AllRegion" : "PartRegion";
                    eumMatchSearchMethod = (EumMatchSearchMethod)Enum.Parse(typeof(EumMatchSearchMethod), search);
                    cobxMatchSearchRegion.SelectedIndex = (int)eumMatchSearchMethod;
                    btnModelSearchRegion.Enabled = eumMatchSearchMethod == EumMatchSearchMethod.PartRegion;
                    setModelROIData = GeneralUse.ReadSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                                             "\\" + "modelfile\\GlueTapModel\\模板创建区域.rectf");
                    matchSearchRegion = GeneralUse.ReadSerializationFile<RectangleF>("配方\\" + CurrRecipeName +
                                "\\" + "modelfile\\GlueTapModel\\模板搜索区域.rectf");

                    LoadCircleMatchParmas(dirFileName + "\\" + "modelfile\\GlueTapModel\\");

                    chxbModelMatch.Checked = isUsingModelMatch = bool.Parse(GeneralUse.ReadValue("模板", "模板匹配", "config",
                                        "true", dirFileName + "\\modelfile\\GlueTapModel\\Config"));
                    chxbAutoCircleMatch.Checked = isUsingAutoCircleMatch = bool.Parse(GeneralUse.ReadValue("模板", "自动圆匹配", "config",
                                                "false", dirFileName + "\\modelfile\\GlueTapModel\\Config"));
                    modelOrigion = GeneralUse.ReadValue("模板", "模板基准点", "config", "0,0,0",
                        "配方\\" + CurrRecipeName + "\\modelfile\\GlueTapModel\\Config");
                    matchBaseInfo = GeneralUse.ReadSerializationFile<MatchBaseInfo>("配方\\" + CurrRecipeName +
            "\\modelfile\\GlueTapModel\\Config\\基准轮廓信息.ini");

                    if (isUsingModelMatch)
                        toolList.Add("模板匹配");
                    else if (isUsingAutoCircleMatch)
                        toolList.Add("自动圆匹配");

                }

                if (cobxTemplateMatchMethod.SelectedIndex == (int)currMatchMethod)
                    reloadDataOfMatchMethod(currMatchMethod);
                else
                    cobxTemplateMatchMethod.SelectedIndex = (int)currMatchMethod;


                if (matchBaseInfo == null)
                    matchBaseInfo = new MatchBaseInfo();
                lIstModelInfo.Items.Clear();
                lIstModelInfo.Items.Add(new ListViewItem(
                    new string[] { "BaseX", matchBaseInfo.BaseX.ToString("f3") }));
                lIstModelInfo.Items.Add(new ListViewItem(
                  new string[] { "BaseY", matchBaseInfo.BaseY.ToString("f3") }));
                lIstModelInfo.Items.Add(new ListViewItem(
                  new string[] { "BaseAngle", matchBaseInfo.BaseAngle.ToString("f3") }));
                if (currMatchMethod == EumMatchMethod.ShapeMatch)
                {
                    lIstModelInfo.Items.Add(new ListViewItem(
                         new string[] { "ContourLength", matchBaseInfo.ContourLength.ToString("f3") }));
                    lIstModelInfo.Items.Add(new ListViewItem(
                         new string[] { "ContourArea", matchBaseInfo.ContourArea.ToString("f3") }));
                  
                    if (templateContour == null || templateContour.Length < 1)
                    {
                        //MessageBox.Show("参数加载失败");
                        Appentxt("参数加载失败:模板为空！");
                    }

                }
                else
                {

                    if (this.modeltp .Empty()|| this.modeltp .Width<=0)
                    {
                        //MessageBox.Show("参数加载失败");
                        Appentxt("参数加载失败:模板为空！");
                    }
                }


            }
            catch (Exception er)
            { //MessageBox.Show("参数加载失败");
                Appentxt("参数加载失败:" + er.Message);
            }

            Appentxt(string.Format("当前加载模板：{0}",
                        Enum.GetName(typeof(EumModelType), currModelType)) +

                        ((currModelType == EumModelType.ProductModel_1 ||
                           currModelType == EumModelType.ProductModel_2 ||
                               currModelType == EumModelType.GluetapModel) ? "，模板基准点:" + modelOrigion : "")
                        );

        }
        //加载圆匹配参数
        void LoadCircleMatchParmas(string direFilePath)
        {
            parmaData = GeneralUse.ReadSerializationFile<HoughCircleData>(direFilePath + CirleMatchconfigPath);
            if (parmaData == null)
                parmaData = new HoughCircleData();
            else
            {
                NumMinDist.Value = (decimal)(parmaData as HoughCircleData).MinDist;
                NumParam1.Value = (decimal)(parmaData as HoughCircleData).Param1;
                NumParam2.Value = (decimal)(parmaData as HoughCircleData).Param2;
                numberMinRadius.Value = (decimal)(parmaData as HoughCircleData).MinRadius;
                numberMaxRadius.Value = (decimal)(parmaData as HoughCircleData).MaxRadius;

            }
        }
        //加载直线检测参数
        void LoadLineParmas(string direFilePath)
        {
            //直线1
            parmaData = GeneralUse.ReadSerializationFile<LinearCaliperData>(direFilePath + Line1configPath);
            if (parmaData == null)
                parmaData = new LinearCaliperData();
            NumcaliperWidth1.Value = (decimal)(parmaData as LinearCaliperData).caliperWidth;
            NumcaliperNum1.Value = (decimal)(parmaData as LinearCaliperData).caliperNum;
            NumedgeThreshold1.Value = (decimal)(parmaData as LinearCaliperData).edgeThreshold;
            string tem = Enum.GetName(typeof(EumFittingMethod), (parmaData as LinearCaliperData).fitMethod);
            CobxfitMethod1.Text = tem == "Least_square_method" ? "最小二乘法" : "随机采样一致性";
            string tem12 = Enum.GetName(typeof(EumEdgePolarity), (parmaData as LinearCaliperData).edgePolarity);
            cobxPolarity1.Text = tem12;
        
            //直线2
            parmaData = GeneralUse.ReadSerializationFile<LinearCaliperData>(direFilePath + Line2configPath);
            if (parmaData == null)
                parmaData = new LinearCaliperData();
            NumcaliperWidth2.Value = (decimal)(parmaData as LinearCaliperData).caliperWidth;
            NumcaliperNum2.Value = (decimal)(parmaData as LinearCaliperData).caliperNum;
            NumedgeThreshold2.Value = (decimal)(parmaData as LinearCaliperData).edgeThreshold;
            string tem2 = Enum.GetName(typeof(EumFittingMethod), (parmaData as LinearCaliperData).fitMethod);
            CobxfitMethod2.Text = tem == "Least_square_method" ? "最小二乘法" : "随机采样一致性";
            string tem22 = Enum.GetName(typeof(EumEdgePolarity), (parmaData as LinearCaliperData).edgePolarity);
            cobxPolarity2.Text = tem22;
        }
        //加载圆检测参数
        void LoadCirlceParmas(string direFilePath)
        {
            parmaData = GeneralUse.ReadSerializationFile<CircularCaliperData>(direFilePath + CircleconfigPath);
            if (parmaData == null)
                parmaData = new CircularCaliperData();

            NumcaliperWidth.Value = (decimal)(parmaData as CircularCaliperData).caliperWidth;
            NumcaliperNum.Value = (decimal)(parmaData as CircularCaliperData).caliperNum;
            NumedgeThreshold.Value = (decimal)(parmaData as CircularCaliperData).edgeThreshold;
            cobxCircleDir.Text= Enum.GetName(typeof(EumDirectionOfCircle), (parmaData as CircularCaliperData).circleDir);
            cobxPolarity3.Text = Enum.GetName(typeof(EumEdgePolarity), (parmaData as CircularCaliperData).edgePolarity);
           
        }
        //加载Blob检测参数
        void LoadBlobParmas(string direFilePath)
        {

            parmaData = GeneralUse.ReadSerializationFile<Blob3Data>(direFilePath + BlobconfigPath);
            if (parmaData == null)
                parmaData = new Blob3Data();
            //Blob3检测参数
            Numminthd.Value = (decimal)(parmaData as Blob3Data).edgeThreshold;
            NumareaLow.Value = (decimal)(parmaData as Blob3Data).minArea;
            NumareaHigh.Value = (decimal)(parmaData as Blob3Data).maxArea;
            cobxPolarity.Text = Enum.GetName(typeof(EumWhiteOrBlack), (parmaData as Blob3Data).eumWhiteOrBlack);

        }
        // 加载测试工具
        void LoadTestingTools(string dirFileName, EumModelType _currModelType = EumModelType.None)
        {
            if (!Directory.Exists(dirFileName))
                Directory.CreateDirectory(dirFileName);

            if (_currModelType == EumModelType.None)
            {
                string buf = GeneralUse.ReadValue("当前模板", "类型", "模板匹配类型", "ProductModel_1", dirFileName + "\\Config");
                if (buf == "CaliModel" || buf == "CalibModel")
                    buf = "CaliBoardModel";
                currModelType = (EumModelType)Enum.Parse(typeof(EumModelType), buf);
            }
            else
                currModelType = _currModelType;


            if (currModelType == EumModelType.ProductModel_1)
            {
                //定位检测工具
                LoadLineParmas(dirFileName + "\\" +
                    "modelfile\\ProductModel_1\\");
                LoadCirlceParmas(dirFileName + "\\" +
                    "modelfile\\ProductModel_1\\");
                LoadBlobParmas(dirFileName + "\\" +
                    "modelfile\\ProductModel_1\\");

                //附加工具
                CheckBoxselectID = int.Parse(GeneralUse.ReadValue("附加工具", "工具编号",
                  "附加工具类型", "-1", dirFileName + "\\" +
                  "modelfile\\ProductModel_1"));
                ExchangeSelect(CheckBoxselectID);
                if (CheckBoxselectID == 1)
                    toolList.Add("直线定位");
                else if (CheckBoxselectID == 2)
                    toolList.Add("找圆定位");
                else if (CheckBoxselectID == 3)
                    toolList.Add("Blob定位");

                //附加工具检测区域
                SearchROI1 = GeneralUse.ReadSerializationFile<List<Object>>(dirFileName + "\\" +
                      "modelfile\\ProductModel_1\\" + inspectToolPath);
                if (SearchROI1 == null)
                {
                    SearchROI1 = new List<Object>();
                    Appentxt("当前定位只使用模板(产品模板1)定位，未使用其它附件检测工具!");
                }
            }
            else if (currModelType == EumModelType.ProductModel_2)
            {

                //定位检测工具
                LoadLineParmas(dirFileName + "\\" +
                    "modelfile\\ProductModel_2\\");
                LoadCirlceParmas(dirFileName + "\\" +
                    "modelfile\\ProductModel_2\\");
                LoadBlobParmas(dirFileName + "\\" +
                    "modelfile\\ProductModel_2\\");

                //附加工具
                CheckBoxselectID = int.Parse(GeneralUse.ReadValue("附加工具", "工具编号",
                 "附加工具类型", "-1", dirFileName + "\\" +
                 "modelfile\\ProductModel_2"));
                ExchangeSelect(CheckBoxselectID);
                if (CheckBoxselectID == 1)
                    toolList.Add("直线定位");
                else if (CheckBoxselectID == 2)
                    toolList.Add("找圆定位");
                else if (CheckBoxselectID == 3)
                    toolList.Add("Blob定位");

                //附加工具检测区域
                SearchROI2 = GeneralUse.ReadSerializationFile<List<Object>>(dirFileName + "\\" +
                    "modelfile\\ProductModel_2\\" + inspectToolPath);
                if (SearchROI2 == null)
                {
                    SearchROI2 = new List<Object>();
                    Appentxt("当前定位只使用模板(产品模板2)定位，未使用其它附件检测工具!");
                }
            }
            else if (currModelType == EumModelType.CaliBoardModel)
            {
                //附加工具
                CheckBoxselectID = -1;
                ExchangeSelect(CheckBoxselectID);
                //定位检测工具
                LoadLineParmas(dirFileName + "\\" + "modelfile\\CaliBoardModel\\");
                LoadCirlceParmas(dirFileName + "\\" + "modelfile\\CaliBoardModel\\");
                LoadBlobParmas(dirFileName + "\\" + "modelfile\\CaliBoardModel\\");

            }
            else
            {
                //定位检测工具
                LoadLineParmas(dirFileName + "\\" + "modelfile\\GlueTapModel\\");
                LoadCirlceParmas(dirFileName + "\\" + "modelfile\\GlueTapModel\\");
                LoadBlobParmas(dirFileName + "\\" + "modelfile\\GlueTapModel\\");

                //附加工具
                CheckBoxselectID = int.Parse(GeneralUse.ReadValue("附加工具", "工具编号",
                  "附加工具类型", "-1", dirFileName + "\\" +
                  "modelfile\\GlueTapModel"));
                ExchangeSelect(CheckBoxselectID);
                if (CheckBoxselectID == 1)
                    toolList.Add("直线定位");
                else if (CheckBoxselectID == 2)
                    toolList.Add("找圆定位");
                else if (CheckBoxselectID == 3)
                    toolList.Add("Blob定位");
                //附加工具检测区域
                SearchROI3 = GeneralUse.ReadSerializationFile<List<Object>>(dirFileName + "\\" +
                  "modelfile\\GlueTapModel\\" + inspectToolPath);
                if (SearchROI3 == null)
                {
                    SearchROI3 = new List<Object>();
                    Appentxt("当前定位只使用模板(胶水模板)定位，未使用其它附件检测工具!");
                }
            }
        }

        #endregion

        #region---------胶水检测--------------    
        /// <summary>
        /// 开启胶水检测功能，同时加载参数
        /// </summary>
        public void OpenGlueCheckFunction()
        {
            setStyle(IsOpenAllFunctions, true);
            bool flag = LoadGlueCheckParam();
            showData();
        }
        /// <summary>
        /// 关闭胶水检测功能,同时保存参数
        /// </summary>
        public void CloseGlueCheckFunction()
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(CloseGlueCheckFunction));
            else
            {
                btnSaveParamOfGlueCheck_Click(null, null);
                setStyle(IsOpenAllFunctions, false);
            }
        }
        /// <summary>
        /// 新建检测点位
        /// </summary>
        /// <param name="posName"></param>
        public void NewCkeckPos(string posName)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action<string>(NewCkeckPos), posName);
            else
            {
                if (regionParamArrayList.Exists(t => t.currRegionName == posName))
                {
                    Appentxt("点位已存在无需新建！");
                    return;
                }
                else
                {
                    RegionParamArray regionParamArray = new RegionParamArray();
                    regionParamArray.currRegionName = posName;
                    regionParamArray.isUsePosiCorrect = chxbUsePosCorrect.Checked;

                    regionParamArray.regionParam = new RegionParam();
                    regionParamArrayList.Add(regionParamArray);

                    cobxRegonNames.Items.Add(posName);
                    cobxRegonNames.SelectedItem = posName;
                }
            }
        }

        /// <summary>
        /// 检测点位删除
        /// </summary>
        /// <param name="posName"></param>
        public void DelCheckPos(string posName)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action<string>(DelCheckPos), posName);
            else
            {
                if (regionParamArrayList.Exists(t => t.currRegionName == posName))
                {
                    int index = regionParamArrayList.FindIndex(t => t.currRegionName == posName);
                    regionParamArrayList.RemoveAt(index);
                    RegionInfoCollection.RemoveAt(index);
                    cobxRegonNames.Items.Remove(posName);

                    if (cobxRegonNames.Items.Count > 0)
                        cobxRegonNames.SelectedIndex = 0;
                }
                else
                {

                    Appentxt("点位不存在无需删除！");
                    return;
                }
            }
        }
        /// <summary>
        /// 删除所有检测点位
        /// </summary>
        public void DelAllCheckPos()
        {

            if (this.InvokeRequired)
                this.Invoke(new Action(DelAllCheckPos));
            else
            {
                regionParamArrayList.Clear();
                RegionInfoCollection.Clear();
                cobxRegonNames.Items.Clear();

                if (cobxRegonNames.Items.Count > 0)
                    cobxRegonNames.SelectedIndex = 0;
            }
        }
        /// <summary>
        /// 保存检测点位
        /// </summary>
        public void SaveCheckPos()
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(SaveCheckPos));
            else
            {
                btnSaveRegion_Click(null, null);
            }
        }
        /// <summary>
        /// 表格数据更新
        /// </summary>
        /// <param name="dg"></param>
        /// <param name="baseInfo"></param>
        void updateDataGrid(BaseInfo baseInfo)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action<BaseInfo>(updateDataGrid), baseInfo);
            else
            {
                if (baseInfo is DatagridOfRegionInfo)
                {
                    //同名则不添加
                    if (RegionInfoCollection.Count <= 0 ||
                             !RegionInfoCollection.Exists(t => t.name == (baseInfo as DatagridOfRegionInfo).name))
                    {
                        this.RegionInfoCollection.Add(baseInfo as DatagridOfRegionInfo);

                        DataGridViewRow dr = new DataGridViewRow();
                        dr.CreateCells(this.dgOfRegionInfo);//给行添加单元格  
                        (baseInfo as DatagridOfRegionInfo).dataToRow(ref dr);
                        dgOfRegionInfo.Rows.Add(dr);
                        dgOfRegionInfo.Update();
                    }
                    else if (RegionInfoCollection.Exists(t => t.name == (baseInfo as DatagridOfRegionInfo).name))
                    {
                        int index = RegionInfoCollection.FindIndex(t => t.name == (baseInfo as DatagridOfRegionInfo).name);

                        this.RegionInfoCollection[index] = (baseInfo as DatagridOfRegionInfo);

                        //DataGridViewRow dr = new DataGridViewRow();
                        //dr.CreateCells(this.dgOfRegionInfo);//给行添加单元格  
                        //(baseInfo as DatagridOfRegionInfo).dataToRow(ref dr);
                        //dgOfRegionInfo.Rows.Add(dr);
                        //dgOfRegionInfo.Update();
                    }

                }
                else
                {

                    dgOfGlueCheckInfo.DataSource = null;
                    this.GlueCheckInfoCollection.Add(baseInfo as DatagridOfGlueCheckInfo);
                    dgOfGlueCheckInfo.DataSource = this.GlueCheckInfoCollection;
                    dgOfGlueCheckInfo.Refresh();
                    glueLog.Info("胶水结果", (baseInfo as DatagridOfGlueCheckInfo).ToString());
                }
            }
        }
        /// <summary>
        /// 表格操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgOfRegionInfo_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            //删除操作
            if (e.ColumnIndex == 3)
            {
                string[] names = DatagridOfRegionInfo.dataToRegionInfo(dgOfRegionInfo.Rows[e.RowIndex]);

                //点位删除
                DelCheckPos(names[1]);

                if (dgOfRegionInfo[e.ColumnIndex, e.RowIndex].GetType() != typeof(DataGridViewButtonCell))
                    return;
                //表格信息删除
                DeleteRecord(e.RowIndex);
            }
            //check操作
            else if (e.ColumnIndex == 0)
            {
                if (dgOfRegionInfo[e.ColumnIndex, e.RowIndex].GetType() != typeof(DataGridViewCheckBoxCell))
                    return;
                //dataGridView1[e.ColumnIndex, e.RowIndex].Value = true;
                string[] names = DatagridOfRegionInfo.dataToRegionInfo(dgOfRegionInfo.Rows[e.RowIndex]);

                if (regionParamArrayList.Exists(t => t.currRegionName == names[1]))
                {
                    int index = regionParamArrayList.FindIndex(t => t.currRegionName == names[1]);

                    bool flag = (bool)(((DataGridViewCheckBoxCell)this.dgOfRegionInfo.Rows[e.RowIndex].Cells[e.ColumnIndex]).Value);

                    ((DataGridViewCheckBoxCell)this.dgOfRegionInfo.Rows[e.RowIndex].Cells[e.ColumnIndex]).Value = !flag;
                    regionParamArrayList[index].isUse =
                             (bool)(((DataGridViewCheckBoxCell)this.dgOfRegionInfo.Rows[e.RowIndex].Cells[e.ColumnIndex]).Value);
                }

            }
        }


        private void dgOfRegionInfo_DoubleClick(object sender, EventArgs e)
        {
            if (dgOfRegionInfo.SelectedRows.Count <= 0) return;
            int index = dgOfRegionInfo.SelectedRows[0].Index;
            string regionName = regionParamArrayList[index].currRegionName;
            loadRegionSetWindowsImg(regionName);

        }



        //删除记录
        void DeleteRecord(int RowIndex)
        {
            if (RowIndex < 0) return;
            dgOfRegionInfo.Rows.Remove(dgOfRegionInfo.Rows[RowIndex]);
            dgOfRegionInfo.Update();
        }
        /// <summary>
        /// 加载胶水检测项目相关参数
        /// </summary>
        private bool LoadGlueCheckParam()
        {
            string glueCheckFilePath = "配方\\" + CurrRecipeName + "\\胶水检测";

            try
            {
                if (!Directory.Exists(glueCheckFilePath))
                    Directory.CreateDirectory(glueCheckFilePath);
                regionParamArrayList = GeneralUse.ReadSerializationFile<List<RegionParamArray>>
                                                          (glueCheckFilePath + "\\区域提取参数集合");
                glueCheckParam = GeneralUse.ReadSerializationFile<GlueCheckParam>(glueCheckFilePath + "\\胶水检测参数");

                RegionInfoCollection = GeneralUse.ReadSerializationFile<List<DatagridOfRegionInfo>>
                                                                (glueCheckFilePath + "\\区域表格信息");

                return true;
            }
            catch (Exception er)
            {
                Appentxt(string.Format("胶水检测相关参数加载失败:{0}", er.Message));
                return false;
            }
        }

        /// <summary>
        /// 保存胶水检测区域参数
        /// </summary>
        private bool SaveGlueRegionParam()
        {
            string glueCheckFilePath = "配方\\" + CurrRecipeName + "\\胶水检测";

            try
            {
                if (!Directory.Exists(glueCheckFilePath))
                    Directory.CreateDirectory(glueCheckFilePath);
                GeneralUse.WriteSerializationFile<List<RegionParamArray>>
                             (glueCheckFilePath + "\\区域提取参数集合", regionParamArrayList);

                GeneralUse.WriteSerializationFile<List<DatagridOfRegionInfo>>
                                      (glueCheckFilePath + "\\区域表格信息", RegionInfoCollection);

                return true;
            }
            catch (Exception er)
            {
                Appentxt(string.Format("胶水检测区域参数保存失败:{0}", er.Message));
                return false;
            }
        }
        /// <summary>
        /// 保存胶水检测参数
        /// </summary>
        private bool SaveGlueCheckParam()
        {
            string glueCheckFilePath = "配方\\" + CurrRecipeName + "\\胶水检测";

            try
            {
                if (!Directory.Exists(glueCheckFilePath))
                    Directory.CreateDirectory(glueCheckFilePath);
                //GeneralUse.WriteSerializationFile<List<RegionParamArray>>
                //             (glueCheckFilePath + "\\区域提取参数集合", regionParamArrayList);
                GeneralUse.WriteSerializationFile<GlueCheckParam>
                               (glueCheckFilePath + "\\胶水检测参数", glueCheckParam);

                //GeneralUse.WriteSerializationFile<List<DatagridOfRegionInfo>>
                //                      (glueCheckFilePath + "\\区域表格信息", RegionInfoCollection);

                return true;
            }
            catch (Exception er)
            {
                Appentxt(string.Format("胶水检测参数保存失败:{0}", er.Message));
                return false;
            }
        }

        /// <summary>
        /// 保存区域设定时的窗体图片，图片名称需附上.png
        /// </summary>
        private void saveRegionSetWindowsImg(string regionName, Point2f[] PolygonRegion)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action<string, Point2f[]>(saveRegionSetWindowsImg), regionName, PolygonRegion);
            else
            {

                string glueCheckFilePath = "配方\\" + CurrRecipeName + "\\胶水检测\\检测区域设定窗体图";
                if (!Directory.Exists(glueCheckFilePath))
                    Directory.CreateDirectory(glueCheckFilePath);
                GrabImg.DrawPolygon(PolygonRegion);
                Cv2.ImWrite(glueCheckFilePath + "\\" + regionName + ".png", GrabImg);

            }
        }
        /// <summary>
        /// 加载区域设定的窗体图片
        /// </summary>
        /// <param name="regionName"></param>
        public void loadRegionSetWindowsImg(string regionName)
        {
            string glueCheckFilePath = "配方\\" + CurrRecipeName + "\\胶水检测\\检测区域设定窗体图\\";
            if (File.Exists(glueCheckFilePath + regionName + ".png"))
            {
                Mat readImg = Cv2.ImRead(glueCheckFilePath + regionName + ".png", ImreadModes.AnyColor);

                currvisiontool.dispImage(readImg);

            }
            else
            {
                Appentxt(string.Format("路径：{0}下不存在区域设定的图片：{1}",
                   "配方\\" + CurrRecipeName + "\\胶水检测\\检测区域设定窗体图",
                   regionName + ".png"));
            }
        }
        /// <summary>
        /// 显示数据
        /// </summary>
        void showData()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(showData));
            }
            else
            {
                cobxRegonNames.Items.Clear();
                foreach (var s in regionParamArrayList)
                    cobxRegonNames.Items.Add(s.currRegionName);
                //自动新增一个
                //if (cobxRegonNames.Items.Count <= 0)
                //    btnAddTestPos_Click(null, null);
                //else
                if (cobxRegonNames.Items.Count > 0)
                    cobxRegonNames.SelectedIndex = 0;

                {
                    numMinGray.Value = glueCheckParam.MinGrayOfThreshold;
                    numMaxGray.Value = glueCheckParam.MaxGrayOfThreshold;
                    numScaleGrayDown.Value = glueCheckParam.scaleGrayMin;
                    numScaleGrayUp.Value = glueCheckParam.scaleGrayMax;
                    numMinArea.Value = glueCheckParam.MinAreaOfGlue;
                    numMaxArea.Value = glueCheckParam.MaxAreaOfGlue;
                }
                //初始化区域显示表格
                foreach (var s in RegionInfoCollection)
                {
                    DataGridViewRow dr = new DataGridViewRow();
                    dr.CreateCells(this.dgOfRegionInfo);//给行添加单元格            
                    s.dataToRow(ref dr);
                    dgOfRegionInfo.Rows.Add(dr);
                }
                dgOfRegionInfo.Update();
                dgOfRegionInfo.MultiSelect = false;
                if (dgOfRegionInfo.Rows.Count > 0)
                    dgOfRegionInfo.Rows[0].Selected = true;
            }
        }
        private void btnSaveRegion_Click(object sender, EventArgs e)
        {
            //有则更新
            if (regionParamArrayList.Exists(t => t.currRegionName == cobxRegonNames.Text))
            {
                int index = regionParamArrayList.FindIndex(t => t.currRegionName == cobxRegonNames.Text);
                regionParamArrayList[index].isUsePosiCorrect = chxbUsePosCorrect.Checked;
                regionParamArrayList[index].regionSetBaseOnAutoExtract = chxbUseAutoExtra.Checked;
                regionParamArrayList[index].regionParam.updateParam(int.Parse(cobxMaskWidth.Text),
                   int.Parse(cobxMaskHeight.Text), (byte)numRegionMinGray.Value,
                    (byte)numRegionMaxGray.Value, (int)numRegionMinArea.Value,
                    (int)numRegionMaxArea.Value);
                if (currGlueSetRegion != null && currGlueSetRegion.Count > 0)
                    regionParamArrayList[index].DetectionROI = new PolygonF(currGlueSetRegion);

            }
            //无则新增
            else
            {
                RegionParamArray regionParamArray = new RegionParamArray();
                regionParamArray.currRegionName = cobxRegonNames.Text;
                regionParamArray.isUsePosiCorrect = chxbUsePosCorrect.Checked;
                regionParamArray.regionSetBaseOnAutoExtract = chxbUseAutoExtra.Checked;
                regionParamArray.regionParam = new RegionParam(int.Parse(cobxMaskWidth.Text),
                    int.Parse(cobxMaskHeight.Text), (byte)numRegionMinGray.Value,
                    (byte)numRegionMaxGray.Value, (int)numRegionMinArea.Value,
                    (int)numRegionMaxArea.Value);
                if (currGlueSetRegion != null && currGlueSetRegion.Count > 0)
                    regionParamArray.DetectionROI = new PolygonF(currGlueSetRegion);

                regionParamArrayList.Add(regionParamArray);
            }
            foreach (var s in regionParamArrayList)
                updateDataGrid(new DatagridOfRegionInfo(s.currRegionName, s.isUse));
            Point2f[] temRegion = new Point2f[currGlueSetRegion.Count];
            for (int i = 0; i < currGlueSetRegion.Count; i++)
                temRegion[i] = new Point2f(currGlueSetRegion[i].X, currGlueSetRegion[i].Y);
            saveRegionSetWindowsImg(cobxRegonNames.Text, temRegion);
            if (!SaveGlueRegionParam())
                MessageBox.Show("区域参数保存失败！");
        }

        private void btnSaveParamOfGlueCheck_Click(object sender, EventArgs e)
        {
            glueCheckParam.MinGrayOfThreshold = (byte)numMinGray.Value;
            glueCheckParam.MaxGrayOfThreshold = (byte)numMaxGray.Value;
            glueCheckParam.scaleGrayMin = (byte)numScaleGrayDown.Value;
            glueCheckParam.scaleGrayMax = (byte)numScaleGrayUp.Value;
            glueCheckParam.MinAreaOfGlue = (int)numMinArea.Value;
            glueCheckParam.MaxAreaOfGlue = (int)numMaxArea.Value;

            if (!SaveGlueCheckParam())
                MessageBox.Show("检测参数保存失败！");
        }
        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            frmAgreement2 fa = new frmAgreement2();
            //fa.Owner = this;
            fa.Show();
        }
        //手动检测胶水
        private void btnManualTest_Click(object sender, EventArgs e)
        {
            if (GrabImg == null || GrabImg.Empty() || GrabImg.Width <= 0)
            {
                MessageBox.Show("图像为空");
                return;
            }
            //point[]转cvpoint[]
            glueCheckParam.detectROI = pointList2cvpointList(currGlueSetRegion);
            ResultOfGlueCheck resultOfGlueCheck = GlueCheckTask.action(GrabImg, glueCheckParam);
            currvisiontool.dispImage(GrabImg);
            if (!resultOfGlueCheck.runFlag)
            {
                Appentxt(string.Format("胶水检测失败：{0}", resultOfGlueCheck.errInfo));
                currvisiontool.DrawText(new TextEx("胶水检测失败")
                   { x = 1000, y = 100, brush = new SolidBrush(Color.Red) });

                return;
            }

            Mat dst = new Mat();
            Cv2.CopyTo(GrabImg, dst);
            Cv2.CvtColor(dst, dst, ColorConversionCodes.GRAY2BGR);

            int num = resultOfGlueCheck.region.Count;
            for (int i = 0; i < num; i++)
                dst.DrawContours(resultOfGlueCheck.region, i, Scalar.Red);

            currvisiontool.dispImage(dst);
            //胶水检测区域
            currvisiontool.DrawRegion(new RegionEx(new PolygonF(currGlueSetRegion), Color.Green, 16));
            currvisiontool.AddRegionBuffer(new RegionEx(new PolygonF(currGlueSetRegion), Color.Green, 16));
            if (resultOfGlueCheck.n_count > 0)
            {
                currvisiontool.DrawText(new TextEx(string.Format("胶水NG！数量{0}", resultOfGlueCheck.n_count))
                { x = 1000, y = 100, brush = new SolidBrush(Color.Red) });
                currvisiontool.AddTextBuffer(new TextEx(string.Format("胶水NG！数量{0}", resultOfGlueCheck.n_count))
                { x = 1000, y = 100, brush = new SolidBrush(Color.Red) });

            }
            else
            {
                currvisiontool.DrawText(new TextEx("胶水OK！",
                      new Font("宋体", 16f), new SolidBrush(Color.Green), 100, 100));
                currvisiontool.AddTextBuffer(new TextEx("胶水OK！",
                     new Font("宋体", 16f), new SolidBrush(Color.Green), 100, 100));

            }

            updateDataGrid(new DatagridOfGlueCheckInfo(DateTime.Now,
               resultOfGlueCheck.n_count <= 0, resultOfGlueCheck.n_count, resultID));
            resultID++;
        }

        //胶水检测区域重设定
        private void btnRegionMaskSet_Click(object sender, EventArgs e)
        {
            if (GrabImg == null || GrabImg.Empty() || GrabImg.Width <= 0)
            {
                MessageBox.Show("图像为空！", "Information",
                         MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (chxbUseAutoExtra.Checked)
            {
                if (currGlueSetRegion == null || currGlueSetRegion.Count <= 0)
                {
                    MessageBox.Show("未提取！", "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                frmGlueRegionMaking f = new frmGlueRegionMaking(GrabImg, currGlueSetRegion);
                f.SetModelMaskROIHandle += new EventHandler(SetGlueRegionMaskEvent);
                f.Show();
            }
            else
            {
                frmGlueRegionMaking f = new frmGlueRegionMaking(GrabImg);
                f.SetModelMaskROIHandle += new EventHandler(SetGlueRegionMaskEvent);
                f.Show();
            }

        }
        /// <summary>
        /// 区域重设定返回事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SetGlueRegionMaskEvent(object sender, EventArgs e)
        {
            currGlueSetRegion = sender as List<Point>;
            if (currGlueSetRegion == null || currGlueSetRegion.Count <= 0)
            {
                Appentxt("区域设置无效，请重新设置！");
                MessageBox.Show("区域设置无效，请重新设置！");
                return;
            }
            else
            {
                currvisiontool.clearOverlay();
                currvisiontool.dispImage(GrabImg);
                currvisiontool.DrawRegion(new RegionEx(new PolygonF(currGlueSetRegion), Color.Green, 16));
                currvisiontool.AddRegionBuffer(new RegionEx(new PolygonF(currGlueSetRegion), Color.Green, 16));

            }
        }

        //区域自提取
        private void btnAutoExtract_Click(object sender, EventArgs e)
        {
            int index = regionParamArrayList.FindIndex(t => t.currRegionName == cobxRegonNames.Text);

            Mat temimg = new Mat();

            Cv2.CopyTo(GrabImg, temimg);

            RegionParam regionParam = new RegionParam(int.Parse(cobxMaskWidth.Text),
                    int.Parse(cobxMaskHeight.Text), (byte)numRegionMinGray.Value,
                    (byte)numRegionMaxGray.Value, (int)numRegionMinArea.Value,
                    (int)numRegionMaxArea.Value);

            ResultOfRegionExtra resultOfRegionExtra = RegionSet.action(temimg, regionParam);
            //暂时先默认第一个为需要的提取区域
            if (resultOfRegionExtra.region.Count > 0)
                currGlueSetRegion = cvpointList2pointList(resultOfRegionExtra.region[0].ToList<CVPoint>());
            if (!resultOfRegionExtra.runFlag)
            {
                Appentxt(resultOfRegionExtra.errInfo);
                return;
            }
            Mat dst = new Mat();
            Cv2.CopyTo(GrabImg, dst);

            //轮廓数量
            int num = resultOfRegionExtra.region.Count;
            for (int i = 0; i < num; i++)
            {
                CVRRect cVRRect = Cv2.MinAreaRect(resultOfRegionExtra.region[i]);
                double area = Cv2.ContourArea(resultOfRegionExtra.region[i]);
                Cv2.DrawContours(dst, resultOfRegionExtra.region, i, Scalar.Green);
                dst.drawCross(new CVPoint(cVRRect.Center.X, cVRRect.Center.Y), Scalar.Red, 20, 2);

                Cv2.PutText(dst, area.ToString("f3"), new CVPoint(cVRRect.Center.X,
                               cVRRect.Center.Y), HersheyFonts.Italic, 0.5,
                                Scalar.Red, 1, LineTypes.AntiAlias, false);//绘制文本

                //currvisiontool.DrawText(new TextEx(area.ToString("f3"),new Font("宋体",16f)
                //    ,new SolidBrush (Color.Red), cVRRect.Center.X, cVRRect.Center.Y));
                //currvisiontool.AddTextBuffer(new TextEx(area.ToString("f3"), new Font("宋体", 16f)
                //    , new SolidBrush(Color.Red), cVRRect.Center.X, cVRRect.Center.Y));
            }
            currvisiontool.dispImage(dst);
            temimg.Dispose();
        }
        /// <summary>
        /// 新增测试点位
        /// </summary>
        int addSign = 1;
        private void btnAddTestPos_Click(object sender, EventArgs e)
        {
            int count = cobxRegonNames.Items.Count;
            string buf = "";

            if (count > 0)
            {
                ++addSign;
                buf = cobxRegonNames.Items[count - 1].ToString() + addSign.ToString();
            }
            else
            {
                buf = "Posi" + addSign.ToString();
                addSign++;
            }

            RegionParamArray regionParamArray = new RegionParamArray();
            regionParamArray.currRegionName = buf;
            regionParamArray.isUsePosiCorrect = chxbUsePosCorrect.Checked;
            regionParamArray.regionSetBaseOnAutoExtract = chxbUseAutoExtra.Checked;
            //regionParamArray.regionParam = new RegionParam((int)cobxMaskWidth.SelectedValue,
            //    (int)cobxMaskHeight.SelectedValue, (byte)numRegionMinGray.Value,
            //    (byte)numRegionMaxGray.Value, (int)numRegionMinArea.Value,
            //    (int)numRegionMaxArea.Value);
            regionParamArray.regionParam = new RegionParam();
            regionParamArrayList.Add(regionParamArray);

            cobxRegonNames.Items.Add(buf);
            cobxRegonNames.SelectedItem = buf;
        }
        /// <summary>
        /// 区域选择并重载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cobxRegonNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            string name = cobxRegonNames.Text;
            if (!regionParamArrayList.Exists(t => t.currRegionName == name))
            {
                MessageBox.Show("区域参数文件不存在！");
            }
            else
            {
                int index = regionParamArrayList.FindIndex(t => t.currRegionName == cobxRegonNames.Text);
                if (index >= 0)
                {
                    chxbUsePosCorrect.Checked = regionParamArrayList[index].isUsePosiCorrect;
                    chxbUseAutoExtra.Checked = regionParamArrayList[index].regionSetBaseOnAutoExtract;
                    cobxMaskWidth.Text = regionParamArrayList[index].regionParam.MaskWidth.ToString();
                    cobxMaskHeight.Text = regionParamArrayList[index].regionParam.MaskHeight.ToString();

                    numRegionMinGray.Value = regionParamArrayList[index].regionParam.MinGrayOfThreshold;
                    numRegionMaxGray.Value = regionParamArrayList[index].regionParam.MaxGrayOfThreshold;
                    numRegionMinArea.Value = regionParamArrayList[index].regionParam.MinAreaOfGlue;
                    numRegionMaxArea.Value = regionParamArrayList[index].regionParam.MaxAreaOfGlue;
                    currGlueSetRegion = regionParamArrayList[index].DetectionROI.Points;
                    glueCheckParam.detectROI = pointList2cvpointList(currGlueSetRegion);

                    if (dgOfRegionInfo.Rows.Count > index)
                        dgOfRegionInfo.Rows[index].Selected = true;
                }
            }

        }


        #endregion

        #region------------九点标定-----------
        private void btnConvert_Click(object sender, EventArgs e)
        {
            coorditionConvert();
        }
        /// <summary>
        /// 坐标系配方设置画面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCoordiRecipe_Click(object sender, EventArgs e)
        {
            mp.Show();
        }

        string saveToUsePath = AppDomain.CurrentDomain.BaseDirectory + "配方\\default";

        //配方重载
        void RecipeSaveEvent(object sender, EventArgs e)
        {

            if (sender != null)
                saveToUsePath = sender.ToString();
            else
            {
                string _recipeName = GeneralUse.ReadValue("配方", "使用配方名称", "config", "");
                if (_recipeName == string.Empty)
                {
                    Appentxt("配方名称解析错误！");
                    return;
                }
                saveToUsePath = AppDomain.CurrentDomain.BaseDirectory + "配方\\" + _recipeName;
            }


            string[] temarray = saveToUsePath.Split('\\');
            string recipeName = temarray[temarray.Length - 1];
            if (recipeName == "")
                recipeName = "default";
            if (!Directory.Exists("配方\\" + recipeName))
            {
                Directory.CreateDirectory("配方\\" + recipeName);
                Appentxt(string.Format("配方文件加载失败，当前配方:{0}不存在，请确认！", recipeName));
                MessageBox.Show(string.Format("配方文件加载失败，当前配方:{0}不存在，请确认！", recipeName));
                return;
            }
            Appentxt(string.Format("当前使用配方为：{0}", recipeName));
            CurrRecipeName = recipeName;

            //9点标定文件
            LoadConfigOfNightPCali("配方\\" + recipeName);
            //旋转中心文件
            LoadConfigOfRorateParma("配方\\" + recipeName);
            //检测文件文件
            toolList.Clear();

            this.Invoke(new Action(() =>
            {
                LoadPreTools("配方\\" + recipeName);
                ModelMatchLoadParma("配方\\" + recipeName);
                LoadTestingTools("配方\\" + recipeName);

                EnableDetectionControl();
                CamExposure = (int)float.Parse(GeneralUse.ReadValue("相机", "曝光", "config", "10000", 
                    "配方\\" + recipeName + "\\modelfile\\" + Enum.GetName(typeof(EumModelType), currModelType)));
                CamExposureBar.Value = CamExposure;
                lblCamExposure.Text = CamExposure.ToString();
                CamGain = (int)float.Parse(GeneralUse.ReadValue("相机", "增益", "config", "0", 
                    "配方\\" + recipeName + "\\modelfile\\" + Enum.GetName(typeof(EumModelType), currModelType)));
                CamGainBar.Value = CamGain;
                lblCamGain.Text = CamGain.ToString();

                if (cobxModelType.Text != Enum.GetName(typeof(EumModelType), currModelType))
                    cobxModelType.SelectedIndex = (int)currModelType;
              
                listViewFlow.Items.Clear();
                for (int i = 0; i < toolList.Count; i++)
                    listViewFlow.Items.Add(new ListViewItem(new string[] { i.ToString(), toolList[i] }));

                currvisiontool.clearAll();
                currvisiontool.dispImage(GrabImg);
            }));
        }

        //检测相关控件使能
        void EnableDetectionControl()
        {
            lock (locker1)
            {
                if (currModelType == EumModelType.ProductModel_1
               || currModelType == EumModelType.ProductModel_2)
                {
                    chxbPretreatmentTool.Enabled = true;
                    chxbLinesIntersect.Enabled = true;
                    cobxLine1or2.Enabled = true;
                    LinesIntersectPanel.Enabled = true;
                    chxbFindCircle.Enabled = true;
                    FindCirclePanel.Enabled = true;
                    chxbBlobCentre.Enabled = true;
                    BlobCentrePanel.Enabled = true;
                }
                else if (currModelType == EumModelType.GluetapModel)
                {
                    chxbPretreatmentTool.Enabled = false;
                    chxbPretreatmentTool.Checked = false;
                    PretreatmentToolPanel.Enabled = false;
                    chxbLinesIntersect.Enabled = false;
                    cobxLine1or2.Enabled = false;
                    chxbLinesIntersect.Checked = false;
                    LinesIntersectPanel.Enabled = false;
                    chxbFindCircle.Enabled = false;
                    chxbFindCircle.Checked = false;
                    FindCirclePanel.Enabled = false;
                    chxbBlobCentre.Enabled = true;
                    BlobCentrePanel.Enabled = true;
                }
                else
                {
                    chxbPretreatmentTool.Enabled = false;
                    chxbPretreatmentTool.Checked = false;
                    PretreatmentToolPanel.Enabled = false;
                    chxbLinesIntersect.Enabled = false;
                    cobxLine1or2.Enabled = false;
                    chxbLinesIntersect.Checked = false;
                    LinesIntersectPanel.Enabled = false;
                    chxbFindCircle.Enabled = false;
                    chxbFindCircle.Checked = false;
                    FindCirclePanel.Enabled = false;
                    chxbBlobCentre.Enabled = false;
                    chxbBlobCentre.Checked = false;
                    BlobCentrePanel.Enabled = false;

                }
                if (isUsingAutoCircleMatch)
                {
                    chxbPretreatmentTool.Enabled = true;
                    PretreatmentToolPanel.Enabled = true;

                    chxbLinesIntersect.Enabled = false;
                    cobxLine1or2.Enabled = false;
                    chxbLinesIntersect.Checked = false;
                    LinesIntersectPanel.Enabled = false;

                    chxbFindCircle.Enabled = false;
                    chxbFindCircle.Checked = false;
                    FindCirclePanel.Enabled = false;

                    chxbBlobCentre.Enabled = false;
                    chxbBlobCentre.Checked = false;
                    BlobCentrePanel.Enabled = false;

                }

            }
        }
        //9点标定，Mark点像素坐标获取
        private void btnGetPixelPoint_Click(object sender, EventArgs e)
        {
            if (!CurrCam.IsAlive)
            {
                MessageBox.Show("相机未连接!");
                return;
            }
            workstatus = EunmcurrCamWorkStatus.NinePointcLocation;
            OneGrab();
        }
        //新增像素坐标
        private void btnNewPixelPoint_Click(object sender, EventArgs e)
        {
            i++;
            string[] temarray = new string[3] { i.ToString(), txbpixelX.Text, txbpixelY.Text };
            SetValueToListItem(listViewPixel, temarray);
            txbpixelX.Clear();
            txbpixelY.Clear();
        }
        //修改像素坐标点
        private void btnModifyPixelPoint_Click(object sender, EventArgs e)
        {
            if (listViewPixel.Items.Count <= 0 || listViewPixel.SelectedItems == null)
                return;
            if (MessageBox.Show("确认修改？", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                int index = listViewPixel.SelectedIndices[0];
                listViewPixel.Items[index].SubItems[1].Text = txbpixelX.Text.Trim();
                listViewPixel.Items[index].SubItems[2].Text = txbpixelY.Text.Trim();
            }
        }
        //删除像素坐标点
        private void btnDeletePixelPoint_Click(object sender, EventArgs e)
        {
            if (listViewPixel.Items.Count <= 0 || listViewPixel.SelectedItems.Count == 0)
                return;
            if (MessageBox.Show("确认删除？", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                int index = listViewPixel.SelectedIndices[0];
                listViewPixel.Items.RemoveAt(index);

            }
        }
        //新增机器人坐标点
        private void BtnNewRobotPoint_Click(object sender, EventArgs e)
        {
            j++;
            string[] temarray2 = new string[3] { j.ToString(), txbrobotX.Text, txbrobotY.Text };
            SetValueToListItem(listViewRobot, temarray2);
            txbrobotX.Clear();
            txbrobotY.Clear();
        }
        //修改机器人坐标点
        private void btnModifyRobotPoint_Click(object sender, EventArgs e)
        {
            if (listViewRobot.Items.Count <= 0 || listViewRobot.SelectedItems == null)
                return;
            if (MessageBox.Show("确认修改？", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                int index = listViewRobot.SelectedIndices[0];
                listViewRobot.Items[index].SubItems[1].Text = txbrobotX.Text.Trim();
                listViewRobot.Items[index].SubItems[2].Text = txbrobotY.Text.Trim();
            }
        }
        //删除机器人坐标点
        private void btnDeleteRobotPoint_Click(object sender, EventArgs e)
        {
            if (listViewRobot.Items.Count <= 0 || listViewRobot.SelectedItems.Count == 0)
                return;
            if (MessageBox.Show("确认删除？", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                int index = listViewRobot.SelectedIndices[0];
                listViewRobot.Items.RemoveAt(index);

            }
        }
        //坐标系转换，矩阵计算
        void coorditionConvert()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(coorditionConvert));
            }
            else
            {
                if (listViewPixel.Items.Count != 9 || listViewRobot.Items.Count != 9)
                {
                    MessageBox.Show("点位坐标数据不足9条，请确认!");
                    return;
                }
                pixelList.Clear();
                for (int i = 0; i < 9; i++)
                {
                    pixelList.Add(new Point2d(
                                 double.Parse(listViewPixel.Items[i].SubItems[1].Text),
                               double.Parse(listViewPixel.Items[i].SubItems[2].Text)
                                 ));
                }
                robotList.Clear();
                for (int i = 0; i < 9; i++)
                {
                    robotList.Add(new Point2d(
                                 double.Parse(listViewRobot.Items[i].SubItems[1].Text),
                               double.Parse(listViewRobot.Items[i].SubItems[2].Text)
                                 ));
                }
                Hom_mat2d = CalibrationTool.VectorToHomMat2d(pixelList, robotList);
                Hom_mat2d_inv = CalibrationTool.VectorToHomMat2d(robotList, pixelList);

                double[] Coefficient = CalibrationTool.GetMatrixCoefficient(Hom_mat2d);


                txbSx.Text = Coefficient[1].ToString("f5");
                txbSy.Text = Coefficient[3].ToString("f5");
                txbPhi.Text = Coefficient[0].ToString("f5");
                txbTheta.Text = Coefficient[4].ToString("f5");
                txbTx.Text = Coefficient[2].ToString("f5");
                txbTy.Text = Coefficient[5].ToString("f5");
                double[] rms = CalibrationTool.calRMS(Hom_mat2d, pixelList, robotList);
                txbXRms.Text = rms[0].ToString("f3");
                txbYRms.Text = rms[1].ToString("f3");

            }
        }

        //根据标定矩阵关系，由像素坐标点转换成机器人坐标点
        private void picConvertPixelToRobot_Click(object sender, EventArgs e)
        {
            Point2d robotP = CalibrationTool.AffineTransPoint2d(Hom_mat2d,
                new Point2d(double.Parse(txbMarkPixelX.Text),
                double.Parse(txbMarkPixelY.Text)));

            txbMarkRobotX.Text = robotP.X.ToString("f3");
            txbMarkRobotY.Text = robotP.Y.ToString("f3");
        }

        //根据标定矩阵关系，由机器人坐标点转换成像素坐标点
        private void picConvertRobotToPixel_Click(object sender, EventArgs e)
        {
            Point2d pixelP = CalibrationTool.AffineTransPoint2d(Hom_mat2d_inv,
              new Point2d(double.Parse(txbMarkRobotX.Text),
              double.Parse(txbMarkRobotY.Text)));
            txbMarkPixelX.Text = pixelP.X.ToString("f3");
            txbMarkPixelY.Text = pixelP.Y.ToString("f3");

        }

        //control数据显示
        void showData(pixelPointDataClass pixeld1, robotPointDataClass robotd2,
                     converCoorditionDataClass converd3)
        {
            listViewPixel.Items.Clear();
            listViewRobot.Items.Clear();
            i = 0; j = 0;
            if (pixeld1 != null)
                foreach (var s in pixeld1.ListPoint)
                {
                    i++;
                    ListViewItem tem = new ListViewItem(new string[3] { i.ToString(), s.X.ToString(), s.Y.ToString() });
                    listViewPixel.Items.Add(tem);
                }

            if (robotd2 != null)
                foreach (var s in robotd2.ListPoint)
                {
                    j++;
                    ListViewItem tem = new ListViewItem(new string[3] { j.ToString(), s.X.ToString(), s.Y.ToString() });
                    listViewRobot.Items.Add(tem);
                }


            txbSx.Text = converd3.Sx.ToString();

            txbSy.Text = converd3.Sy.ToString();

            txbPhi.Text = converd3.Phi.ToString();

            txbTheta.Text = converd3.Theta.ToString();

            txbTx.Text = converd3.Tx.ToString();

            txbTy.Text = converd3.Ty.ToString();

            txbXRms.Text = converd3.XRms.ToString();

            txbYRms.Text = converd3.YRms.ToString();
        }
        //control数据保存
        void setData(ref pixelPointDataClass pixeld1, ref robotPointDataClass robotd2, ref converCoorditionDataClass converd3)

        {
            if (listViewPixel.Items.Count != 9 || listViewRobot.Items.Count != 9)
            {
                MessageBox.Show("点位坐标数据不足9条，请确认!");
                return;
            }
            pixeld1.ListPoint.Clear();
            foreach (ListViewItem s in listViewPixel.Items)
                pixeld1.ListPoint.Add(new PointF(float.Parse(s.SubItems[1].Text),
                    float.Parse(s.SubItems[2].Text)));

            robotd2.ListPoint.Clear();
            foreach (ListViewItem s in listViewRobot.Items)
                robotd2.ListPoint.Add(new PointF(float.Parse(s.SubItems[1].Text),
                     float.Parse(s.SubItems[2].Text)));

            if (!string.IsNullOrEmpty(txbSx.Text))
            {
                if (checkValueNumber(txbSx.Text))
                    converd3.Sx = double.Parse(txbSx.Text);
                if (checkValueNumber(txbSy.Text))
                    converd3.Sy = double.Parse(txbSy.Text);
                if (checkValueNumber(txbPhi.Text))
                    converd3.Phi = double.Parse(txbPhi.Text);
                if (checkValueNumber(txbTheta.Text))
                    converd3.Theta = double.Parse(txbTheta.Text);
                if (checkValueNumber(txbTx.Text))
                    converd3.Tx = double.Parse(txbTx.Text);
                if (checkValueNumber(txbTx.Text))
                    converd3.Ty = double.Parse(txbTy.Text);
                if (checkValueNumber(txbXRms.Text))
                    converd3.XRms = double.Parse(txbXRms.Text);
                if (checkValueNumber(txbYRms.Text))
                    converd3.YRms = double.Parse(txbYRms.Text);
            }

        }

        //9点标定配置文件保存
        public void SaveCofig(string dircFileName)
        {
            try
            {

                if (!Directory.Exists($"{dircFileName}\\Calib"))

                    Directory.CreateDirectory($"{dircFileName}\\Calib");

                GeneralUse.WriteSerializationFile<pixelPointDataClass>(dircFileName + "\\Calib\\pixelPointData", d_pixelPointDataClass);
                GeneralUse.WriteSerializationFile<robotPointDataClass>(dircFileName + "\\Calib\\robotPointData", d_robotPointDataClass);
                GeneralUse.WriteSerializationFile<converCoorditionDataClass>(dircFileName + "\\Calib\\converCoorditionData", d_converCoorditionDataClass);

                if (Hom_mat2d != null && Hom_mat2d.Width > 0)
                {
                    string templatePath = dircFileName + "\\cv_HomMat2D";
                    MatDataWriteRead.WriteMatri(Hom_mat2d, templatePath, "mainNode");
                }
                if (Hom_mat2d_inv != null && Hom_mat2d_inv.Width > 0)
                {
                    string templatePath = dircFileName + "\\cv_HomMat2D_inv";
                    MatDataWriteRead.WriteMatri(Hom_mat2d_inv, templatePath, "mainNodeinv");
                }
                
            }
            catch (Exception er)
            {
                MessageBox.Show(er.Message);

            }
        }
        public void LoadConfigOfNightPCali(string dirFileName)
        {
            if (!Directory.Exists(dirFileName))
                Directory.CreateDirectory(dirFileName);
            try
            {

                d_pixelPointDataClass = GeneralUse.ReadSerializationFile<pixelPointDataClass>(dirFileName + "\\Calib\\pixelPointData");
                if (d_pixelPointDataClass == null)
                    d_pixelPointDataClass = new pixelPointDataClass();

                d_robotPointDataClass = GeneralUse.ReadSerializationFile<robotPointDataClass>(dirFileName + "\\Calib\\robotPointData");
                if (d_robotPointDataClass == null)
                    d_robotPointDataClass = new robotPointDataClass();

                d_converCoorditionDataClass = GeneralUse.ReadSerializationFile<converCoorditionDataClass>(dirFileName + "\\Calib\\converCoorditionData");
                if (d_converCoorditionDataClass == null)
                    d_converCoorditionDataClass = new converCoorditionDataClass();


                if (File.Exists(dirFileName + "\\" + "cv_HomMat2D"))
                    Hom_mat2d = MatDataWriteRead.ReadMatri(dirFileName + "\\" + "cv_HomMat2D", "mainNode");
                else
                    MessageBox.Show("坐标系文件不存在，请确认！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (File.Exists(dirFileName + "\\" + "cv_HomMat2D_inv"))
                    Hom_mat2d_inv = MatDataWriteRead.ReadMatri(dirFileName + "\\" + "cv_HomMat2D_inv", "mainNodeinv");
                else
                    MessageBox.Show("坐标系文件不存在，请确认！", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            }
            catch (Exception er)
            { MessageBox.Show(er.Message); }
            finally
            {
                showData(d_pixelPointDataClass, d_robotPointDataClass,
              d_converCoorditionDataClass);
            }

        }

        private void 清空ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listViewPixel.Items.Clear();
            i = 0;
        }
        private void 清空ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            listViewRobot.Items.Clear();
            j = 0;
        }
        //自动同步坐标系文件
        bool isAutoAutoCoorSys = false;
        private void chxbAutoCoorSys_CheckedChanged(object sender, EventArgs e)
        {
            isAutoAutoCoorSys = chxbAutoCoorSys.Checked;
            GeneralUse.WriteValue("坐标系", "同步", isAutoAutoCoorSys.ToString(), "config");
        }

        /// <summary>
        /// 自动同步更新9点+旋转中心标定文件
        /// </summary>
        void AutoUpdateCalibFile()
        {
            string _path = AppDomain.CurrentDomain.BaseDirectory + "配方";
            DirectoryInfo di = new DirectoryInfo(_path);
            //查找除去当前配方名称之外剩余的配方名称
            List<DirectoryInfo> directoryInfos1 = di.GetDirectories().ToList()
                              .FindAll(t => t.Name != CurrRecipeName);
            foreach (var s in directoryInfos1)
            {
                CopyFolder(_path + "\\" + CurrRecipeName + "\\Calib", s.FullName + "\\Calib");
                if (File.Exists(_path + "\\" + CurrRecipeName + "\\cv_HomMat2D"))
                    File.Copy(_path + "\\" + CurrRecipeName + "\\cv_HomMat2D",
                         s.FullName + "\\cv_HomMat2D", true);//复制文件
                if (File.Exists(_path + "\\" + CurrRecipeName + "\\cv_HomMat2D_inv"))
                    File.Copy(_path + "\\" + CurrRecipeName + "\\cv_HomMat2D_inv",
                         s.FullName + "\\cv_HomMat2D_inv", true);//复制文件
            }
        }
        /// <summary>
        /// 9点标定数据保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BtnSaveParmasOfNightPoints_click(object sender, EventArgs e)
        {
            try
            {  //九点标定关系参数保存
                setData(ref d_pixelPointDataClass, ref d_robotPointDataClass,
                          ref d_converCoorditionDataClass);
                SaveCofig("配方\\" + CurrRecipeName);
                SaveCaliParmaHandleOfNightPoint?.Invoke("配方\\" + CurrRecipeName + "\\cv_HomMat2D", null);
                if (isAutoAutoCoorSys)
                    AutoUpdateCalibFile();
            }
            catch (Exception er)
            {
                MessageBox.Show("参数保存失败！" + er.Message);
            }

        }

        #endregion

        #region------------旋转中心-----------
        private void chxbAutoCoorSys_CheckedChanged(object sender, bool value)
        {
            isAutoAutoCoorSys = chxbAutoCoorSys.Checked;
            GeneralUse.WriteValue("坐标系", "同步", isAutoAutoCoorSys.ToString(), "config");
        }
        //旋转标定，Mark点像素坐标获取
        private void btnGetRotataPixel_Click(object sender, EventArgs e)
        {
            if (!CurrCam.IsAlive)
            {
                MessageBox.Show("相机未连接!");
                return;
            }
            workstatus = EunmcurrCamWorkStatus.RotatoLocation;
            OneGrab();

        }
        //计算旋转中心
        private void btnCaculateRorateCenter_Click(object sender, EventArgs e)
        {
            CaculateMultorRorateCenter();
        }

        private void btnSaveRotataPixel_Click(object sender, EventArgs e)
        {
            k++;
            string[] temarray = new string[3] { k.ToString(), txbRotataPixelX.Text, txbRotataPixelY.Text };
            SetValueToListItem(RoratepointListview, temarray);
            txbRotataPixelX.Clear();
            txbRotataPixelY.Clear();
        }
        //修改旋转像素坐标点
        private void btnModifyRotataPixel_Click(object sender, EventArgs e)
        {
            if (RoratepointListview.Items.Count <= 0 || RoratepointListview.SelectedItems == null)
                return;
            if (MessageBox.Show("确认修改？", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                int index = RoratepointListview.SelectedIndices[0];
                RoratepointListview.Items[index].SubItems[1].Text = txbRotataPixelX.Text.Trim();
                RoratepointListview.Items[index].SubItems[2].Text = txbRotataPixelY.Text.Trim();
            }
        }

      

        private void btnDeleteRotataPixel_Click(object sender, EventArgs e)
        {
            if (RoratepointListview.Items.Count <= 0 || RoratepointListview.SelectedItems.Count == 0)
                return;
            if (MessageBox.Show("确认删除？", "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Question) ==
                DialogResult.Yes)
            {
                int index = RoratepointListview.SelectedIndices[0];
                RoratepointListview.Items.RemoveAt(index);

            }
        }

        //多点计算旋转中心
        void CaculateMultorRorateCenter()
        {
            this.Invoke(new Action(() =>
            {

                if (RoratepointListview.Items.Count < 5)
                {
                    MessageBox.Show("点位坐标数据不足5条，请确认!");
                    return;
                }


                List<CVPoint> pointlist = new List<CVPoint>();
              //  currvisiontool.clearAll();
                currvisiontool.dispImage(GrabImg);
                Mat dst = new Mat();
                if(GrabImg!=null)
                Cv2.CvtColor(GrabImg, dst, ColorConversionCodes.GRAY2BGR);

                foreach (ListViewItem s in RoratepointListview.Items)
                {
                    pointlist.Add(new CVPoint(
                                double.Parse(s.SubItems[1].Text),
                              double.Parse(s.SubItems[2].Text)));
                    if (GrabImg != null)
                        dst.drawCross(new CVPoint(double.Parse(s.SubItems[1].Text),
                                       double.Parse(s.SubItems[2].Text)), Scalar.Green, 20, 2);
                }

                Point2d centreP = new Point2d(0, 0); double radius = 0;
                AxisCoorditionRotation.FitCircle(pointlist, ref centreP, ref radius);

                if (GrabImg != null)
                    dst.Circle((int)centreP.X, (int)centreP.Y, (int)radius, Scalar.Green, 2);

                Point2d robotP = CalibrationTool.AffineTransPoint2d(Hom_mat2d, new Point2d(centreP.X, centreP.Y));

                setCalCentreHandle?.Invoke(new string[] { robotP.X.ToString("f3"),
                  robotP.Y.ToString("f3")}, null);
                setCalCentreHandle?.Invoke(new string[] { robotP.X.ToString("f3"),
                  robotP.Y.ToString("f3")}, null);

                txbCurrRorateCenterX.Text = robotP.X.ToString("f3");
                txbCurrRorateCenterY.Text = robotP.Y.ToString("f3");

                if (GrabImg != null)
                    currvisiontool.dispImage(dst);


            }));

        }

        void setData(ref RotatePointDataClass RotateP)
        {
            RotateP.ListPoint.Clear();
            foreach (ListViewItem s in RoratepointListview.Items)
                RotateP.ListPoint.Add(new PointF(float.Parse(s.SubItems[1].Text),
                     float.Parse(s.SubItems[2].Text)));
        }

        void showData(RotatePointDataClass RotateP)
        {
            k = 0;
            RoratepointListview.Items.Clear();
            if (RotateP != null)
                foreach (var s in RotateP.ListPoint)
                {
                    k++;
                    ListViewItem tem = new ListViewItem(new string[3] { k.ToString(), s.X.ToString(), s.Y.ToString() });
                    RoratepointListview.Items.Add(tem);
                }
        }

        void LoadConfigOfRorateParma(string dirFileName)
        {
            if (!Directory.Exists(dirFileName))
                Directory.CreateDirectory(dirFileName);
            d_RotatePointDataClass = GeneralUse.ReadSerializationFile<RotatePointDataClass>(dirFileName + "\\Calib\\RotatePointData");
            if (d_RotatePointDataClass == null)
                d_RotatePointDataClass = new RotatePointDataClass();

            d_RotateCentrePointDataClass = GeneralUse.ReadSerializationFile<RotateCentrePointDataClass>(dirFileName + "\\Calib\\RotateCentrePointData");
            if (d_RotateCentrePointDataClass == null)
                d_RotateCentrePointDataClass = new RotateCentrePointDataClass();

            txbCurrRorateCenterX.Text = d_RotateCentrePointDataClass.Rx.ToString("f3");
            //RotatoX = double.Parse(txbCurrRorateCenterX.Text);
            txbCurrRorateCenterY.Text = d_RotateCentrePointDataClass.Ry.ToString("f3");
            //RotatoY = double.Parse(txbCurrRorateCenterY.Text);

            showData(d_RotatePointDataClass);

        }
        //旋转中心计算有关参数保存
        private void btnRatitoCaliDataSave_Click(object sender, EventArgs e)
        {
            try
            {
                setData(ref d_RotatePointDataClass);

                GeneralUse.WriteSerializationFile<RotatePointDataClass>("配方\\" + CurrRecipeName + "\\Calib\\RotatePointData", d_RotatePointDataClass);
                double RotatoX = double.Parse(txbCurrRorateCenterX.Text);
                double RotatoY = double.Parse(txbCurrRorateCenterY.Text);
                d_RotateCentrePointDataClass.Rx = RotatoX;
                d_RotateCentrePointDataClass.Ry = RotatoY;
                GeneralUse.WriteSerializationFile<RotateCentrePointDataClass>("配方\\" + CurrRecipeName + "\\Calib\\RotateCentrePointData", d_RotateCentrePointDataClass);

                setCalCentreHandle?.Invoke(new string[] { RotatoX.ToString("f3"),
                  RotatoY.ToString("f3")}, null);
                AutoUpdateCalibFile();
                // MessageBox.Show("参数保存成功！");
            }
            catch (Exception er)
            {
                MessageBox.Show("参数保存失败！" + er.Message);
            }

        }

        private void 清除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RoratepointListview.Items.Clear();
            k = 0;
        }
        #endregion
    }
    /// <summary>
    /// 角度输出方式
    /// </summary>
    public enum EumOutAngleMode
    {
        [Description("绝对的")]
        Absolute,
        [Description("相对的")]
        Relative
    }

    /// <summary>
    /// 员工操作权限
    /// </summary>
    public enum EumOperationAuthority
    {
        [Description("无")]
        None = 0,
        [Description("操作员")]
        Operator = 1,
        [Description("程序员")]
        Programmer,
        [Description("管理员")]
        Administrators
    }

    /// <summary>
    /// 当前相机工作方式
    /// </summary>
  public  enum EunmcurrCamWorkStatus
    {
        /// <summary>
        /// 自由模式
        /// </summary>
        freestyle,
        /// <summary>
        /// 9点标定
        /// </summary>
        NinePointcLocation,
        /// <summary>
        /// 旋转标定
        /// </summary>
        RotatoLocation,
        /// <summary>
        /// 产品点位1测试
        /// </summary>
        NormalTest_T1,
        /// <summary>
        /// 产品点位2测试
        /// </summary>
        NormalTest_T2,
        /// <summary>
        /// 胶水测试
        /// </summary>
        NormalTest_G,
        /// <summary>
        /// 无
        /// </summary>
        None
    }

    public enum EumModelType
    {

        None = -1,

        ProductModel_1,  //当前为产品1模板 ,default

        ProductModel_2,  //当前为产品1模板 ,default

        CaliBoardModel, //当前为标定板模板

        GluetapModel   //点胶阀
    }

    /// <summary>
    /// 九点标定像素坐标数据集合
    /// </summary>
    [Serializable]
    public class pixelPointDataClass
    {
        public pixelPointDataClass()
        {
            ListPoint = new List<PointF>(capacity);
        }
        ~pixelPointDataClass()
        {
            ListPoint.Clear();
        }
        const int capacity = 9;
        public List<PointF> ListPoint { get; set; }

    }
    /// <summary>
    /// 九点标定机械坐标数据集合
    /// </summary>
    [Serializable]
    public class robotPointDataClass
    {
        public robotPointDataClass()
        {
            ListPoint = new List<PointF>(capacity);
        }
        ~robotPointDataClass()
        {
            ListPoint.Clear();
        }
        const int capacity = 9;
        public List<PointF> ListPoint { get; set; }
    }
    /// <summary>
    /// 旋转中心计算数据集合
    /// </summary>
    [Serializable]
    public class RotatePointDataClass
    {
        public RotatePointDataClass()
        {
            ListPoint = new List<PointF>(capacity);
        }
        ~RotatePointDataClass()
        {
            ListPoint.Clear();
        }
        const int capacity = 6;
        public List<PointF> ListPoint { get; set; }

    }
    /// <summary>
    /// 旋转中心
    /// </summary>
    [Serializable]
    public class RotateCentrePointDataClass
    {
        public double Rx { get; set; } = 0;
        public double Ry { get; set; } = 0;
    }
    /// <summary>
    /// 坐标系转换数据
    /// </summary>
    [Serializable]
    public class converCoorditionDataClass
    {
        public converCoorditionDataClass()
        {

        }
        //X缩放
        public double Sx { get; set; }
        //Y缩放
        public double Sy { get; set; }
        //旋转角(弧度)
        public double Phi { get; set; }
        //倾斜角(弧度)
        public double Theta { get; set; }
        //X偏移量
        public double Tx { get; set; }
        //Y偏移量
        public double Ty { get; set; }

        //X偏差
        public double XRms { get; set; }
        //Y偏差
        public double YRms { get; set; }

    }

    [Serializable]
    public class MatchBaseInfo
    {
        /// <summary>
        /// 基准点X
        /// </summary>
        public double BaseX { get; set; } = 0;
        /// <summary>
        /// 基准点Y
        /// </summary>
        public double BaseY { get; set; } = 0;
        /// <summary>
        /// 基准角度
        /// </summary>
        public double BaseAngle { get; set; } = 0;
        /// <summary>
        /// 基准轮廓长度
        /// </summary>
        public double ContourLength { get; set; } = 0;
        /// <summary>
        /// 基准轮廓面积
        /// </summary>
        public double ContourArea { get; set; } = 0;

    }

    /// <summary>
    /// 前处理工具数据
    /// </summary>
    [Serializable]
    public class PreToolDataClass
    {
        public PreToolDataClass()
        {
            ListToolName = new List<string>();
        }
        ~PreToolDataClass()
        {
            ListToolName.Clear();
        }
        public List<string> ListToolName { get; set; }

    }

    /// <summary>
    /// 膨胀
    /// </summary>
    [Serializable]
    public class gray_dilation_rect
    {
        public gray_dilation_rect() { }

        public gray_dilation_rect(int markWidth, int markHeight)
        {
            MarkWidth = markWidth;
            MarkHeight = markHeight;
        }
        public int MarkWidth { get; set; } = 3;
        public int MarkHeight { get; set; } = 3;
    }
    /// <summary>
    /// 腐蚀
    /// </summary>
    [Serializable]
    public class gray_erosion_rect
    {
        public gray_erosion_rect() { }
        public gray_erosion_rect(int markWidth, int markHeight)
        {
            MarkWidth = markWidth;
            MarkHeight = markHeight;
        }
        public int MarkWidth { get; set; } = 3;
        public int MarkHeight { get; set; } = 3;

    }
    /// <summary>
    /// 开运算
    /// </summary>
    [Serializable]
    public class gray_opening_rect
    {
        public gray_opening_rect() { }
        public gray_opening_rect(int markWidth, int markHeight)
        {
            MarkWidth = markWidth;
            MarkHeight = markHeight;
        }
        public int MarkWidth { get; set; } = 3;
        public int MarkHeight { get; set; } = 3;

    }
    /// <summary>
    /// 闭运算
    /// </summary>
    [Serializable]
    public class gray_closing_rect
    {
        public gray_closing_rect() { }
        public gray_closing_rect(int markWidth, int markHeight)
        {
            MarkWidth = markWidth;
            MarkHeight = markHeight;
        }
        public int MarkWidth { get; set; } = 3;
        public int MarkHeight { get; set; } = 3;

    }
    /// <summary>
    /// 图像常数处理
    /// </summary>
    [Serializable]
    public class scale_image
    {
        public scale_image() { }
        public scale_image(float mult, int add)
        {
            Mult = mult;
            Add = add;
        }
        public float Mult { get; set; } = 0.01f;
        public int Add { get; set; } = 0;

    }

}