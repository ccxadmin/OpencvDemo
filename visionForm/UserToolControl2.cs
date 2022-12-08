using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

namespace visionForm
{
    public partial class UserToolControl2 : UserControl
    {
        public UserToolControl2()
        {
            InitializeComponent();

            titleName = this.chexGetFocus.Text = string.Format("机器人工具{0}点位示教(相对方式)", NumberCount);

        }
        public UserToolControl2(string name)
        {
            this.chexGetFocus.Text = name;
            titleName = name;
        }

        /// <summary>
        /// 传递数据返回用户控件
        /// </summary>
        /// <param name="setdata"></param>
        /// <returns></returns>
        static public UserToolControl2 UserSetData(UserToolControlData2 setdata)
        {

            UserToolControl2 uc = (UserToolControl2)Activator.CreateInstance(typeof(UserToolControl2));
            setdata.chexText = uc.TitleName = setdata.chexText == "" ? uc.TitleName : setdata.chexText;
            NumberCount = int.Parse(uc.TitleName.Substring(5, 1));
            NumberCount++;

            GabolTestFlowClass.NewAddControl(uc);
            BindData(uc, setdata);
            return uc;
        }

        public static Hashtable UcBindUcdata = new Hashtable();
        static public void BindData(UserToolControl2 uc, UserToolControlData2 ucdata)
        {
            UcBindUcdata.Add(uc, ucdata);
        }
        static public void UnBindData(UserToolControl2 uc)
        {
            UcBindUcdata.Remove(uc);
        }
        static public UserToolControlData2 getData(UserToolControl2 uc)
        {
            if (!UcBindUcdata.ContainsKey(uc))
                return null;
            else
                return (UserToolControlData2)UcBindUcdata[uc];
        }

        public void writeData(UserToolControlData2 ucdata)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<UserToolControlData2>(writeData));
            }
            else
            {
                txbProduMarkPx.Text = ucdata.ProduMarkPoint.x.ToString("f3");
                txbProduMarkPy.Text = ucdata.ProduMarkPoint.y.ToString("f3");
                txbProduMarkPr.Text = ucdata.ProduMarkPoint.θ.ToString("f3");

                txbToolGrabPx.Text = ucdata.ToolGrabPoint.x.ToString("f3");
                txbToolGrabPy.Text = ucdata.ToolGrabPoint.y.ToString("f3");
                txbToolGrabPr.Text = ucdata.ToolGrabPoint.θ.ToString("f3");

                txbRotate_calibPx.Text = ucdata.Rotate_calibpoint.x.ToString("f3");
                txbRotate_calibPy.Text = ucdata.Rotate_calibpoint.y.ToString("f3");
                txbRotate_calibPr.Text = ucdata.Rotate_calibpoint.θ.ToString("f3");

                txbCalib_rotatePx.Text = ucdata.calib_RotatePoint.x.ToString("f3");
                txbCalib_rotatePy.Text = ucdata.calib_RotatePoint.y.ToString("f3");
                txbCalib_rotatePr.Text = ucdata.calib_RotatePoint.θ.ToString("f3");
           
                txbGrab_rotatePx.Text = ucdata.Grab_rotatepoint.x.ToString("f3");
                txbGrab_rotatePy.Text = ucdata.Grab_rotatepoint.y.ToString("f3");
                txbGrab_rotatePr.Text = ucdata.Grab_rotatepoint.θ.ToString("f3");
           
            }

        }
        public void readData(ref UserToolControlData2 ucdata)
        {
            ucdata.ProduMarkPoint.x = double.Parse(txbProduMarkPx.Text.Trim());
            ucdata.ProduMarkPoint.y = double.Parse(txbProduMarkPy.Text.Trim());
            ucdata.ProduMarkPoint.θ = double.Parse(txbProduMarkPr.Text.Trim());

            ucdata.ToolGrabPoint.x = double.Parse(txbToolGrabPx.Text.Trim());
            ucdata.ToolGrabPoint.y = double.Parse(txbToolGrabPy.Text.Trim());
            ucdata.ToolGrabPoint.θ = double.Parse(txbToolGrabPr.Text.Trim());

            ucdata.Rotate_calibpoint.x = double.Parse(txbRotate_calibPx.Text.Trim());
            ucdata.Rotate_calibpoint.y = double.Parse(txbRotate_calibPy.Text.Trim());
            ucdata.Rotate_calibpoint.θ = double.Parse(txbRotate_calibPr.Text.Trim());

            ucdata.calib_RotatePoint.x = double.Parse(txbCalib_rotatePx.Text.Trim());
            ucdata.calib_RotatePoint.y = double.Parse(txbCalib_rotatePy.Text.Trim());
            ucdata.calib_RotatePoint.θ = double.Parse(txbCalib_rotatePr.Text.Trim());
        
            ucdata.Grab_rotatepoint.x = double.Parse(txbGrab_rotatePx.Text.Trim());
            ucdata.Grab_rotatepoint.y = double.Parse(txbGrab_rotatePy.Text.Trim());
            ucdata.Grab_rotatepoint.θ = double.Parse(txbGrab_rotatePr.Text.Trim());
       
        }

        public static int NumberCount { get; set; } = 1;
        private string titleName;
        public string TitleName
        {
            get => titleName;
            set
            {
                titleName = value;
                if (chexGetFocus.InvokeRequired)
                    chexGetFocus.Invoke(new Action(() => {
                        chexGetFocus.Text = value;
                    }));
                else
                    chexGetFocus.Text = value;

            }
        }

        private bool isGetFocus = false;
        public bool IsGetFocus
        {
            get => isGetFocus;
            set
            {
                this.isGetFocus = value;

                if (chexGetFocus.InvokeRequired)
                    chexGetFocus.Invoke(new Action(() => {
                        chexGetFocus.Checked = value;
                    }));
                else
                    chexGetFocus.Checked = value;

            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //frmDescription fd = new frmDescription();
            //fd.StartPosition = FormStartPosition.CenterParent;
            //fd.TopMost = true;
            //fd.ShowDialog();

        }

        private void chexGetFocus_CheckedChanged(object sender, EventArgs e)
        {
            isGetFocus = chexGetFocus.Checked;
        }

        public void SetGrabDataOfProductMark(toolPoint dp)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action<toolPoint>(SetGrabDataOfProductMark));
            else
            {
                this.txbProduMarkPx.Text = dp.x.ToString("f3");
                this.txbProduMarkPy.Text = dp.y.ToString("f3");
                this.txbProduMarkPr.Text = dp.θ.ToString("f3");
            }

        }


        public EventHandler GetProductMarkPHandle;
        //产品Mark点坐标获取
        private void btnProduMarkPoint_Click(object sender, EventArgs e)
        {
            if (GetProductMarkPHandle != null)
                GetProductMarkPHandle(titleName, null);

        }
        //工具抓取位坐标获取
        private void btnToolGrabPoint_Click(object sender, EventArgs e)
        {

        }
        //旋转中心标定位坐标获取
        private void btnRotate_calibpoint_Click(object sender, EventArgs e)
        {

        }
        //标定位旋转中心坐标获取
        private void btnCalib_rotatepoint_Click(object sender, EventArgs e)
        {

        }
        //抓取位旋转中心坐标计算
        private void btnGrab_rotatepoint_Click(object sender, EventArgs e)
        {
            //等待位旋转中心计算(等待位)

            double x = double.Parse(txbToolGrabPx.Text) - double.Parse(txbRotate_calibPx.Text) +
                  double.Parse(txbCalib_rotatePx.Text);
            double y = double.Parse(txbToolGrabPy.Text) - double.Parse(txbRotate_calibPy.Text) +
               double.Parse(txbCalib_rotatePy.Text);

            txbGrab_rotatePx.Text = x.ToString("f3");
            txbGrab_rotatePy.Text = y.ToString("f3");

        }

    }

    //相对方式控件数据点
    [Serializable]
    public class UserToolControlData2
    {
        public UserToolControlData2()
        {
            chexText = "";
            ProduMarkPoint = new toolPoint();
            ToolGrabPoint = new toolPoint();
            Rotate_calibpoint = new toolPoint();
            calib_RotatePoint = new toolPoint();        
            Grab_rotatepoint = new toolPoint();
           
        }
        public string chexText;
        public toolPoint ProduMarkPoint;
        public toolPoint ToolGrabPoint;
        public toolPoint Rotate_calibpoint;
        public toolPoint calib_RotatePoint;      
        public toolPoint Grab_rotatepoint;
      
    }
}
