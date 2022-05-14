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
using cliente.model;
using System.Linq;

namespace cliente.view
{
    /// <summary>
    /// Lógica de interacción para TableroView.xaml
    /// </summary>
    public partial class TableroView : Page
    {
        private List<Jugador> jugadores = new List<Jugador>();
        private Jugador crupier;

        private Dictionary<String, Object> globalData = new Dictionary<string, object>();
        private static Frame view;
        private static byte[] bytes = new byte[1024];
        private static Socket socket;
        private static Thread thread;
        private static int muerteHilo = 0;

        public TableroView(Frame w, Dictionary<String, Object> data)
        {
            view = w;
            InitializeComponent();
            globalData = data;
            socket = (Socket)getObjectKey(globalData, "socket");
            inicializarTablero();

            thread = new Thread(recibirM);
            thread.Start();

            btn_pedir.IsEnabled = false;
            btn_plantar.IsEnabled = false;

            Dictionary<String, String> msgInput = new Dictionary<String, String>();
            msgInput.Add("modo", "juego");
            msgInput.Add("tipo", "entrar");
            msgInput.Add("jugador", (String)getObjectKey(globalData, "jugador"));
            Console.WriteLine((String)getObjectKey(globalData, "jugador"));
            sendMessage(msgInput);

        }

        private void onClickBtnPedir(object sender, RoutedEventArgs e)
        {
            if (apuestaValida())
            {
                Dictionary<String, String> msgInput = new Dictionary<String, String>();
                msgInput.Add("modo", "juego");
                msgInput.Add("tipo", "pedir");
                msgInput.Add("jugador", jugadores[0].getNombre());
                msgInput.Add("apuesta", txt_apuesta.Text);
                sendMessage(msgInput);
                lbl_info.Content = "";
                btn_pedir.IsEnabled = false;
                btn_plantar.IsEnabled = false;
            } else
            {
                lbl_info.Content = "*Ingrese una apuesta valida";
            }
        }

        private void onClickBtnPlantar(object sender, RoutedEventArgs e)
        {
            if (apuestaValida())
            {
                Dictionary<String, String> msgInput = new Dictionary<String, String>();
                msgInput.Add("modo", "juego");
                msgInput.Add("tipo", "plantar");
                msgInput.Add("jugador", jugadores[0].getNombre());
                msgInput.Add("apuesta", txt_apuesta.Text);
                sendMessage(msgInput);
                lbl_info.Content = "";
                btn_pedir.IsEnabled = false;
                btn_plantar.IsEnabled = false;
            }
            else
            {
                lbl_info.Content = "*Ingrese una apuesta valida";
            }
        }

        private void onClickBtnSalir(object sender, RoutedEventArgs e)
        {
            /*Dictionary<String, String> msgInput = new Dictionary<String, String>();
            msgInput.Add("modo", "juego");
            msgInput.Add("tipo", "salir");
            msgInput.Add("jugador", jugadores[0].getNombre());
            sendMessage(msgInput);*/
            socket.Close();
            view.Content = new InicioView(view);
            //System.Windows.Application.Current.Shutdown();
        }

        private bool apuestaValida()
        {
            string apuesta = txt_apuesta.Text;
            if (Int32.TryParse(apuesta, out int result))
            {
                if ( result >= 10 && result <= 50)
                {
                    return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }
        private void recibirM()
        {
            String msg;
            Dictionary<String, String> msgInput = new Dictionary<String, String>();
            try
            {
                while (true)
                {
                    Console.WriteLine("inicio del ailo");
                    if (muerteHilo == 1)
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                        thread.Interrupt();
                        break;
                    }
                    int bytesRec = socket.Receive(bytes);
                    msgInput = JsonSerializer.Deserialize<Dictionary<String, String>>(Encoding.UTF8.GetString(bytes, 0, bytesRec));

                    this.Dispatcher.Invoke(() =>
                    {
                        identificarMsg(msgInput);
                    });

                    msgInput.TryGetValue("tipo", out msg);
                    if (msg == "end")
                    {
                        muerteHilo = 1;
                    }
                }
            } catch (Exception e)
            {
                this.Dispatcher.Invoke(() =>
                {
                    lbl_info.Content = "*Ha ocurrido un error \n con el servidor, \n se cerrara la ventana";
                    Thread.Sleep(3000);
                    view.Content = new InicioView(view);
                });
            }
           
        }

        private void identificarMsg(Dictionary<String, String> msg)
        {
            if (getKey(msg, "modo") == "juego") // si al final no hay otras de modo juego se quita este if
            {
                switch (getKey(msg, "tipo"))
                {
                    case "partida":
                        cargarPartida(msg);
                        break;
                    case "inicio":
                        iniciar(msg);
                        break;
                    case "turno": // resaltar el nombre del jugador en turno
                        darTurno(msg);
                        break;
                    case "plantar":
                        plantar(msg);
                        break;
                    case "pedir":
                        pedir(msg);
                        break;
                    case "perdio":
                        perdio(msg);
                        break;
                    case "carta":
                        darCartas(msg);
                        break;
                    case "revelar":
                        revelarCartas();
                        break;
                    case "ganador":
                        ganadorRonda(msg);
                        break;
                    case "nuevo jugador":
                        nuevoJugador(msg);
                        break;
                    case "remover jugador": // cuando un jugador se desconecte
                        removerJugador(msg);
                        break;
                    default:
                        break;
                }
            }
        }

        private void cargarPartida(Dictionary<String, String> msg)
        {
            string[] nombre = getKey(msg, "nombres").Split("|:|");
            string[] dinero = getKey(msg, "dineros").Split("|:|");
            string[] carta1 = getKey(msg, "cartas1").Split("|:|");
            string[] carta2 = getKey(msg, "cartas2").Split("|:|");
            string[] carta3 = getKey(msg, "cartas3").Split("|:|");

            jugadores[0].activar(getKey(msg, "jugador"), getKey(msg, "dinero"));

            crupier.setCarta1(carta1[0]);
            crupier.setCarta2(carta2[0]);
            crupier.setCarta3(carta3[0]);
            for (int i = 1; i<= nombre.Length-2; i++)
            {
                foreach(Jugador j in jugadores)
                {
                    if (!j.isActivo() && nombre[i] != jugadores[0].getNombre())
                    {
                        j.activar(nombre[i], dinero[i]);
                        j.setCarta1(carta1[i]);
                        j.setCarta2(carta2[i]);
                        j.setCarta3(carta3[i]);
                        break;
                    }
                }
            }
            // enviar mensaje de que se esta listo
            Dictionary<String, String> msgOutput = new Dictionary<string, string>();
            msgOutput.Add("modo","juego");
            msgOutput.Add("jugador", (String) getObjectKey(globalData, "jugador"));
            msgOutput.Add("tipo", "listo");
            sendMessage(msgOutput);

        }

        private void iniciar(Dictionary<String, String> msg)
        {
            jugadores[0].activar(getKey(msg, "jugador"), getKey(msg, "dinero"));

            Dictionary<String, String> msgOutput = new Dictionary<string, string>();
            msgOutput.Add("modo", "juego");
            msgOutput.Add("jugador", (String)getObjectKey(globalData, "jugador"));
            msgOutput.Add("tipo", "listo");
            sendMessage(msgOutput);
        }

        private void darTurno(Dictionary<String, String> msg)
        {
            if (jugadores[0].getNombre() == getKey(msg, "jugador"))
            {
                
                btn_pedir.IsEnabled = true;
                btn_plantar.IsEnabled = true;
                lblRonda.Content += "\n Tu turno ";
            }
            else
            {
                btn_pedir.IsEnabled = false;
                btn_plantar.IsEnabled = false;
                for (int i=1; i<7; i++) // cambiar a foreach
                {
                    if (jugadores[i].getNombre() == getKey(msg, "jugador"))
                    {
                        //cambiarle el color al tag del usuario
                        lblRonda.Content += "\n Es el turno del jugador " + jugadores[i].getNombre();
                        break;
                    }
                }
            }
        }

        private void plantar(Dictionary<String, String> msg)
        {

            if (getKey(msg, "jugador").Equals("crupier"))
            {
                lblRonda.Content += "\n El Crupier se planto";
            }
            else
            {
                foreach (Jugador jugador in jugadores)
                {
                    if (jugador.getNombre() == getKey(msg, "jugador"))
                    {
                        lblRonda.Content += "\n El jugador " + jugador.getNombre() + " se planto, \napostando " + getKey(msg, "apuesta");
                        break;
                    }
                }
            }
        }

        private void pedir(Dictionary<String, String> msg)
        {
            if (getKey(msg, "jugador").Equals("crupier"))
            {
                crupier.setCarta3(getKey(msg, "carta3"));
                lblRonda.Content += "\n El Crupier pidio una carta";
            }
            else
            {
                foreach (Jugador jugador in jugadores)
                {
                    if (jugador.getNombre() == getKey(msg, "jugador"))
                    {
                        jugador.setCarta3(getKey(msg, "carta3"));
                        lblRonda.Content += "\n El jugador " + jugador.getNombre() + " pidio una carta, \napostando " + getKey(msg, "apuesta");
                        break;
                    }
                }
            }
            
        }

        private void perdio(Dictionary<String, String> msg)
        {
            if (getKey(msg, "jugador").Equals("crupier"))
            {
                lblRonda.Content += "\n El crupier  perdio por pasarse  \n de 21 al pedir una carta";
                crupier.limpiarMano();
                crupier.setCarta1("none");
            }
            else
            {
                foreach (Jugador jugador in jugadores)
                {
                    if (jugador.getNombre() == getKey(msg, "jugador"))
                    {
                        lblRonda.Content += "\n El jugador " + jugador.getNombre() + " perdio por pasarse  \n de 21 al pedir una carta, \napostando " + getKey(msg, "apuesta");
                        jugador.setDinero(getKey(msg, "dinero"));
                        jugador.limpiarMano();
                        jugador.setCarta1("none");
                    }
                }
            }
        }

        private void darCartas(Dictionary<String, String> msg) // creo qeu esto va a fallar
        {
            if (getKey(msg, "jugador") == "crupier")
            {
                lblRonda.Content = "Iniciando nueva ronda";
                crupier.setCarta1(getKey(msg, "carta1"));
                crupier.setCarta2(getKey(msg, "carta2"));
            }
            foreach (Jugador jugador in jugadores)
            {
                if (jugador.getNombre() == getKey(msg, "jugador"))
                {
                    jugador.setCarta1(getKey(msg, "carta1"));
                    jugador.setCarta2(getKey(msg, "carta2"));
                    if (jugador.getNombre() == jugadores[0].getNombre())
                    {
                        jugador.revelarCarta1();
                    }
                }
            }
        }

        private void revelarCartas()
        {
            crupier.revelarCarta1();
            foreach (Jugador jugador in jugadores)
            {
                if (jugador.isActivo())
                {
                    jugador.revelarCarta1();
                }
            }
        }

        private void ganadorRonda(Dictionary<String, String> msg)
        {

            lblRonda.Content += "\n Se acabo la ronda";


            if (getKey(msg, "jugador").Equals("crupier"))
            {
                lblRonda.Content += "\n El Ganador fue el crupier con una ganancia de \n" + getKey(msg, "ganancia"); 
            }
            foreach (Jugador jugador in jugadores)
            {
                if (jugador.isActivo())
                {
                    jugador.limpiarMano();
                    if(jugador.getNombre() == getKey(msg, "jugador"))
                    {
                        lblRonda.Content += "\n El Ganador fue " + jugador.getNombre() + " con una ganancia de: \n $" + getKey(msg, "ganancia"); // estaria bien poner las sumas de todos en la juan
                        jugador.setDinero(getKey(msg, "dinero"));
                    }
                }
            }

            string[] nombre = getKey(msg, "nombres").Split("|:|");
            string[] dinero = getKey(msg, "dineros").Split("|:|");
            for (int i = 0; i< nombre.Length-1; i++)
            {
                foreach(Jugador j in jugadores)
                {
                    if (j.getNombre() == nombre[i])
                    {
                        j.setDinero(dinero[i]);
                        break;
                    }
                }
            }

            crupier.limpiarMano();
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


        private void nuevoJugador(Dictionary<String, String> msg)
        {
            foreach (Jugador j in jugadores)
            {
                if (!j.isActivo() && jugadores[0].getNombre() != getKey(msg, "jugador"))
                {
                    //Se desactiva cuando lo encuentra
                    j.activar(getKey(msg, "jugador"), getKey(msg, "dinero"));
                    break;
                }
            }
        }

        private void removerJugador(Dictionary<String, String> msg)
        {
            foreach (Jugador j in jugadores)
            {
                if (j.getNombre() == getKey(msg, "jugador"))
                {
                    lblRonda.Content += "\n El jugador " + j.getNombre() + " se desconecto";
                    j.desactivar();
                    break;
                }
            }
        }


        private void inicializarTablero()
        {
            crupier = new Jugador(null, null, C1J7, C2J7, C3J7);
            //Jugador principal
            jugadores.Add(new Jugador(U0, D0, C1J0, C2J0, C3J0));
            //Demas jugadores
            jugadores.Add(new Jugador(U2, D2, C1J2, C2J2, C3J2));
            jugadores.Add(new Jugador(U5, D5, C1J5, C2J5, C3J5));
            jugadores.Add(new Jugador(U1, D1, C1J1, C2J1, C3J1));
            jugadores.Add(new Jugador(U6, D6, C1J6, C2J6, C3J6));
            jugadores.Add(new Jugador(U3, D3, C1J3, C2J3, C3J3));
            jugadores.Add(new Jugador(U4, D4, C1J4, C2J4, C3J4));

            foreach (Jugador jugador in jugadores)
            {
                jugador.desactivar();
            }
            crupier.limpiarMano();
        }

    }
}
