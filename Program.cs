using System;
using System.Collections.Generic;

namespace price_tracker_cli
{

    class Program
    {
        static void Main(string[] args)
        {
            var listURLs = new List<string>();
            var listKeyWords = new List<string>();

            Console.Clear();
            Console.WriteLine("## ANFANG");

            if (args.Length > 0)
            {
                ReadUrlsFromCommandLine(args, listURLs);
            }
            else
            {
                GetDefaultUrls(listURLs);
            }

                
            listKeyWords.Add("gutschein");
            listKeyWords.Add("amd");
            listKeyWords.Add("jochen schweizer");
            listKeyWords.Add("suche");

            // Objekt aus Klasse Plattform erstellen
            var plattform = new Plattform(listURLs, listKeyWords);
                
            // Mit .fetch() die Analyse ausführen.
            plattform.Fetch();
                
            // Für jede url wird ein searchItem angelegt. Hier jedes durchgehen und die Ergebnisse ausgeben.
            foreach (SearchItem searchItem in plattform.searchItems)
            {
                // Wenn .searched nicht true ist, wurde die Suche noch nicht ausgeführt, wird hier also ignoriert.
                if (!searchItem.searched)
                {
                    Console.WriteLine("searchItem hat noch nicht gesucht. Ignorieren...");
                    continue;
                }
                WriteResults(searchItem);
            }

            Console.WriteLine("## ENDE");
        }

        private static void WriteResults(SearchItem searchItem)
        {
            // Console.WriteLine("Suche: " + searchItem.searchQuery);
            // Console.WriteLine(string.Format("Suche: {0}", searchItem.searchQuery));
            Console.WriteLine($"Suche: {searchItem.searchQuery}");
            Console.WriteLine($"Artikel: {searchItem.quantity}");
            Console.WriteLine($"Ignorierte Artikel: {searchItem.quantityIgnored}");
            Console.WriteLine($"Median: {searchItem.GetFormatedMedian()}");
            Console.WriteLine($"Seiten: {searchItem.pages}");
        }

        private static void GetDefaultUrls(List<string> listURLs)
        {
            Console.WriteLine("StandardURL zwecks Test hinzufügen");
            listURLs.Add("https://www.ebay-kleinanzeigen.de/s-macbook-2015/k0");
        }

        private static void ReadUrlsFromCommandLine(string[] args, List<string> listURLs)
        {
            foreach (string url in args)
            {
                // url in listURLs eintragen
                if (UriValidator(url))
                {
                    listURLs.Add(url);
                }
            }
        }

        /// <summary>
        /// Funktion checkt, ob es sich um einen validen Link handelt.
        /// </summary>
        private static bool UriValidator(string url)
        {
            bool result = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            return result;
        }
    }
}
