using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Coding4Fun.Kinect.Wpf;
using KinectClassLibrary;

namespace KinectWpfApplication
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// KinectSensorChooser
        /// </summary>
        private KinectSensorChooser sensorChooser = new KinectSensorChooser();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Windowがロードされるときのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.sensorChooser.KinectChanged += this.sensorChooser_KinectChanged;
            this.sensorChooser.Start();
        }

        /// <summary>
        /// Windowが閉じられたあとのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (!this.IsLoaded) { return; }

            this.sensorChooser.Stop();
        }

        /// <summary>
        /// Kinectの状態変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void sensorChooser_KinectChanged(object sender, KinectChangedEventArgs e)
        {
            if (e.NewSensor != null)
            {
                this.StartKinect(e.NewSensor);
            }

            if (e.OldSensor != null)
            {
                this.StopKinect(e.OldSensor);
                this.imageRgb.Source = null;
                this.imageDepth.Source = null;
                this.canvasSkeleton.Children.Clear();
            }
        }

        /// <summary>
        /// Kinectの動作を開始する
        /// </summary>
        /// <param name="kinect"></param>
        private void StartKinect(KinectSensor kinect)
        {
            if (kinect == null) { return; }

            try
            {
                kinect.ColorStream.Enable();
                kinect.DepthStream.Enable();
                kinect.SkeletonStream.Enable();
                kinect.DepthStream.Range = DepthRange.Near;
                kinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                kinect.SkeletonStream.EnableTrackingInNearRange = true;
                kinect.AllFramesReady += kinect_AllFramesReady;

                this.InitializeTiltAngleComboBox(this.comboTiltAngle, kinect);
                this.InitializeRangeComboBox(this.comboRange, kinect);
                this.InitializeTrackingModeComboBox(this.comboTrackingMode, kinect);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        /// <summary>
        /// チルトモータの角度用ComboBoxを初期化する
        /// </summary>
        /// <param name="comboBox"></param>
        /// <param name="kinect"></param>
        private void InitializeTiltAngleComboBox(ComboBox comboBox, KinectSensor kinect)
        {
            comboBox.Items.Clear();
            for (int i = kinect.MaxElevationAngle; kinect.MinElevationAngle <= i; i--)
            {
                comboBox.Items.Add(i);
            }
            comboBox.SelectedItem = kinect.ElevationAngle;
        }

        /// <summary>
        /// Range用ComboBoxを初期化する
        /// </summary>
        /// <param name="comboBox"></param>
        private void InitializeRangeComboBox(ComboBox comboBox, KinectSensor kinect)
        {
            comboBox.Items.Clear();
            foreach (var item in Enum.GetValues(typeof(DepthRange)))
            {
                comboBox.Items.Add(item);
            }
            comboBox.SelectedItem = kinect.DepthStream.Range;
        }

        /// <summary>
        /// TrackingMode用ComboBoxを初期化する
        /// </summary>
        /// <param name="comboBox"></param>
        private void InitializeTrackingModeComboBox(ComboBox comboBox, KinectSensor kinect)
        {
            comboBox.Items.Clear();
            foreach (var item in Enum.GetValues(typeof(SkeletonTrackingMode)))
            {
                comboBox.Items.Add(item);
            }
            comboBox.SelectedItem = kinect.SkeletonStream.TrackingMode;
        }

        /// <summary>
        /// Kinectの動作を停止する
        /// </summary>
        /// <param name="kinect"></param>
        private void StopKinect(KinectSensor kinect)
        {
            if (kinect == null) { return; }

            kinect.AllFramesReady -= this.kinect_AllFramesReady;
            kinect.ColorStream.Disable();
            kinect.DepthStream.Disable();
            kinect.SkeletonStream.Disable();
        }

        /// <summary>
        /// RGBカメラ，距離カメラ，骨格のフレーム更新イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void kinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (var colorFrame = e.OpenColorImageFrame())
            using (var depthFrame = e.OpenDepthImageFrame())
            using (var skeletonFrame = e.OpenSkeletonFrame())
            {
                if (colorFrame == null || depthFrame == null || skeletonFrame == null) { return; }

                var kinect = sender as KinectSensor;

                this.DrawRgbImage(this.imageRgb, colorFrame);
                this.DrawDepthImage(this.imageDepth, depthFrame, kinect);
                this.DrawSkeleton(this.canvasSkeleton, skeletonFrame, kinect);
            }
        }

        /// <summary>
        /// RGB画像を描画する
        /// </summary>
        /// <param name="image"></param>
        /// <param name="colorFrame"></param>
        private void DrawRgbImage(Image image, ColorImageFrame colorFrame)
        {
            if (!this.checkRgb.IsChecked.HasValue || !this.checkRgb.IsChecked.Value)
            {
                this.imageRgb.Source = null;
                return;
            }

            image.Source = colorFrame.ToBitmapSource();
        }

        /// <summary>
        /// 深度画像を描画する
        /// </summary>
        /// <param name="image"></param>
        /// <param name="depthFrame"></param>
        /// <param name="kinect"></param>
        private void DrawDepthImage(Image image, DepthImageFrame depthFrame, KinectSensor kinect)
        {
            if (!this.checkDepth.IsChecked.HasValue || !this.checkDepth.IsChecked.Value)
            {
                this.imageDepth.Source = null;
                return;
            }

            if (kinect.SkeletonStream.TrackingMode == SkeletonTrackingMode.Default)
            {
                image.Source = depthFrame.ToBitmapSource(kinect);
            }
            else
            {
                image.Source = depthFrame.ToBitmapSource();
            }
        }

        /// <summary>
        /// スケルトンを描画する
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="skeletonFrame"></param>
        /// <param name="kinect"></param>
        private void DrawSkeleton(Canvas canvas, SkeletonFrame skeletonFrame, KinectSensor kinect)
        {
            this.canvasSkeleton.Children.Clear();

            if (!this.checkSkeleton.IsChecked.HasValue || !this.checkSkeleton.IsChecked.Value) { return; }

            foreach (var item in skeletonFrame.ToSkeletonData())
            {
                item.DrawSkeletonLines(canvas, kinect);
                item.DrawSkeletonEllipses(canvas, kinect);
            }
        }

        /// <summary>
        /// チルトモータの角度用ComboBoxの選択イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboTiltAngle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.sensorChooser.Kinect == null || e.AddedItems.Count == 0) { return; }

            try
            {
                this.sensorChooser.Kinect.ElevationAngle = (int)e.AddedItems[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Range用ComboBoxの選択イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.sensorChooser.Kinect == null || e.AddedItems.Count == 0) { return; }

            try
            {
                this.sensorChooser.Kinect.DepthStream.Range = (DepthRange)e.AddedItems[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// TrackingMode用ComboBoxの選択イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboTrackingMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.sensorChooser.Kinect == null || e.AddedItems.Count == 0) { return; }

            try
            {
                this.sensorChooser.Kinect.SkeletonStream.TrackingMode = (SkeletonTrackingMode)e.AddedItems[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
