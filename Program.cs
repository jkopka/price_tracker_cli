using System;
using System.Linq;
using System.Collections.Generic;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using ScrapySharp.Network;
using System.Globalization;

namespace price_tracker_cli
{
    
    class Program
    {
        static void Main(string[] args)
        {
            List<string> listURLs = new List<string>();
            List<string> listKeyWords = new List<string>();

            Console.Clear();
            Console.WriteLine("## ANFANG");

            // Kommandozeilenargumente lesen
            if (args.Length > 0)
            {
                foreach (string url in args)
                {
                    // url in listURLs eintragen
                    // Hier sollte vielleicht noch direkt gecheckt werden, ob url eine valide URL ist.
                    listURLs.Add(url);
                }
            }
            // Momentan noch eine else-Anweisung zum testen.
            else { Console.WriteLine("StandardURL hinzufügen"); listURLs.Add("https://www.ebay-kleinanzeigen.de/s-macbook-2015/k0"); }

            //listKeyWords.Add("Gutschein".ToLower());
            //listKeyWords.Add("Jochen Schweizer".ToLower());

            // Objekt aus Klasse Plattform erstellen
            Plattform plattform = new Plattform(listURLs, listKeyWords);
            Console.WriteLine("Plattform erstellt");
            // Mit .fetch() die Analyse ausführen. Bei false trat ein Fehler auf
            if (plattform.Fetch() == true)
            {
                // Für jede url wird ein searchItem angelegt. Hier jedes durchgehen und die Ergebnisse ausgeben.
                Console.WriteLine("Anzahl an searchItems: " + plattform.searchItems.Count.ToString());
                foreach (SearchItem searchItem in plattform.searchItems)
                {
                    // Wenn .searched nicht true ist, wurde die Suche noch nicht ausgeführt, wird hier also ignoriert.
                    if (searchItem.searched != true)
                    {
                        Console.WriteLine("searchItem hat noch nicht gesucht. Ignorieren...");
                        continue;
                    }
                    Console.WriteLine("Suche: " + searchItem.searchQuery);
                    Console.WriteLine("Anzahl Artikel: " + searchItem.quantity);
                    Console.WriteLine("Anzahl ignorierter Artikel: " + searchItem.quantity_ignored);
                    Console.WriteLine("Median: " + searchItem.getFormatedMedian());
                    Console.WriteLine("Seiten: " + searchItem.pages);
                }
            }
            else { Console.WriteLine("Fehler!"); }
            
            
            Console.WriteLine("## ENDE");
            //Console.ReadKey();
        }



    }
}
