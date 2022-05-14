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
    /// Lógica de interacción para SignUpView.xaml
    /// </summary>
    public partial class SignUpView : Page
    {

        public static Frame view;
        private Dictionary<String, Object> globalData = new Dictionary<string, object>();
        static Socket socket;
        static byte[] bytes = new byte[1024];
        public SignUpView(Frame v, Dictionary<String, Object> data)
        {
            
            InitializeComponent();
            view = v;
            globalData = data;
            socket = (Socket)getObjectKey(globalData, "socket");
        }

        private void onClickBtnLogView(object sender, RoutedEventArgs e)
        {
            view.Content = new LoginView(view, globalData);
        }

        private void onClickBtnRegistrar(object sender, RoutedEventArgs e)
        {

            if (!string.IsNullOrEmpty(txt_username.Text) && !string.IsNullOrEmpty(txt_password.Text))
            {
                Dictionary<String, String> msgOutput = new Dictionary<String, String>();
                msgOutput.Add("modo", "signup");
                msgOutput.Add("username", txt_username.Text);
                msgOutput.Add("password", txt_password.Text);
                sendMessage(msgOutput);

                Dictionary<String, String> msgInput;
                int bytesRec = socket.Receive(bytes);
                msgInput = JsonSerializer.Deserialize<Dictionary<String, String>>(Encoding.UTF8.GetString(bytes, 0, bytesRec));
                if (getKey(msgInput, "respuesta") == "correcto")
                {
                    view.Content = new LoginView(view, globalData);
                }
                else
                {
                    Console.WriteLine("pues algo peto");
                }
            } else
            {
                lbl_info.Content = "*Debe llenar todos los campos";
            }
        }

        private void sendMessage(Dictionary<String, String> data) // serializa el mensaje y lo envia
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

        private static Object getObjectKey(Dictionary<String, Object> data, String key)
        {
            data.TryGetValue(key, out Object dato);
            return dato;
        }

    }
}
