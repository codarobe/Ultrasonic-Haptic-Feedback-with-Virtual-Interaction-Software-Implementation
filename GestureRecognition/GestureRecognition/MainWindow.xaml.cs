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

namespace GestureRecognition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void kinectViewerButton_Click(object sender, RoutedEventArgs e)
        {
            Window window = new KinectDisplayWindow();
            window.Show();
        }

        private void graphicsViewerButton_Click(object sender, RoutedEventArgs e)
        {
            using (GraphicsViewer viewer = new GraphicsViewer())
            {
                viewer.Run(30, 30);
            }
        }

        private void networkedGesturesButton_Click(object sender, RoutedEventArgs e)
        {
            Window window = new NetworkedGestures();
            window.Show();
        }
    }
}
