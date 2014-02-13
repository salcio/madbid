using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Xml.Serialization;
using MadBidFetcher.Model;
using MadBidFetcher.Services;

namespace MadBidFetcher
{
    [Serializable]
    public class Auction
    {
		public static int[] ActivaPlayersTimes = new int[] { 10, 20, 30 };
		[NotMapped]
		private Dictionary<int, List<PlayerBids>> _activePlayers;

	    [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public DateTime? LastBidDate { get; set; }
        public int BidTimeOut { get; set; }
        public AuctionStatus Status { get; set; }
        public string Title { get; set; }

        public string AllImages { get { return Images == null ? null : string.Join(";", Images); } set { Images = value == null ? null : value.Split(';'); } }
        [NotMapped]
        public string[] Images { get; set; }
        public string Description { get; set; }
        public float CreditCost { get; set; }
        public float Price { get; set; }
        public float RetailPrice { get; set; }
        public string EndTime { get; set; }
        public string StartTime { get; set; }
        public DateTime? DateOpens { get; set; }
        public bool CurrentUserAuction { get; set; }
        public bool Pinned { get; set; }
        public int ProductId { get; set; }
        public int TimingsInterval { get; set; }

        public ICollection<Player> Players { get; set; }
        public ICollection<Bid> Bids { get; set; }

		[NotMapped]
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


        public Auction()
        {
            Description = "";
            Title = "";
        }

        public static Auction FromModel(Model.Auction auction)
        {
            return new Auction
                {
                    Id = auction.Id,
                    LastBidDate = auction.LastBidDate,
                    BidTimeOut = auction.BidTimeOut,
                    Status = auction.Status,
                    Title = auction.Title,
                    Images = auction.Images,
                    Description = auction.Description,
                    CreditCost = auction.CreditCost,
                    Price = auction.Price,
                    RetailPrice = auction.RetailPrice,
                    EndTime = auction.EndTime,
                    StartTime = auction.StartTime,
                    DateOpens = auction.DateOpens,
                    CurrentUserAuction = auction.CurrentUserAuction,
                    Pinned = auction.Pinned,
                    ProductId = auction.ProductId,
                    TimingsInterval = auction.TimingsInterval
                };
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

		[NotMapped]
		private List<BidStatistics> _timngs;

		[NotMapped]
		private int _timingsInterval = 5;
		[NotMapped]
		private int _lastTimingsTime = -1;
		[NotMapped]
		private int _lastTimingsBidsCount = -1;
		[NotMapped]

		public List<BidStatistics> Timings
		{
			get
			{
				var startTime = GetStartDate();
				var maxTime = (int)((DateTime.Now - startTime.Value).TotalMinutes / TimingsInterval);
				var toSkip = _lastTimingsTime < 0
					? 0
					: Bids.Count - Math.Max(20, (maxTime - _lastTimingsTime + 2) * TimingsInterval * 60 / BidTimeOut);
				var times = (_timngs == null || toSkip < 0
								 ? Bids.AsEnumerable()
								 : Bids.Skip(toSkip))
					.Where(t => t.Time != null)
					.GroupBy(g => (int)((g.Time.Value - startTime.Value).TotalMinutes / TimingsInterval))
					.Select(
						g =>
						new BidStatistics()
						{
							Time = g.Key,
							Count = g.Count(),
							Players = g.Select(b => b.Player).Distinct().Count()
						})
					.OrderBy(s => s.Time)
					.ToList();
				if (_timngs == null || toSkip < 0)
					_timngs = times;
				else
				{
					foreach (var time in times)
					{
						if (_timngs.Count > time.Time)
						{
							_timngs[time.Time].Players = Math.Max(time.Players, _timngs[time.Time].Players);
							_timngs[time.Time].Count = Math.Max(time.Count, _timngs[time.Time].Count);
						}
						else
							_timngs.Add(time);
					}
				}
				for (var i = _lastTimingsTime < 3 ? 0 : _lastTimingsTime - 2; i < maxTime; i++)
				{
					if (_timngs.Count <= i)
					{
						Enumerable.Range(0, i - _timngs.Count + 1)
								  .ToList()
								  .ForEach(
									  index =>
									  _timngs.Add(new BidStatistics { Time = i - index, Count = -1, Players = -1 }));
					}
					if (_timngs[i].Time != i)
					{
						_timngs.Insert(i, new BidStatistics { Time = i, Count = -1, Players = -1 });
					}
				}
				_lastTimingsTime = maxTime;
				_lastTimingsBidsCount = Bids.Count;
				return _timngs;
			}
		}
	}
}