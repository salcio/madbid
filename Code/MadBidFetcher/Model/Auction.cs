using System;
using System.Collections.Generic;
using System.Linq;
using MadBidFetcher.Services;

namespace MadBidFetcher.Model
{
	[Serializable]
	public class Auction
	{
		public int Id { get; set; }
		public Dictionary<string, Player> Players { get; set; }
		public DateTime LastBidDate { get; set; }
		public int BidTime { get; set; }
		public AuctionStatus Status { get; set; }

		public float Price { get; set; }
		public DateTime LastDeltaReset { get; set; }

		public Auction()
		{
			Players = new Dictionary<string, Player>();
		}

		public void ResetDeltas()
		{
			Players.Values.ToList().ForEach(p => p.Delta = 0);
			LastDeltaReset = DateTime.Now;
		}
	}
}