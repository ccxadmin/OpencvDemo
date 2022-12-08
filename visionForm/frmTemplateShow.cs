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
    public partial class frmTemplateShow : Form
    {
        private frmTemplateShow()
        {
            InitializeComponent();
        }
        private frmTemplateShow(Bitmap showimg):this()
        {          
            imageShow = showimg;
            if(imageShow.Height>0)
            {
                float k = (float)imageShow.Height / imageShow.Width;
                this.Height = (int)(this.Width * k);
            }          
        }

        static  private frmTemplateShow _frmTemplateShow = null;

        static public frmTemplateShow createInstance(Bitmap showimg)
        {
            if (_frmTemplateShow == null)
                _frmTemplateShow = new frmTemplateShow(showimg);
            return _frmTemplateShow;
        }
        static public frmTemplateShow createInstance()
        {
            if (_frmTemplateShow == null)
                _frmTemplateShow = new frmTemplateShow();
            return _frmTemplateShow;
        }
        private Bitmap imageShow;
        public Bitmap ImageShow
        {
            get => this.imageShow;
            set { this.imageShow = value; }
        }

        public void UpdateShow()
        {
            this.pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
           
            this.pictureBox1.Image = imageShow;
            if (imageShow.Height > 0)
            {
                float k = (float)imageShow.Height / imageShow.Width;
                this.Height = (int)(this.Width * k);
            }
        }

    }
}
