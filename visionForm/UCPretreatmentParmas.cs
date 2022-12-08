using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace visionForm
{
    public partial class UCPretreatmentParmas : UserControl
    {
        public UCPretreatmentParmas()
        {
            InitializeComponent();

            cobxMaskWidth.MouseWheel += new MouseEventHandler(Num_DiscountAmount_MouseWheel);
            cobxMarkHeight.MouseWheel += new MouseEventHandler(Num_DiscountAmount_MouseWheel);
         

        }
        //禁用numericUpDown控件鼠标中键滚轮消息响应
        private void Num_DiscountAmount_MouseWheel(object sender, MouseEventArgs e)
        {

            HandledMouseEventArgs h = e as HandledMouseEventArgs;
            if (h != null)
            {
                h.Handled = true;
            }
        }


        public  ComboBox ComboBoxMarkWidth
        {

            get => this.cobxMaskWidth;
        }

        public ComboBox ComboBoxMarkHeight
        {

            get => this.cobxMarkHeight;
        }


        public  void SetData(int MaskWidth,int MarkHeight)
        {
            cobxMaskWidth.Text = MaskWidth.ToString();
            cobxMarkHeight.Text = MarkHeight.ToString();
        }

        public   void GetData(ref int MaskWidth,ref int MarkHeight)
        {
            MaskWidth = int.Parse(cobxMaskWidth.Text);
            MarkHeight = int.Parse(cobxMarkHeight.Text);
        }

       
     
        static bool IsNumeric(string str)
        {
            System.Text.RegularExpressions.Regex reg1
                = new System.Text.RegularExpressions.Regex(@"^[-]?\d+[.]?\d*$");
            return reg1.IsMatch(str);
        }

      
        private void cobxMaskWidth_KeyUp(object sender, KeyEventArgs e)
        {
            if (!IsNumeric(cobxMaskWidth.Text.Trim()))
            {
                errorProvider1.SetError(this.cobxMaskWidth, "禁止非法数字");
                MessageBox.Show("禁止非法数字");
                cobxMaskWidth.Text = MaskWidthbuff;

            }
            Maskwidth_heightSaveHandle?.Invoke(cobxMaskWidth, null);
        }

        private void cobxMarkHeight_KeyUp(object sender, KeyEventArgs e)
        {
            if (!IsNumeric(cobxMarkHeight.Text.Trim()))
            {
                errorProvider1.SetError(this.cobxMarkHeight, "禁止非法数字");
                MessageBox.Show("禁止非法数字");
                cobxMarkHeight.Text = MarkHeightbuff;
            }
            Maskwidth_heightSaveHandle?.Invoke(cobxMarkHeight, null);
        }

        string MaskWidthbuff = string.Empty;
        private void cobxMaskWidth_KeyPress(object sender, KeyPressEventArgs e)
        {
            MaskWidthbuff = cobxMaskWidth.Text.Trim();
        }

        string MarkHeightbuff = string.Empty;
        private void cobxMarkHeight_KeyPress(object sender, KeyPressEventArgs e)
        {
            MarkHeightbuff = cobxMarkHeight.Text.Trim();
        }

        public EventHandler Maskwidth_heightSaveHandle;

      
    }
}
