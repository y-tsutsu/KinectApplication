using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace KinectWpfApplication
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 多重起動防止用ミューテックス
        /// </summary>
        private System.Threading.Mutex mutex = new System.Threading.Mutex(false, "KinectWpfApplication");

        /// <summary>
        /// アプリのスタートアップイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!this.mutex.WaitOne(0, false))
            {
                MessageBox.Show("すでにアプリが起動しています！", "Kinect WPF Application", MessageBoxButton.OK, MessageBoxImage.Information);
                this.mutex.Close();
                this.mutex = null;

                this.Shutdown();
            }
        }

        /// <summary>
        /// アプリの終了イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (this.mutex != null)
            {
                this.mutex.ReleaseMutex();
                this.mutex.Close();
            }
        }
    }
}
