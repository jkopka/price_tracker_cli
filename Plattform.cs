using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using ScrapySharp.Network;

namespace price_tracker_cli
{
    public class Plattform
    {
        List<String> urls = new List<String>();
        List<String> keywords = new List<String>();
        public List<SearchItem> searchItems = new List<SearchItem>();

        private string baseUrlEbayKleinanzeigen = "https://www.ebay-kleinanzeigen.de";
        private string base_url_ebay_de = "https://www.ebay.de";
        private int max_articles = 100;

        private List<double> results_median = new List<double>();
        private List<double> results = new List<double>();

        // Standardbrowser anlegen
        private ScrapingBrowser browser = new ScrapingBrowser();

        public Plattform(List<String> urls_parameter, List<String> keywords_parameter)
        {
            // Initialisierung der Klasse
            // Übergabeparameter: urls, keywords

            
            browser.AllowAutoRedirect = true; // Browser has settings you can access in setup
            browser.AllowMetaRedirect = true;

            // Jede url wird geprüft und wenn valid, wird sie zu urls hinzugefügt und jeweils ein SearchItem angelegt
            Console.WriteLine("Anzahl an URLs: " + urls_parameter.Count.ToString());
            foreach (var url in urls_parameter)
            {
                if(uri_validator(url))
                {
                    Console.WriteLine(url + " ist gültig!");
                    urls.Add(GetWebVersion(url));
                    SearchItem search_item = new SearchItem(url);
                    searchItems.Add(search_item);
                }
                else
                {
                    Console.WriteLine(url + " ist ungültig!");
                }
            }
            keywords = keywords_parameter;
        }


        /// <summary>
        /// Funktion checkt, ob es sich bei dem Link um die mobile Website hält.Wenn ja, wird der Link zur Desktopversion geholt.
        /// </summary>
        /// <param name="url"></param>
        /// <returns>url</returns>
        private string GetWebVersion(string url)
        {
            // Todo: Es fehlt noch der Teil für eBay.de
            if(url.IndexOf("m.ebay-kleinanzeigen") > -1)
            {
                
                WebPage PageResult = browser.NavigateToPage(new Uri(url));
                HtmlNode node = PageResult.Html.CssSelect("footer-webversion-link").First();
                String mobileWebPageLink = node.Attributes["href"].Value;
                url = baseUrlEbayKleinanzeigen + mobileWebPageLink;
            }
            return url;

        }

        /// <summary>
        /// fetch crawled jede URL. Bei Erfolg true, sonst false.
        /// </summary>
        public bool Fetch()
        {
            if (urls.Count == 0)
            { Console.WriteLine("Gibt keine urls. Also zurück."); return false; }

            // Wie geht eine Liste an Objekten? Jetzt wird erst einmal der medan gespeichert.
            results_median.Clear();
            Console.WriteLine("Anzahl searchItems: " + searchItems.Count.ToString());
            foreach (var searchItem in searchItems)
            {
                Console.WriteLine("searchItem wird bearbeitet.");
                if (searchItem.url.IndexOf("ebay-kleinanzeigen.de") > -1)
                {
                    Console.WriteLine("Ist ebay-Kleinanzeigen.");
                    FetchEbayKleinanzeigen(searchItem);
                    results_median.Add(50);
                }
                else
                {
                    Console.WriteLine("Unbekannter Link! " + searchItem.url);
                }
                // Hier fehlt noch der Teil für ebay.de
                //else if (url.IndexOf("ebay.de") > -1)
                //{
                //    results_median.Add(calcMedian(fetch_ebay_de(url).ToArray()));
                //}

            }
            // Wenn eine der mediane -1 ist, trat irgendwo ein Fehler auf. Es wird dann false zurück gegeben.
            foreach (double result in results_median)
            {
                if(result == -1)
                { return false; }
            }
            return true;

        }

        /// <summary>
        /// Original Preis übergeben und verschiedene Optionen filtern.False wird zurückgegeben, wenn der Preis nicht eindeutig ist.
        /// </summary>
        /// <param name="price"></param>
        /// <returns>price</returns>
        private double CleanPrice(string price)
        {
            price = price.Trim();
            if(price == "VB" || price.IndexOf("bis") > -1 || price == "Zu verschenken" || price == "")
            {
                return -1;
            }
            price = price.Replace("VB", "");
            price = price.Replace(" ", "");
            price = price.Replace(".", "");
            price = price.Replace("€", "");
            price = price.Replace(",", ".");

            return double.Parse(price);
        }

        /// <summary>
        /// Holt sich die URL, liest alle Preise aus und packt sie in die Liste allPrices. Diese wird zurückgegeben.
        /// </summary>
        /// <param name="searchItem"></param>
        /// <returns>false im Fehlerfall sonst true</returns>
        public bool FetchEbayKleinanzeigen(SearchItem searchItem)
        {
            WebPage PageResult;
            try
            {
                PageResult = browser.NavigateToPage(new Uri(searchItem.url));
                // Console.WriteLine(PageResult.RawResponse.StatusCode);
            }
            catch (AggregateException)
            {
                return false;
            }
            //Console.WriteLine(PageResult.Html.OuterHtml);

            // Der Pagetitle sollte noch aus dem <title>-Tag gelesen werden.
            // HtmlNode TitleNode = PageResult.Html.CssSelect("title").First();
            // string PageTitle = TitleNode.InnerText;

            // Wonach wurde gesucht? Der Suchstring wird in searchQuery gespeichert.
            if (PageResult.Html.CssSelect(".breadcrump-summary").Count() > 0)
            {
                searchItem.searchQuery = PageResult.Html.CssSelect(".breadcrump-summary").First().InnerText;
            }
            else { searchItem.searchQuery = ""; }


            // Durch "article.aditem" bekommen wir alle Artikel
            var nodes = PageResult.Html.CssSelect("article.aditem");

            string CellLink;
            string price;
            List<double> allPrices = new List<double>();
            double cleanedPrice;

            foreach (var cell in nodes)
            {
                HtmlNode node = cell.CssSelect("a.ellipsis").First();

                // Wenn ein Schlüsselwort aus keywords im title ist, dann ignorieren.
                foreach (string keyword in keywords)
                {
                    
                    if (node.InnerText.ToLower().IndexOf(keyword) > -1)
                    {
                        searchItem.quantity_ignored += 1; continue;
                    }
                }

                // Preis holen, checken und wenn nicht -1 dann der Liste mit allen Preisen hinzufügen.
                CellLink = node.Attributes["href"].Value;
                price = cell.CssSelect(".aditem-details > strong").First().InnerText;
                cleanedPrice = CleanPrice(price);
                if (cleanedPrice == -1)
                { continue; }
                else
                {
                    searchItem.quantity += 1;
                    allPrices.Add(cleanedPrice);
                    //Console.WriteLine(FormatPrice(cleanedPrice));
                }
            }

            // Link zur nächsten Seite suchen, speichern und rekursiv die Funktion aufrufen
            if(PageResult.Html.CssSelect(".pagination-next").Count() > 0 && searchItem.quantity < max_articles)
            {
                String nextPageLink = PageResult.Html.CssSelect(".pagination-next").First().Attributes["href"].Value;
                searchItem.url = baseUrlEbayKleinanzeigen + nextPageLink;
                searchItem.pages += 1;
                FetchEbayKleinanzeigen(searchItem);
            }
            // searchItem.all_prices = allPrices;
            searchItem.all_prices = searchItem.all_prices.Concat(allPrices).ToList();
            searchItem.searched = true;
            return true;
        }

        /// <summary>
        /// Setzt die Anzahl an maximal zu holenden Artikeln
        /// </summary>
        public void setMaxArticles(int maxArticles)
        {
            if(maxArticles > 0)
            {
                max_articles = maxArticles;
            }
            else { max_articles = 1000; }

        }

        /// <summary>
        /// Funktion checkt, ob es sich um einen validen Link handelt.
        /// </summary>
        private bool uri_validator(string url)
        {
            Uri uriResult;
            bool result = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            return result;
        }

        /// <summary>
        /// Funktion formatiert einen double Preis in deutscher Formatierung.
        /// </summary>
        public static string FormatPrice(double price)
        {
            CultureInfo culture = new CultureInfo("de-DE");
            return price.ToString("C0", culture);
        }
    }
}
