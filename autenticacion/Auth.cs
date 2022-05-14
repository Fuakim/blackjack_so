using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.DirectoryServices;
using System.Management;
using System.Security.Principal;


namespace autenticacion
{
    public class Auth
    {
        public bool iniciarSesion(string dominio, string user, string pass)
        {

            //Aquí va el path URL del servicio de directorio LDAP
            string path = "LDAP://una.cr/DC=una,DC=cr";

            if (estaAutenticado(dominio, user, pass, path) == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool estaAutenticado(string dominio, string usuario, string pwd, string path)
        {
            //Se hace la cadena del dominio
            string domainAndUsername = dominio + @"\" + usuario;
            //Creamos un objeto DirectoryEntry al cual le pasamos el URL, dominio/usuario y la contraseña
            DirectoryEntry entry = new DirectoryEntry(path, domainAndUsername, pwd);
            try
            {
                DirectorySearcher search = new DirectorySearcher(entry);
                //Verificamos que los datos de logeo proporcionados son correctos
                SearchResult result = search.FindOne();
                if (result == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool registrarUsuario(string user, string password)
        {
            try
            {
                DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
                DirectoryEntry NewUser = AD.Children.Add(user, "user");
                NewUser.Invoke("SetPassword", new object[] { password });
                NewUser.CommitChanges();

                Console.WriteLine("Account Created Successfully");
                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                //Console.ReadLine();
                return false;
            }
        }
    }
}
