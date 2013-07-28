using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Kinect;
using Kinect.Toolbox;

namespace StandAloneComplex
{
    /// <summary>
    /// ジェスチャー認識器
    /// </summary>
    class GestureRecognizer
    {
        /// <summary>
        /// スワイプ認識器
        /// </summary>
        private SwipeGestureDetector swipeDetector = new SwipeGestureDetector();

        /// <summary>
        /// ジェスチャー検出用のタイマー
        /// </summary>
        private DispatcherTimer timer = new DispatcherTimer();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public GestureRecognizer()
        {
            this.swipeDetector.OnGestureDetected += this.swipeDetector_OnGestureDetected;

            this.timer.Interval = new TimeSpan(0, 0, 2);
            this.timer.Tick += this.timer_Tick;
        }

        /// <summary>
        /// ジェスチャー検出用にジョイントのポジションを追加
        /// </summary>
        /// <param name="position"></param>
        /// <param name="kinect"></param>
        public void AddJoint(SkeletonPoint position, KinectSensor kinect)
        {
            this.swipeDetector.Add(position, kinect);
        }

        /// <summary>
        /// ジェスチャーの検出イベント
        /// </summary>
        /// <param name="s"></param>
        private void swipeDetector_OnGestureDetected(string s)
        {
            if (s == "SwipeToLeft" && !this.timer.IsEnabled)
            {
                this.timer.Start();
            }

            if (s == "SwipeToRight" && this.timer.IsEnabled)
            {
                this.OnGestureRecognized(new GestureEventArgs(GestureTypes.SwipeRightLeft));
            }
        }

        /// <summary>
        /// ジェスチャー検出用のタイマーイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e)
        {
            this.timer.Stop();
        }

        /// <summary>
        /// ジェスチャー認識のイベントハンドラ
        /// </summary>
        public event EventHandler<GestureEventArgs> GestureRecognized;

        /// <summary>
        /// ジェスチャー認識のイベント発行
        /// </summary>
        /// <param name="e"></param>
        private void OnGestureRecognized(GestureEventArgs e)
        {
            if (this.GestureRecognized != null)
            {
                this.GestureRecognized(this, e);
            }
        }
    }

    /// <summary>
    /// 顔のタイプ
    /// </summary>
    enum GestureTypes
    {
        SwipeRightLeft
    }

    /// <summary>
    /// ジェスチャーイベント引数
    /// </summary>
    class GestureEventArgs : EventArgs
    {
        /// <summary>
        /// 顔のタイプ
        /// </summary>
        public GestureTypes GestureType { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="gestureType"></param>
        public GestureEventArgs(GestureTypes gestureType)
        {
            this.GestureType = gestureType;
        }
    }
}
