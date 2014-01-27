﻿using System;
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
		public string FilesPath { get; set; }
		public static Updater Instance { get; private set; }

		public static Updater Initialize(string auctionsPath)
		{
			var files = Directory.Exists(auctionsPath)
							? Directory.GetFiles(auctionsPath).OrderByDescending(f => new FileInfo(f).LastWriteTime).ToArray()
							: new string[0];
			return Instance = files.Length > 0
					   ? new Updater
							 {
								 Auctions = ObjectExtensions.DeSerializeObject<Dictionary<int, Auction>>(files[0]),
								 FilesPath = auctionsPath
							 }
					   : new Updater { FilesPath = auctionsPath };
		}

		public void Save()
		{
			if (!Directory.Exists(FilesPath))
			{
				Directory.CreateDirectory(FilesPath);
			}
			Auctions.SerializeObject(Path.Combine(FilesPath, string.Format("data-{0}.dat", DateTime.Now.ToString("ddMMyyyyhhmmss"))));
			try
			{
				Directory.GetFiles(FilesPath)
					.Select(f => new FileInfo(f))
					.Where(f => (DateTime.Now - f.LastWriteTime).TotalMinutes > 5)
					.ToList()
					.ForEach(f => f.Delete());
			}
			catch (Exception e) { }
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
			DateTime? lastRefreshTime = null;
			while (true)
			{
				try
				{
					if (lastRefreshTime == null || (DateTime.Now - lastRefreshTime.Value).TotalSeconds > 30)
					{
						RefreshAll();
						lastRefreshTime = DateTime.Now;
						Save();
					}
					else
						Update();
					Thread.Sleep(2000);
				}
				catch (Exception e)
				{
				}
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
									 auction.Images = a.images.Select(i => string.Format("{0}{2}/{3}/{4}/{1}.normal.jpg", r.reference.image_base, i, i[i.Length - 3], i[i.Length - 2], i[i.Length - 1])).ToArray();
									 auction.Description = a.description + a.description_summary;
									 auction.BidTimeOut = a.auction_data.timeout;
									 auction.Price = a.auction_data.last_bid.highest_bid;
									 auction.RetailPrice = a.rrp;
									 auction.CreditCost = a.auction_data.credit_cost;
									 auction.LastBidDate = a.auction_data.last_bid.Date;
									 auction.Status = (AuctionStatus)(a.auction_data.state % 100);
									 auction.ActivePlayers = null;
									 auction.StartTime = a.auction_data.availability.time_start;
									 auction.EndTime = a.auction_data.availability.time_end;
									 var bidsToCheck = auction.Bids.Count > 100
														   ? auction.Bids.Skip(auction.Bids.Count - 20).Take(20).ToList()
														   : auction.Bids;
									 a.auction_data.bidding_history
										 .OrderBy(b => b.bid_value)
										 .ToList()
										 .ForEach(b =>
													  {
														  var player = auction.Players.GetOrAdd(b.user_name, () => new Player { Name = b.user_name });
														  if (bidsToCheck.Any(bid => Math.Abs(bid.Value - b.bid_value) < 0.001))
															  return;
														  var time = a.auction_data.last_bid.Date != null
																	 && Math.Abs(b.bid_value - a.auction_data.last_bid.highest_bid) < 0.001
																	 && b.user_name == a.auction_data.last_bid.highest_bidder
																		 ? a.auction_data.last_bid.Date
																		 : (DateTime?)null;
														  auction.Bids.Add(new Bid { Auction = auction, Player = player, Value = b.bid_value, Time = time });
													  });
									 auction.Bids = auction.Bids.OrderBy(a1 => a1.Value).ToList();
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
									 DateTime date;
									 if (!DateTime.TryParse(a.date_bid, out date) || auction.LastBidDate == date)
										 return;
									 auction.LastBidDate = date;
									 auction.Price = a.highest_bid;
									 auction.ActivePlayers = null;
									 var player = auction.Players.GetOrAdd(a.highest_bidder, () => new Player { Name = a.highest_bidder });
									 auction.Bids = auction.Bids ?? new List<Bid>();
									 var lastBid = auction.Bids.LastOrDefault();
									 if (lastBid != null && lastBid.Value - a.highest_bid > 0.001)
										 return;
									 auction.Bids.Add(new Bid { Auction = auction, Player = player, Time = date, Value = a.highest_bid });
								 });
			}

		}
	}
}