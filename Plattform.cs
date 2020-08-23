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
        List<string> urls = new List<string>();
        List<string> keywords = new List<string>();
        public List<SearchItem> searchItems = new List<SearchItem>();

        private string baseUrlEbayKleinanzeigen = "https://www.ebay-kleinanzeigen.de";
        private string baseUrlEbayDe = "https://www.ebay.de";
        private int maxArticles = 1000;

        private List<double> resultsMedian = new List<double>();
        private List<double> results = new List<double>();

        // Standardbrowser anlegen
        private ScrapingBrowser browser = new ScrapingBrowser();

        public Plattform(List<String> urlsParameter, List<String> keywords_parameter)
        {
            if (urlsParameter == null)
            {
                throw new ArgumentNullException(nameof(urlsParameter));
            }

            foreach (string url in urlsParameter)
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                }
            }

            // Initialisierung der Klasse
            // Übergabeparameter: urls, keywords

            browser.AllowAutoRedirect = true; // Browser has settings you can access in setup
            browser.AllowMetaRedirect = true;

            // Jede url wird geprüft und wenn valid, wird sie zu urls hinzugefügt und jeweils ein SearchItem angelegt
            Console.WriteLine("Anzahl an URLs: " + urlsParameter.Count.ToString());
            foreach (var url in urlsParameter)
            {
                if (UriValidator(url))
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
            if (url.IndexOf("m.ebay-kleinanzeigen") > -1)
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
        public void Fetch()
        {
            if (urls.Count == 0)
            {
                throw new PriceTrackerException("Gibt keine urls. Also zurück.");
            }

            resultsMedian.Clear();
            foreach (var searchItem in searchItems)
            {
                Console.WriteLine("searchItem wird bearbeitet.");
                if (searchItem.url.IndexOf("ebay-kleinanzeigen.de") > -1)
                {
                    Console.WriteLine("Ist ebay-Kleinanzeigen.");
                    if (!FetchEbayKleinanzeigen(searchItem))
                    {
                        throw new PriceTrackerException("Fehler beim fetchen!");
                    }
                    double median = searchItem.GetMedian();
                    resultsMedian.Add(median);
                }
                else if (searchItem.url.IndexOf("localhost") > -1)
                {
                    Console.WriteLine("Ist lokal.");
                    if (!FetchEbayKleinanzeigen(searchItem))
                    {
                        throw new PriceTrackerException("Fehler beim fetchen!");
                    }
                    resultsMedian.Add(searchItem.GetMedian());
                }
                else
                {
                    Console.WriteLine("Unbekannter Link! " + searchItem.url);
                }
                // Hier fehlt noch der Teil für ebay.de
                //else if (url.IndexOf("ebay.de") > -1)
                //{
                //    results_median.Add(calcMedian(FetchEbayDe(url).ToArray()));
                //}

            }
        }

        /// <summary>
        /// Original Preis übergeben und verschiedene Optionen filtern.False wird zurückgegeben, wenn der Preis nicht eindeutig ist.
        /// </summary>
        /// <param name="price"></param>
        /// <returns>price</returns>
        private double CleanPrice(string price)
        {
            price = price.Trim();
            if (price == "VB" || price.IndexOf("bis") > -1 || price == "Zu verschenken" || price == "")
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
            // Console.WriteLine(searchItem.url);
            try
            {
                PageResult = browser.NavigateToPage(new Uri(searchItem.url));
                // Console.WriteLine(PageResult.RawResponse.StatusCode);
            }
            catch (Exception exception)
            {
                // eBay Kleinanzeigen gibt bei zu vielen Anfragen einen 503 zurück. Dieser müsste noch sauber abgefangen werden.
                Console.WriteLine(exception);
                return false;
                // throw new PriceTrackerException("Fehler bei der Erstellung des browser-Moduls.");
            }
            //Console.WriteLine(PageResult.Html.OuterHtml);

            // Der Pagetitle sollte noch aus dem <title>-Tag gelesen werden.
            // HtmlNode TitleNode = PageResult.Html.CssSelect("title").First();
            // string PageTitle = TitleNode.InnerText;

            // Wonach wurde gesucht? Der Suchstring wird in searchQuery gespeichert.
            if (string.IsNullOrEmpty(searchItem.searchQuery))
            {
                searchItem.searchQuery = EbayKleinanzeigenGetSearchString(PageResult);
            }
            


            // Durch "article.aditem" bekommen wir alle Artikel
            var nodes = PageResult.Html.CssSelect("article.aditem");

            // string CellLink;
            string price;
            var allPrices = new List<double>();
            double cleanedPrice;

            foreach (var cell in nodes)
            {
                HtmlNode node = cell.CssSelect("a.ellipsis").First();

                // Wenn ein Schlüsselwort aus keywords im title ist, dann ignorieren.
                if (!HasKeyword(node.InnerText.ToLower()))
                {
                    searchItem.quantityIgnored++;
                    continue;
                }

                // CellLink = node.Attributes["href"].Value;

                // Preis holen, checken und wenn nicht -1 dann der Liste mit allen Preisen hinzufügen.
                price = cell.CssSelect(".aditem-details > strong").First().InnerText;
                cleanedPrice = CleanPrice(price);
                if (cleanedPrice == -1)
                { continue; }
                else
                {
                    searchItem.quantity++;
                    allPrices.Add(cleanedPrice);
                    //Console.WriteLine(FormatPrice(cleanedPrice));
                }
            }

            // Link zur nächsten Seite suchen, speichern und rekursiv die Funktion aufrufen
            if (PageResult.Html.CssSelect(".pagination-next").Count() > 0 && searchItem.quantity < maxArticles)
            {
                string nextPageLink = PageResult.Html.CssSelect(".pagination-next").First().Attributes["href"].Value;
                searchItem.url = baseUrlEbayKleinanzeigen + nextPageLink;
                searchItem.pages++;
                Console.WriteLine("   Nächste Seite");
                FetchEbayKleinanzeigen(searchItem);
            }
            // searchItem.all_prices = allPrices;
            searchItem.allPrices = searchItem.allPrices.Concat(allPrices).ToList();
            searchItem.searched = true;

            return true;
        }

        private bool HasKeyword(string article)
        {
            foreach (string keyword in keywords)
            {

                if (article.IndexOf(keyword) > -1)
                {
                    return false;
                }
            }
            return true;
        }

        private static string EbayKleinanzeigenGetSearchString(WebPage PageResult)
        {
            if (PageResult.Html.CssSelect(".breadcrump-summary").Count() > 0)
            {
                return PageResult.Html.CssSelect(".breadcrump-summary").First().InnerText;
            }
            else { return ""; }
        }

        /// <summary>
        /// Setzt die Anzahl an maximal zu holenden Artikeln. Standard ist 1000.
        /// </summary>
        public void SetMaxArticles(int maxArticles)
        {
            if (maxArticles > 0)
            {
                this.maxArticles = maxArticles;
            }
            else { this.maxArticles = 1000; }

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

        /// <summary>
        /// Funktion formatiert einen double Preis in deutscher Formatierung.
        /// </summary>
        public static string FormatPrice(double price)
        {
            var culture = new CultureInfo("de-DE");
            return price.ToString("C0", culture);
        }
    }
}
