using System;
using System.Collections.Generic;
using System.Text;

namespace servidor.model
{
    public class Carta
    {
        private int num;
        private String palo; // T=trebol, C=corazon, D=diamante, S=pica


        public Carta()
        {
            num = 0;
            palo = "";
        }

        public Carta(int n, String p)
        {
            num = n;
            palo = p;
        }

        public int getNum()
        {
            return num;
        }

        public String getPalo()
        {
            return palo;
        }

        public String toString()
        {
            return "" + num + palo;
        }



    }



}
