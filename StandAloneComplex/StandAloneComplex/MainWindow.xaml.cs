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
using System.Windows.Threading;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.FaceTracking;
using Microsoft.Kinect.Toolkit.Interaction;
using Coding4Fun.Kinect.Wpf;
using KinectClassLibrary;

namespace StandAloneComplex
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
        /// FaceTracker
        /// </summary>
        private FaceTrackerTriangles faceTracker = new FaceTrackerTriangles();

        /// <summary>
        /// 顔のタイプ
        /// </summary>
        private FaceTypes faceType = FaceTypes.Unknown;

        /// <summary>
        /// 笑い男の文字回転用タイマー
        /// </summary>
        private DispatcherTimer laughingManTimer = new DispatcherTimer();

        /// <summary>
        /// 笑い男の文字の回転角度
        /// </summary>
        private double laughingManAngle;

        /// <summary>
        /// 音声認識器
        /// </summary>
        private SpeechRecognizer speechRecognizer = new SpeechRecognizer();

        /// <summary>
        /// ジェスチャー認識器
        /// </summary>
        private GestureRecognizer gestureRecognizer = new GestureRecognizer();

        /// <summary>
        /// 光学迷彩
        /// </summary>
        private OpticalCamouflage opticalCamouflage = new OpticalCamouflage();

        /// <summary>
        /// ハンドトラッカー
        /// </summary>
        private HandTracker handTracker = new HandTracker();

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

            this.gestureRecognizer.GestureRecognized += this.gestureRecognizer_GestureRecognized;
            this.speechRecognizer.SpeechRecognized += this.speechRecognizer_SpeechRecognized;
            this.handTracker.HandPush += this.handTracker_HandPush;

            this.laughingManTimer.Interval = new TimeSpan(12000);
            this.laughingManTimer.Tick += this.laughingManTimer_Tick;
            this.HideFaceImges();
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
                this.canvasHandTracking.Children.Clear();
                this.canvasFaceModel.Children.Clear();
                this.faceType = FaceTypes.Unknown;
                this.HideFaceImges();
                if (this.laughingManTimer.IsEnabled) { this.laughingManTimer.Stop(); }
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

                this.faceTracker.Initialize(kinect);
                this.handTracker.Initialize(kinect);
                this.speechRecognizer.Start(kinect);
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

            this.speechRecognizer.Stop();
            this.handTracker.Uninitialize();
            this.faceTracker.Uninitialize();
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

                this.UpdateOpticalCamouflage(this.opticalCamouflage, colorFrame, depthFrame, kinect);
                this.UpdateGestureRecognizer(this.gestureRecognizer, skeletonFrame, kinect);
                this.DrawRgbImage(this.imageRgb, colorFrame, depthFrame, kinect);
                this.DrawDepthImage(this.imageDepth, depthFrame, kinect);
                this.DrawSkeleton(this.canvasSkeleton, skeletonFrame, kinect);
                this.DrawTrackingFace(this.faceType, colorFrame, depthFrame, skeletonFrame, kinect);
                this.DrawHandTracker(this.canvasHandTracking, colorFrame, depthFrame, skeletonFrame, kinect);
            }
        }

        /// <summary>
        /// 光学迷彩を更新する
        /// </summary>
        /// <param name="opticalCamouflage"></param>
        /// <param name="colorFrame"></param>
        /// <param name="depthFrame"></param>
        /// <param name="kinect"></param>
        private void UpdateOpticalCamouflage(OpticalCamouflage opticalCamouflage, ColorImageFrame colorFrame, DepthImageFrame depthFrame, KinectSensor kinect)
        {
            if (!opticalCamouflage.IsInitialized)
            {
                opticalCamouflage.Initialize(colorFrame);
            }

            //opticalCamouflage.UpdateBackupPixel(kinect, colorFrame, depthFrame);
        }

        /// <summary>
        /// ジェスチャー認識期を更新する
        /// </summary>
        /// <param name="gestureRecognizer"></param>
        /// <param name="skeletonFrame"></param>
        /// <param name="kinect"></param>
        private void UpdateGestureRecognizer(GestureRecognizer gestureRecognizer, SkeletonFrame skeletonFrame, KinectSensor kinect)
        {
            var skeleton = skeletonFrame.GetFirstTrackedSkeleton();
            if (skeleton != null)
            {
                var handRight = skeleton.Joints[JointType.HandRight];
                if (handRight.TrackingState == JointTrackingState.Tracked)
                {
                    gestureRecognizer.AddJoint(handRight.Position, kinect);
                }
            }
        }

        /// <summary>
        /// RGB画像を描画する
        /// </summary>
        /// <param name="image"></param>
        /// <param name="colorFrame"></param>
        private void DrawRgbImage(Image image, ColorImageFrame colorFrame, DepthImageFrame depthFrame, KinectSensor kinect)
        {
            if (!this.checkRgb.IsChecked.HasValue || !this.checkRgb.IsChecked.Value)
            {
                this.imageRgb.Source = null;
                return;
            }

            if (this.opticalCamouflage != null && this.opticalCamouflage.IsActive)
            {
                image.Source = this.opticalCamouflage.GetCamouflageBitmapSource(kinect, colorFrame, depthFrame);
            }
            else
            {
                image.Source = colorFrame.ToBitmapSource();
            }
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
        /// 顔を追跡する
        /// </summary>
        /// <param name="colorFrame"></param>
        /// <param name="depthFrame"></param>
        /// <param name="skeletonFrame"></param>
        /// <param name="faceType"></param>
        private void DrawTrackingFace(FaceTypes faceType, ColorImageFrame colorFrame, DepthImageFrame depthFrame, SkeletonFrame skeletonFrame, KinectSensor kinect)
        {
            if (faceType == FaceTypes.Unknown) { return; }
            if (colorFrame.FrameNumber % 3 != 0) { return; }

            this.HideFaceImges();

            foreach (var skeleton in skeletonFrame.GetTrackedSkeletons())
            {
                var faceFrame = this.faceTracker.GetFaceTrackFrame(colorFrame, depthFrame, skeleton);
                if (faceFrame.TrackSuccessful)
                {
                    if (faceType == FaceTypes.StoneMask)
                    {
                        this.DrawFaceModel(kinect, faceFrame, this.canvasFaceModel);
                    }
                    else
                    {
                        this.DisplayFaceImages(faceFrame.FaceRect, colorFrame);
                    }
                }
            }
        }

        /// <summary>
        /// 顔を追跡する
        /// </summary>
        /// <param name="colorFrame"></param>
        /// <param name="depthFrame"></param>
        /// <param name="skeletonFrame"></param>
        /// <param name="faceType"></param>
        private void TrackFace(FaceTypes faceType, ColorImageFrame colorFrame, DepthImageFrame depthFrame, SkeletonFrame skeletonFrame, KinectSensor kinect)
        {
            if (faceType == FaceTypes.Unknown) { return; }
            if (colorFrame.FrameNumber % 3 != 0) { return; }

            foreach (var skeleton in skeletonFrame.GetTrackedSkeletons())
            {
                var faceFrame = this.faceTracker.GetFaceTrackFrame(colorFrame, depthFrame, skeleton);
                if (faceFrame.TrackSuccessful)
                {
                    if (faceType == FaceTypes.StoneMask)
                    {
                        this.DrawFaceModel(kinect, faceFrame, this.canvasFaceModel);
                    }
                    else
                    {
                        this.DisplayFaceImages(faceFrame.FaceRect, colorFrame);
                    }
                }
                else
                {
                    this.HideFaceImges();
                }
            }
        }

        /// <summary>
        /// フェイスモデルを描画する
        /// </summary>
        /// <param name="kinect"></param>
        /// <param name="faceFrame"></param>
        /// <param name="canvas"></param>
        private void DrawFaceModel(KinectSensor kinect, FaceTrackFrame faceFrame, Canvas canvas)
        {
            this.canvasFaceModel.Children.Clear();

            var faceModel = this.faceTracker.GetFaceModelTriangles(faceFrame);

            foreach (var item in faceModel)
            {
                canvas.DrawLine(1.0, Brushes.LightGray, item.Point1, item.Point2, kinect);
                canvas.DrawLine(1.0, Brushes.LightGray, item.Point2, item.Point3, kinect);
                canvas.DrawLine(1.0, Brushes.LightGray, item.Point3, item.Point1, kinect);
            }
        }

        /// <summary>
        /// 顔イメージを表示する
        /// </summary>
        /// <param name="faceRect"></param>
        /// <param name="colorFrame"></param>
        private void DisplayFaceImages(Microsoft.Kinect.Toolkit.FaceTracking.Rect faceRect, ColorImageFrame colorFrame)
        {
            var length = this.AdjustLengthByCharacter(Math.Max(faceRect.Width, faceRect.Height), this.faceType);
            var position = this.AdjustPositionByCharacter(new System.Windows.Point
            {
                X = faceRect.Left - (length - faceRect.Width) / 2,
                Y = faceRect.Top - (length - faceRect.Height) / 2
            }, length, this.faceType);

            length = KinectExtensions.ScaleTo(length, colorFrame.Height, this.imageRgb.ActualHeight);
            position.X = KinectExtensions.ScaleTo(position.X, colorFrame.Width, this.imageRgb.ActualWidth);
            position.Y = KinectExtensions.ScaleTo(position.Y, colorFrame.Height, this.imageRgb.ActualHeight);

            position = this.AdjustPositionByMargin(position);

            var faceImages = this.GetFaceImages(this.faceType);
            foreach (var item in faceImages)
            {
                item.Margin = new Thickness(position.X, position.Y, 0, 0);
                item.Width = length;
                item.Height = length;
                item.Visibility = System.Windows.Visibility.Visible;
            }
        }

        /// <summary>
        /// 顔イメージを取得する
        /// </summary>
        /// <param name="faceType"></param>
        /// <returns></returns>
        private IEnumerable<Image> GetFaceImages(FaceTypes faceType)
        {
            switch (faceType)
            {
                case FaceTypes.Unknown:
                    break;
                case FaceTypes.LaughingMan:
                    yield return this.imageLaughingManFrame;
                    yield return this.imageLaughingManFace;
                    break;
                case FaceTypes.MickeyMouse:
                    yield return this.imageMickeyMouse;
                    break;
                case FaceTypes.HelloKitty:
                    yield return this.imageHelloKitty;
                    break;
                case FaceTypes.Tomodachi:
                    yield return this.imageTomodachi;
                    break;
                case FaceTypes.StoneMask:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 顔のイメージを隠す（フェイスモデルも消す）
        /// </summary>
        /// <param name="currentImage"></param>
        private void HideFaceImges()
        {
            var images = new Image[] { this.imageLaughingManFace, this.imageLaughingManFrame, this.imageMickeyMouse, this.imageHelloKitty, this.imageTomodachi };

            foreach (var item in images)
            {
                item.Visibility = System.Windows.Visibility.Hidden;
            }

            this.canvasFaceModel.Children.Clear();
        }

        /// <summary>
        /// Windowの余白分を位置調整
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private System.Windows.Point AdjustPositionByMargin(System.Windows.Point point)
        {
            return new System.Windows.Point
            {
                X = point.X + (this.ActualWidth - 21 - this.imageRgb.ActualWidth) / 2,
                Y = point.Y + (this.ActualHeight - 41 - this.imageRgb.ActualHeight) / 2
            };
        }

        /// <summary>
        /// キャラクタごとの顔のイメージの大きさを調整する
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        private double AdjustLengthByCharacter(double length, FaceTypes faceType)
        {
            switch (faceType)
            {
                case FaceTypes.Unknown:
                    break;
                case FaceTypes.LaughingMan:
                    return length * 1.9;
                case FaceTypes.MickeyMouse:
                    return length * 2.1;
                case FaceTypes.HelloKitty:
                    return length * 1.7;
                case FaceTypes.Tomodachi:
                    return length * 2.3;
                case FaceTypes.StoneMask:
                    break;
                default:
                    break;
            }

            return length;
        }

        /// <summary>
        /// キャラクタごとの顔のイメージの位置を調整する．
        /// </summary>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private System.Windows.Point AdjustPositionByCharacter(System.Windows.Point position, double length, FaceTypes faceType)
        {
            switch (faceType)
            {
                case FaceTypes.Unknown:
                    break;
                case FaceTypes.LaughingMan:
                    var jitter = new Random().Next(8, 12) / 10.0;
                    return new System.Windows.Point { X = position.X + length * 0.05 * jitter, Y = position.Y - length * 0.15 * jitter };
                case FaceTypes.MickeyMouse:
                    return new System.Windows.Point { X = position.X - length * 0.1, Y = position.Y - length * 0.23 };
                case FaceTypes.HelloKitty:
                    return new System.Windows.Point { X = position.X, Y = position.Y - length * 0.15 };
                case FaceTypes.Tomodachi:
                    return new System.Windows.Point { X = position.X, Y = position.Y - length * 0.10 };
                case FaceTypes.StoneMask:
                    break;
                default:
                    break;
            }

            return position;
        }

        /// <summary>
        /// ハンドトラッカーを更新
        /// </summary>
        /// <param name="kinect"></param>
        /// <param name="colorFrame"></param>
        /// <param name="depthFrame"></param>
        /// <param name="skeletonFrame"></param>
        /// <param name="canvas"></param>
        private void DrawHandTracker(Canvas canvas, ColorImageFrame colorFrame, DepthImageFrame depthFrame, SkeletonFrame skeletonFrame, KinectSensor kinect)
        {
            this.handTracker.Update(kinect, depthFrame, skeletonFrame);

            canvas.Children.Clear();

            foreach (var item in this.handTracker.LeftPoints)
            {
                var brush = new SolidColorBrush { Color = Colors.LightGreen, Opacity = item.Opacity };
                canvas.DrawEllipse(10.0, brush, item.Position, kinect);
            }

            foreach (var item in this.handTracker.RightPoints)
            {
                var brush = new SolidColorBrush { Color = Colors.LightPink, Opacity = item.Opacity };
                canvas.DrawEllipse(10.0, brush, item.Position, kinect);
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

        /// <summary>
        /// 笑い男の文字回転用タイマーイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void laughingManTimer_Tick(object sender, EventArgs e)
        {
            if (this.imageLaughingManFrame.Visibility == Visibility.Hidden) { return; }

            this.imageLaughingManFrame.RenderTransform = new RotateTransform
            {
                Angle = --this.laughingManAngle,
                CenterX = this.imageLaughingManFrame.Width / 2,
                CenterY = this.imageLaughingManFrame.Height / 2
            };
        }

        /// <summary>
        /// 音声を認識したときのイベント
        /// </summary>
        /// <param name="e"></param>
        private void speechRecognizer_SpeechRecognized(object sender, SpeechEventArgs e)
        {
            try
            {
                this.HideFaceImges();
                if (this.laughingManTimer.IsEnabled) { this.laughingManTimer.Stop(); }

                this.faceType = e.FaceType;
                if (this.faceType == FaceTypes.LaughingMan) { this.laughingManTimer.Start(); }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// ジェスチャーを認識したときのイベント
        /// </summary>
        /// <param name="e"></param>
        private void gestureRecognizer_GestureRecognized(object sender, GestureEventArgs e)
        {
            if (e.GestureType == GestureTypes.SwipeRightLeft)
            {
                if (this.canvasHandTracking.Children.Count != 0) { return; }

                this.opticalCamouflage.Start();
            }
        }

        /// <summary>
        /// 手のプッシュイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handTracker_HandPush(object sender, HandEventArgs e)
        {
            if (e.HandType == InteractionHandType.Left)
            {
                this.opticalCamouflage.Stop();
            }
        }
    }
}
