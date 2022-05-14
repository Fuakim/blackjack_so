using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace servidor.model
{
    class Juego
    {

        private List<Jugador> jugadores;

        private List<Jugador> jugadores2;

        private Jugador crupier;

        private List<Carta> baraja;

        private static bool isAccion;
        private static String jugadorTurno; // nombre del jugador en turno
        
        private static int ganancia;

        private Thread iniRonda;

        int posBaraja;
        public Juego()
        {
            crupier = new Jugador("crupier", null);
            jugadores = new List<Jugador>();
            jugadores2 = new List<Jugador>();
            inicializarBaraja();
            ganancia = 0;
            iniRonda = new Thread(iniciarRondas);
            iniRonda.Start();

        }

        private void iniciarRondas()
        {
            while (true)
            {
                if (jugadores.Count > 0 && jugadores[0].isListo())
                {
                    Console.WriteLine("iniciando ronda");
                    rondas();
                    break;
                }
            }
        }

        public void evaluarMsg(Dictionary<String, String> data)
        {
            Dictionary<String, String> response = new Dictionary<string, string>();
            //hacer un switch y que reciba un array de string 
            switch (getKey(data, "tipo"))
            {
                case "pedir":
                    pedirCarta(data);
                    break;
                case "plantar":
                    plantar(data);
                    break;
                case "entrar": // se le envian los datos de los demas jugadores
                    entrar(data);
                    break;
                case "listo": // se le cambia el estado al jugador a listo
                    listo(data);
                    // cambiarle el estado despues de enviar eso y si es el unico jugador se inicia la partida
                    break;
                case "salir":
                    removerJugador(data);
                    break;
                default:
                    break;
            }
        }

        private void entrar(Dictionary<String, String> data)
        {
            Dictionary<String, String> response = new Dictionary<string, string>();
            if (jugadores.Count >= 1)
            {
                enviarPartida(data);
            }//
            else
            {
                response.Add("modo", "juego");
                response.Add("tipo", "inicio");
                response.Add("jugador", getKey(data, "jugador"));
                response.Add("dinero", "" + getJugador(getKey(data, "jugador")).getDinero());
                byte[] msgs = setMessage(response);
                getJugador(getKey(data, "jugador")).getSocket().Send(msgs);
            }
        }

        private void listo(Dictionary<String, String> data)
        {
            Dictionary<String, String> response = new Dictionary<string, string>();
            if (jugadores.Count >= 1)
            {
                response.Add("modo", "juego");
                response.Add("tipo", "nuevo jugador");
                response.Add("jugador", getKey(data, "jugador"));
                response.Add("dinero", "" + getJugador(getKey(data, "jugador")).getDinero());
                getJugador(getKey(data, "jugador")).setEstado("R");
                enviarAJugadores(response);
            }
            else
            {
                getJugador(getKey(data, "jugador")).setEstado("R");
                moverJugadores();
            }
        }

        private void removerJugador(Dictionary<String, String> data)
        {
            Dictionary<String, String> response = new Dictionary<string, string>();
            foreach (Jugador jugador in jugadores)
            {
                if (jugador.getNombre() == getKey(data, "jugador"))
                {
                    response.Add("modo", "remover jugador");
                    response.Add("jugador", jugador.getNombre());
                    jugador.matar();
                    jugadores.Remove(jugador);
                    enviarAJugadores(response);
                    break;
                }
            }
            
        }

        private void enviarPartida(Dictionary<String, String> data)
        {
            Dictionary<String, String> msgOutput = new Dictionary<string, string>();

            msgOutput.Add("modo", "juego");
            msgOutput.Add("tipo", "partida");
            msgOutput.Add("jugador", getKey(data, "jugador"));
            msgOutput.Add("dinero", ""+ getJugador(getKey(data, "jugador")).getDinero());
            String nombres = "";
            String dineros = "";
            String cartas1 = "";
            String cartas2 = "";
            String cartas3 = "";

            nombres += "crupier|:|";
            dineros += 0 + "|:|";
            cartas1 += ((crupier.getCartaMano(0) == null) ? "none" : crupier.getCartaMano(0).toString()) + "|:|";
            cartas2 += ((crupier.getCartaMano(1) == null) ? "none" : crupier.getCartaMano(1).toString()) + "|:|";
            cartas3 += ((crupier.getCartaMano(2) == null) ? "none" : crupier.getCartaMano(2).toString()) + "|:|";

            foreach (Jugador j in jugadores)
            {
                if (j.getNombre() != getJugador(getKey(data, "jugador")).getNombre() && (j.isJugando() || j.isListo()))
                {
                    nombres += j.getNombre() + "|:|";
                    dineros += j.getDinero() + "|:|";
                    cartas1 += ((j.getCartaMano(0) == null) ? "none" : j.getCartaMano(0).toString()) + "|:|";
                    cartas2 += ((j.getCartaMano(1) == null) ? "none" : j.getCartaMano(1).toString()) + "|:|";
                    cartas3 += ((j.getCartaMano(2) == null) ? "none" : j.getCartaMano(2).toString()) + "|:|";
                }
            }
            msgOutput.Add("nombres", nombres);
            msgOutput.Add("dineros", dineros);
            msgOutput.Add("cartas1", cartas1);
            msgOutput.Add("cartas2", cartas2);
            msgOutput.Add("cartas3", cartas3);
            Console.WriteLine("nombres {0}", nombres);
            byte[] msgs = setMessage(msgOutput);
            getJugador(getKey(data, "jugador")).getSocket().Send(msgs);
        }

        private void pedirCarta(Dictionary<String, String> data)
        {
            foreach (Jugador jugador in jugadores)
            {
                if (jugador.getNombre() == getKey(data, "jugador"))
                {
                    // hacer validacion de si se pasa de 21 c mamo
                    Dictionary<String, String> msgOutput = new Dictionary<string, string>();

                    jugador.addMano(baraja[posBaraja]);
                    jugador.setApuesta(Int32.Parse(getKey(data, "apuesta")));

                    msgOutput.Add("modo", "juego");
                    msgOutput.Add("carta3", baraja[posBaraja].toString());
                    msgOutput.Add("jugador", jugador.getNombre());
                    msgOutput.Add("apuesta", ""+jugador.getApuesta());
                    posBaraja++;

                    if (jugador.getSumMano() > 21)
                    {
                        Console.WriteLine("el jugador que pidio perdio");
                        jugador.setEstado("R");
                        jugador.setDinero(jugador.getDinero() - jugador.getApuesta());
                        ganancia += jugador.getApuesta();
                        msgOutput.Add("dinero", ""+jugador.getDinero());
                        msgOutput.Add("tipo", "perdio");
                    }
                    else
                    {
                        Console.WriteLine("el jugador pudo pedir sin problemas");
                        msgOutput.Add("tipo", "pedir");
                    }
                        

                    enviarAJugadores(msgOutput);
                    Thread.Sleep(2000);
                    isAccion = true;
                    break;
                }
            }
        }


        private void plantar(Dictionary<String, String> data)
        {
            Dictionary<String, String> response = new Dictionary<string, string>();
            response.Add("modo", "juego");
            response.Add("tipo", "plantar");
            response.Add("jugador", getKey(data, "jugador"));
            response.Add("apuesta", getKey(data, "apuesta"));
            getJugador(getKey(data, "jugador")).setApuesta(Int32.Parse(getKey(data, "apuesta")));
            enviarAJugadores(response);
            Thread.Sleep(2000);
            isAccion = true;
        }

        private void inicializarBaraja()
        {
            baraja = new List<Carta>();
            for (int i=1; i<=13; i++)
            {
                baraja.Add(new Carta(i, "D"));
                baraja.Add(new Carta(i, "T"));
                baraja.Add(new Carta(i, "C"));
                baraja.Add(new Carta(i, "S"));
            }
            barajar();
        }

        private void barajar() // revuelve dos veces la baraja
        {
            Carta aux;
            int auxr;
            var rnd = new Random();
            for (int i=0;i<52; i++)
            {
                auxr = rnd.Next(0, 51);
                aux = baraja[i];
                baraja[i] = baraja[auxr];
                baraja[auxr] = aux;
            }
            for (int i = 0; i < 52; i++)
            {
                auxr = rnd.Next(0, 51);
                aux = baraja[i];
                baraja[i] = baraja[auxr];
                baraja[auxr] = aux;
            }
        }

        public void addJugador(Jugador j)
        {
            if (contarJugadores() >= 7)
            {
                j.setEstado("W");
                jugadores2.Add(j);
                Dictionary<String, String> msgOutput = new Dictionary<string, string>();
                msgOutput.Add("respuesta", "esperar");
                byte[] msgs = setMessage(msgOutput);
                j.getSocket().Send(msgs);
                // se envia el mensaje para esperar
            }
            else
            {
                jugadores2.Add(j);
                Dictionary<String, String> msgOutput = new Dictionary<string, string>();
                msgOutput.Add("respuesta", "entrar");
                byte[] msgs = setMessage(msgOutput);
                j.getSocket().Send(msgs);
                // se envia mensaje de que puede seguir
            }
        }

        private int contarJugadores()
        {
            int cantJugadores = 0;
            foreach (Jugador jugador in jugadores)
            {
                if (jugador.isListo() || jugador.isJugando())
                {
                    cantJugadores++;
                }
            }

            foreach (Jugador jugador in jugadores2)
            {
                if (jugador.isListo() || jugador.isJugando())
                {
                    cantJugadores++;
                }
            }
            return cantJugadores;
        }
        //jugador.isEntrando() || jugador.isListo() || jugador.isJugando()
        public void setJugadoresEstado()
        {
            int cantJugadores = 0;
            foreach (Jugador jugador in jugadores)
            {
                if (jugador.isListo() || jugador.isJugando())
                {
                    jugador.setEstado("J");
                    cantJugadores++;
                }
                if (cantJugadores==7)
                {
                    break;
                }
            }
        }

        private void rondas()
        {
            while (true)
            {
                if (jugadores.Count==0)
                {
                    break;
                }
                setJugadoresEstado();
                Console.WriteLine("iniciando ronda");
                ganancia = 0;
                repartirCartas();
                foreach (Jugador j in jugadores) // no se si esto funque en un array
                {
                    if (j.isJugando())
                    {
                        Console.WriteLine("turno de {0}", j.getNombre());
                        isAccion = false;
                        jugadorTurno = j.getNombre();
                        Dictionary<String, String> msg = new Dictionary<string, string>();
                        msg.Add("modo", "juego");
                        msg.Add("tipo", "turno");
                        msg.Add("jugador", j.getNombre()); // se enviar el nombre del jugador en turno
                        enviarAJugadores(msg);
                        int cont = 0;
                        while (true) // para esperar que el usuario responda
                        {
                            Thread.Sleep(1000);
                            if (isAccion || cont==30)
                            {
                                break;
                            }
                            cont++;
                        }
                        if (!isAccion)
                        {
                            Dictionary<String, String> msgOutput = new Dictionary<string, string>();
                            j.setEstado("N");
                            msgOutput.Add("modo", "juego");
                            msgOutput.Add("tipo", "remover jugador");
                            msgOutput.Add("jugador", j.getNombre());
                            enviarAJugadores(msgOutput);
                            Thread.Sleep(1000);
                        }
                    }
                }
                jugadorTurno = "";
                turnoCrupier();
                Thread.Sleep(2000);
                //turno rapido del crupier
                // aqui seria ver quien gana y que revelen todas sus cartas
                decidirGanador();
                removerJugadores();
                moverJugadoresEsperando();
                moverJugadores();
                barajar(); // se baraja despues de cada ronda
            }
        }

        private void turnoCrupier()
        {
            if (crupier.getSumMano() <= 16)
            {
                Dictionary<String, String> msgOutput = new Dictionary<string, string>();

                crupier.addMano(baraja[posBaraja]);

                msgOutput.Add("modo", "juego");
                msgOutput.Add("carta3", baraja[posBaraja].toString());
                msgOutput.Add("jugador", "crupier");
                posBaraja++;

                if (crupier.getSumMano() > 21)
                {
                    msgOutput.Add("dinero", "");
                    msgOutput.Add("tipo", "perdio");
                }
                else
                {
                    msgOutput.Add("tipo", "pedir");
                }

                enviarAJugadores(msgOutput);
            }
            else
            {
                //plantarse
                Dictionary<String, String> msgOutput = new Dictionary<string, string>();
                msgOutput.Add("modo", "juego");
                msgOutput.Add("tipo", "plantar");
                msgOutput.Add("jugador", "crupier");
                enviarAJugadores(msgOutput);
            }
        }

        private void removerJugadores()
        {
            foreach(Jugador jugador in jugadores)
            {
                if(jugador.isMuerto())
                {
                    jugador.getSocket(); // matar el socket si se puede y tambien el hilo
                    jugador.getSocket().Close();
                    // enviar el mensaje de remover jugador
                }
            }
            jugadores.RemoveAll(estanMuertos);
        }

        private static bool estanMuertos(Jugador jug) // en teoria es un predicate para juanear
        {
            return jug.isMuerto();
        }

        private void moverJugadoresEsperando()
        {
            foreach (Jugador jugador in jugadores)
            {
                if (contarJugadores() < 7 && jugador.isEsperando())
                {
                    Dictionary<String, String> msgOutput = new Dictionary<string, string>();
                    msgOutput.Add("respuesta", "entrar");
                    byte[] msgs = setMessage(msgOutput);
                    jugador.getSocket().Send(msgs);
                    Thread.Sleep(3000);
                    // se envia mensaje de que puede seguir
                }
            }
        }

        private void moverJugadores()
        {
            Console.WriteLine("se activo moverJugadores");
            foreach (Jugador jugador in jugadores2)
            {
                jugadores.Add(jugador);
            }
            jugadores2.Clear();
        }

        private void repartirCartas() // se reparten las cartas a todos los jugadores que estaban 
        {                               // presentes cuando se inicio la ronda
            posBaraja = 0;

            Dictionary<String, String> msg = new Dictionary<string, string>();
            msg.Add("modo", "juego");
            msg.Add("tipo", "carta");
            crupier.setMano(new List<Carta>());
            crupier.addMano(baraja[posBaraja]);
            msg.Add("jugador", "crupier");
            msg.Add("carta1", baraja[posBaraja].toString());
            posBaraja++;
            crupier.addMano(baraja[posBaraja]);
            msg.Add("carta2", baraja[posBaraja].toString());
            posBaraja++;
            enviarAJugadores(msg);
            Thread.Sleep(1000);

            foreach (Jugador j in jugadores) 
            {
                if (j.isJugando())
                {
                    Dictionary<String, String> msg2 = new Dictionary<string, string>();
                    msg2.Add("modo", "juego");
                    msg2.Add("tipo", "carta");
                    j.setMano(new List<Carta>());
                    j.addMano(baraja[posBaraja]);
                    msg2.Add("jugador", j.getNombre());
                    msg2.Add("carta1", baraja[posBaraja].toString());
                    posBaraja++;
                    j.addMano(baraja[posBaraja]);
                    msg2.Add("carta2", baraja[posBaraja].toString());
                    posBaraja++;
                    
                    enviarAJugadores(msg2);
                    Thread.Sleep(1000);
                }
            }
        }
        private void decidirGanador()
        {
            Dictionary<String, String> msg = new Dictionary<string, string>();
            msg.Add("modo", "juego");
            msg.Add("tipo", "revelar");
            enviarAJugadores(msg);
            Jugador ganador = null;
            int sumCartas = 0;
            foreach (Jugador j in jugadores)
            {
                if (j.isJugando())
                {
                    if (j.getSumMano() > sumCartas)
                    {
                        sumCartas = j.getSumMano();
                        ganador = j;
                    }
                }
            }
            String nombres = "";
            String dineros = "";
            foreach (Jugador j in jugadores)
            {
                if (j.isJugando() && (ganador == null || j.getNombre() != ganador.getNombre()))
                {
                    ganancia += j.getApuesta();
                    j.setDinero(j.getDinero() - j.getApuesta());
                    if (j.getDinero() <= 0)
                    {
                        j.setEstado("N");
                    }
                    nombres += j.getNombre() + "|:|";
                    dineros += j.getDinero() + "|:|";
                }
            }
            Thread.Sleep(2000);
            Dictionary<String, String> msg1 = new Dictionary<string, string>();
            
            msg1.Add("modo", "juego");
            msg1.Add("tipo", "ganador"); // enviar vectores para actualizar los dineros
            if (ganador == null)
            {
                msg1.Add("jugador", "crupier"); // se enviar el nombre del jugador en turno
                msg1.Add("ganancia", "" + ganancia);
            }
            else
            {
                ganador.setDinero(ganador.getDinero() + ganancia);
                msg1.Add("jugador", ganador.getNombre()); // se enviar el nombre del jugador en turno
                msg1.Add("dinero", "" + ganador.getDinero());
                msg1.Add("ganancia", "" + ganancia);
            }
            msg1.Add("nombres", nombres);
            msg1.Add("dineros", dineros);
            enviarAJugadores(msg1);
            Thread.Sleep(4000);
        }

        private void enviarAJugadores(Dictionary<string, string> data)
        {
            byte[] msgs = setMessage(data);
            foreach (Jugador jugador in jugadores)
            {
                if (jugador.isListo() || jugador.isJugando())
                {
                    jugador.getSocket().Send(msgs);
                }
            }
        }

        private void salirJugador(Dictionary<string, string> data)
        {

        }

        private Jugador getJugador(string nombre)
        {
            foreach (Jugador jugador in jugadores)
            {
                if (jugador.getNombre() == nombre)
                {
                    return jugador;
                }
            }
            foreach (Jugador jugador2 in jugadores2)
            {
                if (jugador2.getNombre() == nombre)
                {
                    return jugador2;
                }
            }
            return null;
        }

        private bool isJugador(string nombre)
        {
            foreach (Jugador jugador in jugadores)
            {
                if (jugador.getNombre() == nombre)
                {
                    return true;
                }
            }
            return false;
        }

        public void avisarMuerte(String j) // cuando pete el socket porque murio el cliente
        {
            foreach (Jugador jugador in jugadores)
            {
                if (jugador.getNombre().Equals(j))
                {
                    jugador.setEstado("N");
                    Dictionary<String, String> msgOutput = new Dictionary<string, string>();
                    msgOutput.Add("modo", "juego");
                    msgOutput.Add("tipo", "remover jugador");
                    msgOutput.Add("jugador", jugador.getNombre());
                    enviarAJugadores(msgOutput);
                    if (jugador.getNombre() == jugadorTurno)
                    {
                        isAccion = true;
                    }
                }
            }
            foreach (Jugador jugador2 in jugadores2)
            {
                if (jugador2.getNombre() == j)
                {
                    jugador2.setEstado("N");
                }
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
    }
}
