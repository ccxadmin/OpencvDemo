using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class UserControl2 : System.Windows.Forms.TabControl
    {
        #region 属性、构造
        Color SelectedColor = Color.FromArgb(255, 109, 60);
        Color MoveColor = Color.Orange;
        Color FontColor = Color.White;
        int TextLeft = 10;
        [Browsable(true)]
        [Description("选项卡标题左边距"), Category("TextLeft"), DefaultValue(typeof(Int32), "10")]
        public int TitleTextLeft
        {
            get { return TextLeft; }
            set { this.TextLeft = value; }
        }

        [Browsable(true)]
        [Description("选项卡标题字体颜色"), Category("TitleColor"), DefaultValue(typeof(Color), "black")]
        public Color TitleFontColor
        {
            get { return FontColor; }
            set { this.FontColor = value; }
        }

        [Browsable(true)]
        [Description("选项卡标题字体选中颜色"), Category("TitleColor"), DefaultValue(typeof(Color), "white")]
        public Color TitleSelectedColor
        {
            get { return SelectedColor; }
            set { this.SelectedColor = value; }
        }

        [Browsable(true)]
        [Description("选项卡标题字体悬浮颜色"), Category("TitleColor"), DefaultValue(typeof(Color), "White")]
        public Color TitleMoveColor
        {
            get { return MoveColor; }
            set { this.MoveColor = value; }
        }

        [Browsable(true), Description("整个控件的背景色"), Category("外观")]
        public Color TabControlBackColor { get; set; }

        [Browsable(true), Description("TabControl ItemSize"), Category("外观")]
        public Size TabControlItemSize { get; set; }

        public UserControl2()
        {
            this.SuspendLayout();
            this.DrawMode = TabDrawMode.OwnerDrawFixed;
            this.ResumeLayout(false);
            this.SizeMode = TabSizeMode.Fixed;
            this.Multiline = true;
            this.TabControlBackColor = Color.SeaShell;
            this.TabControlItemSize = new Size(100, 28);
            this.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.tabMenu_DrawItem);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MainTabControl_MouseDown);
        }
        #endregion

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle tcRec = this.ClientRectangle;//整个tabControl的边框
            e.Graphics.FillRectangle(new SolidBrush(this.TabControlBackColor), tcRec);
            if (this.ItemSize != this.TabControlItemSize)
            {
                this.ItemSize = TabControlItemSize;
            }

            StringFormat sf = new StringFormat();//封装文本布局信息
            sf.LineAlignment = StringAlignment.Center;
            sf.Alignment = StringAlignment.Near;
            for (int i = 0; i < this.TabCount; i++)
            {
                Graphics g = e.Graphics;
                // int width = (int)g.MeasureString(this.Controls[i].Text, this.Font).Width + 40;
                Rectangle rect = this.GetTabRect(i);
                //  rect.Width = width;
                if (this.SelectedIndex == i)
                    g.FillRectangle(new SolidBrush(MoveColor), rect);
                else g.FillRectangle(new SolidBrush(SelectedColor), rect);

                SolidBrush brush = new SolidBrush(FontColor);
                //  rect.Width = width;
                rect.X += TextLeft;
                g.DrawString(this.Controls[i].Text, this.Font, brush, rect, sf);
                //using (Pen objpen = new Pen(Color.Black))
                //{
                //    int tx = (int)(rect.X + (rect.Width - 30));
                //    rect.X = tx - 2;
                //    Point p5 = new Point(tx, 8);
                //    Font font = new System.Drawing.Font("微软雅黑", 12);
                //    g.DrawString("〇", font, brush, rect, sf);
                //    font = new System.Drawing.Font("微软雅黑", 11);
                //    rect.X = tx + 2;
                //    rect.Y = rect.Y - 1;
                //    g.DrawString("×", font, brush, rect, sf);
                //}
            }
        }

        public override Rectangle DisplayRectangle
        {
            get
            {
                Rectangle rect = base.DisplayRectangle;
                return new Rectangle(rect.Left - 2, rect.Top - 2, rect.Width + 4, rect.Height + 5);
            }
        }

        int index = -1;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            int Count = 0;
            try
            {
                Graphics g = this.CreateGraphics();
                SolidBrush brush = new SolidBrush(FontColor);
                StringFormat sf = new StringFormat();//封装文本布局信息
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Near;

                for (int i = 0; i < this.TabPages.Count; i++)
                {
                    TabPage tp = this.TabPages[i];
                    if (this.GetTabRect(i).Contains(e.Location) && tp != this.SelectedTab)
                    {
                        if (index != i)
                        {
                            if (Count == 0)
                            {
                                if (index != -1 && this.TabPages[index] != this.SelectedTab)
                                {
                                    g.FillRectangle(new SolidBrush(SelectedColor), this.GetTabRect(index));

                                    RectangleF tRectangle = this.GetTabRect(index);
                                    tRectangle.X += TextLeft;
                                    g.DrawString(this.Controls[index].Text, this.Font, brush, tRectangle, sf);
                                }
                                Count = 1;
                            }
                            index = i;
                            g.FillRectangle(new SolidBrush(SelectedColor), this.GetTabRect(i));
                            RectangleF tRectangleF = this.GetTabRect(i);
                            tRectangleF.X += TextLeft;
                            g.DrawString(this.Controls[i].Text, this.Font, brush, tRectangleF, sf);
                            //using (Pen objpen = new Pen(Color.Black))
                            //{
                            //    int tx = (int)(tRectangleF.X + (tRectangleF.Width - 30));
                            //    tRectangleF.X = tx - 2;
                            //    brush.Color = Color.White;
                            //    Font font = new System.Drawing.Font("微软雅黑", 12);
                            //    g.DrawString("〇", font, brush, tRectangleF, sf);
                            //    font = new System.Drawing.Font("微软雅黑", 11);
                            //    tRectangleF.X = tx + 2;
                            //    tRectangleF.Y = tRectangleF.Y - 1;
                            //    g.DrawString("×", font, brush, tRectangleF, sf);
                            //}
                        }
                    }
                    if (this.GetTabRect(i).Contains(e.Location) && tp == this.SelectedTab)
                    {
                        if (index != -1 && index != this.SelectedIndex)
                        {
                            g.FillRectangle(new SolidBrush(SelectedColor), this.GetTabRect(index));
                            RectangleF tRectangleF = this.GetTabRect(index);
                            tRectangleF.X += TextLeft;
                            g.DrawString(this.Controls[index].Text, this.Font, brush, tRectangleF, sf);
                            //using (Pen objpen = new Pen(Color.Black))
                            //{
                            //    int tx = (int)(tRectangleF.X + (tRectangleF.Width - 30));
                            //    tRectangleF.X = tx - 2;
                            //    Font font = new System.Drawing.Font("微软雅黑", 12);
                            //    g.DrawString("〇", font, brush, tRectangleF, sf);
                            //    font = new System.Drawing.Font("微软雅黑", 11);
                            //    tRectangleF.X = tx + 2;
                            //    tRectangleF.Y = tRectangleF.Y - 1;
                            //    g.DrawString("×", font, brush, tRectangleF, sf);
                            //}
                        }
                        index = -1;
                    }
                }
            }
            catch (Exception)
            {
            }
            Count = 0;
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            try
            {
                Graphics g = this.CreateGraphics();
                if (index != -1 && this.TabPages[index] != this.SelectedTab)
                {
                    g.FillRectangle(new SolidBrush(SelectedColor), this.GetTabRect(index));
                    SolidBrush brush = new SolidBrush(FontColor);
                    RectangleF tRectangleF = this.GetTabRect(index);
                    StringFormat sf = new StringFormat();//封装文本布局信息
                    sf.LineAlignment = StringAlignment.Center;
                    sf.Alignment = StringAlignment.Near;
                    tRectangleF.X += TextLeft;
                    g.DrawString(this.Controls[index].Text, this.Font, brush, tRectangleF, sf);
                    //using (Pen objpen = new Pen(Color.Black))
                    //{
                    //    int tx = (int)(tRectangleF.X + (tRectangleF.Width - 30));
                    //    tRectangleF.X = tx - 2;
                    //    Point p5 = new Point(tx, 8);
                    //    Font font = new System.Drawing.Font("微软雅黑", 12);
                    //    g.DrawString("〇", font, brush, tRectangleF, sf);
                    //    font = new System.Drawing.Font("微软雅黑", 11);
                    //    tRectangleF.X = tx + 2;
                    //    tRectangleF.Y = tRectangleF.Y - 1;
                    //    g.DrawString("×", font, brush, tRectangleF, sf);
                    //}
                }
            }
            catch (Exception)
            {
            }
            index = -1;
            base.OnMouseLeave(e);
        }
        /// <summary>
        /// 重绘控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabMenu_DrawItem(object sender, DrawItemEventArgs e)
        {
            this.SetStyle(
            ControlStyles.UserPaint |                      // 控件将自行绘制，而不是通过操作系统来绘制 
            ControlStyles.OptimizedDoubleBuffer |          // 该控件首先在缓冲区中绘制，而不是直接绘制到屏幕上，这样可以减少闪烁 
            ControlStyles.AllPaintingInWmPaint |           // 控件将忽略 WM_ERASEBKGND 窗口消息以减少闪烁 
            ControlStyles.ResizeRedraw |                   // 在调整控件大小时重绘控件 
            ControlStyles.SupportsTransparentBackColor,    // 控件接受 alpha 组件小于 255 的 BackColor 以模拟透明 
            true);                                         // 设置以上值为 true 
            this.UpdateStyles();
        }

        //关闭按钮功能
        private void MainTabControl_MouseDown(object sender, MouseEventArgs e)
        {
            int closeSize = 20;
            if (e.Button == MouseButtons.Left)
            {
                int x = e.X, y = e.Y;
                //计算关闭区域  
                Rectangle tab = this.GetTabRect(this.SelectedIndex);
                tab.Offset(tab.Width - (closeSize + 3), 4);
                tab.Width = closeSize;
                tab.Height = closeSize;

                if (this.TabCount == 1) return;

                //如果鼠标在区域内就关闭选项卡  
                bool isClose = x > tab.X && x < tab.Right && y > tab.Y && y < tab.Bottom;
                if (isClose == true)
                {
                    this.TabPages.Remove(this.SelectedTab);
                }
            }
        }
    }
}
