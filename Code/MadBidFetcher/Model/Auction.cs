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
		public DateTime? LastBidDate { get; set; }
		public int BidTimeOut { get; set; }
		public AuctionStatus Status { get; set; }
		public string Title { get; set; }
		public string[] Images { get; set; }
		public string Description { get; set; }
		public float CreditCost { get; set; }
		public float Price { get; set; }

		public Dictionary<string, Player> Players { get; set; }
		public List<Bid> Bids { get; set; }

		public Auction()
		{
			Players = new Dictionary<string, Player>();
			Bids = new List<Bid>();
		}
	}
}