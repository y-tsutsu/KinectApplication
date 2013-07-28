using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Interaction;
using Coding4Fun.Kinect.Wpf;

namespace KinectClassLibrary
{
    /// <summary>
    /// 拡張メソッドクラス
    /// </summary>
    public static class KinectExtensions
    {
        /// <summary>
        /// ピクセルあたりのByte数（BGR32）
        /// </summary>
        private static readonly int Bgr32BytesPerPixel = PixelFormats.Bgr32.BitsPerPixel / 8;

        /// <summary>
        /// KinectSensorの動作を停止する（Dispose付き）
        /// </summary>
        /// <param name="kinect"></param>
        public static void Close(this KinectSensor kinect)
        {
            if (!kinect.IsRunning) { return; }

            kinect.Stop();
            kinect.Dispose();
        }

        /// <summary>
        /// ColorImageFrame -> byte[]
        /// </summary>
        /// <param name="colorFrame"></param>
        /// <returns></returns>
        public static byte[] ToPixelData(this ColorImageFrame colorFrame)
        {
            var pixels = new byte[colorFrame.PixelDataLength];
            colorFrame.CopyPixelDataTo(pixels);
            return pixels;
        }

        /// <summary>
        /// DepthImageFrame -> short[]
        /// </summary>
        /// <param name="depthFrame"></param>
        /// <returns></returns>
        public static short[] ToPixelData(this DepthImageFrame depthFrame)
        {
            var pixels = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(pixels);
            return pixels;
        }

        /// <summary>
        /// DepthImageFrame -> DepthImagePixel[]
        /// </summary>
        /// <param name="depthFrame"></param>
        /// <returns></returns>
        public static DepthImagePixel[] ToDepthImagePixelData(this DepthImageFrame depthFrame)
        {
            var pixels = new DepthImagePixel[depthFrame.PixelDataLength];
            depthFrame.CopyDepthImagePixelDataTo(pixels);
            return pixels;
        }

        /// <summary>
        /// DepthImagePixel[] -> ColorImagePoint[]
        /// </summary>
        /// <param name="depthFrame"></param>
        /// <param name="kinect"></param>
        /// <returns></returns>
        public static ColorImagePoint[] ToColorImagePointData(this DepthImageFrame depthFrame, KinectSensor kinect)
        {
            return depthFrame.ToColorImagePointData(kinect, depthFrame.ToDepthImagePixelData());
        }

        /// <summary>
        /// DepthImagePixel[] -> ColorImagePoint[]
        /// </summary>
        /// <param name="depthFrame"></param>
        /// <param name="kinect"></param>
        /// <param name="depthPixels"></param>
        /// <returns></returns>
        public static ColorImagePoint[] ToColorImagePointData(this DepthImageFrame depthFrame, KinectSensor kinect, DepthImagePixel[] depthPixels)
        {
            var colorPoints = new ColorImagePoint[depthPixels.Length];
            kinect.CoordinateMapper.MapDepthFrameToColorFrame(kinect.DepthStream.Format, depthPixels, kinect.ColorStream.Format, colorPoints);
            return colorPoints;
        }

        /// <summary>
        /// DepthImageFrame -> BitmapSource
        /// </summary>
        /// <param name="depthFrame"></param>
        /// <param name="kinect"></param>
        /// <returns></returns>
        public static BitmapSource ToBitmapSource(this DepthImageFrame depthFrame, KinectSensor kinect)
        {
            var bitmap = depthFrame.ConvertDepthFrameToBitmap(kinect);
            return bitmap.ToBitmapSource(depthFrame.Width, depthFrame.Height);
        }

        /// <summary>
        /// DepthImageFrame -> byte[]
        /// </summary>
        /// <param name="depthFrame"></param>
        /// <param name="kinect"></param>
        /// <returns></returns>
        private static byte[] ConvertDepthFrameToBitmap(this DepthImageFrame depthFrame, KinectSensor kinect)
        {
            var depthPixels = depthFrame.ToDepthImagePixelData();
            var colorPoints = depthFrame.ToColorImagePointData(kinect, depthPixels);
            var bitmap = new byte[colorPoints.Length * KinectExtensions.Bgr32BytesPerPixel];

            for (int i = 0; i < depthPixels.Length; i++)
            {
                int x = Math.Min(colorPoints[i].X, kinect.ColorStream.FrameWidth - 1);
                int y = Math.Min(colorPoints[i].Y, kinect.ColorStream.FrameHeight - 1);
                int colorIndex = ((y * depthFrame.Width) + x) * KinectExtensions.Bgr32BytesPerPixel;

                if (depthPixels[i].IsKnownDepth)
                {
                    byte b = depthPixels[i].Depth.CalculateIntensityFromDepth();
                    bitmap[colorIndex + 2] = b;
                    bitmap[colorIndex + 1] = b;
                    bitmap[colorIndex] = b;
                }
                else
                {
                    bitmap[colorIndex + 2] = 66;
                    bitmap[colorIndex + 1] = 66;
                    bitmap[colorIndex] = 33;
                }

                depthPixels[i].SkeletonOverlay(ref bitmap[colorIndex + 2], ref bitmap[colorIndex + 1], ref bitmap[colorIndex]);
            }

            return bitmap;
        }

        /// <summary>
        /// 深度データから色味を算出する
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        private static byte CalculateIntensityFromDepth(this short depth)
        {
            return (byte)(255f - 255f * Math.Max((float)depth - 800f, 0f) / 3200f);
        }

        /// <summary>
        /// スケルトンを重ね合わせ
        /// </summary>
        /// <param name="depthPixel"></param>
        /// <param name="redByte"></param>
        /// <param name="greenByte"></param>
        /// <param name="blueByte"></param>
        private static void SkeletonOverlay(this DepthImagePixel depthPixel, ref byte redByte, ref byte greenByte, ref byte blueByte)
        {
            switch (depthPixel.PlayerIndex)
            {
                case 1:
                    greenByte = 0;
                    blueByte = 0;
                    break;
                case 2:
                    redByte = 0;
                    greenByte = 0;
                    break;
                case 3:
                    redByte = 0;
                    blueByte = 0;
                    break;
                case 4:
                    greenByte = 0;
                    break;
                case 5:
                    blueByte = 0;
                    break;
                case 6:
                    redByte = 0;
                    break;
                case 7:
                    redByte /= 2;
                    blueByte = 0;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// ColorImagePoint -> byte[]のIndex
        /// </summary>
        /// <param name="colorPoint"></param>
        /// <param name="colorFrame"></param>
        /// <param name="depthFrame"></param>
        /// <param name="pixelFormat"></param>
        /// <returns></returns>
        public static int ToByteArrayIndex(this ColorImagePoint colorPoint, ColorImageFrame colorFrame, DepthImageFrame depthFrame, System.Windows.Media.PixelFormat pixelFormat)
        {
            int x = Math.Min(colorPoint.X, colorFrame.Width - 1);
            int y = Math.Min(colorPoint.Y, colorFrame.Width - 1);

            return ((y * depthFrame.Width) + x) * pixelFormat.BitsPerPixel / 8;
        }

        /// <summary>
        /// SkeletonFrame -> Skeleton[]
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static Skeleton[] ToSkeletonData(this SkeletonFrame frame)
        {
            var skeletons = new Skeleton[frame.SkeletonArrayLength];
            frame.CopySkeletonDataTo(skeletons);
            return skeletons;
        }

        /// <summary>
        /// SkeletonFrame -> 追跡中のIEnumerable<Skeleton>
        /// </summary>
        /// <param name="skeletonFrame"></param>
        /// <returns></returns>
        public static IEnumerable<Skeleton> GetTrackedSkeletons(this SkeletonFrame skeletonFrame)
        {
            return skeletonFrame.ToSkeletonData().Where(s => s.TrackingState == SkeletonTrackingState.Tracked);
        }

        /// <summary>
        /// SkeletonFrame -> はじめの追跡中のSkeletonを取得する
        /// </summary>
        /// <param name="skeletonFrame"></param>
        /// <returns></returns>
        public static Skeleton GetFirstTrackedSkeleton(this SkeletonFrame skeletonFrame)
        {
            return skeletonFrame.GetTrackedSkeletons().FirstOrDefault();
        }

        /// <summary>
        /// SkeletonPoint -> ColorImagePoint
        /// </summary>
        /// <param name="point"></param>
        /// <param name="kinect"></param>
        /// <returns></returns>
        public static ColorImagePoint ToColorImagePoint(this SkeletonPoint point, KinectSensor kinect)
        {
            return kinect.CoordinateMapper.MapSkeletonPointToColorPoint(point, kinect.ColorStream.Format);
        }

        /// <summary>
        /// スケルトンの線を描画する
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="canvas"></param>
        /// <param name="kinect"></param>
        public static void DrawSkeletonLines(this Skeleton skeleton, Canvas canvas, KinectSensor kinect)
        {
            if (skeleton.TrackingState != SkeletonTrackingState.Tracked) { return; }

            const double thickness = 3.0;
            var lineBrush = Brushes.DimGray;

            var jointPairs = skeleton.GetJointPairs(kinect);
            foreach (var item in jointPairs)
            {
                canvas.DrawLineBySkeletonPoint(thickness, lineBrush, item[0].Position, item[1].Position, kinect);
            }
        }

        /// <summary>
        /// スケルトン結ぶ線を描画する
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="thickness"></param>
        /// <param name="brush"></param>
        /// <param name="position1"></param>
        /// <param name="position2"></param>
        /// <param name="kinect"></param>
        public static void DrawLineBySkeletonPoint(this Canvas canvas, double thickness, Brush brush, SkeletonPoint position1, SkeletonPoint position2, KinectSensor kinect)
        {
            var colorPoint1 = position1.ToColorImagePoint(kinect);
            var colorPoint2 = position2.ToColorImagePoint(kinect);
            var point1 = new System.Windows.Point(colorPoint1.X, colorPoint1.Y);
            var point2 = new System.Windows.Point(colorPoint2.X, colorPoint2.Y);
            canvas.DrawLine(thickness, brush, point1, point2, kinect);
        }

        /// <summary>
        /// 線を描画する
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="thickness"></param>
        /// <param name="brush"></param>
        /// <param name="position1"></param>
        /// <param name="position2"></param>
        /// <param name="kinect"></param>
        public static void DrawLine(this Canvas canvas, double thickness, Brush brush, System.Windows.Point position1, System.Windows.Point position2, KinectSensor kinect)
        {
            var point1 = new System.Windows.Point
            {
                X = (int)KinectExtensions.ScaleTo(position1.X, kinect.ColorStream.FrameWidth, canvas.Width),
                Y = (int)KinectExtensions.ScaleTo(position1.Y, kinect.ColorStream.FrameHeight, canvas.Height)
            };
            var point2 = new System.Windows.Point
            {
                X = (int)KinectExtensions.ScaleTo(position2.X, kinect.ColorStream.FrameWidth, canvas.Width),
                Y = (int)KinectExtensions.ScaleTo(position2.Y, kinect.ColorStream.FrameHeight, canvas.Height)
            };

            canvas.Children.Add(new Line
            {
                X1 = point1.X,
                Y1 = point1.Y,
                X2 = point2.X,
                Y2 = point2.Y,
                Stroke = brush,
                StrokeThickness = thickness
            });
        }

        /// <summary>
        /// スケルトンの丸を描画する
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="canvas"></param>
        /// <param name="kinect"></param>
        public static void DrawSkeletonEllipses(this Skeleton skeleton, Canvas canvas, KinectSensor kinect)
        {
            const double radius = 5.0;
            var brush = Brushes.MidnightBlue;

            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                foreach (Joint item in skeleton.Joints)
                {
                    if (item.TrackingState == JointTrackingState.NotTracked) { continue; }

                    canvas.DrawEllipseBySkeletonPoint(radius, brush, item.Position, kinect);
                }
            }
            else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly)
            {
                canvas.DrawEllipseBySkeletonPoint(radius, brush, skeleton.Position, kinect);
            }
        }

        /// <summary>
        /// スケルトンの位置を丸く描画する
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="radius"></param>
        /// <param name="brush"></param>
        /// <param name="position"></param>
        /// <param name="kinect"></param>
        public static void DrawEllipseBySkeletonPoint(this Canvas canvas, double radius, Brush brush, SkeletonPoint position, KinectSensor kinect)
        {
            var colorPoint = position.ToColorImagePoint(kinect);
            var point = new System.Windows.Point(colorPoint.X, colorPoint.Y);
            canvas.DrawEllipse(radius, brush, point, kinect);
        }

        /// <summary>
        /// 丸を描画する
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="radius"></param>
        /// <param name="brush"></param>
        /// <param name="position"></param>
        /// <param name="kinect"></param>
        public static void DrawEllipse(this Canvas canvas, double radius, Brush brush, System.Windows.Point position, KinectSensor kinect)
        {
            var point = new System.Windows.Point
            {
                X = (int)KinectExtensions.ScaleTo(position.X, kinect.ColorStream.FrameWidth, canvas.Width),
                Y = (int)KinectExtensions.ScaleTo(position.Y, kinect.ColorStream.FrameHeight, canvas.Height)
            };

            canvas.Children.Add(new Ellipse
            {
                Fill = brush,
                Margin = new Thickness(point.X - radius, point.Y - radius, 0, 0),
                Width = radius * 2,
                Height = radius * 2
            });
        }

        /// <summary>
        /// 座標をサイズにあわせて変換
        /// </summary>
        /// <param name="value">座標</param>
        /// <param name="source">変換元のスケール</param>
        /// <param name="dest">変換先のスケール</param>
        /// <returns></returns>
        public static double ScaleTo(double value, double source, double dest)
        {
            return (value * dest) / source;
        }

        /// <summary>
        /// Skeleton -> ラインでつなぐための隣り合ったJointのペアを取得する
        /// </summary>
        /// <param name="skeleton"></param>
        /// <param name="kinect"></param>
        /// <returns></returns>
        private static IEnumerable<Joint[]> GetJointPairs(this Skeleton skeleton, KinectSensor kinect)
        {
            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                yield return new Joint[] { skeleton.Joints[JointType.Head], skeleton.Joints[JointType.ShoulderCenter] };
                yield return new Joint[] { skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderRight] };
                yield return new Joint[] { skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight] };
                yield return new Joint[] { skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.WristRight] };
                yield return new Joint[] { skeleton.Joints[JointType.WristRight], skeleton.Joints[JointType.HandRight] };
                yield return new Joint[] { skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderLeft] };
                yield return new Joint[] { skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft] };
                yield return new Joint[] { skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.WristLeft] };
                yield return new Joint[] { skeleton.Joints[JointType.WristLeft], skeleton.Joints[JointType.HandLeft] };

                if (kinect.SkeletonStream.TrackingMode == SkeletonTrackingMode.Default)
                {
                    yield return new Joint[] { skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.Spine] };
                    yield return new Joint[] { skeleton.Joints[JointType.Spine], skeleton.Joints[JointType.HipCenter] };
                    yield return new Joint[] { skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipRight] };
                    yield return new Joint[] { skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipLeft] };
                    yield return new Joint[] { skeleton.Joints[JointType.HipRight], skeleton.Joints[JointType.KneeRight] };
                    yield return new Joint[] { skeleton.Joints[JointType.KneeRight], skeleton.Joints[JointType.AnkleRight] };
                    yield return new Joint[] { skeleton.Joints[JointType.AnkleRight], skeleton.Joints[JointType.FootRight] };
                    yield return new Joint[] { skeleton.Joints[JointType.HipLeft], skeleton.Joints[JointType.KneeLeft] };
                    yield return new Joint[] { skeleton.Joints[JointType.KneeLeft], skeleton.Joints[JointType.AnkleLeft] };
                    yield return new Joint[] { skeleton.Joints[JointType.AnkleLeft], skeleton.Joints[JointType.FootLeft] };
                }
            }
        }

        /// <summary>
        /// InteractionFrame -> UserInfo[]
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static UserInfo[] ToUserInfoData(this InteractionFrame frame)
        {
            var userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];
            frame.CopyInteractionDataTo(userInfos);
            return userInfos;
        }
    }
}
