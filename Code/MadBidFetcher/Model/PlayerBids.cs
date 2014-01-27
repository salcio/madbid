using System;
using System.Collections.Generic;

namespace MadBidFetcher.Model
{
	[Serializable]
	public class PlayerBids
	{
		public Player Player { get; set; }
		public List<Bid> Bids  { get; set; }
	}
}