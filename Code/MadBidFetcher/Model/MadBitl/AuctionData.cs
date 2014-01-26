using System;

namespace MadBidFetcher.Model.MadBitl
{
	public class AuctionData
	{
		public object availability { get; set; }
		public AuctionBid[] bidding_history { get; set; }
		public int credit_cost { get; set; }
		public DateTime date_opens { get; set; }
		public DateTime date_timeout { get; set; }
		public object flags { get; set; }
		public AuctionLastBid last_bid { get; set; }
		public int state { get; set; }
		public int timeout { get; set; }
	}
}