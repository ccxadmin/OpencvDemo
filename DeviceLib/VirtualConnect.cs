using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace DeviceLib
{
    public class VirtualConnect : IDisposable
    {
        public VirtualConnect()
        {

        }
        public VirtualConnect(string _taskName)
        {
            taskName = _taskName;
            
        }
        ~VirtualConnect()
        {
            IsRunning = false;
           
        }
        private string currData = string.Empty;
         Queue<string> getDataQueue = new Queue<string>();
        public delegate void SendDataHandle(string data);
        public event SendDataHandle sendDataHandle = null;

        public EventHandler GetDataHandle = null;
        //任务名
        private string taskName;
        public string TaskName
        {
            get => this.taskName;
            set{this.taskName = value; }
        }

       //数据接受运行状态
        public bool IsRunning { get; set; } = false;
        /// <summary>
        /// 开启连接
        /// </summary>
        /// <returns></returns>
        public bool StartConnect()
        {
            IsRunning = true;
            getDataQueue.Clear();
            ThreadPool.QueueUserWorkItem(new WaitCallback(SignalInteractionThd), taskName);
            return true;
        }
        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public  bool Disconnnect()
        {
            IsRunning = false;
            return true;
        }
        /// <summary>
        /// 信号交互线程
        /// </summary>
        /// <param name="state"></param>
        void SignalInteractionThd(object state)
        {
            Console.WriteLine("交互线程："+state.ToString()+"已开启！");
            while (IsRunning)
            {
                if (getDataQueue.Count > 0)
                    currData = getDataQueue.Dequeue();
                else
                {
                    Thread.Sleep(50);
                    continue; //无数据则重新循环
                }
                 

                if (currData != string.Empty)
                    GetDataHandle.Invoke(currData, null);   //发送数据

                currData = string.Empty;
                Thread.Sleep(50);
            }
        
        }

       public void ReadData(string cmd)
        {
            if (getDataQueue.Contains(cmd)) return;   //防止数据重复
            getDataQueue.Enqueue(cmd);

        }

       public void WriteData(string ouputData)
        {
            sendDataHandle?.Invoke(ouputData);
        }


        public void Dispose()
        {
            this.Disconnnect();
            this.Dispose();
        }
    }
}
