using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using ThridLibray;
using FilesRAW.Common;
using OSLog;

namespace DeviceLib.Cam
{
    /// <summary>
    /// 大华相机
    /// </summary>
     public  class CCD_DaHua:Icam
    {
        Log camLog;
        Thread continueGrapThread;           /*连续采集图像线程*/
         const int DEFAULT_INTERVAL = 40;       /*控制采集帧率25FPS*/
         Stopwatch m_stopWatch = new Stopwatch();    /* 时间统计器 */
         private IDevice m_dev;    /* 设备对象 */
        string CamUserName = "DHCam";
        public event ImgGetHandle setImgGetHandle = null;
        public event EventHandler CamConnectHnadle = null;
        Bitmap ImgBuffer;

        int cam_index = -1; //相机索引编号
        bool isalive = false;
        int camNum = 0;

        public int CamNum { get => camNum;}
        public bool IsAlive { get => isalive; }
        public int CamIndex { get => cam_index; }
        public CamType currCamType { get;  }

        public CCD_DaHua(string userName)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            currCamType = CamType.大华;
        
            CamUserName = userName;
            camLog = new Log(CamUserName);
            try
            {
                camNum = Enumerator.EnumerateDevices().Count;
            }
            catch(Exception er)
            {
                camLog.Error(strfucName, er.Message);
            }          
        }

        public bool IsGrabing
        {
            get
            {
                if (m_dev == null) return false;
                return m_dev.StreamGrabber.IsStart;
            }
        }
        //~CCD_DaHua()
        //{
        //    CloseCam();
        //    if (ImgBuffer != null)
        //    {
        //        ImgBuffer.Dispose();
        //    }

        //}

        public bool  OpenCam(int camIndex, ref string Msg)
        {
            try
            {            
                /* 设备搜索 */
                List<IDeviceInfo> lstCam = Enumerator.EnumerateDevices();
                if (lstCam.Count == 0)
                {
                    Msg = "未发现可用相机";
                
                    return false;
                }
                if (camIndex >= lstCam.Count)
                {
                    Msg = "相机索引超出范围，请重新设置相机索引！";
                
                    return false;
                }
              
                /* 根据索引获取 */
                m_dev = Enumerator.GetDeviceByIndex(camIndex);

                /* 注册链接事件 */
                m_dev.CameraOpened += OnCameraOpen;
                m_dev.ConnectionLost += OnConnectLoss;
                m_dev.CameraClosed += OnCameraClose;

                /* 打开设备 */
                if (!m_dev.Open())
                {
                    Msg = "相机打开失败";
                    return false;
                }

                /* 打开Software Trigger */
                m_dev.TriggerSet.Open(TriggerSourceEnum.Software);

                /* 设置图像格式 */
                using (IEnumParameter p = m_dev.ParameterCollection[ParametrizeNameSet.ImagePixelFormat])
                {
                    p.SetValue("Mono8");
                }
                /* 设置缓存个数为8（默认值为16） */
                m_dev.StreamGrabber.SetBufferCount(8);

                /* 注册码流回调事件 */
                m_dev.StreamGrabber.ImageGrabbed += OnImageGrabbed;
                m_dev.StreamGrabber.GrabStarted += new EventHandler<EventArgs>(GrabStartedEvent);
                m_dev.StreamGrabber.GrabStoped += new EventHandler<EventArgs>(GrabStopedEvent);
                /* 开启码流 */
                if (!m_dev.GrabUsingGrabLoopThread())
                {
                    Msg = "开启码流失败";
                 
                    return false ;
                }
                isalive = true;
                cam_index = camIndex;
                if (CamConnectHnadle != null)
                    CamConnectHnadle(isalive, null);
                return true;
            }
            catch (Exception er)
            {
                Msg = er.Message;
                isalive = false;
             
                if (CamConnectHnadle != null)
                    CamConnectHnadle(isalive, null);
                return false;
            }
        
        }

        /*码流开始*/
        void GrabStartedEvent(object sender, EventArgs e)
        {
            m_stopWatch.Reset();
        }

        /*码流结束*/
        void GrabStopedEvent(object sender, EventArgs e)
        {
            m_stopWatch.Reset();
        }


        /* 相机打开回调 */
        private void OnCameraOpen(object sender, EventArgs e)
        {
          
        }

        /* 相机关闭回调 */
        private void OnCameraClose(object sender, EventArgs e)
        {
           
        }

        /* 相机丢失回调 */
        private void OnConnectLoss(object sender, EventArgs e)
        {         
            //SetCamLost();
        }

        /* 码流数据回调 */
        private void OnImageGrabbed(Object sender, GrabbedEventArgs e)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
                  
             IGrabbedRawData frame = e.GrabResult.Clone();
            // Check if the image can be displayed.
            if (frame.ImageSize > 0)
            {
               // HImage himage = new HImage();
                try
                {
                    if (!m_stopWatch.IsRunning || m_stopWatch.ElapsedMilliseconds > DEFAULT_INTERVAL)
                    {
                        m_stopWatch.Restart();
                        ImgBuffer = frame.ToBitmap(false);
                        //如果是黑白图像，则利用Halcon图像库中的GenImage1算子来构建图像
                        //himage.GenImage1("byte", (int)frame.Width, (int)frame.Height, e.GrabResult.Raw);
                        //ImgBuffer.Dispose();
                        //HOperatorSet.CopyObj(himage, out ImgBuffer, 1, 1);
                        setImgGetHandle?.Invoke(ImgBuffer);
                    }

                }
                catch (Exception exception)
                {
                    camLog.Error(strfucName, exception.Message);
                }
                finally
                {
                //    himage.Dispose();
                //    himage = null;
                }
                /* 主动调用回收垃圾 */
                GC.Collect();
            }
                    
        }    

        /// <summary>
        /// 单次采集
        /// </summary>
        /// <returns></returns>
        public    bool OneShot()
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            if (m_dev == null)
            {
                camLog.Info(strfucName, "相机不存在");
                return false;
            }
            try
            {
                m_dev.ExecuteSoftwareTrigger();
                return true;
            }
            catch (Exception exception)
            {
                camLog.Info(strfucName, exception.Message);
                return false;
            }
        }

        /// <summary>
        /// 连续采集
        /// </summary>
        /// <returns></returns>
        public  bool ContinueGrab()
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            if (m_dev == null)
            {
                camLog.Info(strfucName, "Device is invalid");
               return false;
            }
            if (continueGrapThread == null)
                continueGrapThread = new Thread(continueGrapLoop);
            continueGrapThread.IsBackground = true;
            continueGrapThread.Start();
            return true;
        }
          
        void continueGrapLoop()
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            while (true)
            {
                try
                {
                    m_dev.ExecuteSoftwareTrigger();
                }
                catch(Exception er)
                {
                    camLog.Info(strfucName, er.Message);
                }
                Thread.Sleep(5);
            
            }     
        }
          /// <summary>
         /// 停止采集
          /// </summary>
          /// <returns></returns>
        public  void StopGrab()
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {             
                if (continueGrapThread != null)
                {
                    continueGrapThread.Abort();
                    continueGrapThread = null;
                }
              
            }
            catch (Exception exception)
            {
                camLog.Info(strfucName, exception.Message);
               
            }
        }

        /// <summary>
        /// 关闭相机
        /// </summary>
        public void CloseCam()
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            if (m_dev == null) { return; }
            try
            {
                StopGrab();
                if (m_dev.IsOpen)
                {               
                    m_dev.StreamGrabber.ImageGrabbed -= OnImageGrabbed;         /* 反注册回调 */
                    m_dev.ShutdownGrab();                                       /* 停止码流 */                  
                    m_dev.Close();                                              /* 关闭相机 */
                    m_dev.Dispose();                                             /* 资源释放 */
                    
                }
                isalive = false;
                cam_index = -1;
                if (CamConnectHnadle != null)
                    CamConnectHnadle(isalive, null);
            }
            catch (Exception exception)
            {
                camLog.Info(strfucName, exception.Message);
            }
        }

        /******************    相机参数设置   ********************/
     
        /// <summary>
        /// 设置相机曝光时间
        /// </summary>
        /// <param name="value"></param>
        public bool SetExposureTime(long value=1000)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                using (IFloatParameter p = m_dev.ParameterCollection[ParametrizeNameSet.ExposureTime])
                {
                    p.SetValue(value);
                }
                return true;
            }
            catch (Exception e)
            {
                camLog.Error(strfucName, e.Message);
                return false;
            }
        }
        public bool GetExposureTime(out long ExposureTime)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                using (IFloatParameter p = m_dev.ParameterCollection[ParametrizeNameSet.ExposureTime])
                {
                    ExposureTime= (long)p.GetValue();
                }
                return true;
            }
            catch (Exception e)
            {
                ExposureTime = 0;
                camLog.Error(strfucName, e.Message);
                return false;
            }
        }
        /// <summary>
        /// 设置增益
        /// </summary>
        /// <param name="value"></param>
        public bool SetGain(long value=0)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                /* 设置增益 */
                using (IFloatParameter p = m_dev.ParameterCollection[ParametrizeNameSet.GainRaw])
                {
                    p.SetValue(value);
                }
                return true;
            }
            catch (Exception e)
            {
                camLog.Error(strfucName, e.Message);
                return false;
            }
        }
     
        public bool GetGain(out long Gain)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            { /* 设置增益 */
                using (IFloatParameter p = m_dev.ParameterCollection[ParametrizeNameSet.GainRaw])
                {
                    Gain= (long)p.GetValue();
                }
              
                return true;
            }
            catch (Exception e)
            {
                Gain = 0;
                camLog.Error(strfucName, e.Message);
                return false;
            }
        }

      

        public void Dispose()
        {
            CloseCam();
            if (ImgBuffer != null)
            {
                ImgBuffer.Dispose();
            }
        }

    }
}
