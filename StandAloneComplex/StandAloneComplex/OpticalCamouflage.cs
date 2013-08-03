using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using KinectClassLibrary;

namespace StandAloneComplex
{
    /// <summary>
    /// 光学迷彩（スタートしてから10秒のみ有効）
    /// </summary>
    class OpticalCamouflage
    {
        /// <summary>
        /// 光学迷彩で置き換え用のバックアップ画像データ
        /// </summary>
        private byte[] backupPixels;

        /// <summary>
        /// 有効/無効
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 初期化済み判定
        /// </summary>
        public bool IsInitialized
        {
            get { return this.backupPixels != null; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public OpticalCamouflage()
        {
            this.IsActive = false;
        }

        /// <summary>
        /// 初期化する
        /// </summary>
        /// <param name="colorFrame"></param>
        public void Initialize(ColorImageFrame colorFrame)
        {
            if (colorFrame == null) { return; }

            this.backupPixels = colorFrame.ToPixels();
        }

        /// <summary>
        /// バックアップ画像データを更新する
        /// </summary>
        /// <param name="kinect"></param>
        /// <param name="colorFrame"></param>
        /// <param name="depthFrame"></param>
        public void UpdateBackupPixel(KinectSensor kinect, ColorImageFrame colorFrame, DepthImageFrame depthFrame)
        {
            if (kinect == null || colorFrame == null || depthFrame == null || !this.IsInitialized) { return; }

            var colorPixels = colorFrame.ToPixels();
            var depthPixels = depthFrame.ToDepthImagePixels();
            var colorPoints = depthFrame.ToColorImagePoints(kinect, depthPixels);

            for (int i = 0; i < depthPixels.Length; i++)
            {
                if (depthPixels[i].PlayerIndex != 0) { continue; }

                var colorIndex = colorPoints[i].ToByteArrayIndex(colorFrame, depthFrame, PixelFormats.Bgr32);

                this.backupPixels[colorIndex] = colorPixels[colorIndex];
                this.backupPixels[colorIndex + 1] = colorPixels[colorIndex + 1];
                this.backupPixels[colorIndex + 2] = colorPixels[colorIndex + 2];
            }
        }

        /// <summary>
        /// 光学迷彩でカモフラージュ済みのBitmapSourceを取得（解除中はそのままのRGB画像を返す）
        /// </summary>
        /// <param name="kinect"></param>
        /// <param name="colorFrame"></param>
        /// <param name="depthFrame"></param>
        /// <returns></returns>
        public BitmapSource GetCamouflageBitmapSource(KinectSensor kinect, ColorImageFrame colorFrame, DepthImageFrame depthFrame)
        {
            if (kinect == null || colorFrame == null || depthFrame == null || !this.IsActive) { return null; }

            var colorPixels = colorFrame.ToPixels();
            var depthPixels = depthFrame.ToDepthImagePixels();
            var colorPoints = depthFrame.ToColorImagePoints(kinect, depthPixels);

            for (int i = 0; i < depthPixels.Length; i++)
            {
                if (depthPixels[i].PlayerIndex == 0) { continue; }

                var colorIndex = colorPoints[i].ToByteArrayIndex(colorFrame, depthFrame, PixelFormats.Bgr32);

                colorPixels[colorIndex] = this.backupPixels[colorIndex];
                colorPixels[colorIndex + 1] = this.backupPixels[colorIndex + 1];
                colorPixels[colorIndex + 2] = this.backupPixels[colorIndex + 2];
            }

            return colorPixels.ToBitmapSource(colorFrame.Width, colorFrame.Height);
        }

        /// <summary>
        /// 光学迷彩を有効にする
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (this.IsActive) { return false; }

            this.IsActive = true;
            return true;
        }

        /// <summary>
        /// 光学迷彩を無効にする
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            if (!this.IsActive) { return false; }

            this.IsActive = false;
            return true;
        }
    }
}
