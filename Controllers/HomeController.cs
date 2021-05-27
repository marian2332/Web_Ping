using Kasy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;


namespace Kasy.Controllers
{
    public class HomeController : Controller
    {
        SqlCommand com = new SqlCommand();
        SqlDataReader dr;
        SqlConnection con = new SqlConnection();
        List<Address> addresses = new List<Address>();
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        int poziom_polaczenia_1;
        string adressy;       


        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            //Pobieranie danych z pliku appsettings.json           
            //Połączenie z bazą danych
            con.ConnectionString = "Server = " + _configuration.GetValue<string>("ConnectionStrings:serverSQL") + ";" +
           "Database=" + _configuration.GetValue<string>("ConnectionStrings:baseGT") + ";" +
           "Integrated Security=" + _configuration.GetValue<string>("ConnectionStrings:autentykacjawindows") + ";" +
            "User Id=" + _configuration.GetValue<string>("ConnectionStrings:userSQL") + ";" +
            "Password=" + _configuration.GetValue<string>("ConnectionStrings:passSQL") + ";";
            
            //Zapytanie SQL
            com.CommandText = _configuration.GetValue<string>("sqlcommand:Defaultsqlcommand");

            //Zakres dobrego połączenia
            poziom_polaczenia_1 = _configuration.GetValue<int>("statusconnect:green1");         
        }

        //Załadowanie widoku z indexem
        public IActionResult Index()
        {
            return View();
        }
        
        //Załadowanie widoku z przetestowanymi adresami
        public ActionResult ladowanie()
        {           
            FetchData();            
            return View("Index", addresses);
        }          
        
        string testowanie;
        //Pobranie danych 
        public void FetchData()
        {
            if (addresses.Count > 0)
            {
                addresses.Clear();
            }
            
                con.Open();
                com.Connection = con;
                dr = com.ExecuteReader();

                while (dr.Read())
                {
                    adressy = dr["Adres"].ToString();
                    Pingowanie();               
            }
                con.Close();   
            
            
        }      



        //Pingowanie
        public void Pingowanie()
        {
            //PODAĆ ILOŚĆ PINGÓW O 1 WIĘCEJ!!!
            int ile_razy_pingowac = 6;
            int iloscpingow = ile_razy_pingowac - 1;
            Console.WriteLine("Ile razy pingowac adres: "+iloscpingow);
            int bledy_polaczenia = 0;
            long czas_pingowania = 0;
            string poziom_polaczenia_kolor = "";
            int ilosc_poprawnych_pingow = 0;
            int sredni_czas_polaczenia = 0;
            string koncowy_czas_polaczenia = "";           
            //           
                Ping pingSender = new Ping();
                byte[] buffer = new byte[1000];
                int timeout = 1000;

            for (int i = 1; i < ile_razy_pingowac; i++)
            {
                PingReply reply = pingSender.Send(adressy, timeout, buffer);
                if (reply.Status == IPStatus.Success)
                {
                    ilosc_poprawnych_pingow += 1;
                    czas_pingowania += reply.RoundtripTime;
                    Console.WriteLine("adres: " +reply.Address );
                    Console.WriteLine("Status: "+reply.Status);
                    Console.WriteLine("Czas pingowania: "+czas_pingowania);
                    Thread.Sleep(500);
                }
                else
                {
                    bledy_polaczenia += 1;
                    Console.WriteLine("adres: " +reply.Address );
                    Console.WriteLine("Status: "+reply.Status);
                    Console.WriteLine("Czas pingowania: "+czas_pingowania);
                }
            }
           

                if (bledy_polaczenia==iloscpingow)
                {
                    czas_pingowania = 0;
                    ilosc_poprawnych_pingow = 0;
                    bledy_polaczenia = 0;
                    //2 sprawdzenie
                    Console.WriteLine("Drugie sprawdzenie połączenia po braku odpowiedzi przy pierwszym");
                    Thread.Sleep(3000);
                    for (int i = 1; i < ile_razy_pingowac; i++)
                    {
                        PingReply reply = pingSender.Send(adressy, timeout, buffer);
                        if (reply.Status == IPStatus.Success)
                        {
                            ilosc_poprawnych_pingow += 1;
                            czas_pingowania += reply.RoundtripTime;
                            Console.WriteLine("adres: " +reply.Address );
                            Console.WriteLine("Status: "+reply.Status);
                            Console.WriteLine("Czas pingowania: "+czas_pingowania);
                            Thread.Sleep(500);
                        }
                        else
                        {
                            bledy_polaczenia += 1;
                            Console.WriteLine("adres: " +reply.Address );
                            Console.WriteLine("Status: "+reply.Status);
                            Console.WriteLine("Czas pingowania: "+czas_pingowania);
                        }
                    }
                }

                if (bledy_polaczenia != iloscpingow)
                {
                    sredni_czas_polaczenia = (int)(czas_pingowania / ilosc_poprawnych_pingow);
                    Console.WriteLine("Średni czas połączenia: "+sredni_czas_polaczenia);
                }                
            

                //Błąd
                if (bledy_polaczenia == iloscpingow)
                {                     
                    koncowy_czas_polaczenia = "Brak połączenia";
                    poziom_polaczenia_kolor = "red";
                    Console.WriteLine("Brak połączenia");
                }

                //Dobry
              else  if (sredni_czas_polaczenia > 0 && sredni_czas_polaczenia <= poziom_polaczenia_1)
                {
                    poziom_polaczenia_kolor = "green";
                    Console.WriteLine("Połączono - Dobry");
                    koncowy_czas_polaczenia = sredni_czas_polaczenia.ToString() + " ms";
                }

                //Słaby
                else if (sredni_czas_polaczenia > poziom_polaczenia_1)
                {
                    poziom_polaczenia_kolor = "yellow";
                    Console.WriteLine("Połączono - Średni");
                    koncowy_czas_polaczenia = sredni_czas_polaczenia.ToString() + " ms";
                }

                else
                {
                    sredni_czas_polaczenia = 0;
                    poziom_polaczenia_kolor = "red";
                    Console.WriteLine("Błąd połączenia");
                    koncowy_czas_polaczenia = "Błąd połączenia";
                }

                adressy = null;
                addresses.Add(new Address() { Adres = dr["Adres"].ToString(), Nazwa = dr["Nazwa"].ToString(), Ping = koncowy_czas_polaczenia, Kolor = poziom_polaczenia_kolor });

           
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
