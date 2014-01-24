using System;

namespace MadBidFetcher.Model.MadBitl
{
	public class AuctionUpdate
	{
		public int timeout;
		public int state;
		public int auction_id { get; set; }
		public string highest_bidder { get; set; }
		public string date_bid { get; set; }
		public float highest_bid { get; set; }
	}
}