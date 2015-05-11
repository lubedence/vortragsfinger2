using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace kinectTest
{
    /// <summary>
    /// Interaktionslogik für SketchCanvas.xaml
    /// </summary>
    public partial class SketchCanvas : InkCanvas
    {

        private const int LINE_RESUME_THRESHOLD     = 100;
        private const int RUBBER_SIZE               = 30;

        public enum UserAction {Draw, Move, Cancel};

        private List<Point> stroke = new List<Point>();
        private Point lastDrawnPoint;
        bool isUserDrawing = false;

        private double lineThickness = 10;
        private Color lineColor = Color.FromRgb(0, 0, 0);

        public double LineThickness
        {
            get { return this.lineThickness; }
            set { this.lineThickness = value; }
        }

        public Color LineColor
        {
            get { return this.lineColor; }
            set { this.lineColor = value; }
        }

        public SketchCanvas()
        {
            InitializeComponent();
        }

        private void drawStrokePart(Point nextPoint)
        {
            if (!isUserDrawing)
            {
                isUserDrawing = true;
            }

            Line line = new Line();
            line.Stroke = new SolidColorBrush(lineColor);

            line.StrokeThickness = lineThickness*1.5;

            line.X1 = lastDrawnPoint.X;
            line.Y1 = lastDrawnPoint.Y;
            line.X2 = nextPoint.X;
            line.Y2 = nextPoint.Y;
            line.StrokeDashCap = PenLineCap.Round;
            line.StrokeStartLineCap = PenLineCap.Round;
            line.StrokeEndLineCap = PenLineCap.Round;
            this.Children.Add(line);

            if(stroke.Count == 0)
                stroke.Add(new Point(lastDrawnPoint.X, lastDrawnPoint.Y));

            stroke.Add(nextPoint);
            
            lastDrawnPoint = nextPoint;
        }

        private void evaluateStrokePart(Point nextPoint)
        {
            int distToLastDrawnPoint = (int)Math.Sqrt(Math.Pow((lastDrawnPoint.X - nextPoint.X), 2) + Math.Pow((lastDrawnPoint.Y - nextPoint.Y), 2));

            if (isUserDrawing && distToLastDrawnPoint > LINE_RESUME_THRESHOLD)
            {
                isUserDrawing = false;

                if (stroke.Count > 0)
                {
                    Stroke _s = new Stroke(new StylusPointCollection(stroke));
                    _s.DrawingAttributes.Color = lineColor;
                    _s.DrawingAttributes.Width = lineThickness*0.8;
                    _s.DrawingAttributes.Height = lineThickness;
                    this.Strokes.Add(_s);
                    stroke.Clear();
                }
                this.Children.Clear();

            }

            if (distToLastDrawnPoint > LINE_RESUME_THRESHOLD)
                lastDrawnPoint = new Point(nextPoint.X, nextPoint.Y);
        }

        private void deleteStrokes(Point nextPoint)
        {
            this.Strokes.Remove(this.Strokes.HitTest(nextPoint, RUBBER_SIZE));
        }

        public void updateStrokes(Point nextPoint, UserAction ua)
        {
            switch (ua)
            {
                case UserAction.Move:
                    evaluateStrokePart(nextPoint);
                    break;
                case UserAction.Draw:
                    drawStrokePart(nextPoint);
                    break;
                case UserAction.Cancel:
                    deleteStrokes(nextPoint);
                    break;
                default:
                    return;
            }
        }
    }
}
