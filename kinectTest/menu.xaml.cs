﻿using System;
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
        public menu()
        {
            InitializeComponent();
        }

        private void onColorSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double val = e.NewValue;
            SolidColorBrush brushColor = (SolidColorBrush)this.Resources["brushColor"];
            brushColor.Color = Color.FromRgb((byte)val, (byte)val, (byte)val);
        }

        private void onLineSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double val = e.NewValue;
        }
    }
}
