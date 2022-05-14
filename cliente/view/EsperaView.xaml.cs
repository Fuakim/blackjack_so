using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace cliente.view
{
    /// <summary>
    /// Lógica de interacción para EsperaView.xaml
    /// </summary>
    public partial class EsperaView : Page
    {

        private Dictionary<String, Object> globalData = new Dictionary<string, object>();

        public static Frame view;
        static byte[] bytes = new byte[1024];
        static Socket socket;
        static Thread thread;
        static int muerteHilo = 0;
        bool global = false;

        public EsperaView(Frame w, Dictionary<String, Object> data)
        {

            view = w;
            globalData = data;
            InitializeComponent();
            socket = (Socket)getObjectKey(globalData, "socket");
            thread = new Thread(esperaT);
            thread.Start();
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

        private void espera(object sender, MouseEventArgs e)
        {
            if (!global)
            {
                global = true;
                
            }
        }

        private void esperaT()
        {
            try
            {
                Dictionary<String, String> msgInput;
                int bytesRec = socket.Receive(bytes);
                msgInput = JsonSerializer.Deserialize<Dictionary<String, String>>(Encoding.UTF8.GetString(bytes, 0, bytesRec));

                if (getKey(msgInput, "respuesta") == "entrar")
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        view.Content = new TableroView(view, globalData);
                    });
                    
                }
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(() =>
                {
                    view.Content = new InicioView(view);
                });
                
            }
        }
    }
}
