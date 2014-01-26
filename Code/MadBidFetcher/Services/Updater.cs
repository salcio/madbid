using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using MadBidFetcher.Extensions;
using MadBidFetcher.Model;
using MadBidFetcher.Model.MadBitl;

namespace MadBidFetcher.Services
{
	public class Updater
	{
		public Dictionary<int, Auction> Auctions { get; protected set; }
		public string FilePath { get; set; }
		public static Updater Instance { get; private set; }

		public static Updater Initialize(string auctionsFile)
		{
			return Instance = File.Exists(auctionsFile)
					   ? new Updater
							 {
								 Auctions = ObjectExtensions.DeSerializeObject<Dictionary<int, Auction>>(auctionsFile),
								 FilePath = auctionsFile
							 }
					   : new Updater { FilePath = auctionsFile };
		}

		public void Save()
		{
			Auctions.SerializeObject(FilePath);
		}

		public Updater()
		{
			Auctions = new Dictionary<int, Auction>();
		}

		public void UpdateAsyncLoop(int saveAndResetEveryNUpdates)
		{
			ThreadPool.QueueUserWorkItem(UpdateLoop, saveAndResetEveryNUpdates);
		}

		public void UpdateLoop(object state)
		{
			var sinceReset = 0;
			try
			{
				while (true)
				{
					if (sinceReset > (int)state)
					{
						sinceReset = 0;
						Save();
					}
					sinceReset++;
					Update();
					Thread.Sleep(2000);
				}
			}
			finally
			{
				Save();
			}
		}

		public virtual void RefreshAll()
		{
			var serializer = new JavaScriptSerializer();
			using (var client = new WebClient())
			{
				client.Headers.Add(HttpRequestHeader.Accept, "/*/");
				client.Headers.Add(HttpRequestHeader.UserAgent,
								   "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:25.0) Gecko/20100101 Firefox/25.0");
				var r = serializer
					.Deserialize<Results<RefreshResponse>>(client.DownloadString("http://uk.madbid.com/json/site/load/current/refresh/"))
					.Response;

				r.items.ToList()
					.ForEach(a =>
								 {
									 var auction = Auctions.GetOrAdd(a.auction_id, () => new Auction { Id = a.auction_id });
									 auction.Title = a.title;
									 auction.Images = a.images.Select(i => string.Format("{0}/{1}", r.reference.image_base, i)).ToArray();
									 auction.Description = a.description + a.description_summary;
									 auction.BidTimeOut = a.auction_data.timeout;
									 auction.Price = a.auction_data.last_bid.highest_bid;
									 auction.CreditCost = a.auction_data.credit_cost;
									 auction.LastBidDate = a.auction_data.last_bid.Date;
									 auction.Status = (AuctionStatus) (a.auction_data.state%100);
									 a.auction_data.bidding_history
										 .OrderBy(b => b.bid_value)
										 .ToList()
										 .ForEach(b =>
													  {
														  var player = auction.Players.GetOrAdd(b.user_name, () => new Player { Name = b.user_name });
														  var last = auction.Bids.LastOrDefault();
														  if (last!=null && b.bid_value <= last.Value)
															  return;
														  var time = a.auction_data.last_bid.Date != null
														             && Math.Abs(b.bid_value - a.auction_data.last_bid.highest_bid) < 0.001
														             && b.user_name == a.auction_data.last_bid.highest_bidder
															             ? a.auction_data.last_bid.Date
															             : (DateTime?) null;
														  auction.Bids.Add(new Bid { Auction = auction, Player = player, Value = b.bid_value, Time = time });
													  });

								 });
			}
		}

		public virtual void Update()
		{
			var serializer = new JavaScriptSerializer();
			using (var client = new WebClient())
			{
				client.Headers.Add(HttpRequestHeader.Accept, "/*/");
				client.Headers.Add(HttpRequestHeader.UserAgent,
								   "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:25.0) Gecko/20100101 Firefox/25.0");
				serializer
					.Deserialize<Results<UpdateResponse>>(client.DownloadString("http://uk.madbid.com/json/auction/update/"))
					.Response
					.items
					.Where(a => a.highest_bid > 0.001)
					.ToList()
					.ForEach(a =>
								 {
									 var auction = Auctions.GetOrAdd(a.auction_id, () => new Auction { Id = a.auction_id });
									 auction.BidTimeOut = a.timeout;
									 auction.Status = (AuctionStatus)(a.state % 100);
									 auction.Price = a.highest_bid;
									 DateTime date;
									 if (!DateTime.TryParse(a.date_bid, out date) || auction.LastBidDate == date)
										 return;
									 auction.LastBidDate = date;
									 var player = auction.Players.GetOrAdd(a.highest_bidder, () => new Player { Name = a.highest_bidder });
									 player.Count++;
									 //player.Delta++;
								 });
			}

		}
	}
}