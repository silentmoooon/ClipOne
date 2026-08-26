using ClipOne.model;
using ClipOne.service;
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
        private System.Threading.Mutex _mutex;
        void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            System.IO.File.WriteAllText("error.log", "UI Error: " + e.Exception.ToString());
            MessageBox.Show("应用程序遇到错误: \n" + e.Exception.Message + "\n\n" + e.Exception.StackTrace, "ClipOne Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            Current.Shutdown();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Exception ex = args.ExceptionObject as Exception;
                System.IO.File.WriteAllText("error.log", "AppDomain Error: " + ex?.ToString());
                MessageBox.Show("后台线程遇到错误: \n" + ex?.Message + "\n\n" + ex?.StackTrace, "ClipOne Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                System.IO.File.WriteAllText("error.log", "Task Error: " + args.Exception.ToString());
                MessageBox.Show("异步任务遇到错误: \n" + args.Exception.Message + "\n\n" + args.Exception.StackTrace, "ClipOne Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.SetObserved();
            };

            bool createdNew;
            _mutex = new System.Threading.Mutex(true, "ClipOne_Unique_Application_Mutex", out createdNew);

            if (!createdNew)
            {
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        

    }

}
