using System.Collections.Generic;

namespace MadBidFetcher.Model
{
	public class AuctionModel
	{
		public List<Player> Players { get; set; }

		public Auction Auction { get; set; }

		public bool AutoUpdate { get; set; }
	}
}