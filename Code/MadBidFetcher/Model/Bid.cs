using System;

namespace MadBidFetcher.Model
{
	public class Bid
	{
		public Auction Auction { get; set; }
		public Player Player { get; set; }
		public float Value { get; set; }
		public DateTime? Time { get; set; }
	}
}