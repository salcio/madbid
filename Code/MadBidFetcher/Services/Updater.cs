using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
		public Dictionary<int, Model.Auction> Auctions { get; protected set; }
		public HashSet<string> Players { get; protected set; }
		public string FilesPath { get; set; }
		public static Updater Instance { get; private set; }

		public static Updater Initialize(string auctionsPath)
		{
			var files = Directory.Exists(auctionsPath)
				            ? Directory.GetFiles(auctionsPath)
					              .Select(f => new FileInfo(f))
					              .Where(f => f.Name.StartsWith("data-"))
					              .OrderByDescending(f => f.LastWriteTime)
					              .Select(f => f.FullName)
					              .ToArray()
				            : new string[0];
			if (files.Length > 0)
			{
				var auctions = ObjectExtensions.DeSerializeObject<Dictionary<int, Model.Auction>>(files[0]);
				var players = new HashSet<string>();
				auctions.Values.ToList().ForEach(a =>
													 {
														 a.Bids = a.Bids.Distinct().OrderBy(v => v.Value).ToList();
														 a.Players.Values.ToList().ForEach(p =>
																							   {
																								   p.Bids = p.Bids.Distinct().OrderBy(v => v.Value).ToList();
																								   if (!players.Contains(p.Name))
																									   players.Add(p.Name);
																							   });
													 });
				return Instance = new Updater
						   {
							   Auctions = auctions,
							   FilesPath = auctionsPath
						   };
			}
			return Instance = new Updater { FilesPath = auctionsPath };

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
					.OrderByDescending(f => f.LastWriteTime)
					.Skip(3)
					.ToList()
					.ForEach(f => f.Delete());
			}
			catch (Exception e) { }
		}

		public Updater()
		{
			Auctions = new Dictionary<int, Model.Auction>();
			Players = new HashSet<string>();
		}

		public void UpdateAsyncLoop(int saveAndResetEveryNUpdates)
		{
			ThreadPool.QueueUserWorkItem(UpdateLoop, saveAndResetEveryNUpdates);
		}

		public void UpdateLoop(object state)
		{
			DateTime? lastRefreshTime = null;
			DateTime? lastSaveTime = null;
			while (true)
			{
				try
				{
					if (lastRefreshTime == null || (DateTime.Now - lastRefreshTime.Value).TotalSeconds > 20)
					{
						RefreshAll();
						lastRefreshTime = DateTime.Now;
					}
					else
						Update();
					if (lastSaveTime == null || (DateTime.Now - lastSaveTime.Value).TotalSeconds > 120)
					{
						lastSaveTime = DateTime.Now;
						Save();
					}
					Thread.Sleep(2000);
				}
				catch (Exception e)
				{
				}
			}
		}

		public virtual void RefreshAll()
		{
			Refresh("http://uk.madbid.com/json/site/load/current/refresh/");
		}

		public virtual void Refresh(int auctionId)
		{
			Refresh(string.Format("http://uk.madbid.com/json/site/load/auction/{0}/extend/refresh/", auctionId));
		}

		public virtual void Refresh(string url)
		{
			var serializer = new JavaScriptSerializer();
			using (var client = new WebClient())
			{
				client.Headers.Add(HttpRequestHeader.Accept, "/*/");
				client.Headers.Add(HttpRequestHeader.UserAgent,
								   "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:25.0) Gecko/20100101 Firefox/25.0");
				var r = serializer
					.Deserialize<Results<RefreshResponse>>(client.DownloadString(url))
					.Response;

				r.items.ToList()
					.ForEach(a =>
								 {
									 var auction = Auctions.GetOrAdd(a.auction_id, () => new Model.Auction { Id = a.auction_id });
									 auction.Title = a.title;
									 if (auction.Images == null || auction.Images[0].StartsWith("http", true, CultureInfo.CurrentCulture))
									 {
										 var images = a.images
											 .Select(
												 i =>
												 new Uri(string.Format("{0}{2}/{3}/{4}/{1}.normal.jpg", r.reference.image_base, i, i[i.Length - 3], i[i.Length - 2],
															   i[i.Length - 1])))
											 .ToList();

										 var imagesfolder = Path.Combine(FilesPath, "images");
										 if (!Directory.Exists(imagesfolder))
										 {
											 Directory.CreateDirectory(imagesfolder);
										 }
										 images
											 .ForEach(i =>
														  {
															  var fileName = Path.Combine(FilesPath, "images", i.Segments.Last());
															  if (File.Exists(fileName))
																  return;
															  client.DownloadFile(i, fileName);
														  });

										 auction.Images = images.Select(i => Path.Combine(FilesPath, "images", i.Segments.Last())).ToArray();
									 }
									 auction.Description = a.description + a.description_summary;
									 auction.BidTimeOut = a.auction_data.timeout;
									 auction.Price = a.auction_data.last_bid.highest_bid;
									 auction.RetailPrice = a.rrp;
									 auction.CreditCost = a.auction_data.credit_cost;
									 auction.LastBidDate = a.auction_data.last_bid.Date;
									 auction.Status = (AuctionStatus)(a.auction_data.state % 100);
									 auction.ActivePlayers = null;
									 auction.DateOpens = a.auction_data.date_opens;
									 auction.StartTime = a.auction_data.availability.time_start;
									 auction.EndTime = a.auction_data.availability.time_end;
									 auction.ProductId = a.product_id;
									 a.auction_data.bidding_history
										 .OrderBy(b => b.bid_value)
										 .ToList()
										 .ForEach(b =>
													  {
														  var player = auction.Players.GetOrAdd(b.user_name, () => new Model.Player { Name = b.user_name });
														  var time = a.auction_data.last_bid.Date != null
																	 && Math.Abs(b.bid_value - a.auction_data.last_bid.highest_bid) < 0.001
																	 && b.user_name == a.auction_data.last_bid.highest_bidder
																		 ? a.auction_data.last_bid.Date
																		 : (DateTime?)null;
														  var newbid = new Model.Bid { Auction = auction, Player = player, Value = b.bid_value, Time = time };
														  InsertBid(auction.Bids, newbid);
														  InsertBid(player.Bids, newbid);
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
									 var auction = Auctions.GetOrAdd(a.auction_id, () => new Model.Auction { Id = a.auction_id });
									 auction.BidTimeOut = a.timeout;
									 auction.Status = (AuctionStatus)(a.state % 100);
									 DateTime date;
									 if (!DateTime.TryParse(a.date_bid, out date) || auction.LastBidDate == date)
										 return;
									 auction.LastBidDate = date;
									 auction.Price = a.highest_bid;
									 auction.ActivePlayers = null;
									 var player = auction.Players.GetOrAdd(a.highest_bidder, () => new Model.Player { Name = a.highest_bidder });
									 var newBid = new Model.Bid { Auction = auction, Player = player, Time = date, Value = a.highest_bid };
									 InsertBid(auction.Bids, newBid);
									 InsertBid(player.Bids, newBid);
								 });
			}

		}

		private static void InsertBid(List<Model.Bid> bids, Model.Bid newBid)
		{
			lock (bids)
			{
				var index = bids.Count - 20;
				if (bids.Count > 0)
				{
					for (index = index < 0 ? 0 : index; index < bids.Count; index++)
					{
						if (bids[index].Equals(newBid))
							return;
						if (bids[index].Value > newBid.Value)
							break;
					}
				}
				if (index >= bids.Count || index < 0)
					bids.Add(newBid);
				else
					bids.Insert(index, newBid);
			}
		}
	}
}