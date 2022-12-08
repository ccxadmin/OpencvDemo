using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Basler.Pylon;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FilesRAW.Common;
using System.IO;
using OSLog;

namespace DeviceLib.Cam
{
    /// <summary>
    /// Basler相机
    /// </summary>
   public class CCD_BaslerGIGE: Icam
    {

        Log camLog = new Log("BaslerGIGE");
        Camera m_Cam;
        private PixelDataConverter converter = new PixelDataConverter();
        static Version Sfnc2_0_0 = new Version(2, 0, 0); /// if >= Sfnc2_0_0,说明是ＵＳＢ３的相机
        private Stopwatch stopWatch = new Stopwatch();
        private IntPtr dataPointer = IntPtr.Zero;
        private Bitmap ImgBuffer; 
        public event ImgGetHandle setImgGetHandle = null;
        public event EventHandler CamConnectHnadle = null;
        public long minExposureTime,  maxExposureTime;
        public long minGain, maxGain;
        private int camNum = 0;
        private long grabTime = 0;          // 采集图像时间

        string CamUserName = "BaslerGIGE";

        int cam_index = -1; //相机索引编号
        bool isalive = false;
        int t1 = Environment.TickCount;
            
        public bool IsAlive { get => isalive;          
        }
        //驱动信息
        public static string driveInfo = "";
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
        public int CamIndex
        {
            get => cam_index;
           
        }
       
        public CamType currCamType { get;  }

        public CCD_BaslerGIGE(string userName)
        {
            currCamType = CamType.巴斯勒;
           
            CamUserName = userName;
      
            if (driveInfo.Contains("外部组件"))
                return;
            camNum = CameraFinder.Enumerate().Count;

        }

        public bool IsGrabing
        {
            get
            {
                if (m_Cam == null) return false;
                return m_Cam.StreamGrabber.IsGrabbing;
            }
        }
        //~CCD_BaslerGIGE()
        //{
        //    try
        //    {
        //        CloseCam();
        //        if (image != null)
        //        {
        //            image.Dispose();
        //        }
        //    }
        //    catch { };
        //}

        /******************    相机操作     ******************/
        public bool OpenCam(int camIndex,ref string Msg)
        {         
            try
            {
                List<ICameraInfo> lstCam = CameraFinder.Enumerate();
             
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
                ICameraInfo camInfo = lstCam[camIndex];
                m_Cam = new Camera(camInfo);
               
                if (m_Cam.IsOpen || m_Cam.IsConnected)
                {
                    Msg = "此相机已打开";
                    cam_index = camIndex;
                    return false;
                }

                m_Cam.ConnectionLost += m_Cam_ConnectionLost;
                m_Cam.CameraOpened += Configuration.AcquireContinuous;
                m_Cam.StreamGrabber.ImageGrabbed += StreamGrabber_ImageGrabbed;
                m_Cam.StreamGrabber.GrabStarted += StreamGrabber_GrabStarted;
                m_Cam.StreamGrabber.GrabStopped += StreamGrabber_GrabStopped;
                m_Cam.Open();
                imageWidth = m_Cam.Parameters[PLCamera.Width].GetValue();               // 获取图像宽 
                imageHeight = m_Cam.Parameters[PLCamera.Height].GetValue();              // 获取图像高
                GetMinMaxExposureTime(out minExposureTime,out maxExposureTime);
                GetMinMaxGain(out minGain,out maxGain);
                m_Cam.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.SingleFrame);
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
        public bool isBusy()
        {
            return m_Cam.StreamGrabber.IsGrabbing;
          
        }
        public bool OneShot()
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                if (m_Cam.StreamGrabber.IsGrabbing)
                {
                    m_Cam.StreamGrabber.Stop();
                    camLog.Error(strfucName, "相机已在Busy状态,开启了停止,然后重新采集");
                    System.Threading.Thread.Sleep(10);
                }

                camLog.Info(strfucName, "相机开启了单帧采集！");

                m_Cam.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.SingleFrame);
                m_Cam.StreamGrabber.Start(1, GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
            }
            catch (Exception e)
            {
                camLog.Error(strfucName, e.Message);
                return false;
            }
            Console.WriteLine("单帧采集完成");
            return true;
        }
        /// <summary>
        /// 连续采集
        /// </summary>
        /// <returns></returns>
        public  bool ContinueGrab()
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                if (m_Cam.StreamGrabber.IsGrabbing)
                {
                    m_Cam.StreamGrabber.Stop();
                    camLog.Error(strfucName, "相机已在Busy状态,开启了停止,然后重新采集");
                    System.Threading.Thread.Sleep(10);
                }

                camLog.Info(strfucName, "相机开启了连续采集！");

                m_Cam.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                m_Cam.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber); ;

            }
            catch (Exception e)
            {
                camLog.Error(strfucName, e.Message);
                return false;
            }
            Console.WriteLine("连续采集开始");
            return true;
        }
        public  void StopGrab()
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                if (m_Cam.StreamGrabber.IsGrabbing)
                {
                    m_Cam.StreamGrabber.Stop();
                    System.Threading.Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                camLog.Error(strfucName, e.Message);
            }
        }          
        public void CloseCam()
        {
            if (m_Cam == null) { return; }
            if (m_Cam.IsOpen)
            {
                if (m_Cam.StreamGrabber.IsGrabbing)
                    m_Cam.StreamGrabber.Stop();
                m_Cam.Close();
                m_Cam = null;
            }

            if (dataPointer != null)
            {
                Marshal.FreeHGlobal(dataPointer);
                dataPointer = IntPtr.Zero;
            }
            isalive = false;
            cam_index = -1;
            if (CamConnectHnadle != null)
                CamConnectHnadle(isalive, null);
        }

        /****************  图像响应事件函数  ****************/
        // 相机取像回调函数.
        void StreamGrabber_GrabStopped(object sender, GrabStopEventArgs e)
        {
            stopWatch.Reset();
        }
        void StreamGrabber_GrabStarted(object sender, EventArgs e)
        {
            stopWatch.Reset();
        }
        int ta = 0;
        void StreamGrabber_ImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            //Console.WriteLine(Environment.TickCount-ta);
            //int time = Environment.TickCount - ta;
            //camLog.Info("时间", time.ToString());
            //ta = Environment.TickCount;
         
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
 
            //HObject imgBuffer = null;
            //HOperatorSet.GenEmptyObj(out imgBuffer);
            try
            {
                // Acquire the image from the camera. Only show the latest image. The camera may acquire images faster than the images can be displayed.
                // Get the grab result.
                // System.Diagnostics.Debug.WriteLine(string.Format("T:{0}",Environment.TickCount-t1));
                IGrabResult grabResult = e.GrabResult;
                // Check if the image can be displayed.
                if (grabResult.IsValid)
                {
                    // Reduce the number of displayed images to a reasonable amount if the camera is acquiring images very fast.
                    if (!stopWatch.IsRunning || stopWatch.ElapsedMilliseconds > 33)
                    {
                        stopWatch.Restart();
                       
                        // 判断是否是黑白图片格式
                      //  if (grabResult.PixelTypeValue == PixelType.Mono8)
                        {
                            //allocate the m_stream_size amount of bytes in non-managed environment 
                          

                            ImgBuffer = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format32bppRgb);
                            // Lock the bits of the bitmap.
                            BitmapData bmpData = ImgBuffer.LockBits(new Rectangle(0, 0, ImgBuffer.Width, ImgBuffer.Height), ImageLockMode.ReadWrite, ImgBuffer.PixelFormat);
                            // Place the pointer to the buffer of the bitmap.
                            converter.OutputPixelFormat = PixelType.BGRA8packed;
                            IntPtr ptrBmp = bmpData.Scan0;
                            converter.Convert(ptrBmp, bmpData.Stride * ImgBuffer.Height, grabResult);
                            ImgBuffer.UnlockBits(bmpData);
                        }
                                            
                        setImgGetHandle?.Invoke(ImgBuffer);
             
                    }
                }
            }
            catch (Exception exception)
            {
                camLog.Error(strfucName, exception.Message);
            }
            finally
            {
                // Dispose the grab result if needed for returning it to the grab loop.
                e.DisposeGrabResultIfClone();
               
              //  GC.Collect();
            }
        
        }              
        
        void m_Cam_ConnectionLost(object sender, EventArgs e)
        {
            //SetCamLost();
        }
        /******************    相机参数设置   ********************/
        /// <summary>
        /// 设置Gige相机心跳时间
        /// </summary>
        /// <param name="value"></param>
        public void SetHeartBeatTime(long value)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                // 判断是否是网口相机
                if (m_Cam.GetSfncVersion() < Sfnc2_0_0)
                {
                    m_Cam.Parameters[PLGigECamera.GevHeartbeatTimeout].SetValue(value);
                }
            }
            catch (Exception e)
            {
                camLog.Error(strfucName,e.Message);          
            }
        }

        /// <summary>
        /// 设置相机曝光时间
        /// </summary>
        /// <param name="value"></param>
        public bool SetExposureTime(long value)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                //要手动设置曝光时间 需要把自动曝光参数设置为false  把相机曝光模式设置为延时曝光模式
                m_Cam.Parameters[PLCamera.ExposureAuto].TrySetValue(PLCamera.ExposureAuto.Off);
               // m_Cam.Parameters[PLCamera.ExposureMode].TrySetValue(PLCamera.ExposureMode.Timed);

                if (m_Cam.GetSfncVersion() < Sfnc2_0_0)
                {
                    //long min = m_Cam.Parameters[PLCamera.ExposureTimeRaw].GetMinimum();
                    //long max = m_Cam.Parameters[PLCamera.ExposureTimeRaw].GetMaximum();
                    //long incr = m_Cam.Parameters[PLCamera.ExposureTimeRaw].GetIncrement();
                    //if (value < min)
                    //{
                    //    value = min;
                    //}
                    //else if (value > max)
                    //{
                    //    value = max;
                    //}
                    //else
                    //{
                    //    value = min + (((value - min) / incr) * incr);
                    //}
                    //long tem = value / 35;
                    //value = tem * 35;
                    m_Cam.Parameters[PLCamera.ExposureTimeRaw].SetValue(value);
                }
                else
                {
                    m_Cam.Parameters[PLUsbCamera.ExposureTime].SetValue((double)value);
                }
                return true;
            }
            catch (Exception e)
            {
                camLog.Error(strfucName, e.Message);
                return false;
            }
        }

        /// <summary>
        /// 获取最小最大曝光时间
        /// </summary>
        public bool GetMinMaxExposureTime(out long minExposureTime,out long maxExposureTime)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                if (m_Cam.GetSfncVersion() < Sfnc2_0_0)
                {
                    minExposureTime = m_Cam.Parameters[PLCamera.ExposureTimeRaw].GetMinimum();
                    maxExposureTime = m_Cam.Parameters[PLCamera.ExposureTimeRaw].GetMaximum();
                }
                else
                {
                    minExposureTime = (long)m_Cam.Parameters[PLUsbCamera.ExposureTime].GetMinimum();
                    maxExposureTime = (long)m_Cam.Parameters[PLUsbCamera.ExposureTime].GetMaximum();
                }
                return true;
            }
            catch (Exception e)
            {
                minExposureTime = 0;
                maxExposureTime = 0;
                camLog.Error(strfucName, e.Message);
                return false;
            }
        }

        public bool GetExposureTime(out long ExposureTime)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                if (m_Cam.GetSfncVersion() < Sfnc2_0_0)
                {
                    ExposureTime = m_Cam.Parameters[PLCamera.ExposureTimeRaw].GetValue();
                  
                }
                else
                {
                    ExposureTime = (long)m_Cam.Parameters[PLUsbCamera.ExposureTime].GetValue();
                   
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
        public bool SetGain(long value)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                m_Cam.Parameters[PLCamera.GainAuto].TrySetValue(PLCamera.GainAuto.Off);

                if (m_Cam.GetSfncVersion() < Sfnc2_0_0)
                {
                    //long min = m_Cam.Parameters[PLCamera.GainRaw].GetMinimum();
                    //long max = m_Cam.Parameters[PLCamera.GainRaw].GetMaximum();
                    //long incr = m_Cam.Parameters[PLCamera.GainRaw].GetIncrement();
                    //if (value < min)
                    //{
                    //    value = min;
                    //}
                    //else if (value > max)
                    //{
                    //    value = max;
                    //}
                    //else
                    //{
                    //    value = min + (((value - min) / incr) * incr);
                    //}
                    //value += 51;
                    m_Cam.Parameters[PLCamera.GainRaw].SetValue(value);
                }
                else
                {
                    m_Cam.Parameters[PLUsbCamera.Gain].SetValue(value);
                }
                return true;
            }
            catch (Exception e)
            {
                camLog.Error(strfucName, e.Message);
                return false;
            }
        }

        /// <summary>
        /// 获取最小最大增益
        /// </summary>
        public bool GetMinMaxGain(out long minGain,out long maxGain)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                if (m_Cam.GetSfncVersion() < Sfnc2_0_0)
                {
                     minGain = m_Cam.Parameters[PLCamera.GainRaw].GetMinimum();
                     maxGain = m_Cam.Parameters[PLCamera.GainRaw].GetMaximum();
                }
                else
                {
                     minGain = (long)m_Cam.Parameters[PLUsbCamera.Gain].GetMinimum();
                     maxGain = (long)m_Cam.Parameters[PLUsbCamera.Gain].GetMaximum();
                }
                return true;
            }
            catch (Exception e)
            {
                minGain = 0;
                maxGain = 0;
                camLog.Error(strfucName, e.Message);
                return false;
            }
        }

        public bool GetGain(out long Gain)
        {
            string strfucName = System.Reflection.MethodBase.GetCurrentMethod().ToString();
            try
            {
                if (m_Cam.GetSfncVersion() < Sfnc2_0_0)
                {
                    Gain = m_Cam.Parameters[PLCamera.GainRaw].GetValue();                 
                }
                else
                {
                   Gain = (long)m_Cam.Parameters[PLUsbCamera.Gain].GetValue();
                  
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
          
        }
    }
}
