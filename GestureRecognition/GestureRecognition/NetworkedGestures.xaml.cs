using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GestureRecognition
{
    /// <summary>
    /// Interaction logic for NetworkedGestures.xaml
    /// </summary>
    public partial class NetworkedGestures : Window, GraphicsManipulator
    {
        GestureMessage message = new GestureMessage();
        Socket socket;
        GestureRecognizer recognizer;

        public NetworkedGestures()
        {
            socket = new Socket(SocketType.Stream, ProtocolType.IP);
            recognizer = new GestureRecognizer(this);

            InitializeComponent();
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
           
            IPAddress ip = IPAddress.Parse(ipInput.Text);
            int port = Int32.Parse(portInput.Text);
            IPEndPoint endPoint = new IPEndPoint(ip, port);

            try
            {
                socket.Connect(endPoint);
            }
            catch (SocketException se)
            {
                statusText.Content = "Status: Failed";
                socket = new Socket(SocketType.Stream, ProtocolType.IP);
            }

            if (socket.Connected)
            {
                statusText.Content = "Status: Connected";
            }
            
        }

        public void Translate(float x, float y, float z)
        {
            byte[] buffer = message.generateTranslationMessage(x, y, z);
            SendMessage(buffer);
        }

        public void Rotate(float x, float y, float z)
        {
            byte[] buffer = message.generateRotationMessage(x, y, z);
            SendMessage(buffer);
        }

        public void Scale(float scale)
        {
            byte[] buffer = message.generateScaleMessage(scale);
            SendMessage(buffer);
        }

        private void SendMessage(byte[] message)
        {
            try
            {
                socket.Send(message);
            }
            catch (SocketException se)
            {
                Console.WriteLine("Failed to send message");
            }
        }
    }
}
