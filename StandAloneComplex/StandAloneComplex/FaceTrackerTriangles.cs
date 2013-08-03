using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.FaceTracking;
using KinectClassLibrary;

namespace StandAloneComplex
{
    /// <summary>
    /// フェイストラッカー（トライアングルモデル）
    /// </summary>
    class FaceTrackerTriangles : IDisposable
    {
        /// <summary>
        /// Dispose済みフラグ
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// フェイストラッカー
        /// </summary>
        private FaceTracker faceTracker;

        /// <summary>
        /// トライアングルモデルのインデックス情報
        /// </summary>
        private static FaceTriangle[] faceTriangles;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="kinect"></param>
        public FaceTrackerTriangles()
        {   
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~FaceTrackerTriangles()
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
        /// フェイストラッカーの初期化を行う
        /// </summary>
        /// <param name="kinect"></param>
        public void Initialize(KinectSensor kinect)
        {
            this.faceTracker = new FaceTracker(kinect);
        }

        /// <summary>
        /// フェイストラッカーの初期化を解除する
        /// </summary>
        /// <param name="kinect"></param>
        public void Uninitialize()
        {
            if (this.faceTracker != null)
            {
                this.faceTracker.Dispose();
                this.faceTracker = null;
            }
        }

        /// <summary>
        /// フェイストラックフレームを取得する
        /// </summary>
        /// <param name="colorFrame"></param>
        /// <param name="depthFrame"></param>
        /// <param name="skeleton"></param>
        /// <returns></returns>
        public FaceTrackFrame GetFaceTrackFrame(ColorImageFrame colorFrame, DepthImageFrame depthFrame, Skeleton skeleton)
        {
            if (colorFrame == null || depthFrame == null || skeleton == null) { return null; }

            return this.faceTracker.Track(colorFrame.Format, colorFrame.ToPixels(), depthFrame.Format, depthFrame.ToPixels(), skeleton);
        }

        /// <summary>
        /// フェイスモデルのトライアングル情報を取得する
        /// </summary>
        /// <param name="faceFrame"></param>
        /// <returns></returns>
        public IEnumerable<FaceModelTriangle> GetFaceModelTriangles(FaceTrackFrame faceFrame)
        {
            if (faceTriangles == null) { faceTriangles = faceFrame.GetTriangles(); }

            var facePoints = faceFrame.GetProjected3DShape();

            foreach (var item in faceTriangles)
            {
                var triangle = new FaceModelTriangle();
                triangle.Point1 = new System.Windows.Point(facePoints[item.First].X, facePoints[item.First].Y);
                triangle.Point2 = new System.Windows.Point(facePoints[item.Second].X, facePoints[item.Second].Y);
                triangle.Point3 = new System.Windows.Point(facePoints[item.Third].X, facePoints[item.Third].Y);

                yield return triangle;
            }
        }
    }

    /// <summary>
    /// フェイスモデルのトライアングル情報
    /// </summary>
    struct FaceModelTriangle
    {
        public System.Windows.Point Point1 { get; set; }
        public System.Windows.Point Point2 { get; set; }
        public System.Windows.Point Point3 { get; set; }
    }
}
