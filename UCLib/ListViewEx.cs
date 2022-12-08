using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Collections;

namespace UCLib
{
   

    [ToolboxItem(true)]
    public partial class ListViewEx : ListView
    {
        /// <summary>
        /// 获得当前进程，以便重绘控件
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(
          IntPtr hWnd, int Msg, int wParam, ref RECT lParam);
        [DllImport("user32.dll")]
        public static extern int SendMessage(
          IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(
         IntPtr hwnd, int msg, int wParam, ref HDITEM lParam);


        [DllImport("user32.dll")]
        public extern static int OffsetRect(ref RECT lpRect, int x, int y);


        // <summary>
        /// 获取窗体的工作区域
        /// </summary>
        [DllImport("user32.dll")]
        public static extern int GetWindowRect(IntPtr hwnd, ref RECT lpRect);

        public ListViewEx() : base()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            // 该调用是 Windows.Forms 组件设计器所必需的。
            // InitializeComponent();

            // TODO: 在 InitComponent 调用后添加任何初始化
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        }
        private int _HeaderHeight = 0;
        private int _cpadding = 3;
        private ArrayList _EmbeddedItems = new ArrayList();
        /// <summary>
        /// 是否启用热点效果
        /// </summary>
        private bool _HotTrack = true;
        private Color _SelectedBeginColor = Color.FromArgb(211, 238, 255);
        private Color _SelectedEndColor = Color.FromArgb(175, 225, 253);
        private Color _HeaderBeginColor = Color.FromArgb(253, 253, 253);
        private Color _HeaderEndColor = Color.FromArgb(235, 235, 235);
        private Color _RowBackColor1 = Color.FromArgb(255, 255, 254);
        private Color _RowBackColor2 = Color.FromArgb(243, 246, 253);
        /// <summary>
        /// 边框颜色
        /// </summary>
        private Color _BorderColor = Color.FromArgb(0xA7, 0xA6, 0xAA);

        /// <summary>
        /// 热点边框颜色
        /// </summary>
        private Color _HotColor = Color.FromArgb(0x33, 0x5E, 0xA8);

        /// <summary>
        /// 是否鼠标MouseOver状态
        /// </summary>
        private bool _IsMouseOver = false;

        private Font _Font =new System.Drawing.Font("宋体", 9F);
        // /// <summary>
        // /// 设计器支持所需的方法 - 不要使用代码编辑器
        // /// 修改此方法的内容。
        // /// </summary>
        // private void InitializeComponent()
        // {
        // components = new System.ComponentModel.Container();
        // }

        // /// <summary>
        // /// 清理所有正在使用的资源。
        // /// </summary>
        // protected override void Dispose( bool disposing )
        // {
        // if( disposing )
        // {
        // if (components != null)
        // {
        // components.Dispose();
        // }
        // }
        // base.Dispose( disposing );
        // }

        #region 属性
        [Category("TXProperties")]
        [Description("行交替颜色1")]
        public Color RowBackColor1
        {
            get { return _RowBackColor1; }
            set
            {
                _RowBackColor1 = value;
                base.Invalidate();
            }
        }

        [Category("TXProperties")]
        [Description("行交替颜色2")]
        public Color RowBackColor2
        {
            get { return _RowBackColor2; }
            set
            {
                _RowBackColor2 = value;
                base.Invalidate();
            }
        }

        [Category("TXProperties")]
        [Description("选择状态颜色")]
        public Color SelectedBeginColor
        {
            get { return _SelectedBeginColor; }
            set
            {
                this._SelectedBeginColor = value;
                base.Invalidate(true);
            }
        }

        [Category("TXProperties")]
        [Description("选择状态颜色")]
        public Color SelectedEndColor
        {
            get { return _SelectedEndColor; }
            set
            {
                this._SelectedEndColor = value;
                base.Invalidate(true);
            }
        }

        [Category("TXProperties")]
        public override Font Font
        {
            get
            {
                return this._Font;
            }
            set
            {
                this._Font = value;
                base.Font = new Font(value.FontFamily, value.Size + 0);
                base.Invalidate();
            }
        }
        [Category("TXProperties")]
        [Description("标题颜色")]
        public Color HeaderBeginColor
        {
            get { return _HeaderBeginColor; }
            set
            {
                this._HeaderBeginColor = value;
                base.Invalidate(true);
            }
        }

        [Category("TXProperties")]
        [Description("标题颜色")]
        public Color HeaderEndColor
        {
            get { return _HeaderEndColor; }
            set
            {
                this._HeaderEndColor = value;
                base.Invalidate(true);
            }
        }
        /// <summary>
        /// 是否启用热点效果
        /// </summary>
        [Category("行为"),
        Description("获得或设置一个值，指示当鼠标经过控件时控件边框是否发生变化。只在控件的BorderStyle为FixedSingle时有效"),
        DefaultValue(true)]
        public bool HotTrack
        {
            get
            {
                return this._HotTrack;
            }
            set
            {
                this._HotTrack = value;
                //在该值发生变化时重绘控件，下同
                //在设计模式下，更改该属性时，如果不调用该语句，
                //则不能立即看到设计试图中该控件相应的变化
                this.Invalidate();
            }
        }
        /// <summary>
        /// 边框颜色
        /// </summary>
        [Category("外观"),
        Description("获得或设置控件的边框颜色"),
        DefaultValue(typeof(Color), "#A7A6AA")]
        public Color BorderColor
        {
            get
            {
                return this._BorderColor;
            }
            set
            {
                this._BorderColor = value;
                this.Invalidate();
            }
        }
        /// <summary>
        /// 热点时边框颜色
        /// </summary>
        [Category("外观"),
        Description("获得或设置当鼠标经过控件时控件的边框颜色。只在控件的BorderStyle为FixedSingle时有效"),
        DefaultValue(typeof(Color), "#335EA8")]
        public Color HotColor
        {
            get
            {
                return this._HotColor;
            }
            set
            {
                this._HotColor = value;
                this.Invalidate();
            }
        }
        #endregion 属性

        /// <summary>
        /// 鼠标移动到该控件上时
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            //鼠标状态
            this._IsMouseOver = true;
            //重绘
            this.Invalidate();
            base.OnMouseMove(e);
        }
        /// <summary>
        /// 当鼠标从该控件移开时
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            this._IsMouseOver = false;
            this.Invalidate();
            base.OnMouseLeave(e);
        }

        /// <summary>
        /// 当该控件获得焦点时
        /// </summary>
        /// <param name="e"></param>
        protected override void OnGotFocus(EventArgs e)
        {
            this.Invalidate();
            base.OnGotFocus(e);
        }
        /// <summary>
        /// 当该控件失去焦点时
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLostFocus(EventArgs e)
        {
            this.Invalidate();
            base.OnLostFocus(e);
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
        /// 使用渐变色填充区域
        /// </summary>
        /// <param name="g">The g.</param>
        /// <param name="roundRect">The round rect.</param>
        /// <param name="color1">The color1.</param>
        /// <param name="color2">The color2.</param>
        /// User:Ryan  CreateTime:2012-8-4 19:15.
        public static void FillPath(Graphics g, RoundRectangle roundRect, Color color1, Color color2)
        {
            if (roundRect.Rect.Width <= 0 || roundRect.Rect.Height <= 0)
            {
                return;
            }

            using (GraphicsPath path = roundRect.ToGraphicsBezierPath())
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(roundRect.Rect, color1, color2, LinearGradientMode.Vertical))
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
        /// User:Ryan  CreateTime:2011-07-28 16:11.
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
        // <summary>
        /// 绘制指定区域路径的边框
        /// </summary>
        /// User:Ryan  CreateTime:2011-07-28 16:11.
        public static void DrawPathBorder(Graphics g, RoundRectangle roundRect, Color color)
        {
            DrawPathBorder(g, roundRect, color, 1);
        }
        protected TextFormatFlags GetFormatFlags(HorizontalAlignment align)
        {
            TextFormatFlags flags =
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.VerticalCenter;

            switch (align)
            {
                case HorizontalAlignment.Center:
                    flags |= TextFormatFlags.HorizontalCenter;
                    break;
                case HorizontalAlignment.Right:
                    flags |= TextFormatFlags.Right;
                    break;
                case HorizontalAlignment.Left:
                    flags |= TextFormatFlags.Left;
                    break;
            }

            return flags;
        }

        /// <summary>
        /// 在指定的区域绘制绘制图像和文字
        /// </summary>
        /// <param name="g">The g.</param>
        /// <param name="roundRect">The roundRect.</param>
        /// <param name="image">The image.</param>
        /// <param name="imageSize">Size of the image.</param>
        /// <param name="text">The text.</param>
        /// <param name="font">The font.</param>
        /// <param name="forceColor">Color of the force.</param>
        /// User:K.Anding  CreateTime:2011-7-24 22:07.
        public static void DrawImageAndString(Graphics g, Rectangle rect, Image image, Size imageSize, string text, Font font, Color forceColor)
        {
            int x = rect.X, y = rect.Y, len;
            SizeF sf = g.MeasureString(text, font);
            len = Convert.ToInt32(sf.Width);
            x += rect.Width / 2 - len / 2;
            if (image != null)
            {
                x -= imageSize.Width / 2;
                Rectangle imageRect = new Rectangle(x, y + rect.Height / 2 - imageSize.Height / 2, imageSize.Width, imageSize.Height);
                g.DrawImage(image, imageRect);
                x += imageSize.Width + 2;
            }

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using (SolidBrush brush = new SolidBrush(forceColor))
            {
                g.DrawString(text, font, brush, x, y + rect.Height / 2 - Convert.ToInt32(sf.Height) / 2 + 2);
            }
        }
        protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
        {
            base.OnDrawColumnHeader(e);
            Graphics g = e.Graphics;
            InitializeGraphics(g);
            Rectangle bounds = e.Bounds;
            FillPath(g, new RoundRectangle(bounds, 0), this._HeaderBeginColor, this._HeaderEndColor);
            bounds.Height--;
            if (this.BorderStyle != BorderStyle.None)
            {
                using (Pen p = new Pen(this.BorderColor))
                {
                    g.DrawLine(p, new Point(bounds.Right, bounds.Bottom), new Point(bounds.Right, bounds.Top));
                    g.DrawLine(p, new Point(bounds.Left, bounds.Bottom), new Point(bounds.Right, bounds.Bottom));
                }
            }
            else
            {
               DrawPathBorder(g, new RoundRectangle(bounds, 0), this._BorderColor);
            }

            bounds.Height++;
            TextFormatFlags flags = GetFormatFlags(e.Header.TextAlign);
            Rectangle textRect = new Rectangle(
                       bounds.X + 3,
                       bounds.Y,
                       bounds.Width - 6,
                       bounds.Height); ;
            Image image = null;
            Size imgSize = new System.Drawing.Size(16, 16);
            Rectangle imageRect = Rectangle.Empty;
            if (e.Header.ImageList != null)
            {
                image = e.Header.ImageIndex == -1 ?
                    null : e.Header.ImageList.Images[e.Header.ImageIndex];
            }

           DrawImageAndString(g, bounds, image, imgSize, e.Header.Text, this._Font, e.ForeColor);
        }

        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            base.OnDrawItem(e);
            if (this.View != View.Details)
            {
                e.DrawDefault = true;
            }
        }
        /// <summary>
        /// 渲染圆角矩形区域（级渲染）
        /// </summary>
        /// <param name="g">The Graphics.</param>
        /// <param name="roundRect">The RoundRectangle.</param>
        /// <param name="color1">The color1.</param>
        /// <param name="color2">The color2.</param>
        /// <param name="blend">色彩混合渲染方案</param>
        /// User:K.Anding  CreateTime:2011-7-20 23:27.
        public static void FillPath(Graphics g, RoundRectangle roundRect, Color color1, Color color2, Blend blend)
        {
            GradientColor color = new GradientColor(color1, color2, blend.Factors, blend.Positions);
            FillRectangle(g, roundRect, color);
        }
        /// <summary>
        /// 渲染一个圆角矩形区域（渐变色）
        /// Fills the rectangle.
        /// </summary>
        /// <param name="g">The g.</param>
        /// <param name="roundRect">The round rect.</param>
        /// <param name="color">The color.</param>
        /// User:Ryan  CreateTime:2012-8-3 21:45.
        public static void FillRectangle(Graphics g, RoundRectangle roundRect, GradientColor color)
        {
            if (roundRect.Rect.Width <= 0 || roundRect.Rect.Height <= 0)
            {
                return;
            }

            using (GraphicsPath path = roundRect.ToGraphicsBezierPath())
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(roundRect.Rect, color.First, color.Second, LinearGradientMode.Vertical))
                {
                    brush.Blend.Factors = color.Factors;
                    brush.Blend.Positions = color.Positions;
                    g.FillPath(brush, path);
                }
            }
        }
        protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
        {
            base.OnDrawSubItem(e);
            if (View != View.Details || e.ItemIndex == -1)
            {
                return;
            }

            Rectangle bounds = e.Bounds;
            ListViewItemStates itemState = e.ItemState;
            Graphics g = e.Graphics;
            InitializeGraphics(g);
            Blend blen = new Blend();
            blen.Positions = new float[] { 0f, 0.4f, 0.7f, 1f };
            blen.Factors = new float[] { 0f, 0.3f, 0.8f, 0.2f };
            Color c1, c2;
            if ((itemState & ListViewItemStates.Selected) == ListViewItemStates.Selected)
            {
                c1 = this._SelectedBeginColor;
                c2 = this._SelectedEndColor;            
             
                FillPath(g, new RoundRectangle(bounds, 0), c1, c2, blen);
            }
            else
            {
                if (e.ColumnIndex == 0)
                {
                    bounds.Inflate(0, -1);
                }
                c1 = e.ItemIndex % 2 == 0 ? this._RowBackColor1 : this._RowBackColor2;
                c2 = c1;
                FillPath(g, new RoundRectangle(bounds, 0), c1, c2, blen);
            }

            if (e.ColumnIndex == 0)
            {
                this.OnDrawFirstSubItem(e, g);
            }
            else
            {
                this.DrawNormalSubItem(e, g);
            }
        }
        private void DrawNormalSubItem(DrawListViewSubItemEventArgs e, Graphics g)
        {
            TextFormatFlags flags = GetFormatFlags(e.Header.TextAlign);
            Rectangle rect = e.Bounds;
            rect.X += 2; rect.Width -= 4;
            Color c = (e.ItemState & ListViewItemStates.Selected) == ListViewItemStates.Selected ?
                Color.White : e.SubItem.ForeColor;
            TextRenderer.DrawText(g, e.SubItem.Text, this._Font, rect, c, flags);
        }
        protected virtual void OnDrawFirstSubItem(DrawListViewSubItemEventArgs e, Graphics g)
        {
            TextFormatFlags flags = GetFormatFlags(e.Header.TextAlign);
            Rectangle rect = e.Bounds;       
            Rectangle textRect = rect;
            int offset = 2;
                  
            textRect.X += offset;
            textRect.Width -= offset * 2;
            Color c = (e.ItemState & ListViewItemStates.Selected) == ListViewItemStates.Selected ?
                Color.White : e.SubItem.ForeColor;
            TextRenderer.DrawText(g, e.SubItem.Text, this._Font, textRect, c, flags);
        }
        /// <summary>
        /// 获得操作系统消息
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            switch (m.Msg)
            {
                case (int)0x000F:
                    ////绑定控件
                    this.BindEmbeddedItem();                
                    break;
                case (int)0x000A:
                    this.NcPaint(ref m);
                    break;
                case (int)0x0085:
                    this.NcPaint(ref m);
                    break;
                case (int)0x0047:
                    IntPtr result = m.Result;
                    this.NcPaint(ref m);                
                    m.Result = result;
                    break;
            }
        }
 
        private IntPtr HeaderWnd
        {
            get { return new IntPtr(SendMessage(base.Handle, (int)(0x1000+31), 0, 0)); }
        }
        private int ColumnCount
        {
            get { return SendMessage(HeaderWnd, (int)0x1200, 0, 0); }
        }
        private int ColumnAtIndex(int column)
        {
            HDITEM hd = new HDITEM();
            hd.mask = (int)0x0080;
            for (int i = 0; i < ColumnCount; i++)
            {
                if (SendMessage(HeaderWnd, (int)(0x1200+3), column, ref hd) != IntPtr.Zero)
                {
                    return hd.iOrder;
                }
            }
            return 0;
        }
        private Rectangle HeaderEndRect()
        {
            RECT rect = new RECT();
            IntPtr headerWnd = HeaderWnd;
            SendMessage(
                headerWnd, (int)(0x1200+7), ColumnAtIndex(ColumnCount - 1), ref rect);
            int left = rect.right;
            GetWindowRect(headerWnd, ref rect);
            OffsetRect(ref rect, -rect.left, -rect.top);
            rect.left = left;
            return Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
        }

        /// <summary>
        /// 绑定控件到subitem上
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="item">The item.</param>
        /// <param name="itemIndex">Index of the item.</param>
        public void AddControlToSubItem(Control control, ListViewItem.ListViewSubItem item, int itemIndex)
        {
            this.Controls.Add(control);
            EmbeddedItem ei;
            ei.EmbeddedControl = control;
            ei.SubItem = item;
            ei.ItemIndex = itemIndex;
            this._EmbeddedItems.Add(ei);
        }

        /// <summary>
        /// 清除列表中绑定的控件
        /// </summary>     
        public void ClearEmbeddedItems()
        {
            foreach (EmbeddedItem item in this._EmbeddedItems)
            {
                item.EmbeddedControl.Visible = false;
                item.EmbeddedControl.Dispose();
            }

            this._EmbeddedItems.Clear();
        }

        /// <summary>
        /// 绑定内嵌到subItem的控件，主要在onpaint事件中调用。
        /// </summary> 
        private void BindEmbeddedItem()
        {
            if (this._HeaderHeight <= 0)
            {
                this._HeaderHeight = this.HeaderEndRect().Height;
            }

            Rectangle r;
            using (Graphics g = this.CreateGraphics())
            {
                foreach (EmbeddedItem item in this._EmbeddedItems)
                {
                    r = item.SubItem.Bounds;
                    ////不是第一列要特殊处理，不然会不兼容
                    if (r.Y > (this._HeaderHeight - r.Height) && r.Y > 0 && r.Y < this.ClientRectangle.Height)
                    {
                        item.EmbeddedControl.Visible = true;
                        int w = Convert.ToInt32(g.MeasureString(item.EmbeddedControl.Text, item.SubItem.Font).Width) + 2 * _cpadding;
                        if (r.X <= 10 && w >= this.Columns[0].Width)
                        {
                            w = this.Columns[0].Width - 2 * _cpadding;
                        }

                        item.EmbeddedControl.Bounds = new Rectangle(r.X + _cpadding, r.Y + _cpadding, w, r.Height - (2 * _cpadding));
                    }
                    else
                    {
                        item.EmbeddedControl.Visible = false;
                    }
                }
            }
        }
        private void NcPaint(ref Message msg)
        {
            //只有在边框样式为FixedSingle时自定义边框样式才有效
            if (base.BorderStyle == BorderStyle.None)
            {
                return;
            }
            //拦截系统消息，获得当前控件进程以便重绘。
            //一些控件（如TextBox、Button等）是由系统进程绘制，重载OnPaint方法将不起作用.
            //所有这里并没有使用重载OnPaint方法绘制TextBox边框。
            //
            //MSDN:重写 OnPaint 将禁止修改所有控件的外观。
            //那些由 Windows 完成其所有绘图的控件（例如 Textbox）从不调用它们的 OnPaint 方法，
            //因此将永远不会使用自定义代码。请参见您要修改的特定控件的文档，
            //查看 OnPaint 方法是否可用。如果某个控件未将 OnPaint 作为成员方法列出，
            //则您无法通过重写此方法改变其外观。
            //
            //MSDN:要了解可用的 Message.Msg、Message.LParam 和 Message.WParam 值，
            //请参考位于 MSDN Library 中的 Platform SDK 文档参考。可在 Platform SDK（“Core SDK”一节）
            //下载中包含的 windows.h 头文件中找到实际常数值，该文件也可在 MSDN 上找到。
            IntPtr hDC = GetWindowDC(msg.HWnd);
            if (hDC == IntPtr.Zero)
            {
                return;
            }

            //边框Width为1个像素
            //System.Drawing.Pen pen = new Pen(this._BorderColor, 1); ;
            using (Pen pen = new Pen(this._BorderColor, 1))
            {
                if (this._HotTrack)
                {
                    if (this.Focused)
                        pen.Color = this._HotColor;
                    else
                    {
                        if (this._IsMouseOver)
                            pen.Color = this._HotColor;
                        else
                            pen.Color = this._BorderColor;
                    }
                }
              if(this.Enabled)
                    pen.Color = this._BorderColor;
              else
                    pen.Color = Color.Gray;
                //绘制边框
                using (Graphics g = Graphics.FromHdc(hDC))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
                }
            }
            //绘制边框
            //System.Drawing.Graphics g = Graphics.FromHdc(hDC);
            //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            //g.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            //pen.Dispose();

            //返回结果
            msg.Result = IntPtr.Zero;
            //释放
            ReleaseDC(msg.HWnd, hDC);
        }
        
    }
    #region RECT

    /// <summary>
    /// Win32中的矩形结构，及与.net中的Rectangle的处理
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        public RECT(Rectangle rect)
        {
            this.left = rect.Left;
            this.right = rect.Right;
            this.top = rect.Top;
            this.bottom = rect.Bottom;
        }

        public Rectangle Rect
        {
            get
            {
                return new Rectangle(this.left, this.top, this.right - this.left, this.bottom - this.top);
            }
        }

        public static RECT FromXYWH(int x, int y, int width, int height)
        {
            return new RECT(x, y, x + width, y + height);
        }

        public static RECT FromRectangle(Rectangle rect)
        {
            return new RECT(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
    }

    #endregion

    #region HDITEM

    [StructLayout(LayoutKind.Sequential)]
    public struct HDITEM
    {
        internal int mask;
        internal int cxy;
        internal IntPtr pszText;
        internal IntPtr hbm;
        internal int cchTextMax;
        internal int fmt;
        internal IntPtr lParam;
        internal int iImage;
        internal int iOrder;
        internal uint type;
        internal IntPtr pvFilter;
    }
    #endregion

    #region struct of EmbeddedItem

    public struct EmbeddedItem
    {
        public ListViewItem.ListViewSubItem SubItem;
        public Control EmbeddedControl;
        public int ItemIndex;
    }

    #endregion

    #region 阶梯渐变色彩 GradientColor
    /// <summary>
    /// 阶梯渐变色彩
    /// </summary>
    public struct GradientColor
    {
        /// <summary>
        /// (构造函数).Initializes a new instance of the <see cref="GradientColor"/> struct.
        /// </summary>
        /// <param name="color1">The color1.</param>
        /// <param name="color2">The color2.</param>
        /// <param name="factors">The factors.</param>
        /// <param name="positions">The positions.</param>
        /// User:Ryan  CreateTime:2012-8-2 23:16.
        public GradientColor(Color color1, Color color2, float[] factors, float[] positions)
        {
            this.First = color1;
            this.Second = color2;
            this.Factors = factors == null ? new float[] { } : factors;
            this.Positions = positions == null ? new float[] { } : positions;
        }

        /// <summary>
        /// 初始色彩
        /// </summary>
        public Color First;

        /// <summary>
        /// 结束色彩
        /// </summary>
        public Color Second;

        /// <summary>
        /// 色彩渲染系数（0到1的浮点数值）
        /// </summary>
        public float[] Factors;

        /// <summary>
        /// 色彩渲染位置（0到1的浮点数值）
        /// </summary>
        public float[] Positions;

    }
    #endregion
}