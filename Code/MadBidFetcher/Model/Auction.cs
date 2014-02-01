using System;
using System.Collections;
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
		private List<Bid> _bids;

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
		public List<Bid> Bids
		{
			get { return _bids = _bids ?? new List<Bid>(); }
			set { _bids = value; }
		}

		[XmlIgnore]
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

		public bool Pinned { get; set; }

		public int ProductId { get; set; }

		public int TimingsInterval
		{
			get { return _timingsInterval = _timingsInterval == 0 ? 5 : _timingsInterval; }
			set { _timingsInterval = value; }
		}

		[NonSerialized]
		private List<BidStatistics> _timngs;

		private int _timingsInterval = 5;

		[XmlIgnore]
		public List<BidStatistics> Timings
		{
			get
			{
				if (_timngs != null)
					return _timngs;
				var startTime = GetStartDate();
				return _timngs = Bids
					.Where(t => t.Time != null)
					.GroupBy(g => (int)((g.Time.Value - startTime.Value).TotalMinutes / TimingsInterval))
					.Select(g => new BidStatistics() { Time = g.Key, Count = g.Count(), Players = g.Select(b => b.Player).Distinct().Count() })
					.ToList();
			}
		}

		public DateTime? GetStartDate()
		{
			var startTime = DateOpens;
			if (startTime == null)
			{
				var first = Bids.FirstOrDefault(b => b.Time != null);
				if (first == null)
				{
					return startTime;
				}
				startTime = first.Time.Value;
			}
			return startTime;
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