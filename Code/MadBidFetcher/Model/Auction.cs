using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using MadBidFetcher.Services;

namespace MadBidFetcher.Model
{
	[Serializable]
	public class Auction
	{
		public static int[] ActivaPlayersTimes = new int[] { 10, 20, 30 };
		private Dictionary<int, List<PlayerBids>> _activePlayers;

		public int Id { get; set; }
		public DateTime? LastBidDate { get; set; }
		public int BidTimeOut { get; set; }
		public AuctionStatus Status { get; set; }
		public string Title { get; set; }
		public string[] Images { get; set; }
		public string Description { get; set; }
		public float CreditCost { get; set; }
		public float Price { get; set; }
		public float RetailPrice { get; set; }
		public string EndTime { get; set; }
		public string StartTime { get; set; }
		public DateTime? DateOpens { get; set; }

		public Dictionary<string, Player> Players { get; set; }
		public List<Bid> Bids { get; set; }
		public Dictionary<int, List<PlayerBids>> ActivePlayers
		{
			get
			{
				return _activePlayers = _activePlayers
										?? ActivaPlayersTimes.ToDictionary(a => a,
																		   a => Bids.Skip(Bids.Count - a).Take(a)
																					.GroupBy(b => b.Player)
																					.Select(g =>
																							new PlayerBids
																								{
																									Player = g.Key,
																									Bids = g.ToList()
																								})
																					.ToList());
			}
			set { _activePlayers = value; }
		}

		[XmlIgnore]
		public bool CurrentUserAuction
		{
			get { return Players.ContainsKey(SessionStore.CurrentUser); }
		}

		public Auction()
		{
			Players = new Dictionary<string, Player>();
			Bids = new List<Bid>();
			Description = "";
			Title = "";
		}
	}
}