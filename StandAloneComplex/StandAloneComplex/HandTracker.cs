using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Interaction;
using KinectClassLibrary;

namespace StandAloneComplex
{
    /// <summary>
    /// ハンドトラッカー
    /// </summary>
    class HandTracker : IDisposable
    {
        /// <summary>
        /// インタラクションクライアント
        /// </summary>
        private class InteractionClient : IInteractionClient
        {
            /// <summary>
            /// InteractionInfoを取得する
            /// </summary>
            /// <param name="skeletonTrackingId"></param>
            /// <param name="handType"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public InteractionInfo GetInteractionInfoAtLocation(int skeletonTrackingId, InteractionHandType handType, double x, double y)
            {
                return new InteractionInfo() { IsGripTarget = true };
            }
        }

        /// <summary>
        /// 手のトラッキング情報
        /// </summary>
        private class HandTrackingInfo
        {
            /// <summary>
            /// グリップしたときのタイムスタンプ
            /// </summary>
            private DateTime glipTime = new DateTime();

            /// <summary>
            /// 握り中フラグ
            /// </summary>
            public bool IsGliping { get; private set; }

            /// <summary>
            /// 座標リスト
            /// </summary>
            public List<HandTrackingPoint> Points { get; set; }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            public HandTrackingInfo()
            {
                this.IsGliping = false;
                this.Points = new List<HandTrackingPoint>();
            }

            /// <summary>
            /// トラッキング情報の更新
            /// </summary>
            /// <param name="eventType"></param>
            /// <param name="position"></param>
            public void Update(InteractionHandEventType eventType)
            {
                if (eventType == InteractionHandEventType.Grip)
                {
                    var now = DateTime.Now;

                    if ((now - this.glipTime) < TimeSpan.FromMilliseconds(800))
                    {
                        this.IsGliping = true;
                    }

                    this.glipTime = now;
                }
                else if (eventType == InteractionHandEventType.GripRelease)
                {
                    this.IsGliping = false;
                }

                this.Points.RemoveAll(h => !h.IsEnabled);
            }

            /// <summary>
            /// 手の座標を追加
            /// </summary>
            /// <param name="point"></param>
            public void AddPoint(Point point)
            {
                if (!this.IsGliping) { return; }

                this.Points.Add(new HandTrackingPoint(point));
            }
        }

        /// <summary>
        /// Dispose済みフラグ
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// インタラクションストリーム
        /// </summary>
        private InteractionStream stream;

        /// <summary>
        /// 手のトラッキング情報
        /// </summary>
        private Dictionary<InteractionHandType, HandTrackingInfo> handTrackingInfos = new Dictionary<InteractionHandType, HandTrackingInfo>();

        /// <summary>
        /// 左手の座標
        /// </summary>
        public List<HandTrackingPoint> LeftPoints
        {
            get { return this.handTrackingInfos[InteractionHandType.Left].Points; }
        }

        /// <summary>
        /// 右手の座標
        /// </summary>
        public List<HandTrackingPoint> RightPoints
        {
            get { return this.handTrackingInfos[InteractionHandType.Right].Points; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="kinect"></param>
        public HandTracker()
        {
            this.handTrackingInfos.Add(InteractionHandType.Left, new HandTrackingInfo());
            this.handTrackingInfos.Add(InteractionHandType.Right, new HandTrackingInfo());
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~HandTracker()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// リソースを開放する
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソースを開放する（マネージリソースの開放をオプションにより切り替え）
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed) { return; }

            if (disposing)
            {
                // マネージリソースの開放
            }

            this.Uninitialize();

            this.disposed = true;
        }

        /// <summary>
        /// ハンドトラッカーの初期化を行う
        /// </summary>
        /// <param name="kinect"></param>
        public void Initialize(KinectSensor kinect)
        {
            this.stream = new InteractionStream(kinect, new InteractionClient());
            this.stream.InteractionFrameReady += this.stream_InteractionFrameReady;
        }

        /// <summary>
        /// ハンドトラッカーの初期化を解除する
        /// </summary>
        /// <param name="kinect"></param>
        public void Uninitialize()
        {
            if (this.stream != null)
            {
                this.stream.Dispose();
                this.stream = null;
            }
        }

        /// <summary>
        /// インタラクションを更新する
        /// </summary>
        /// <param name="kinect"></param>
        /// <param name="depthFrame"></param>
        /// <param name="skeletonFrame"></param>
        public void Update(KinectSensor kinect, DepthImageFrame depthFrame, SkeletonFrame skeletonFrame)
        {
            if (kinect == null || depthFrame == null || skeletonFrame == null) { return; }

            this.stream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);

            var skeletons = skeletonFrame.ToSkeletonData();
            this.stream.ProcessSkeleton(skeletons, kinect.AccelerometerGetCurrentReading(), skeletonFrame.Timestamp);

            foreach (var hand in this.handTrackingInfos)
            {
                if (!hand.Value.IsGliping) { continue; }

                foreach (var skeleton in skeletonFrame.GetTrackedSkeletons())
                {
                    var joint = (hand.Key == InteractionHandType.Left) ? skeleton.Joints[JointType.HandLeft] : skeleton.Joints[JointType.HandRight];
                    var colorPoint = joint.Position.ToColorImagePoint(kinect);
                    hand.Value.AddPoint(new Point(colorPoint.X, colorPoint.Y));
                }
            }
        }

        /// <summary>
        /// インタラクションが更新されたときのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stream_InteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            using (var interactionFrame = e.OpenInteractionFrame())
            {
                if (interactionFrame == null) { return; }

                var userInfos = interactionFrame.ToUserInfoData();
                foreach (var user in userInfos)
                {
                    if (user.SkeletonTrackingId == 0) { continue; }

                    foreach (var hand in user.HandPointers)
                    {
                        if (!this.handTrackingInfos.ContainsKey(hand.HandType)) { continue; }

                        this.handTrackingInfos[hand.HandType].Update(hand.HandEventType);

                        if (hand.IsTracked && hand.IsInteractive && hand.IsPressed && !this.handTrackingInfos[hand.HandType].IsGliping)
                        {
                            this.OnHandPush(new HandEventArgs(hand.HandType));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// プッシュイベント
        /// </summary>
        public event EventHandler<HandEventArgs> HandPush;

        /// <summary>
        /// プッシュのイベント発行
        /// </summary>
        /// <param name="e"></param>
        private void OnHandPush(HandEventArgs e)
        {
            if (this.HandPush != null)
            {
                this.HandPush(this, e);
            }
        }
    }

    /// <summary>
    /// ハンドトラッカーのイベント引数
    /// </summary>
    class HandEventArgs : EventArgs
    {
        /// <summary>
        /// 手のタイプ
        /// </summary>
        public InteractionHandType HandType { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="handType"></param>
        public HandEventArgs(InteractionHandType handType)
        {
            this.HandType = handType;
        }
    }

    

    /// <summary>
    /// 手のトラッキング座標
    /// </summary>
    public class HandTrackingPoint
    {
        /// <summary>
        /// 寿命（ミリ秒）
        /// </summary>
        private static readonly double lifeMilliseconds = 2000.0;

        /// <summary>
        /// 生成時刻
        /// </summary>
        private DateTime time = DateTime.Now;

        /// <summary>
        /// 座標
        /// </summary>
        public Point Position { get; private set; }

        /// <summary>
        /// 有効/無効
        /// </summary>
        public bool IsEnabled
        {
            get { return (DateTime.Now - this.time).TotalMilliseconds < HandTrackingPoint.lifeMilliseconds; }
        }

        /// <summary>
        /// 透明度
        /// </summary>
        public double Opacity
        {
            get { return (HandTrackingPoint.lifeMilliseconds - (DateTime.Now - this.time).TotalMilliseconds) / HandTrackingPoint.lifeMilliseconds; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="position"></param>
        public HandTrackingPoint(Point position)
        {
            this.Position = position;
        }
    }
}
