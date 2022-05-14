using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace servidor.model
{
    public class Jugador
    {

        private String nombre;
        private Socket socket;

        private List<Carta> mano;

        private int dinero;
        private int apuesta;
        private String estado; // J = jugando, R=listo, E=entrando, W=Esperando N=ninguno
        //private Thread thread;

        public Jugador()
        {

        }

        public Jugador(String nom, Socket s)
        {
            nombre = nom;
            dinero = 150;
            socket = s;
            estado = "E";
        }

        public void setNombre(String n)
        {
            nombre = n;
        }

        public String getNombre()
        {
            return nombre;
        }

        public void setSocket(Socket s)
        {
            socket = s;
        }

        public Socket getSocket()
        {
            return socket;
        }

        public void setDinero(int d)
        {
            dinero = d;
        }

        public int getDinero()
        {
            return dinero;
        }

        public void setApuesta(int apu)
        {
            apuesta = apu;
        }

        public int getApuesta()
        {
            return apuesta;
        }

        public void setEstado(String es)
        {
            estado = es;
        }

        public bool isJugando()
        {
            if (estado.Equals("J"))
                return true;
            return false;
        }
        public bool isListo()
        {
            if (estado.Equals("R"))
                return true;
            return false;
        }
        public bool isEntrando()
        {
            if (estado.Equals("E"))
                return true;
            return false;
        }
        public bool isEsperando()
        {
            if (estado.Equals("W"))
                return true;
            return false;
        }
        public bool isMuerto()
        {
            if (estado.Equals("N"))
                return true;
            return false;
        }

        public void setMano(List<Carta> m)
        {
            mano = m;
        }

        public void addMano(Carta c)
        {
            if (mano != null)
            {
                mano.Add(c);
            }
        }

        public int getSumMano()
        {
            if (mano != null)
            {
                int countA = 0;
                int sum=0;
                foreach (Carta c in mano)
                {
                    if (c.getNum() == 1)
                    {
                        countA++;
                    }
                    else if (c.getNum() < 11) // falta el valor del A
                    {
                        sum += c.getNum();
                    }
                    else
                    {
                        sum += 10;
                    }
                }
                if (countA > 0)
                {
                    for (int i =0; i<countA; i++)
                    {
                        if (sum + 11 > 21)
                        {
                            sum += 1;
                        }
                        else
                        {
                           sum += 11;
                        }
                    }
                }
                return sum;
            }
            else
            {
                return 0;
            }
        }

        public Carta getCartaMano(int pos)
        {
            if (mano == null)
            {
                return null;
            }
            else
            {
                if (mano.Count > pos)
                {
                    return mano[pos];
                }
                else
                {
                    return null;
                }
            }
        }

        public void matar()
        {
            estado = "N";
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

    }
}
