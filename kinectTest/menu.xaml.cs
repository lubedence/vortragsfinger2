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

namespace kinectTest
{
    /// <summary>
    /// Interaktionslogik für menu.xaml
    /// </summary>
    public partial class menu : UserControl
    {

        public delegate void ColorChangedEventHandler(Color newColor);
        public event ColorChangedEventHandler ColorChanged;

        public delegate void ThicknessChangedEventHandler(double newThickness);
        public event ThicknessChangedEventHandler ThicknessChanged;

        public delegate void DrawTypeChangedEventHandler(kinectTest.SketchCanvas.DrawType dt);
        public event DrawTypeChangedEventHandler DrawTypeChanged;

        public menu()
        {
            InitializeComponent();
        }

        private void onColorSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double val = e.NewValue;
            SolidColorBrush brushColor = (SolidColorBrush)this.Resources["brushColor"];
            brushColor.Color = Color.FromRgb((byte)val, (byte)val, (byte)val);

            if (this.ColorChanged != null)
            {
                this.ColorChanged(brushColor.Color);
            }
        }

        private void onLineSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.ThicknessChanged != null)
            {
                this.ThicknessChanged(e.NewValue);
            }
        }

        private void RadioButton_Checked_Freehand(object sender, RoutedEventArgs e)
        {
            if (this.DrawTypeChanged != null)
            {
                this.DrawTypeChanged(kinectTest.SketchCanvas.DrawType.Freehand);
            }
        }

        private void RadioButton_Checked_FreehandStraight(object sender, RoutedEventArgs e)
        {
            if (this.DrawTypeChanged != null)
            {
                this.DrawTypeChanged(kinectTest.SketchCanvas.DrawType.FreehandStraight);
            }
        }

        private void RadioButton_Checked_Line(object sender, RoutedEventArgs e)
        {
            if (this.DrawTypeChanged != null)
            {
                this.DrawTypeChanged(kinectTest.SketchCanvas.DrawType.Line);
            }
        }
    }
}
