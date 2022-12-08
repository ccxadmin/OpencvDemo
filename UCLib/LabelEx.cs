using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UCLib
{
    public class LabelEx : Label
    {
        bool isValid = true;
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.Font = new Font("宋体", 9F);
            if (!isValid)
                ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.Gray, ButtonBorderStyle.Solid);
            else
                ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.FromArgb(255, 109, 60),
                     ButtonBorderStyle.Solid);
        }
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            isValid = this.Enabled;
            this.Invalidate();
        }
    }
}