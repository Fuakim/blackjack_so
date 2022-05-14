using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace cliente.model
{
    class Jugador
    {
        private Label lblNombre;
        private Label lblDinero;
        private String nombre;
        private String dinero;

        private bool estado;

        private List<Carta> mano = new List<Carta>();
        private String carta1;

        public Jugador()
        {
        }

        public Jugador(Label nom, Label din, Image carta1, Image carta2, Image carta3)
        {
            estado = false;
            lblNombre = nom;
            lblDinero = din;
            mano.Add(new Carta(carta1));
            mano.Add(new Carta(carta2));
            mano.Add(new Carta(carta3));
            foreach (Carta c in mano)
            {
                c.setCartaSource("dorso");
            }
        }

        public void setNombre(String nom)
        {
            nombre = nom;
            lblNombre.Content = nombre;
        }

        public String getNombre()
        {
            return nombre;
        }

        public void setDinero(String din)
        {
            dinero = din;
            lblDinero.Content = dinero;
        }

        public bool isActivo()
        {
            return estado;
        }

        public void activar(String nom, String din)
        {
            setNombre(nom);
            setDinero(din);
            estado = true;
        }
        public void desactivar()
        {
            estado = false;
            setNombre("");
            setDinero("");
            limpiarMano();
        }

        public void setCarta1(String c1)
        {
            if (c1 != "none")
            {
                mano[0].setCartaSource("dorso");
            }
            carta1 = c1;
        }
        public void revelarCarta1()
        {
            if (carta1 != "none")
            {
                mano[0].setCartaSource(carta1);
            }
            
        }
        public void setCarta2(String c2)
        {
            if (c2 != "none")
            {
                mano[1].setCartaSource(c2);
            }
        }
        public void setCarta3(String c3)
        {
            if (c3!="none")
            {
                mano[2].setCartaSource(c3);
            }
        }

        public void limpiarMano()
        {
            foreach (Carta c in mano)
            {
                c.removeCartaSource();
            }
        }

        public void salir()
        {

        }

    }
}
