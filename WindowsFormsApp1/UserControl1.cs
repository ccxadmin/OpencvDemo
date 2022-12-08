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
    public partial class UserControl1 : System.Windows.Forms.TabControl
    {
        [Browsable(true), Description("整个控件的背景色"), Category("外观")]
        public Color TabControlBackColor { get; set; }

        [Browsable(true), Description("Tab的标题栏边框颜色"), Category("外观")]
        public Color TabBorderColor { get; set; }

        [Browsable(true), Description("当前激活Tab的标题栏背景色"), Category("外观")]
        public Color ActivedTabBackColor { get; set; }

        [Browsable(true), Description("当前激活Tab的标题文字颜色"), Category("外观")]
        public Color ActivedTabLabelColor { get; set; }

        [Browsable(true), Description("未激活Tab的标题栏背景色"), Category("外观")]
        public Color InActivedTabBackColor { get; set; }

        [Browsable(true), Description("未激活Tab的标题文字颜色"), Category("外观")]
        public Color InActivedTabLabelColor { get; set; }

        [Browsable(true), Description("Tab标题栏的大小"), Category("外观")]
        public Size TabSize { get; set; }

        public UserControl1()
        {
            this.InitializeComponent();

            TabSet();

            this.TabBorderColor = Color.Black;
            this.ActivedTabLabelColor = Color.Black;
            this.InActivedTabLabelColor = Color.Black;
            this.ActivedTabBackColor = Color.White;
            this.InActivedTabBackColor = Color.FromArgb(0, 192, 192);
            this.TabControlBackColor = Color.Transparent;
            this.TabSize = new Size(100, 35);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (this.TabPages.Count == 1) return;
            this.TabPages.RemoveAt(this.SelectedIndex);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle tcRec = this.ClientRectangle;//整个tabControl的边框
            e.Graphics.FillRectangle(new SolidBrush(this.TabControlBackColor), tcRec);

            for (int i = 0; i < this.TabPages.Count; i++)
            {
                Rectangle tabRectangle = new Rectangle(1, 1 + i * TabSize.Height, TabSize.Width, TabSize.Height);
                SolidBrush brush = new SolidBrush(this.InActivedTabLabelColor);
                StringFormat sf = new StringFormat();//封装文本布局信息
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Center;
                if (i == this.SelectedIndex)
                {
                    brush = new SolidBrush(this.ActivedTabLabelColor);
                    e.Graphics.FillRectangle(new SolidBrush(ActivedTabBackColor), tabRectangle);
                    e.Graphics.DrawRectangle(new Pen(this.TabBorderColor), tabRectangle);
                }
                else
                {
                    e.Graphics.FillRectangle(new SolidBrush(InActivedTabBackColor), tabRectangle);
                    e.Graphics.DrawRectangle(new Pen(this.TabBorderColor), tabRectangle);
                }
                e.Graphics.DrawString(this.Controls[i].Text, this.Font, brush, tabRectangle, sf);
            }
        }

        /// <summary>
        /// 设定控件绘制模式
        /// </summary>
        private void TabSet()
        {
            this.DrawMode = TabDrawMode.OwnerDrawFixed;
            this.Alignment = TabAlignment.Left;
            this.SizeMode = TabSizeMode.Fixed;
            this.Multiline = true;
            base.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            base.UpdateStyles();
        }

        public override Rectangle DisplayRectangle
        {
            get
            {
                Rectangle rect = base.DisplayRectangle;
                return new Rectangle(rect.Left - 3, rect.Top - 3, rect.Width + 6, rect.Height + 5);
            }
        }
    }
}
