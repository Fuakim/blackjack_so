using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using servidor.model;
using autenticacion;


namespace servidor
{
    class Program
    {

        static Thread t;
        static int muerteHilo = 0; 
        static Auth ad = new Auth();

        public static Juego game;

        //static IPHostEntry host = Dns.GetHostEntry("localhost");
        static IPAddress ipAddress =  IPAddress.Parse("25.94.128.49"); //host.AddressList[1]; 
        static IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

        static Socket listener;

        public static List<Socket> socketList = new List<Socket>();
        public static List<Thread> threadList = new List<Thread>();

        static void Main(string[] args)
        {
            game = new Juego();

            Console.WriteLine("Ip del servidor: [{0}]", ipAddress.ToString());
            Console.WriteLine("Ingrese el caracter 's' para iniciar el servidor");

            string p;
            while (true)
            {
                p = Console.ReadLine();
                if (p == "s")
                {
                    t = new Thread(StartServer);
                    t.Start();
                }
                else if (p == "x")
                {
                    //cerrarServidor();
                    break;
                }
                else
                {
                    Console.WriteLine("Solo se permiten los valores mencionados arriba");
                    break;
                }
            }
        }


        public static void StartServer()
        {
            listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(10);
            while (true)
            {
                try
                {
                    if (muerteHilo == 1)
                    {
                        t.Interrupt();
                        break;
                    }
                    Console.WriteLine("Esperando una nueva conexion...");

                    Socket auxSocket = listener.Accept();
                    int pos = isCampoVacio();
                    if(pos >= 0)
                    {
                        socketList[pos] = auxSocket;
                        // se van creando los hilos que administran la juan
                        Thread a = new Thread(recibirJ);
                        threadList[pos]=a;
                        threadList[pos].Start();
                    }
                    else
                    {
                        socketList.Add(auxSocket);
                        // se van creando los hilos que administran la juan
                        Thread a = new Thread(recibirJ);
                        threadList.Add(a);
                        threadList[threadList.IndexOf(a)].Start();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey();
        }

        private static int isCampoVacio()
        {
            for(int i=0; i <= socketList.Count-1; i++)
            {
                if(socketList[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        private static void recibirJ() // hacer validaciones pa cuando se espiche un socket xD
        {
            int posSocket = -1;
            Thread thr = Thread.CurrentThread;
            try
            {
                foreach (Thread thread in threadList)
                {
                    if (thr.Equals(thread))
                    {
                        posSocket = threadList.IndexOf(thread);
                    }
                }
                if (posSocket == -1)
                {
                    thr.Interrupt();
                }
                Dictionary<String, String> msgInput = new Dictionary<String, String>();
                //string jsonString = JsonSerializer.Serialize(appContext);
                byte[] bytes = new byte[1024];
                while (true)
                {

                    int bytesRec = socketList[posSocket].Receive(bytes);
                    msgInput = JsonSerializer.Deserialize<Dictionary<String, String>>(Encoding.UTF8.GetString(bytes, 0, bytesRec));

                    Console.WriteLine("El mensaje [{0}] llego desde el cliente: {1}", getKey(msgInput, "tipo"), posSocket);

                    identificarMsg(socketList[posSocket], msgInput, thr);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Se perdio la conexion con el cliente: {0}", posSocket+1);
                //socketList[posSocket].Shutdown(SocketShutdown.Both);
                //socketList[posSocket].Close();
                Console.WriteLine("hilo {0}", thr.Name);
                if (thr.Name != null) {
                    game.avisarMuerte(thr.Name);
                    thr.Interrupt();
                }
                socketList[posSocket] = null;
                threadList[posSocket] = null;
            }
        }

        private static void identificarMsg(Socket a, Dictionary<String, String> data, Thread thr)
        {
            Dictionary<String, String> msgOutput = new Dictionary<string, string>();
            byte[] msgs;
            switch (getKey(data, "modo"))
            {
                case "connect": // conectarse
                    Console.WriteLine("Conectando a {0}", a.RemoteEndPoint);
                    msgOutput.Add("respuesta","cambio");
                    msgs = setMessage(msgOutput);
                    a.Send(msgs);
                    break;
                case "login": // logearse
                    
                    if (ad.iniciarSesion("UNA", getKey(data, "username"), getKey(data, "password"))) //ad.iniciarSesion("UNA", getKey(data, "username"), getKey(data, "password"))
                    {
                        thr.Name = getKey(data, "username");
                        game.addJugador(new Jugador(getKey(data, "username"), a)); // esto seria despues de que se valide en el active directory
                    }
                    else
                    {
                        msgOutput.Add("respuesta", "incorrecto");
                        msgs = setMessage(msgOutput);
                        a.Send(msgs);
                    }
                    break;
                case "signup": // registrarse
                    if (ad.registrarUsuario(getKey(data, "username"), getKey(data, "password")))
                    {
                        msgOutput.Add("respuesta", "correcto");
                    }
                    else
                    {
                        msgOutput.Add("respuesta", "incorrecto");
                    }

                    msgs = setMessage(msgOutput);
                    a.Send(msgs);
                    break;
                case "juego": // algo
                    game.evaluarMsg(data);
                    break;
                case "end": // salir
                    //game.(data);
                    break;
                default:
                    break;
            }
        }
        


        private static byte[] setMessage(Dictionary<String, String> data) // serializa el mensaje 
        {
            string jsonString = JsonSerializer.Serialize(data);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        private static String getKey(Dictionary<String, String> data, String key)
        {
            data.TryGetValue(key, out string dato);
            return dato;
        }

        public static void salir(Dictionary<String, String> data, Socket a)
        {

            a.Shutdown(SocketShutdown.Both);
            a.Close();
        }

    }
}
