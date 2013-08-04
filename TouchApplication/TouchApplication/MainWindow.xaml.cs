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

namespace TouchApplication
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 選択モード
        /// </summary>
        private enum SelectMode
        {
            NotSelected,
            Selecting,
            Selected
        }

        /// <summary>
        /// タッチポイントのエラー値
        /// </summary>
        private readonly int ERROR_OF_POINT = -100;

        /// <summary>
        /// KinectSensorChooser
        /// </summary>
        private KinectSensorChooser sensorChooser = new KinectSensorChooser();

        /// <summary>
        /// 現在の領域選択モード
        /// </summary>
        private SelectMode currentMode = SelectMode.NotSelected;
        
        /// <summary>
        /// 指定領域の始点
        /// </summary>
        private Point startPointOfRect;

        /// <summary>
        /// 前フレームのタッチ座標
        /// </summary>
        private Point preTouchPoint;

        /// <summary>
        /// 指定した領域
        /// </summary>
        private Rect selectRegion;

        /// <summary>
        /// 領域内の深度データ
        /// </summary>
        private DepthImagePoint[] backgroundDepthPoints;

        /// <summary>
        /// 深度データ
        /// </summary>
        private DepthImagePoint[] depthPoints;

        /// <summary>
        /// カラーデータ
        /// </summary>
        private ColorImagePoint[] colorPoints;

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
                kinect.AllFramesReady += this.kinect_AllFramesReady;

                this.InitializeTiltAngleComboBox(this.comboTiltAngle, kinect);
                this.InitializeRangeComboBox(this.comboRange, kinect);
                this.InitializeTrackingModeComboBox(this.comboTrackingMode, kinect);

                this.imageOpening.Source = null;
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

                if (this.depthPoints == null)
                {
                    this.depthPoints = new DepthImagePoint[depthFrame.PixelDataLength];
                    this.colorPoints = new ColorImagePoint[depthFrame.PixelDataLength];
                }

                this.DrawTouchImage(this.imageRgb, colorFrame, depthFrame, kinect);
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

            foreach (var item in skeletonFrame.ToSkeletons())
            {
                item.DrawSkeletonLines(canvas, kinect);
                item.DrawSkeletonEllipses(canvas, kinect);
            }
        }

        /// <summary>
        /// RGB画像を描画する
        /// </summary>
        /// <param name="image"></param>
        /// <param name="colorFrame"></param>
        /// <param name="depthFrame"></param>
        /// <param name="kinect"></param>
        private void DrawTouchImage(Image image, ColorImageFrame colorFrame, DepthImageFrame depthFrame, KinectSensor kinect)
        {
            if (!this.checkRgb.IsChecked.HasValue || !this.checkRgb.IsChecked.Value)
            {
                this.imageRgb.Source = null;
                return;
            }

            if (colorFrame.FrameNumber % 3 != 0) { return; }

            this.imageRgb.Source = colorFrame.ToBitmapSource(depthFrame, kinect);

            switch (this.currentMode)
            {
                case SelectMode.Selecting:
                    this.UpdateRectPosition(image);
                    break;
                case SelectMode.Selected:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 選択領域をあらわすRectangleの描画を更新
        /// </summary>
        private void UpdateRectPosition(Image image)
        {
            var currentPoint = Mouse.GetPosition(image);

            Rect rect;

            if (currentPoint.X < this.startPointOfRect.X && currentPoint.Y < this.startPointOfRect.Y)
            {
                rect = new Rect(currentPoint.X, currentPoint.Y, Math.Abs(this.startPointOfRect.X - currentPoint.X), Math.Abs(this.startPointOfRect.Y - currentPoint.Y));
            }
            else if (currentPoint.X < this.startPointOfRect.X)
            {
                rect = new Rect(currentPoint.X, this.startPointOfRect.Y, Math.Abs(this.startPointOfRect.X - currentPoint.X), Math.Abs(this.startPointOfRect.Y - currentPoint.Y));
            }
            else if (currentPoint.Y < this.startPointOfRect.Y)
            {
                rect = new Rect(this.startPointOfRect.X, currentPoint.Y, Math.Abs(this.startPointOfRect.X - currentPoint.X), Math.Abs(this.startPointOfRect.Y - currentPoint.Y));
            }
            else
            {
                rect = new Rect(this.startPointOfRect.X, this.startPointOfRect.Y, Math.Abs(this.startPointOfRect.X - currentPoint.X), Math.Abs(this.startPointOfRect.Y - currentPoint.Y));
            }

            Canvas.SetLeft(this.selectRectangle, rect.X);
            Canvas.SetTop(this.selectRectangle, rect.Y);
            this.selectRectangle.Width = rect.Width;
            this.selectRectangle.Height = rect.Height;

            this.selectRegion = rect;
        }

        /// <summary>
        /// タッチしている所を表すEllipseの描画を更新
        /// </summary>
        /// <param name="p"></param>
        private void UpdateTouchingPointEllipse(Ellipse ellipse, Point point)
        {
            ellipse.Width = 20;
            ellipse.Height = 20;
            Canvas.SetLeft(ellipse, point.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.Height / 2);
        }

        /// <summary>
        /// ペイント用ウィンドウを更新
        /// </summary>
        /// <param name="point"></param>
        private void UpdatePointCanvas(Point point)
        {
            if (point.X == this.preTouchPoint.X && point.Y == this.preTouchPoint.Y)
            {
                return;
            }
            else if ((point.X == this.ERROR_OF_POINT && point.Y == this.ERROR_OF_POINT) ||
                (this.preTouchPoint.X == this.ERROR_OF_POINT && this.preTouchPoint.Y == this.ERROR_OF_POINT))
            {
                this.preTouchPoint = point;
                return;
            }

            this.preTouchPoint = point;
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

        /// <summary>
        /// マウスの左ボタンのダウンイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        /// <summary>
        /// マウスの左ボタンのアップイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        /// <summary>
        /// スタートボタンのクリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
