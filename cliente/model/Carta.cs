using System;
using System.Windows;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace cliente.model
{
    class Carta
    {

        private Image carta;

        public Carta()
        {

        }

        public Carta(Image img)
        {
            carta = img;
        }

        public void setCartaVisible()
        {
            carta.Visibility = Visibility.Visible;
        }

        public void setCartaSource(String source)
        {
            BitmapImage imgSource = new BitmapImage();
            imgSource.BeginInit();
            imgSource.UriSource = new Uri("/imgs/"+source+".png", UriKind.Relative);
            imgSource.EndInit();
            carta.Source = imgSource;
        }

        public void removeCartaSource() //talvez funcione
        {
            BitmapImage imgSource = new BitmapImage();
            carta.Source = imgSource;
        }
    }
}
