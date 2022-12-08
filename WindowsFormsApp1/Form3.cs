using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using visionForm;


namespace WindowsFormsApp1
{
    public partial class Form3 : Form
    {
        frmCalibration f = null;
        public Form3()
        {
            InitializeComponent();
            f = frmCalibration.CreateInstance();
            f.setOperationAuthority(EumOperationAuthority.Administrators);
            f.TopLevel = false;
            f.Show();          
            f.Dock = DockStyle.Fill;
            this.panel1.Controls.Add(f);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            f.createRecipe("00000");
        }
    }
}
