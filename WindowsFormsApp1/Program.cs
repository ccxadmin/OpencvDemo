using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    static class Program
    { /// <summary>
      /// 应用程序的主入口点。
      /// </summary>
        [STAThread]
        static void Main()
        {
            bool isNotRunning;  //互斥体判断
            Mutex instance = new Mutex(true, "MutexName2", out isNotRunning);   //同步基元变量
            if (!isNotRunning)  // 如果不是未运行状态
            {
                MessageBox.Show("程序已在运行");
                Environment.Exit(1);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            Application.Run(new Form1());

        }
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("AppDomain中遇到未处理异常：" + e.ExceptionObject.ToString());
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            MessageBox.Show("Application中遇到未处理异常：" + e.Exception.Message + "\r\n" + e.Exception.StackTrace);
        }
    }
}
