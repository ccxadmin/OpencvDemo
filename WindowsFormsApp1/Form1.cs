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
    public partial class Form1 : Form
    {
        frmCalibration f = null;
       // Icam icam = null;
        public Form1()
        {
            InitializeComponent();         
            f =  frmCalibration.CreateInstance();           
            f.TopLevel = false;
            f.Show();
            f.Hide();
            f.Dock = DockStyle.Fill;
            f.setOperationAuthority(EumOperationAuthority.Administrators);
           
        }
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            switch ((sender as RadioButton).Name)
            {
                case "radioButton1":
                    f.Parent = this.tabPage1;
                    f.SetParametersHide(false);
                    f.Show();             
                    this.tabPage1.Controls.Clear();
                    this.tabPage1.Controls.Add(f);
                    break;
                case "radioButton2":
                    f.Parent = this.tabPage2;
                    f.SetParametersHide(true);
                    f.Show();
                    this.tabPage2.Controls.Clear();
                    this.tabPage2.Controls.Add(f);
                    break;
                case "radioButton3":
                    f.Parent = this.tabPage3;
                    f.SetParametersHide(true);
                    f.Show();
                    this.tabPage3.Controls.Clear();
                    this.tabPage3.Controls.Add(f);
                    break;
            }
        }

    
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            f.Release();
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = comboBox1.SelectedIndex;
            if (index < 0) return;
            f.setOperationAuthority( (EumOperationAuthority)Enum.Parse(typeof(EumOperationAuthority), index.ToString()));
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex == 0)
            {
                f.SetParametersHide(false);
            }
            else if (comboBox2.SelectedIndex == 1)
            {
                f.SetParametersHide(true);
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox3.SelectedIndex == 0)
            {
                f.setStyle(true);
            }
            else if (comboBox3.SelectedIndex == 1)
            {
                f.setStyle(false);
            }

        }
        int i = 0;
        private void button1_Click(object sender, EventArgs e)
        {
           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            f.OpenGlueCheckFunction();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Threading.ThreadPool.QueueUserWorkItem( new System.Threading.WaitCallback(taskTest));
        }

        void taskTest(object t)
        {
            f.workstatus = EunmcurrCamWorkStatus.NinePointcLocation;
            for (int i = 0; i < 9; i++)
            {
                Bitmap bp = new Bitmap("C:\\Users\\ccxadmin\\Desktop\\images\\"+i+".bmp");
                f.getImageDelegate(bp);

                System.Threading.Thread.Sleep(2000);
            }
        }
    }
}
