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
using Microsoft.Kinect.Toolkit.Input;

namespace kinectTest
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IList<Body> _bodies;
        VisualGestureBuilderFrameSource _gestureSource;
        VisualGestureBuilderFrameReader _gestureReader; 
        Body interactingBody = null;

        HandState rHand = HandState.Unknown;
        HandState lHand = HandState.Unknown;
        Stopwatch lastGestureTime = new Stopwatch();

        Gesture openingGesture;
        Gesture closingGesture;

        bool isMenuOpen = false;

        private List<Point> stroke = new List<Point>();

        public MainWindow()
        {
            InitializeComponent();
            KinectSetup();
            MinimizeToTray.Enable(this);
            lastGestureTime.Start();

            kinectMenu.ColorChanged += new menu.ColorChangedEventHandler(kinectMenu_ColorChanged);
            kinectMenu.ThicknessChanged += new menu.ThicknessChangedEventHandler(kinectMenu_ThicknessChanged);
            kinectMenu.DrawTypeChanged += new menu.DrawTypeChangedEventHandler(kinectMenu_DrawTypeChanged);
            
            kinectMenu_ThicknessChanged(10);
        }

        private void kinectMenu_ColorChanged(Color c)
        {
            myCanvas.LineColor = c;
            myCanvas.DefaultDrawingAttributes.Color = c;
        }

        private void kinectMenu_ThicknessChanged(Double t)
        {
            myCanvas.LineThickness = t;
            myCanvas.DefaultDrawingAttributes.Height = t;
            myCanvas.DefaultDrawingAttributes.Width = t;
        }

        private void kinectMenu_DrawTypeChanged(kinectTest.SketchCanvas.DrawType dt)
        {
            myCanvas.LineDrawType = dt;
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
                        if (result.Detected && result.Confidence > Properties.Settings.Default.GESTURE_MIN_CONFIDENCE && lastGestureTime.ElapsedMilliseconds > Properties.Settings.Default.GESTURE_MIN_TIME)
                        {

                            lastGestureTime.Restart();

                            //Closing Gesture started
                            if (isMenuOpen)
                            {
                                hideMenu();
                            }
                            else if (WindowState != System.Windows.WindowState.Minimized)
                            {
                                hideWindow();
                            }

                        }
                    }
                    
                    if (discreteResults.ContainsKey(this.openingGesture))
                    {
                        var result = discreteResults[this.openingGesture];
                        if (result.Detected && result.Confidence > Properties.Settings.Default.GESTURE_MIN_CONFIDENCE && lastGestureTime.ElapsedMilliseconds > Properties.Settings.Default.GESTURE_MIN_TIME)
                        {

                            lastGestureTime.Restart();

                            //Opening Gesture started
                            if (WindowState == System.Windows.WindowState.Minimized)
                            {
                                showWindow();
                            }
                            else if(!isMenuOpen){
                                showMenu();
                            }
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
                    else
                    {
                        OnTrackingIdLost(null, null);
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

            //CANVAS INTERACTION
            if (!isMenuOpen)
            {
                if ((kinectPointerPoint.Properties.HandType == HandType.RIGHT && rHand == HandState.Open) || (kinectPointerPoint.Properties.HandType == HandType.LEFT && lHand == HandState.Open))
                {
                    myCanvas.updateStrokes(pointRelativeToKinectRegion, SketchCanvas.UserAction.Move);
                }
                else if ((kinectPointerPoint.Properties.HandType == HandType.RIGHT && rHand == HandState.Lasso) || (kinectPointerPoint.Properties.HandType == HandType.LEFT && lHand == HandState.Lasso))
                {
                    myCanvas.updateStrokes(pointRelativeToKinectRegion, SketchCanvas.UserAction.Draw);
                }
                else if ((kinectPointerPoint.Properties.HandType == HandType.RIGHT && rHand == HandState.Closed) || (kinectPointerPoint.Properties.HandType == HandType.LEFT && lHand == HandState.Closed))
                {
                    myCanvas.updateStrokes(pointRelativeToKinectRegion, SketchCanvas.UserAction.Cancel);
                }
            }

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

        private void Menu_Button_Click(object sender, RoutedEventArgs e)
        {
            if (isMenuOpen)
            {
                hideMenu();
            }
            else
            {
                showMenu();
            }
        }

        private void Activate_Mouse_Erase_Mode(object sender, RoutedEventArgs e)
        {
            myCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
        }

        private void Activate_Mouse_Draw_Mode(object sender, RoutedEventArgs e)
        {
            myCanvas.EditingMode = InkCanvasEditingMode.Ink;
        }

    }
}
