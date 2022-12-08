using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace visionForm
{
    public enum CommMode
    {
        Tcp_ip,
        SerialPort,
        IO,
        Internal//Vision
    }
    public delegate void CommReceiveData(int type, object objData);
    public interface ITCPclient
    {
      
        event CommReceiveData OnTcpReceive ;
        void Send(string str);
        bool IsConnected();

    }
}
