using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MvCamCtrl.NET;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using FilesRAW.Common;
using OSLog;

namespace DeviceLib.Cam
{
    public class HKCam :  Icam
    {

        Log camLog = new Log("HKCam");
        const bool CO_FAIL = false;
        const bool CO_OK = true;
        int cam_index = -1; //相机索引编号
        bool isalive = false;
        int camNum = 0;

        string CamUserName = "HKCam";
        public event ImgGetHandle setImgGetHandle = null;
        public event EventHandler CamConnectHnadle = null;
        Bitmap ImgBuffer;
        public static string driveInfo = "";
        MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList; //相机列表
        private MyCamera m_pMyCamera ;   //相机句柄
        bool m_bGrabbing;  //是否开启采集，启用码流
        public bool IsGrabing { get => this.m_bGrabbing; }
       
        // MyCamera.cbOutputExdelegate ImageCallback;
        MyCamera.cbOutputdelegate ImageCallback;

        IntPtr pTemp = IntPtr.Zero;
        IntPtr pImageBuf = IntPtr.Zero;


        long imageWidth = 0;
        public long ImageWidth
        {

            get
            { return this.imageWidth; }
        }
        //  public long ImageHeight => throw new NotImplementedException();
        long imageHeight = 0;
        public long ImageHeight
        {
            get
            { return this.imageHeight; }
        }

        public int CamNum { get => camNum;  }
        public bool IsAlive { get => isalive; }
        public int CamIndex { get => cam_index;  }
        public CamType currCamType { get ;}

        public HKCam(string userName)
        {
            currCamType = CamType.海康;
         
             CamUserName = userName;
       
            DeviceListAcq();
        }

        //~HKCam()
        //{
        //    CloseCam();
        //    if (ImgBuffer != null)
        //        ImgBuffer.Dispose();
        //}

        /// <summary>
        /// 查找相机
        /// </summary>
        public bool DeviceListAcq()
        {
            int nRet = -1;
            // ch:创建设备列表 || en: Create device list
            try
            {
                if (driveInfo.Contains("MvCameraControl"))
                    return false;
                m_pDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST(); //相机列表
                nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE, ref m_pDeviceList);
                m_pMyCamera = new MyCamera();   //相机句柄
            }
            catch (Exception er)
            {
                driveInfo = er.Message;
                return false;
            }

            if (0 != nRet)
            {
             
                return false;
            }
            camNum = (int)m_pDeviceList.nDeviceNum;
            return true;          
        }
        
        /// <summary>
        /// 打开相机
        /// </summary>
        /// <param name="camIndex"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public bool OpenCam(int camIndex, ref string msg)
        {
            bool getcamfalg= DeviceListAcq();
            if (!getcamfalg)
            {
                msg += "相机查找失败";
              
                return false;//相机查找失败
            }
            if (m_pDeviceList.nDeviceNum == 0 || m_pDeviceList.nDeviceNum <= camIndex)
            {
                msg += "No device,please select";
             
                return false;
            }
            int nRet = -1;

            //ch:获取选择的设备信息 | en:Get selected device information
            MyCamera.MV_CC_DEVICE_INFO device =
                (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[camIndex],
                                                              typeof(MyCamera.MV_CC_DEVICE_INFO));
            //创建相机
            nRet = m_pMyCamera.MV_CC_CreateDevice_NET(ref device);
            if (MyCamera.MV_OK != nRet)
            {
             
                return false;
            }

            // ch:打开设备 | en:Open device
            nRet = m_pMyCamera.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
             
                msg += "Open Device Fail";
                return false;
            }

            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
         
            // ch:获取高 || en: Get Height
            nRet = m_pMyCamera.MV_CC_GetIntValue_NET("Height", ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                msg += "Get Height Fail";
                return false;
            }
            imageHeight = (int)stParam.nCurValue;

            // ch:获取宽 || en: Get Width
            nRet = m_pMyCamera.MV_CC_GetIntValue_NET("Width", ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                msg += "Get Width Fail";
                return false;
            }
            imageWidth = (int)stParam.nCurValue;

            //// ch:获取帧率 || en: Get AcquisitionFrameRate
            //MyCamera.MVCC_FLOATVALUE stParam2 = new MyCamera.MVCC_FLOATVALUE();
            //// nRet = m_pMyCamera.MV_CC_GetIntValue_NET("AcquisitionFrameRate", ref stParam);
            //nRet = m_pMyCamera.MV_CC_GetFloatValue_NET("ResultingFrameRate", ref stParam2);
            //if (MyCamera.MV_OK != nRet)
            //{
            //    msg += "Get ResultingFrameRate Fail";
            //    return false;
            //}
            //d_stucamInfo.imgFPS = (int)FPS;

            // ch:设置触发模式为off || en:set trigger mode as off
            m_pMyCamera.MV_CC_SetEnumValue_NET("AcquisitionMode", 2);     //设置采集连续模式
            m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerMode", 0); 

            /**********************************************************************************************************/
            // ch:注册回调函数 | en:Register image callback
            ImageCallback = new MyCamera.cbOutputdelegate(GrabImage);
            // ImageCallback = new MyCamera.cbOutputExdelegate(GrabImage);
            nRet = m_pMyCamera.MV_CC_RegisterImageCallBack_NET(ImageCallback, IntPtr.Zero);
            // nRet = m_pMyCamera.MV_CC_RegisterImageCallBackForRGB_NET(ImageCallback, IntPtr.Zero);
            if (MyCamera.MV_OK != nRet)
            {
                msg += "Register Image CallBack Fail";
                return false;
            }
            /**********************************************************************************************************/
            isalive = true;
            cam_index = camIndex;
            if (CamConnectHnadle != null)
                CamConnectHnadle(isalive, null);
            return true;
        }

        /// <summary>
        /// 码流函数
        /// </summary>
        /// <param name="pData"></param>
        /// <param name="pFrameInfo"></param>
        /// <param name="pUser"></param>
        private void GrabImage(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO pFrameInfo, IntPtr pUser)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            int nImageBufSize = 0;
            int nRet = MyCamera.MV_OK;
            if (pData != null)
            {
             
                if (IsColorPixelFormat(pFrameInfo.enPixelType))//彩色
                {
                    //////
                    if (pFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed)
                    {
                        pTemp = pData;
                    }
                    else
                    {
                        if (IntPtr.Zero == pImageBuf || nImageBufSize < (pFrameInfo.nWidth * pFrameInfo.nHeight * 3))
                        {
                            if (pImageBuf != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(pImageBuf);
                                pImageBuf = IntPtr.Zero;
                            }

                            pImageBuf = Marshal.AllocHGlobal((int)pFrameInfo.nWidth * pFrameInfo.nHeight * 3);
                            if (IntPtr.Zero == pImageBuf)
                            {
                                camLog.Error(strfucName, "图像源数据为空");
                                return;
                            }
                            nImageBufSize = pFrameInfo.nWidth * pFrameInfo.nHeight * 3;
                        }

                        MyCamera.MV_PIXEL_CONVERT_PARAM stPixelConvertParam = new MyCamera.MV_PIXEL_CONVERT_PARAM();

                        stPixelConvertParam.pSrcData = pData;//源数据
                        stPixelConvertParam.nWidth = pFrameInfo.nWidth;//图像宽度
                        stPixelConvertParam.nHeight = pFrameInfo.nHeight;//图像高度
                        stPixelConvertParam.enSrcPixelType = pFrameInfo.enPixelType;//源数据的格式
                        stPixelConvertParam.nSrcDataLen = pFrameInfo.nFrameLen;

                        stPixelConvertParam.nDstBufferSize = (uint)nImageBufSize;
                        stPixelConvertParam.pDstBuffer = pImageBuf;//转换后的数据
                        stPixelConvertParam.enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed;
                        nRet = m_pMyCamera.MV_CC_ConvertPixelType_NET(ref stPixelConvertParam);//格式转换
                        if (MyCamera.MV_OK != nRet)
                        {
                            camLog.Error(strfucName, "图像格式转换错误");
                            return;
                        }
                        pTemp = pImageBuf;
                    }
                    try
                    {
                        ImgBuffer = new Bitmap(pFrameInfo.nWidth, pFrameInfo.nHeight, pFrameInfo.nWidth * 3,
                                                      PixelFormat.Format24bppRgb, pTemp);
                        setImgGetHandle?.Invoke(ImgBuffer);
                    }
                    catch(Exception ex)
                    {
                        camLog.Error(strfucName, ex.Message);
                    }                                    
                }
                else if (IsMonoData(pFrameInfo.enPixelType))//黑白
                {
                    if (pFrameInfo.enPixelType == MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8)
                    {
                        pTemp = pData;
                    }
                    else
                    {
                        if (IntPtr.Zero == pImageBuf || nImageBufSize < (pFrameInfo.nWidth * pFrameInfo.nHeight))
                        {
                            if (pImageBuf != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(pImageBuf);
                                pImageBuf = IntPtr.Zero;
                            }

                            pImageBuf = Marshal.AllocHGlobal((int)pFrameInfo.nWidth * pFrameInfo.nHeight);
                            if (IntPtr.Zero == pImageBuf)
                            {
                                camLog.Error(strfucName, "图像源数据为空");
                                return;
                            }
                            nImageBufSize = pFrameInfo.nWidth * pFrameInfo.nHeight;
                        }

                        MyCamera.MV_PIXEL_CONVERT_PARAM stPixelConvertParam = new MyCamera.MV_PIXEL_CONVERT_PARAM();

                        stPixelConvertParam.pSrcData = pData;//源数据
                        stPixelConvertParam.nWidth = pFrameInfo.nWidth;//图像宽度
                        stPixelConvertParam.nHeight = pFrameInfo.nHeight;//图像高度
                        stPixelConvertParam.enSrcPixelType = pFrameInfo.enPixelType;//源数据的格式
                        stPixelConvertParam.nSrcDataLen = pFrameInfo.nFrameLen;

                        stPixelConvertParam.nDstBufferSize = (uint)nImageBufSize;
                        stPixelConvertParam.pDstBuffer = pImageBuf;//转换后的数据
                        stPixelConvertParam.enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
                        nRet = m_pMyCamera.MV_CC_ConvertPixelType_NET(ref stPixelConvertParam);//格式转换
                        if (MyCamera.MV_OK != nRet)
                        {
                            camLog.Error(strfucName, "图像格式转换错误");
                            return;
                        }
                        pTemp = pImageBuf;
                    }

                    try
                    {
                      
                        ImgBuffer = new Bitmap(pFrameInfo.nWidth, pFrameInfo.nHeight, pFrameInfo.nWidth * 1, 
                                                              PixelFormat.Format8bppIndexed, pTemp);
                        ColorPalette cp = ImgBuffer.Palette;
                        // init palette
                        for (int i = 0; i < 256; i++)
                        {
                            cp.Entries[i] = Color.FromArgb(i, i, i);
                        }
                        // set palette back
                        ImgBuffer.Palette = cp;
                        setImgGetHandle?.Invoke(ImgBuffer);
                      
                    }
                    catch (System.Exception ex)
                    {
                        camLog.Error(strfucName, ex.Message);                     
                    }
                   
                }
                else
                {
                    
                    camLog.Error(strfucName, "图像格式异常!"); 
                }        
               // GC.Collect();
               GC.GetTotalMemory(true);//需要实时将图像的内存回收掉，防止占用过多CPU资源控制系统垃圾回收器，专制内存不足   
            }
            return;
        }

        //判断是否为彩色图像
        private bool IsColorPixelFormat(MyCamera.MvGvspPixelType enType)
        {
            switch (enType)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BGR8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_RGBA8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BGRA8_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YUV422_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_YUV422_YUYV_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGB12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerBG12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerRG12_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_BayerGR12_Packed:
                    return true;
                default:
                    return false;
            }
        }
        //判断是否为黑白图像
        private Boolean IsMonoData(MyCamera.MvGvspPixelType enGvspPixelType)
        {
            switch (enGvspPixelType)
            {
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono10_Packed:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12:
                case MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono12_Packed:
                    return true;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 停止采集
        /// </summary>
        /// <param name="msg"></param>
        public void StopGrab()
        {
            if (!m_bGrabbing)
                return;
            int nRet = -1;
            // ch:停止抓图 || en:Stop grab image
            nRet = m_pMyCamera.MV_CC_StopGrabbing_NET();
            //if (nRet != MyCamera.MV_OK)
            //{
            //    msg += "Stop Grabbing Fail";
            //    return;
            //}
            m_bGrabbing = false;


        }

        /// <summary>
        /// 关闭相机
        /// </summary>
        /// <returns></returns>
        public void CloseCam()
        {
            try
            {
                if (!isalive)
                    return;
                if (m_bGrabbing)
                {
                    m_bGrabbing = false;
                    // ch:停止抓图 || en:Stop grab image
                    m_pMyCamera.MV_CC_StopGrabbing_NET();
                }
                //int t1 = Environment.TickCount;
                //while(Environment.TickCount-t1<3000)
                //{
                //    System.Windows.Forms.Application.DoEvents();
                //    System.Threading.Thread.Sleep(50);
                //}
                //关闭相机
                m_pMyCamera.MV_CC_CloseDevice_NET();
                //销毁相机
                m_pMyCamera.MV_CC_DestroyDevice_NET();

                isalive = false;
                cam_index = -1;
                if (CamConnectHnadle != null)
                    CamConnectHnadle(isalive, null);
            }
            catch { };
        }

        /// <summary>
        /// 连续采集
        /// </summary>
        public bool ContinueGrab()
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerMode", 0);
            int nRet = 0;
            if (!m_bGrabbing)
                nRet = m_pMyCamera.MV_CC_StartGrabbing_NET(); // ch:开启抓图 | en:start grab                    
            if (MyCamera.MV_OK != nRet)
            {
                camLog.Error(strfucName, "Start Grabbing Fail");         
                return CO_FAIL;
            }
            else
            {
                m_bGrabbing = true;
                return CO_OK;
            }
             
        }

        /// <summary>
        /// 单次采集
        /// </summary>
        /// <param name="msg"></param>
        public bool OneShot()
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerMode", 1);
            // ch: 触发源设为软触发 || en: set trigger mode as Software
            m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerSource", 7);
            int nRet=0;
            // ch:开启抓图 | en:start grab
           if(!m_bGrabbing)
             nRet = m_pMyCamera.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                camLog.Error(strfucName, "Start Grabbing Fail");
                return false;
            }
            m_bGrabbing = true;
            // ch: 触发命令 || en: Trigger command
            nRet = m_pMyCamera.MV_CC_SetCommandValue_NET("TriggerSoftware");         
            
            if (MyCamera.MV_OK != nRet)
            {
                camLog.Error(strfucName, "Trigger Fail");              
                return CO_FAIL;
            }
            else
                return CO_OK;
        }

        /******************    相机参数设置   ********************/
        //设曝光
        public bool SetExposureTime(long dValue)
        {

            if (!isalive)
                return false;
        
            m_pMyCamera.MV_CC_SetEnumValue_NET("ExposureAuto", 0);
            int nRet = m_pMyCamera.MV_CC_SetFloatValue_NET("ExposureTime", (float)dValue);
            if (MyCamera.MV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;        
        }

        //设置增益
        public bool SetGain(long dValue)
        {
            if (!isalive)
                return false;
           
            m_pMyCamera.MV_CC_SetEnumValue_NET("GainAuto", 0);
            int nRet = m_pMyCamera.MV_CC_SetFloatValue_NET("Gain", (float)dValue);

            if (MyCamera.MV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }

        //查询曝光：dValue曝光值
        public bool GetExposureTime(out long dValue)
        {

            dValue = -999;
            if (!isalive)
                return false;
            float fExposure = 0;
            if (!GetFloatValue("ExposureTime", ref fExposure))
                return false;
            dValue = (long)fExposure;
            return CO_OK;
        }

        //查询增益：dValue增益值
        public bool GetGain(out long dValue)
        {
            dValue = -999;
            if (!isalive)
                return false;
            float fGain = 0;
            if (!GetFloatValue("Gain", ref fGain))
                return false;
            dValue = (long)fGain;
            return CO_OK;
        }
        
       public bool SaveCamParmas()
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            if (!isalive)
                return false;
            int nRet = m_pMyCamera.MV_CC_SetCommandValue_NET("UserSetSave");

            if (MyCamera.MV_OK != nRet)
            {
                return CO_FAIL;
            }
            return CO_OK;
        }


        /****************************************************************************
         * @fn           GetFloatValue
         * @brief        获取Float型参数值
         * @param        strKey                IN        参数键值，具体键值名称参考HikCameraNode.xls文档
         * @param        pValue                OUT       返回值
         * @return       成功：0；错误：-1
         ****************************************************************************/
        private bool GetFloatValue(string strKey, ref float pfValue)
        {
            MyCamera.MVCC_FLOATVALUE stParam = new MyCamera.MVCC_FLOATVALUE();
            int nRet = m_pMyCamera.MV_CC_GetFloatValue_NET(strKey, ref stParam);
            if (MyCamera.MV_OK != nRet)
            {
                return CO_FAIL;
            }

            pfValue = stParam.fCurValue;

            return CO_OK;
        }
            

        public void Dispose()
        {
            CloseCam();
            if (ImgBuffer != null)
                ImgBuffer.Dispose();         
        }
    }
    public struct stucamInfo
    {
        public int imgWidth;
        public int imgHeight;
        public int imgFPS;
    }
}
