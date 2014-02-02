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

        [NonSerialized]
        private int _timingsInterval = 5;
        [NonSerialized]
        private int _lastTimingsTime = -1;
        [NonSerialized]
        private int _lastTimingsBidsCount = -1;

        [XmlIgnore]
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
                if (_timngs == null)
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