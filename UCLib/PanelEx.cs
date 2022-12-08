using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UCLib
{
  public  class PanelEx:Panel
    {
        bool isValid = true;
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
         
            this.Font = new Font("宋体", 9F);
            this.Padding = new Padding(2);
            if (!isValid)
                ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, 
                    Color.Gray, 2, ButtonBorderStyle.Solid,
                    Color.Transparent, 2, ButtonBorderStyle.Solid,
                    Color.Gray, 2, ButtonBorderStyle.Solid,
                    Color.Transparent, 2, ButtonBorderStyle.Solid);
            else
                ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle,
                    Color.FromArgb(255, 109, 60),2, ButtonBorderStyle.Solid,
                    Color.Transparent, 2, ButtonBorderStyle.Solid,
                    Color.FromArgb(255, 109, 60), 2, ButtonBorderStyle.Solid,
                    Color.Transparent, 2, ButtonBorderStyle.Solid);
        }     
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            isValid = this.Enabled;
            this.Invalidate();
        }
    }
}
