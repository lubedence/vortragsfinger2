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
using Microsoft.Kinect.VisualGestureBuilder;

namespace kinectTest
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int       LINE_RESUME_THRESHOLD       = 100;
        private const int       WINDOW_CHANGE_THRESHOLD     = 1000;
        private const double    GESTURE_CONFIDENCE_MIN      = 0.85;

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IList<Body> _bodies;
        VisualGestureBuilderFrameSource _gestureSource;
        VisualGestureBuilderFrameReader _gestureReader; 
        Body interactingBody = null;

        HandState rHand = HandState.Unknown;
        HandState lHand = HandState.Unknown;
        Stopwatch lastWindowChange = new Stopwatch();

        Gesture openingGesture;
        Gesture closingGesture;

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
        }



        //TODO: Close sensor method & call
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
            _reader.MultiSourceFrameArrived += OnMultiSourceFrameArrived;

            LoadGestures();

            return true;
        }


        void LoadGestures()
        {
            VisualGestureBuilderDatabase db = new VisualGestureBuilderDatabase(@"gestures/gestures1.gbd");
            this.openingGesture = db.AvailableGestures.Where(g => g.Name == "HandsApart").Single();
            this.closingGesture = db.AvailableGestures.Where(g => g.Name == "HandsTogether").Single();

            this._gestureSource = new VisualGestureBuilderFrameSource(this._sensor, 0);

            this._gestureSource.AddGesture(this.openingGesture);
            this._gestureSource.AddGesture(this.closingGesture);

            this._gestureSource.TrackingIdLost += OnTrackingIdLost;

            this._gestureReader = this._gestureSource.OpenReader();
            this._gestureReader.IsPaused = true;
            this._gestureReader.FrameArrived += OnGestureFrameArrived; 
        }

        void OnTrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            this._gestureReader.IsPaused = true;
        }

        void OnGestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    var discreteResults = frame.DiscreteGestureResults;
                    if (discreteResults == null) return;

                    if (discreteResults.ContainsKey(this.closingGesture))
                    {
                        var result = discreteResults[this.closingGesture];
                        if (result.Detected && result.Confidence > GESTURE_CONFIDENCE_MIN && lastWindowChange.ElapsedMilliseconds > WINDOW_CHANGE_THRESHOLD)
                        {

                            lastWindowChange.Restart();

                            //Closing Gesture started
                            if (isMenuOpen)
                            {
                                hideMenu();
                            }
                            else if (WindowState != System.Windows.WindowState.Minimized)
                            {
                                hideWindow();
                            }

                            d_closeGesture.Content = "Close: " + result.Confidence;
                        }
                    }
                    
                    if (discreteResults.ContainsKey(this.openingGesture))
                    {
                        var result = discreteResults[this.openingGesture];
                        if (result.Detected && result.Confidence > GESTURE_CONFIDENCE_MIN && lastWindowChange.ElapsedMilliseconds > WINDOW_CHANGE_THRESHOLD)
                        {

                            lastWindowChange.Restart();

                            //Opening Gesture started
                            if (WindowState == System.Windows.WindowState.Minimized)
                            {
                                showWindow();
                            }
                            else if(!isMenuOpen){
                                showMenu();
                            }

                            d_openGesture.Content = "Open: " + result.Confidence;
                        }
                    }
                }
            }
        }


        void OnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);

                    var trackedBody = this._bodies.Where(b => b.IsTracked).FirstOrDefault();


                    if (trackedBody != null)
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

                                if (this._gestureReader != null && this._gestureReader.IsPaused)
                                {
                                    this._gestureSource.TrackingId = trackedBody.TrackingId;
                                    this._gestureReader.IsPaused = false;
                                }

                                interactingBody = trackedBody;

                                rHand = trackedBody.HandRightState;
                                lHand = trackedBody.HandLeftState;
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

        }

        private double calcPointDist(Point a, Point b)
        {
            return Point.Subtract(a, b).LengthSquared;
        }

        private void showMenu()
        {
            this.navigationRegion.Visibility = Visibility.Visible;
            this.myCanvas.IsEnabled = false;
            isMenuOpen = true;
        }

        private void hideMenu()
        {
            this.navigationRegion.Visibility = Visibility.Hidden;
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

    }
}
