using ClipOne.util;
using ClipOne.view;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ClipPlus
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        
        void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            
            var comException = e.Exception as System.Runtime.InteropServices.COMException;

            if (comException != null && comException.ErrorCode == -2147221040)
            {
                
                e.Handled = true;
            }
           
            e.Handled = true;
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Process[] pro = Process.GetProcesses();
            int n = pro.Where(p => p.ProcessName.ToLower().Equals(System.Windows.Forms.Application.ProductName.ToLower())).Count();
            if (n > 1)
            {
                Application.Current.Shutdown();
                return;
            }
           

        }
    }

}
