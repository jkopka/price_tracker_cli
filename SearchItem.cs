using System;
using System.Collections.Generic;
using System.Globalization;

namespace price_tracker_cli
{
    public class SearchItem
    {
        public string url;
        public List<double> allPrices = new List<double>();
        public int quantity = 0;
        public int quantityIgnored = 0;
        public string searchQuery;
        public string urlNextPage;
        public bool searched = false;
        public string error = "";
        public int pages = 1;

        /// <summary>
        /// Konstruktor.
        /// </summary>
        public SearchItem(string url_parameter)
        {
            url = url_parameter;
        }

        /// <summary>
        /// Gibt den Median als formatierte Währungszahl zurück
        /// </summary>
        public string GetFormatedMedian()
        {
            return FormatPrice(CalcMedian(allPrices.ToArray()));
        }

        /// <summary>
        /// Gibt den Median als Double zurück
        /// </summary>
        public double GetMedian()
        {
            return CalcMedian(allPrices.ToArray());
        }

        /// <summary>
        /// Berechnet den Median und gibt ihn zurück.
        /// </summary>
        /// <param name="sourceNumbers"></param>
        /// <returns>median</returns>
        public static double CalcMedian(double[] sourceNumbers)
        {
            if (sourceNumbers == null || sourceNumbers.Length == 0)
                throw new System.Exception("Median of empty array not defined.");

            //make sure the list is sorted, but use a new array
            double[] sortedPNumbers = (double[])sourceNumbers.Clone();
            Array.Sort(sortedPNumbers);

            //get the median
            int size = sortedPNumbers.Length;
            int mid = size / 2;
            double median = (size % 2 != 0) ? (double)sortedPNumbers[mid] : ((double)sortedPNumbers[mid] + (double)sortedPNumbers[mid - 1]) / 2;
            return median;
        }

        /// <summary>
        /// Funktion formatiert den Preis nach deutscher Norm.
        /// </summary>
        public static string FormatPrice(double price)
        {
            CultureInfo culture = new CultureInfo("de-DE");
            return price.ToString("C0", culture);
        }
    }
}
