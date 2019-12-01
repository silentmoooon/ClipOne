using ClipOne.model;
using ClipOne.service;
using ClipOne.util;
using ClipOne.view;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ClipOne
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon _taskbar;
        private Config config;

        void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
             
             
            e.Handled = true;
        }

        //protected override void OnStartup(StartupEventArgs e)
        //{
        //   // base.OnStartup(e);
        //    //_taskbar = (TaskbarIcon)FindResource("Taskbar");
        //    //Process[] pro = Process.GetProcesses();
          
        //    //int n = pro.Where(p => p.ProcessName.ToLower().Equals(Process.GetCurrentProcess().MainModule.ModuleName.ToLower())).Count();
        //    //if (n > 1)
        //    //{
             
        //    //    Current.Shutdown();
        //    //    return;
        //    //}
           

        //}

        [STAThread]
        private void AppStartup(object sender, StartupEventArgs e)
        {
            _taskbar = (TaskbarIcon)FindResource("Taskbar");
            //_taskbar.ContextMenu.Items
            Process[] pro = Process.GetProcesses();

            int n = pro.Where(p => p.ProcessName.ToLower().Equals(Process.GetCurrentProcess().MainModule.ModuleName.ToLower())).Count();
            if (n > 1)
            {

                Current.Shutdown();
                return;
            }
            
            ConfigService configService = new ConfigService();
            config = configService.GetConfig();
            
         
         
            MainWindow win = new MainWindow(config);         
            win.Show();



        }

    }

}
