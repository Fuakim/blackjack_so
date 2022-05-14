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
    /// Lógica de interacción para Page1.xaml
    /// </summary>
    public partial class LoginView : Page
    {

        private Dictionary<String, Object> globalData = new Dictionary<string, object>();


        public static Frame view;
        static byte[] bytes = new byte[1024];
        static Socket socket;
        static Thread t;
        static int muerteHilo = 0;

        public LoginView(Frame w, Dictionary<String, Object> data)
        {
            InitializeComponent();
            view = w;
            globalData = data;
            socket = (Socket) getObjectKey(globalData, "socket");
        }

        private void cerrarM()
        {
            Dictionary<String, String> data = new Dictionary<String, String>();
            data.Add("tipo", "end");
            sendMessage(data);
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

        private void onClickBtnRegistrarView(object sender, RoutedEventArgs e)
        {
            view.Content = new SignUpView(view, globalData);
        }

        private void onClickBtnIngresar(object sender, RoutedEventArgs e)
        {
            // hacer validacion de que no pueden estar los campos vacios

            if (!string.IsNullOrEmpty(txt_username.Text) && !string.IsNullOrEmpty(txt_password.Password))
            {
                Dictionary<String, String> msgOutput = new Dictionary<String, String>();
                msgOutput.Add("modo", "login");
                msgOutput.Add("username", txt_username.Text);
                msgOutput.Add("password", txt_password.Password);
                sendMessage(msgOutput);

                Dictionary<String, String> msgInput;
                int bytesRec = socket.Receive(bytes);
                msgInput = JsonSerializer.Deserialize<Dictionary<String, String>>(Encoding.UTF8.GetString(bytes, 0, bytesRec));

                if (getKey(msgInput, "respuesta") == "entrar")
                {
                    globalData.Add("jugador", txt_username.Text);
                    view.Content = new TableroView(view, globalData); //pa la de juego
                    //view.Content = new EsperaView(view, globalData);
                }
                else if (getKey(msgInput, "respuesta") == "esperar")
                    {
                        globalData.Add("jugador", txt_username.Text);
                        view.Content = new EsperaView(view, globalData); //pa la de juego
                    } else { lbl_info.Content = "*Credenciales no encontradas"; }

            } else
            {
                lbl_info.Content = "*Debe llenar ambos campos";
            }

        }
    }
}
