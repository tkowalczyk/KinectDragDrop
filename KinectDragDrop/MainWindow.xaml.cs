using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Coding4Fun.Kinect.Wpf.Controls;
using Microsoft.Kinect;

namespace KinectDragDrop
{
    public partial class MainWindow : Window
    {
        #region Properties
        private static double _topBoundary;
        private static double _bottomBoundary;
        private static double _leftBoundary;
        private static double _rightBoundary;
        private static double _itemLeft;
        private static double _itemTop;

        private bool isGreenClicked = false;
        private bool isBlueClicked = false;
        private bool isGreenAimClicked = false;
        private bool isBlueAimClicked = false;

        private static int screenWidth = 1024;
        private static int screenHeight = 768;

        private KinectSensor _sensor;
        private Skeleton[] allSkeletons = new Skeleton[6];
        private List<HoverButton> kinectButtons;
        #endregion

        #region Ctor
        public MainWindow()
        {
            InitializeComponent();

            kinectButtons = new List<HoverButton>()
            {
                khbBlue,
                khbBlueCircle,
                khbGreen,
                khbGreenCircle
            };
        }
        #endregion

        #region Window events
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                _sensor = KinectSensor.KinectSensors[0];

                if (_sensor.Status == KinectStatus.Connected)
                {
                    _sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    _sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(_sensor_AllFramesReady);

                    var parameters = new TransformSmoothParameters
                    {
                        Smoothing = 0.3f,
                        Correction = 0.0f,
                        Prediction = 0.0f,
                        JitterRadius = 1.0f,
                        MaxDeviationRadius = 0.5f
                    };
                    _sensor.SkeletonStream.Enable(parameters);

                    _sensor.Start();
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_sensor != null)
            {
                _sensor.Stop();
            }
        }
        #endregion

        #region Sensor events
        void _sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            Skeleton first = GetFirstSkeleton(e);

            if (first == null)
            {
                return;
            }

            ScalePosition(handCursor, first.Joints[JointType.HandRight]);

            ProcessGesture(first.Joints[JointType.HandRight]);

            GetCameraPoint(first, e);
        }
        #endregion

        #region Kinect buttons events
        private void khbBlueCircle_Click(object sender, RoutedEventArgs e)
        {
            khbBlueCircle.Visibility = System.Windows.Visibility.Hidden;
            iBlueCircle.Visibility = System.Windows.Visibility.Visible;
            isBlueClicked = true;
        }

        private void khbGreenCircle_Click(object sender, RoutedEventArgs e)
        {
            khbGreenCircle.Visibility = System.Windows.Visibility.Hidden;
            iGreenCircle.Visibility = System.Windows.Visibility.Visible;
            isGreenClicked = true;
        }

        private void khbBlue_Click(object sender, RoutedEventArgs e)
        {
            if (isBlueClicked)
            {
                isBlueAimClicked = true;
                Canvas.SetLeft(iBlueCircle, 80);
                Canvas.SetTop(iBlueCircle, 200);
            }
        }

        private void khbGreen_Click(object sender, RoutedEventArgs e)
        {
            if (isGreenClicked)
            {
                isGreenAimClicked = true;
                Canvas.SetLeft(iGreenCircle, 80);
                Canvas.SetTop(iGreenCircle, 50);
            }
        }
        #endregion

        #region Kinect buttons helper methods
        private static void CheckButton(HoverButton button, Ellipse thumbStick)
        {
            if (IsItemMidpointInContainer(button, thumbStick))
            {
                button.Hovering();
            }
            else
            {
                button.Release();
            }
        }

        public static bool IsItemMidpointInContainer(FrameworkElement container, FrameworkElement target)
        {
            FindValues(container, target);

            if (_itemTop < _topBoundary || _bottomBoundary < _itemTop)
            {
                //Midpoint of target is outside of top or bottom
                return false;
            }

            if (_itemLeft < _leftBoundary || _rightBoundary < _itemLeft)
            {
                //Midpoint of target is outside of left or right
                return false;
            }

            return true;
        }

        private static void FindValues(FrameworkElement container, FrameworkElement target)
        {
            var containerTopLeft = container.PointToScreen(new Point());
            var itemTopLeft = target.PointToScreen(new Point());

            _topBoundary = containerTopLeft.Y;
            _bottomBoundary = _topBoundary + container.ActualHeight;
            _leftBoundary = containerTopLeft.X;
            _rightBoundary = _leftBoundary + container.ActualWidth;

            //use midpoint of item (width or height divided by 2)
            _itemLeft = itemTopLeft.X + (target.ActualWidth / 2);
            _itemTop = itemTopLeft.Y + (target.ActualHeight / 2);
        }
        #endregion

        #region Skeleton helper methods
        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }

                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                Skeleton first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();

                return first;
            }
        }

        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null || _sensor == null)
                {
                    return;
                }

                DepthImagePoint rightDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandRight].Position);

                ColorImagePoint rightColorPoint =
                    depth.MapToColorImagePoint(rightDepthPoint.X, rightDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                CameraPosition(handCursor, rightColorPoint);

                foreach (HoverButton hb in kinectButtons)
                {
                    CheckButton(hb, handCursor);
                }
            }
        }

        private void ProcessGesture(Joint rightHand)
        {
            SkeletonPoint point = new SkeletonPoint();

            point.X = ScaleVector(screenWidth, rightHand.Position.X);
            point.Y = ScaleVector(screenHeight, -rightHand.Position.Y);
            point.Z = rightHand.Position.Z;

            rightHand.Position = point;

            if (isBlueClicked && !isBlueAimClicked)
            {
                Canvas.SetLeft(iBlueCircle, rightHand.Position.X / 2);
                Canvas.SetTop(iBlueCircle, rightHand.Position.Y / 2);
            }

            if (isGreenClicked && !isGreenAimClicked)
            {
                Canvas.SetLeft(iGreenCircle, rightHand.Position.X / 2);
                Canvas.SetTop(iGreenCircle, rightHand.Position.Y / 2);
            }
        }
        #endregion

        #region Others
        private void ScalePosition(FrameworkElement element, Joint joint)
        {
            //convert the value to X/Y
            //Joint scaledJoint = joint.ScaleTo(1024, 768); 
            SkeletonPoint point = new SkeletonPoint();
            point.X = ScaleVector(screenWidth, joint.Position.X);
            point.Y = ScaleVector(screenHeight, -joint.Position.Y);
            point.Z = joint.Position.Z;

            Joint scaledJoint = joint;
            //Joint scaledJoint = joint.ScaleTo(1920, 1080);

            scaledJoint.TrackingState = JointTrackingState.Tracked;
            scaledJoint.Position = point;

            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y);

        }

        private float ScaleVector(int length, float position)
        {
            float value = (((((float)length) / 1f) / 2f) * position) + (length / 2);
            if (value > length)
            {
                return (float)length;
            }
            if (value < 0f)
            {
                return 0f;
            }
            return value;
        }

        private void CameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            Canvas.SetLeft(element, point.X - element.Width / 2);
            Canvas.SetTop(element, point.Y - element.Height / 2);
        }
        #endregion
    }
}