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
    public partial class UCPretreatmentParmas2 : UserControl
    {
        public UCPretreatmentParmas2()
        {
            InitializeComponent();
            NumMult.MouseWheel += new MouseEventHandler(Num_DiscountAmount_MouseWheel);
            NumAdd.MouseWheel += new MouseEventHandler(Num_DiscountAmount_MouseWheel);
         

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
        public  NumericUpDown  NumericUpDownMult
        {
            get => this.NumMult;

        }

        public NumericUpDown NumericUpDownAdd
        {
            get => this.NumAdd;

        }
        public void SetData(float numMult, int numAdd)
        {
            NumMult.Value = (decimal)numMult;
            NumAdd.Value = (decimal)numAdd;
         
        }

        public void GetData(ref float numMult, ref int numAdd)
        {         
            numMult = (float)NumMult.Value;
            numAdd = (int)NumAdd.Value;
        }

        public EventHandler NumMult_AddSaveHandle;
        private void NumMult_ValueChanged(object sender, EventArgs e)
        {
            NumMult_AddSaveHandle?.Invoke(NumMult,null);
        }

        private void NumAdd_ValueChanged(object sender, EventArgs e)
        {
            NumMult_AddSaveHandle?.Invoke(NumAdd, null);
        }
    }
}
