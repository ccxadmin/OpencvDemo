using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace UCLib
{
    [ToolboxBitmap(typeof(ComboBox))]
    public class ComboBoxEx : ComboBox
    {
        #region fileds

        /// <summary>
        /// 控件的状态
        /// </summary>
        private EnumControlState _ControlState;

        private IntPtr _EditHandle = IntPtr.Zero;

        private int _Margin = 2;

        private bool _BeginPainting = false;

        private int _CornerRadius = 0;

        private Color _BackColor = Color.White;

        #endregion

        #region Initializes

        public ComboBoxEx()
            : base()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            this.UpdateStyles();
            this.Size = new Size(150, 20);
            this._ControlState = EnumControlState.Default;
        }

        #endregion

        #region Properties

        internal Rectangle ButtonRect
        {
            get
            {
                return this.GetDropDownButtonRect();
            }
        }


        [Browsable(false)]
        public new Color BackColor
        {
            get { return base.BackColor; }
        }

        [Browsable(false)]
        public new RightToLeft RightToLeft
        {
            get { return base.RightToLeft; }
        }
        /// <summary>
        /// 获取窗体的工作区域
        /// </summary>
        [DllImport("user32.dll")]
        public static extern int GetWindowRect(IntPtr hwnd, ref RECT lpRect);
        internal Rectangle EditRect
        {
            get
            {
                if (this.DropDownStyle == ComboBoxStyle.DropDownList)
                {
                    Rectangle rect = new Rectangle(
                        this._Margin, this._Margin, Width - this.ButtonRect.Width - this._Margin * 2, Height - this._Margin * 2);
                    if (RightToLeft == RightToLeft.Yes)
                    {
                        rect.X += this.ButtonRect.Right;
                    }

                    return rect;
                }

                if (IsHandleCreated && this._EditHandle != IntPtr.Zero)
                {
                    RECT rcClient = new RECT();
                    GetWindowRect(this._EditHandle, ref rcClient);
                    return RectangleToClient(rcClient.Rect);
                }

                return Rectangle.Empty;
            }
        }

        #endregion

        #region Override methods
        [DllImport("user32.dll")]
         static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT ps);

        [DllImport("user32.dll")]
         static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT ps);
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case (int)0x000F:
                    switch (this.DropDownStyle)
                    {
                        case ComboBoxStyle.DropDown:
                            if (!this._BeginPainting)
                            {
                                PAINTSTRUCT ps = new PAINTSTRUCT();
                                this._BeginPainting = true;
                                BeginPaint(m.HWnd, ref ps);
                                this.DrawComboBox(ref m);
                                EndPaint(m.HWnd, ref ps);
                                this._BeginPainting = false;
                                m.Result = new IntPtr(1);
                            }
                            else
                            {
                                base.WndProc(ref m);
                            }
                            break;
                        case ComboBoxStyle.DropDownList:
                            base.WndProc(ref m);
                            this.DrawComboBox(ref m);
                            break;
                        default:
                            base.WndProc(ref m);
                            break;
                    }
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        #endregion

    
        /// <summary>
        /// 绘制复选框和内容.
        /// </summary>
        /// User:Ryan  CreateTime:2011-07-29 15:44.
        private void DrawComboBox(ref Message msg)
        {
            using (Graphics g = Graphics.FromHwnd(msg.HWnd))
            {
                this.DrawComboBox(g);
            }
        }

        /// <summary>
        /// 初始化Graphics对象为高质量的绘制
        /// </summary>
        /// <param name="g">The g.</param>
        /// User:Ryan  CreateTime:2011-08-19 16:53.
        public static void InitializeGraphics(Graphics g)
        {
            if (g != null)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.CompositingQuality = CompositingQuality.HighQuality;
            }
        }
        /// <summary>
        /// 渲染一个圆角矩形区域（单色）
        /// Fills the rectangle.
        /// </summary>
        /// <param name="g">The g.</param>
        /// <param name="roundRect">The round rect.</param>
        /// <param name="color">The color.</param>
        /// User:Ryan  CreateTime:2012-8-3 21:45.
        public static void FillRectangle(Graphics g, RoundRectangle roundRect, Color color)
        {
            if (roundRect.Rect.Width <= 0 || roundRect.Rect.Height <= 0)
            {
                return;
            }

            using (GraphicsPath path = roundRect.ToGraphicsBezierPath())
            {
                using (Brush brush = new SolidBrush(color))
                {
                    g.FillPath(brush, path);
                }
            }
        }
          
        /// <summary>
        /// 绘制指定区域路径的边框.
        /// 注意:若要设置边框宽度，需要自己调整区域，它是向外绘制的
        /// </summary>
        /// <param name="g">The Graphics.</param>
        /// <param name="roundRect">The RoundRectangle.</param>
        /// <param name="color">The color.</param>
        /// <param name="borderWidth">Width of the border.</param>
        public static void DrawPathBorder(Graphics g, RoundRectangle roundRect, Color color, int borderWidth)
        {
            using (GraphicsPath path = roundRect.ToGraphicsBezierPath())
            {
                using (Pen pen = new Pen(color, borderWidth))
                {
                    g.DrawPath(pen, path);
                }
            }
        }
        /// <summary>
        /// 绘制下拉框区域.
        /// </summary>
        /// <param name="g">The Graphics.</param>
        /// User:Ryan  CreateTime:2011-07-29 15:44.
        private void DrawComboBox(Graphics g)
        {
            InitializeGraphics(g);
            Rectangle rect = new Rectangle(Point.Empty, this.Size);
            rect.Width--; rect.Height--;
            ////背景
            RoundRectangle roundRect = new RoundRectangle(rect, new CornerRadius(this._CornerRadius));
            Color backColor = this.Enabled ? this._BackColor : SystemColors.Control;
            g.SetClip(this.EditRect, CombineMode.Exclude);
            FillRectangle(g, roundRect, backColor);
            g.ResetClip();
            this.DrawButton(g);
            ////边框
            if(this.Enabled)
               DrawPathBorder(g, roundRect,Color.FromArgb(255,109,60),1);
            else
                DrawPathBorder(g, roundRect, Color.Gray, 1);
        }
        /// <summary>
        /// 绘制指针（方向箭头）
        /// </summary>
        /// <param name="g">The Graphics.</param>
        /// <param name="direction">指针的方向.</param>
        /// <param name="rect">绘制的区域.</param>
        /// <param name="arrowSize">指针的大小，即长宽.</param>
        /// <param name="c">指针颜色</param>
        public static void DrawArrow(Graphics g, ArrowDirection direction, Rectangle rect, Size arrowSize, float offset, Color c)
        {
            Point center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            using (GraphicsPath path = new GraphicsPath())
            {
                PointF[] points = null;
                switch (direction)
                {
                    case ArrowDirection.Down:
                        points = new PointF[] {
                            new PointF(center.X,center.Y+arrowSize.Height/2),
                            new PointF(center.X-arrowSize.Width/2,center.Y-arrowSize.Height/2),
                            new PointF(center.X,center.Y-arrowSize.Height/2+offset),
                            new PointF(center.X+arrowSize.Width/2,center.Y-arrowSize.Height/2),};
                        break;
                    case ArrowDirection.Up:
                        points = new PointF[] {
                            new PointF(center.X,center.Y-arrowSize.Height/2),
                            new PointF(center.X-arrowSize.Width/2,center.Y+arrowSize.Height/2),
                            new PointF(center.X,center.Y+arrowSize.Height/2-offset),
                            new Point(center.X+arrowSize.Width/2,center.Y+arrowSize.Height/2),};
                        break;
                    case ArrowDirection.Left:
                        points = new PointF[] {
                            new PointF(center.X-arrowSize.Width/2,center.Y),
                            new PointF(center.X+arrowSize.Width/2,center.Y-arrowSize.Height/2),
                            new PointF(center.X+arrowSize.Width/2-offset,center.Y),
                            new PointF(center.X+arrowSize.Width/2,center.Y+arrowSize.Height/2),};
                        break;
                    case ArrowDirection.Right:
                        points = new PointF[] {
                            new PointF(center.X+arrowSize.Width/2,center.Y),
                            new PointF(center.X-arrowSize.Width/2,center.Y-arrowSize.Height/2),
                            new PointF(center.X-arrowSize.Width/2+offset,center.Y),
                            new PointF(center.X-arrowSize.Width/2,center.Y+arrowSize.Height/2),};
                        break;
                }

                path.AddLines(points);
                using (Brush brush = new SolidBrush(c))
                {
                    g.FillPath(brush, path);
                }
            }
        }   
        /// <summary>
            /// 绘制向两边阶梯渐变的线条
            /// </summary>
        public static void DrawGradientLine(Graphics g, Color lineColor, int angle, int x1, int y1, int x2, int y2)
        {
            Blend blend = new Blend();
            blend.Positions = new float[] { 0f, .15f, .5f, .85f, 1f };
            blend.Factors = new float[] { 1f, .4f, 0f, .4f, 1f };
            DrawGradientLine(g, lineColor, blend, angle, 1, x1, y1, x2, y2);
        }

        /// <summary>
        /// 绘制阶梯渐变的线条，可以在参数Blend对象中设置色彩混合规则
        /// </summary>
        public static void DrawGradientLine(Graphics g, Color lineColor, Blend blend, int angle, int lineWidth, int x1, int y1, int x2, int y2)
        {
            Color c1 = lineColor;
            Color c2 = Color.FromArgb(10, c1);
            Rectangle rect = new Rectangle(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
            using (LinearGradientBrush brush = new LinearGradientBrush(rect, c1, c2, angle))
            {
                brush.Blend = blend;
                using (Pen pen = new Pen(brush, lineWidth))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            }
        }

        /// <summary>
        ///  绘制按钮
        /// </summary>
        /// <param name="g">The Graphics.</param>
        /// User:Ryan  CreateTime:2011-08-02 14:23.
        private void DrawButton(Graphics g)
        {
            Rectangle btnRect;
            EnumControlState btnState = this.GetComboBoxButtonPressed() ? EnumControlState.HeightLight : EnumControlState.Default;
            btnRect = new Rectangle(this.ButtonRect.X-2, this.ButtonRect.Y - 1, this.ButtonRect.Width + 1 + this._Margin, this.ButtonRect.Height + 2);
            RoundRectangle btnRoundRect = new RoundRectangle(btnRect, new CornerRadius(0, this._CornerRadius, 0, this._CornerRadius));
            Blend blend = new Blend(3);
            blend.Positions = new float[] { 0f, 0.5f, 1f };
            blend.Factors = new float[] { 0f, 1f, 0f };
            Color backColor = this.Enabled ? this._BackColor : SystemColors.Control;
            FillRectangle(g, btnRoundRect, backColor);
            Size btnSize = new Size(10, 5);
            ArrowDirection direction = ArrowDirection.Down;
            //if(this.Enabled)
            //  DrawArrow(g, direction, btnRect, btnSize, 0f, Color.FromArgb(255, 109, 60));
            //else
            DrawArrow(g, direction, btnRect, btnSize, 0f, SystemColors.WindowText);

            if (this.Enabled)
            {
                Color lineColor = Color.FromArgb(255, 109, 60);
                DrawGradientLine(g, lineColor, 90, btnRect.X, btnRect.Y, btnRect.X, btnRect.Bottom - 1);
            }
            else
            {
                Color lineColor = Color.Gray;
                DrawGradientLine(g, lineColor, 90, btnRect.X, btnRect.Y, btnRect.X, btnRect.Bottom - 1);
            }
           
        }

        #region GetBoxInfo
        /// <summary>
        /// 获取ComboBox的控件信息
        /// </summary>
        /// <returns></returns>
        [DllImport("user32.dll")]
         static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref ComboBoxInfo info);

        private ComboBoxInfo GetComboBoxInfo()
        {
            ComboBoxInfo cbi = new ComboBoxInfo();
            cbi.cbSize = Marshal.SizeOf(cbi);
            GetComboBoxInfo(base.Handle, ref cbi);
            return cbi;
        }

        private bool GetComboBoxButtonPressed()
        {
            ComboBoxInfo cbi = this.GetComboBoxInfo();
            return cbi.stateButton == ComboBoxButtonState.STATE_SYSTEM_PRESSED;
        }

        private Rectangle GetDropDownButtonRect()
        {
            ComboBoxInfo cbi = this.GetComboBoxInfo();
            return cbi.rcButton.Rect;
        }

        #endregion

        #region ResetRegion

        private void ResetRegion()
        {
            if (this._CornerRadius > 0)
            {
                Rectangle rect = new Rectangle(Point.Empty, this.Size);
                RoundRectangle roundRect = new RoundRectangle(rect, new CornerRadius(this._CornerRadius));
                if (this.Region != null)
                {
                    this.Region.Dispose();
                }

                this.Region = new Region(roundRect.ToGraphicsBezierPath());
            }
        }
        #endregion

        #region 获取值
        /// <summary>
        /// 获取值
        /// </summary>
        private object Value
        {
            get
            {
                ComboBoxItem item = this.SelectedItem as ComboBoxItem;

                if ( item == null )
                {
                    return string.Empty;
                }

                return item.Value;
            }
        }
        #endregion

        #region 类型转换

        #region 转换为日期

        #region 重载1
        /// <summary>
        /// 转换为日期
        /// </summary>
        public DateTime ToDateTime()
        {
            return Convert.ToDateTime( this.Value );
        }
        #endregion  
        #endregion        
    
        #region 将数据转换为字符串
        /// <summary>
        /// 将数据转换为字符串
        /// </summary>
        public override string ToString()
        {
            return Convert.ToString( this.Value );
        }
        #endregion          

        #region 将数据转换为双精度浮点型

        #region 重载1
        /// <summary>
        /// 将Text转换为浮点值
        /// </summary>
        public double ToDouble()
        {
            return Convert.ToDouble( this.Value );
        }
        #endregion   
        #endregion

        #region 将数据转换为Decimal类型

        #region 重载1
        /// <summary>
        /// 将数据转换为Decimal类型
        /// </summary>
        public decimal ToDecimal()
        {
            return Convert.ToDecimal( this.Value );
        }
        #endregion
    
        #endregion


        #endregion
    }
    /// <summary>
    /// ComboBoxButton状态
    /// </summary>
    internal enum ComboBoxButtonState
    {
        STATE_SYSTEM_NONE = 0,

        STATE_SYSTEM_INVISIBLE = 0x00008000,

        STATE_SYSTEM_PRESSED = 0x00000008
    }
    /// <summary>
    /// ComboBox的Windows信息结构
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ComboBoxInfo
    {
        public int cbSize;
        public RECT rcItem;
        public RECT rcButton;
        public ComboBoxButtonState stateButton;
        public IntPtr hwndCombo;
        public IntPtr hwndEdit;
        public IntPtr hwndList;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public int fErase;
        public RECT rcPaint;
        public int fRestore;
        public int fIncUpdate;
        public int Reserved1;
        public int Reserved2;
        public int Reserved3;
        public int Reserved4;
        public int Reserved5;
        public int Reserved6;
        public int Reserved7;
        public int Reserved8;
    }

    /// <summary>
    /// 控件的基本状态
    /// </summary>
    internal enum EnumControlState
    {
        None,

        /// <summary>
        /// 默认状态
        /// </summary>
        Default,

        /// <summary>
        /// 高亮状态（鼠标悬浮）
        /// </summary>
        HeightLight,

        /// <summary>
        /// 焦点（鼠标按下、已选择、输入状态等）
        /// </summary>
        Focused,
    }

    #region ComboBoxItem(绑定下拉框的自定义对象)

    /// <summary>
    /// 绑定下拉框的自定义对象
    /// </summary>
    public class ComboBoxItem
    {
        public ComboBoxItem( string text, string value )
        {
            this.Text = text;
            this.Value = value;
        }

        /// <summary>
        /// 内容文本
        /// </summary>
        /// <value>The text.</value>
        public string Text
        {
            get;
            set;
        }

        /// <summary>
        /// 值
        /// </summary>
        /// <value>The value.</value>
        public string Value
        {
            get;
            set;
        }

        public override string ToString()
        {
            return this.Text;
        }
    }
    #endregion
}
