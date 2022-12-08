using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisionShowLib;
using HalconDotNet;
using FunctionLib.Location;
using FilesRAW.Common;

namespace visionForm
{
    public partial class frmTemplateMaking : Form
    {
        public frmTemplateMaking(HObject img,HObject modelcontour, HObject MaskROI)
        {
            InitializeComponent();
            //画笔大小
            panSize = int.Parse(GeneralUse.ReadValue("画笔大小","配置","Config","1"));
            txbBarValue.Text = panSize.ToString();
            BarPanSize.Value = panSize;
            //画笔类型
            string penType = GeneralUse.ReadValue("画笔类型", "配置", "Config", "圆");
            if(penType!= "圆"&& penType != "矩形")
            {
                MessageBox.Show("画笔类型错误，已默认被修改为圆类型！");
                penType = "圆";
            }
            eumPanType =(EumPanType)Enum.Parse(typeof(EumPanType), penType);
            cobxPanType.Text = penType;

            m_DispContolList.Clear();
            currvisiontool = new UCVisionShowTool();
            HOperatorSet.GenEmptyObj(out originalContour);
            originalContour.Dispose();
            HOperatorSet.CopyObj(modelcontour,out originalContour,1,-1);

            HOperatorSet.GenEmptyObj(out originalMaskROI);
          
            if(GuidePositioning_HDevelopExport.ObjectValided(MaskROI))
            {
                originalMaskROI.Dispose();
                HOperatorSet.CopyObj(MaskROI, out originalMaskROI, 1, -1);
            }

            currvisiontool.Disp_MouseDownHandle += uCvisionLayout1_MouseDown;
            currvisiontool.Disp_MouseUpHandle += uCvisionLayout1_MouseUp;
            currvisiontool.Disp_MouseMoveHandle += uCvisionLayout1_MouseMove;

            currvisiontool.Dock = DockStyle.Fill;
            currvisiontool.SetBackgroundColor(EumControlBackColor.white);
            currvisiontool.chexTitle = "Cam1";      
            currvisiontool.LoadedImageNoticeHandle += new EventHandler(LoadedImageNoticeEvent);
            m_DispContolList.Add(currvisiontool);
            uCvisionLayout1.InitVisionControl(m_DispContolList.ToArray());


            currvisiontool.DispImage(img);
            currvisiontool.D_HImage = GrabImg=img;
            currvisiontool.DispRegion(originalContour,"green");
            currvisiontool.AddregionBuffer(originalContour, "green");
            currvisiontool.DispRegion(originalMaskROI, "orange");
            currvisiontool.AddregionBuffer(originalMaskROI, "orange");

            currvisiontool.RemoveRightMenu();
            uCvisionLayout1.RemoveRightMenu();
            currvisiontool.set缩放();
        }

      
        UCVisionShowTool currvisiontool = null;
        //图像显示控件
        List<UCVisionShowTool> m_DispContolList = new List<UCVisionShowTool>();

        HObject GrabImg = null;
        HObject originalContour = null;
        HObject originalMaskROI = null;
        void LoadedImageNoticeEvent(object sender, EventArgs e)
        {
            HOperatorSet.GenEmptyObj(out GrabImg);
            GrabImg.Dispose();

            GrabImg = currvisiontool.D_HImage;
        }

        private void splitContainer1_SizeChanged(object sender, EventArgs e)
        {
            if (splitContainer1.Width < 200) return;
            splitContainer1.SplitterDistance = splitContainer1.Width - 200;

        }

        private void frmTemplateMaking_Load(object sender, EventArgs e)
        {
            if (splitContainer1.Width < 200) return;
            splitContainer1.SplitterDistance = splitContainer1.Width - 200;

            HOperatorSet.GenEmptyObj(out MarkRegion);
         
            if(GuidePositioning_HDevelopExport.ObjectValided(originalMaskROI))
            {
                MarkRegion.Dispose();
                HOperatorSet.CopyObj(originalMaskROI, out MarkRegion, 1, -1);
            }
             
            cobxPanType.SelectedIndex = 0;
            currvisiontool.Focus();
        }
       
        enum EumPanType
        {
            圆,
            矩形
        }
        EumPanType eumPanType = EumPanType.圆; //画笔类型
        private void cobxPanType_SelectedIndexChanged(object sender, EventArgs e)
        {
            string value = cobxPanType.Text;
            if(value=="圆")
                eumPanType = EumPanType.圆;
            else
                eumPanType = EumPanType.矩形;

            //画笔类型
            string penType = cobxPanType.Text;
            GeneralUse.WriteValue("画笔类型", "配置", penType, "Config");
        }

        int panSize = 1;  //画笔大小
        private void BarPanSize_ValueChanged(object sender, EventArgs e)
        {
            panSize = BarPanSize.Value;
            txbBarValue.Text = panSize.ToString();
            GeneralUse.WriteValue("画笔大小", "配置", panSize.ToString(), "Config");
        }

        enum EumworkType
        {
            None,
            擦拭,
            清除,
            放大,
            缩小,
            自适应,
            平移
        }

        EumworkType eumworkType = EumworkType.None;
       
        HObject MarkRegion = null;

        bool IsStartDrawMask = false;
       
        //绘制重置
        private void btnReset_Click(object sender, EventArgs e)
        {
            MarkRegion.Dispose();
            HOperatorSet.GenEmptyObj(out MarkRegion);
            currvisiontool.ClearAllOverLays();
            currvisiontool.DispImage(GrabImg);
            currvisiontool.DispRegion(originalContour,"green");
            currvisiontool.AddregionBuffer(originalContour, "green");
            HOperatorSet.SetDraw(currvisiontool.HWindowsHandle,"fill");
        }

        private void txbBarValue_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode==Keys.Enter)
            {
                panSize = int.Parse( txbBarValue.Text.Trim());
                if(panSize>100|| panSize<1)
                {
                    MessageBox.Show("值范围错误！");
                    return;
                }
                BarPanSize.Value = panSize;
                GeneralUse.WriteValue("画笔大小", "配置", panSize.ToString(), "Config");
            }
        }

        public EventHandler SetModelMaskROIHandle = null;
        private void 保存toolStripLabel_Click(object sender, EventArgs e)
        {
            SetModelMaskROIHandle?.Invoke(MarkRegion, null) ;
            MessageBox.Show("OK");
        }

        private void frmTemplateMaking_FormClosing(object sender, FormClosingEventArgs e)
        {
            IsStartDrawMask = false; 
            //画笔大小
            GeneralUse.WriteValue("画笔大小", "配置", panSize.ToString(), "Config");
            //画笔类型
            string penType = cobxPanType.Text;
            GeneralUse.WriteValue("画笔类型", "配置", penType, "Config");  
          
        }
          
        private void 擦拭ToolStripMenuItem_Click(object sender, EventArgs e)
        {
           // 无操作toolStripButton_Click(null,null);
            if (!GuidePositioning_HDevelopExport.ObjectValided(GrabImg))
            {
                MessageBox.Show("未加载图像");
                return;
            }
            if (清除ToolStripMenuItem.Checked)
            { 
                清除ToolStripMenuItem.Checked = false;
                清除ToolStripMenuItem.BackColor = SystemColors.Control;
            }
            擦拭ToolStripMenuItem.Checked = !擦拭ToolStripMenuItem.Checked;
            if (擦拭ToolStripMenuItem.Checked)
            {
                擦拭ToolStripMenuItem.BackColor = Color.Green;
                currvisiontool.Focus();
                //BarPanSize.Enabled = false;
                //txbBarValue.Enabled = false;
                //cobxPanType.Enabled = false;
                IsStartDrawMask = true;
                eumworkType = EumworkType.擦拭;

            }
            else
            {
                擦拭ToolStripMenuItem.BackColor = SystemColors.Control;
                //BarPanSize.Enabled = true;
                //txbBarValue.Enabled = true;
                //cobxPanType.Enabled = true;
                IsStartDrawMask = false;
                eumworkType = EumworkType.None;
            }
               
        }

        private void 清除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
          //  无操作toolStripButton_Click(null, null);
            if (!GuidePositioning_HDevelopExport.ObjectValided(GrabImg))
            {
                MessageBox.Show("未加载图像");
                return;
            }
            if (擦拭ToolStripMenuItem.Checked)
            {
                擦拭ToolStripMenuItem.Checked = false;
                擦拭ToolStripMenuItem.BackColor = SystemColors.Control;
            }
            清除ToolStripMenuItem.Checked = !清除ToolStripMenuItem.Checked;
            if (清除ToolStripMenuItem.Checked)
            {
                清除ToolStripMenuItem.BackColor = Color.Red;
                currvisiontool.Focus();
                //BarPanSize.Enabled = false;
                //txbBarValue.Enabled = false;
                //cobxPanType.Enabled = false;
                IsStartDrawMask = true;
                eumworkType = EumworkType.清除;
            }
            else
            {
                清除ToolStripMenuItem.BackColor = SystemColors.Control;
                //BarPanSize.Enabled = true;
                //txbBarValue.Enabled = true;
                //cobxPanType.Enabled = true;
                IsStartDrawMask = false;
                eumworkType = EumworkType.None;
            }
               
        }

        private void uCvisionLayout1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                GuidePositioning_HDevelopExport.isContinue_drawing = true;
                ////////MouseMove----->MouseDown
                {
                    if (!IsStartDrawMask)
                        return;
                    if (eumworkType == EumworkType.擦拭)
                    {
                        if (eumPanType == EumPanType.圆)
                            GuidePositioning_HDevelopExport.AddRegion2(GrabImg, ref MarkRegion,
                                     currvisiontool.HWindowsHandle, panSize, true, originalContour);
                        else
                            GuidePositioning_HDevelopExport.AddRegion2(GrabImg, ref MarkRegion,
                                      currvisiontool.HWindowsHandle, panSize, false, originalContour);

                    }
                    else if (eumworkType == EumworkType.清除)
                    {
                        if (!GuidePositioning_HDevelopExport.ObjectValided(MarkRegion))
                        {
                            //MessageBox.Show("掩膜区域为空，无需清除!");
                            return;
                        }
                        else
                        {
                            HObject temregoion = null;
                            HOperatorSet.GenEmptyObj(out temregoion);
                            temregoion.Dispose();
                            HOperatorSet.CopyObj(MarkRegion, out temregoion, 1, -1);
                            if (eumPanType == EumPanType.圆)
                                GuidePositioning_HDevelopExport.Subregion2(GrabImg, temregoion, ref MarkRegion,
                                          currvisiontool.HWindowsHandle, panSize, true, originalContour);
                            else
                                GuidePositioning_HDevelopExport.Subregion2(GrabImg, temregoion, ref MarkRegion,
                                           currvisiontool.HWindowsHandle, panSize, false, originalContour);
                            temregoion.Dispose();
                        }
                    }
                    else
                    { }
                }

            }      
        }

        private void uCvisionLayout1_MouseUp(object sender, MouseEventArgs e)
        {
            GuidePositioning_HDevelopExport.isContinue_drawing = false;
            if(IsStartDrawMask)
            {
                currvisiontool.ClearAllOverLays(); 
                currvisiontool.DispImage(currvisiontool.D_HImage);
                HOperatorSet.SetDraw(currvisiontool.HWindowsHandle, "fill");
                currvisiontool.DispRegion(originalContour, "green");
                currvisiontool.AddregionBuffer(originalContour, "green");
                currvisiontool.DispRegion(MarkRegion, "orange");
                currvisiontool.AddregionBuffer(MarkRegion, "orange");
            }
          
        }

        private void uCvisionLayout1_MouseMove(object sender, MouseEventArgs e)
        {
            //if (!GuidePositioning_HDevelopExport.isContinue_drawing)
            //    return;
            //if (!IsStartDrawMask)
            //    return;
            //if (eumworkType == EumworkType.擦拭)
            //{
            //    if (eumPanType == EumPanType.Circle)
            //        GuidePositioning_HDevelopExport.AddRegion2(GrabImg, ref MarkRegion,
            //                 currvisiontool.HWindowsHandle, panSize, true, originalContour);
            //    else
            //        GuidePositioning_HDevelopExport.AddRegion2(GrabImg, ref MarkRegion,
            //                  currvisiontool.HWindowsHandle, panSize, false, originalContour);

            //}
            //else if (eumworkType == EumworkType.清除)
            //{
            //    if (!GuidePositioning_HDevelopExport.ObjectValided(MarkRegion))
            //    {
            //        //MessageBox.Show("掩膜区域为空，无需清除!");
            //        return;
            //    }                   
            //    else
            //    {
            //        HObject temregoion = null;
            //        HOperatorSet.GenEmptyObj(out temregoion);
            //        temregoion.Dispose();
            //        HOperatorSet.CopyObj(MarkRegion, out temregoion, 1, -1);
            //        if (eumPanType == EumPanType.Circle)
            //            GuidePositioning_HDevelopExport.Subregion2(GrabImg, temregoion, ref MarkRegion,
            //                      currvisiontool.HWindowsHandle, panSize, true, originalContour);
            //        else
            //            GuidePositioning_HDevelopExport.Subregion2(GrabImg, temregoion, ref MarkRegion,
            //                       currvisiontool.HWindowsHandle, panSize, false, originalContour);
            //        temregoion.Dispose();
            //    }
            //}
            //else
            //{ }

        }

        private void 放大toolStripButton_Click(object sender, EventArgs e)
        {
            eumworkType = EumworkType.放大;
            currvisiontool.scale_img();
        }

        private void 缩小toolStripButton_Click(object sender, EventArgs e)
        {
            eumworkType = EumworkType.缩小;
            currvisiontool.zoom_img();
        }

        private void 自适应toolStripButton_Click(object sender, EventArgs e)
        {
            eumworkType = EumworkType.自适应;

         
            currvisiontool.适应窗口();
        }

        private void 无操作toolStripButton_Click(object sender, EventArgs e)
        {
            eumworkType = EumworkType.None;
            currvisiontool.无操作ToolStripMenuItem_Click(null, null);
        }

        private void 平移toolStripButton_Click(object sender, EventArgs e)
        {
            eumworkType = EumworkType.平移;
            currvisiontool.平移ToolStripMenuItem_Click(null, null);
        }
    }
}
