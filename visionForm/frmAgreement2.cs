using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace visionForm
{
    public partial class frmAgreement2 : Form
    {
        public frmAgreement2()
        {
            InitializeComponent();
            n_width = this.Width;
            n_height = this.Height;
          
        }

        int n_width, n_height;
        Point startP;
        PointF centerP;
        private void frmAgreement_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        //放大镜功能，放大倍数1.5
        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            this.Width = (int)(this.Width * 1.5);
            this.Height = (int)(this.Height * 1.5);
            startP = new Point
            {
                X = (int)( centerP.X - this.Width/2.0) ,
                Y = (int)(centerP.Y - this.Height / 2.0) 
            };
            this.Location = startP;
        }

        private void frmAgreement_Load(object sender, EventArgs e)
        {
            startP = this.Location;
            centerP = new PointF
            {
                X = (float)((2 * startP.X + n_width) / 2.0),
                Y = (float)((2 * startP.Y + n_height) / 2.0)
            };
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            this.Width = n_width;
            this.Height = n_height;
            startP = new Point
            {
                X = (int)(centerP.X - this.Width / 2.0),
                Y = (int)(centerP.Y - this.Height / 2.0)
            };
            this.Location = startP;
        }
    }
}
