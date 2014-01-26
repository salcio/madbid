using System;

namespace MadBidFetcher.Model.MadBitl
{
	public class AuctionLastBid
	{
		public string date_bid { get; set; }
		public DateTime? Date
		{
			get
			{
				DateTime result;
				return DateTime.TryParse(date_bid, out result) ? result : (DateTime?)null;
			}
		}
		public float highest_bid { get; set; }
		public string highest_bidder { get; set; }
	}
}