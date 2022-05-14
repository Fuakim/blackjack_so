using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;


namespace cliente.view
{
    /// <summary>
    /// Lógica de interacción para InicioView.xaml
    /// </summary>
    public partial class InicioView : Page
    {

        public static Frame view;
        static byte[] bytes = new byte[1024];
        static Socket socket;

        static Dictionary<String, Object> globalData = new Dictionary<string, object>();

        public InicioView(Frame w)
        {
            globalData = new Dictionary<string, object>();
            view = w;
            InitializeComponent();
        }

        private void btn_ip_Click(object sender, RoutedEventArgs e)
        {
            if (txt_ip.Text != "")
            {
                StartClient(txt_ip.Text);
            }
        }


        public static void StartClient(String ip)
        {
            try
            {
                //IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = IPAddress.Parse(ip);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);
                
                // Create a TCP/IP  socket.    
                socket = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    // Connect to Remote EndPoint  
                    socket.Connect(remoteEP);
                    globalData.Add("socket", socket);

                    Dictionary<String, String> msgOutput = new Dictionary<String, String>();
                    msgOutput.Add("modo", "connect");
                    sendMessage(msgOutput);

                    Dictionary<String, String> data = new Dictionary<String, String>();
                    int bytesRec = socket.Receive(bytes);
                    data = JsonSerializer.Deserialize<Dictionary<String, String>>(Encoding.UTF8.GetString(bytes, 0, bytesRec));
                    if (getKey(data, "respuesta") == "cambio")
                    {
                        view.Content = new LoginView(view, globalData);
                    }
                    else
                    {

                    }
                    //LoginView.m.Status = msg;
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void sendMessage(Dictionary<String, String> data) // serializa el mensaje y lo envia
        {
            string jsonString = JsonSerializer.Serialize(data);
            byte[] msg = Encoding.UTF8.GetBytes(jsonString); //ASCII.GetBytes(jsonString);
            socket.Send(msg);
        }
        private static String getKey(Dictionary<String, String> data, String key)
        {
            data.TryGetValue(key, out string dato);
            return dato;
        }
    }
}
