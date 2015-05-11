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
using Microsoft.Kinect.Wpf.Controls;
using Microsoft.Kinect.Input;
using System.Diagnostics;
using System.Windows.Ink;
using kinectTest.DataSource;

namespace kinectTest
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int LINE_RESUME_THRESHOLD = 100;
        private const int WINDOW_CHANGE_THRESHOLD = 2000;

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IList<Body> _bodies;
        Body interactingBody = null;

        HandState rHand = HandState.Unknown;
        HandState lHand = HandState.Unknown;
        Stopwatch lastWindowChange = new Stopwatch();

        bool isMenuOpen = false;

        private List<Point> stroke = new List<Point>();

        // Keeps track of last time, so we know when we get a new set of pointers. Pointer events fire multiple times per timestamp, based on how many pointers are present.
        private TimeSpan lastTime;
        

        public MainWindow()
        {
            InitializeComponent();
            KinectSetup();
            MinimizeToTray.Enable(this);
            lastWindowChange.Start();

            //// Add in display content
            var sampleDataSource = SampleDataSource.GetGroup("Group-1");
            this.itemsControl.ItemsSource = sampleDataSource;
        }



        private bool KinectSetup()
        {
            _sensor = KinectSensor.GetDefault();
            if (_sensor == null)
            {
                return false;
            }


            KinectRegion.SetKinectRegion(this, kinectRegion);

            App app = ((App)Application.Current);
            app.KinectRegion = kinectRegion;

            // Use the default sensor
            kinectRegion.KinectSensor = _sensor;

            this.Loaded += Window_Loaded;


            _sensor.Open();

            _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Body);
            _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            return true;
        }


        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);

                    foreach (var body in _bodies)
                    {
                        if (body != null)
                        {
                            if (body.IsTracked)
                            {
                                /*
                                Joint leftHand = body.Joints[JointType.HandLeft];
                                Joint rightHand = body.Joints[JointType.HandRight];
                                CameraSpacePoint cameraPoint = rightHand.Position;
                                ColorSpacePoint colorPoint = _sensor.CoordinateMapper.MapCameraPointToColorSpace(cameraPoint);
                                var screen = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                                float width = screen.Width / 1920;
                                float height = screen.Height / 1080;
                                float scale = 1;

                                if (width < height) scale = width;
                                else scale = height;*/

                                interactingBody = body;

                                rHand = body.HandRightState;
                                lHand = body.HandLeftState;

                                //todo: better gesture
                                if (rHand == HandState.Closed && lHand == HandState.Closed && lastWindowChange.ElapsedMilliseconds > WINDOW_CHANGE_THRESHOLD)
                                {
                                    lastWindowChange.Restart();
                                    if (WindowState == System.Windows.WindowState.Minimized) showWindow();
                                    else hideWindow();
                                }
                                
                            }
                        }
                    }
                }
            }
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            KinectCoreWindow kinectCoreWindow = KinectCoreWindow.GetForCurrentThread();
            kinectCoreWindow.PointerMoved += kinectCoreWindow_PointerMoved;
        }


        private void kinectCoreWindow_PointerMoved(object sender, KinectPointerEventArgs args)
        {
            KinectPointerPoint kinectPointerPoint = args.CurrentPoint;

            //just one of the >2 hands is the active one
            if (!kinectPointerPoint.Properties.IsEngaged) return;

            Point pointRelativeToKinectRegion = new Point(kinectPointerPoint.Position.X * kinectRegion.ActualWidth, kinectPointerPoint.Position.Y * kinectRegion.ActualHeight);

            //NON-MENU INTERACTION
            if (!isMenuOpen)
            {
                if (rHand != HandState.Lasso && rHand != HandState.Closed)
                {
                    myCanvas.updateStrokes(pointRelativeToKinectRegion, SketchCanvas.UserAction.Move);
                }
                else if (rHand == HandState.Lasso)
                {
                    myCanvas.updateStrokes(pointRelativeToKinectRegion, SketchCanvas.UserAction.Draw);
                }
                else
                {
                    myCanvas.updateStrokes(pointRelativeToKinectRegion, SketchCanvas.UserAction.Cancel);
                }
            }

            if (rHand == HandState.Lasso && lHand == HandState.Lasso && lastWindowChange.ElapsedMilliseconds > WINDOW_CHANGE_THRESHOLD)
            {
                lastWindowChange.Restart();
                if (this.itemsControl.IsVisible) hideMenu();
                else showMenu();

            }

        }

        private double calcPointDist(Point a, Point b)
        {
            return Point.Subtract(a, b).LengthSquared;
        }

        private void showMenu()
        {
            this.itemsControl.Visibility = Visibility.Visible;
            this.myCanvas.IsEnabled = false;
            isMenuOpen = true;
        }

        private void hideMenu()
        {
            this.itemsControl.Visibility = Visibility.Hidden;
            this.myCanvas.IsEnabled = true;
            isMenuOpen = false;
        }


        private void hideWindow()
        {
            WindowState = WindowState.Minimized;
        }

        private void showWindow()
        {
            WindowState = WindowState.Maximized;
        }


        private void ButtonClick(object sender, RoutedEventArgs e)
        {


        }
       
    }
}
