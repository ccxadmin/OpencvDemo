using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace DeviceLib.Cam
{
     public  interface Icam:IDisposable
    {

        event ImgGetHandle setImgGetHandle;
        event EventHandler CamConnectHnadle;
        bool OpenCam(int camIndex, ref string msg);

        void CloseCam();

        bool OneShot();

        bool ContinueGrab();

        void StopGrab();

        int CamNum { get; }

        bool IsAlive { get;  }
         bool IsGrabing { get; }
          
        bool SetExposureTime(long dValue);
        bool SetGain(long dValue);
        bool GetExposureTime(out long dValue);
        bool GetGain(out long dValue);
        CamType currCamType { get; }
        int CamIndex { get; }
    }

    public delegate void ImgGetHandle(Bitmap img);

    public enum CamType
    {
        海康,
        大华,
        巴斯勒,
        NONE
    }

}
