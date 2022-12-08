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
    public partial class formshowpic : Form
    {
        public formshowpic()
        {
            InitializeComponent();
        }

        private void formshowpic_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Dispose();
             
        }
    }
}
