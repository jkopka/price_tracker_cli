using System;
namespace price_tracker_cli
{
    public class PriceTrackerException : Exception
    {
        public PriceTrackerException(string message) : base(message)
        {
        }
    }
}
