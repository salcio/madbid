using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	public class DbUpdater
	{
		private MadBidContext ctx;
		public string FilesPath { get; set; }

		public static DbUpdater Instance { get; private set; }

		public static DbUpdater Initialize(string auctionsPath)
		{
			Instance = new DbUpdater(auctionsPath);
			return Instance;
		}

		private DbUpdater(string path)
		{
			FilesPath = path;
		}

		public void UpdateAsyncLoop()
		{
			ThreadPool.QueueUserWorkItem(UpdateLoop);
		}

		public void UpdateLoop(object state)
		{
			DateTime? lastRefreshTime = null;
			while (true)
			{
				using (ctx = new MadBidContext())
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
						Thread.Sleep(2000);
					}
					catch (Exception e)
					{
					}
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
			var disposeContext = false;
			var context = ctx;
			if (context == null)
			{
				context = new MadBidContext();
				disposeContext = true;
			}
			try
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
										 var auction = context.Auctions.FirstOrDefault(au => au.Id == a.auction_id) ??
													   context.Auctions.Add(new Auction { Id = a.auction_id, Players = new Collection<Player>(), Bids = new Collection<Bid>() });
										 auction.Title = a.title;
										 if (auction.Images == null || auction.Images[0].StartsWith("http", true, CultureInfo.CurrentCulture))
										 {
											 var images = a.images
												 .Select(
													 i =>
													 new Uri(string.Format("{0}{2}/{3}/{4}/{1}.normal.jpg", r.reference.image_base, i, i[i.Length - 3],
																		   i[i.Length - 2],
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
										 auction.DateOpens = a.auction_data.date_opens;
										 auction.StartTime = a.auction_data.availability.time_start;
										 auction.EndTime = a.auction_data.availability.time_end;
										 auction.ProductId = a.product_id;
										 a.auction_data.bidding_history
											 .OrderBy(b => b.bid_value)
											 .ToList()
											 .ForEach(b =>
														  {
															  var player = auction.Players.FirstOrDefault(p => p.Name == b.user_name)
																		   ?? context.Players.FirstOrDefault(p => p.Name == b.user_name)
																		   ?? context.Players.Add(new Player { Name = b.user_name });
															  if (player.Name == SessionStore.CurrentUser)
															  {
																  auction.CurrentUserAuction = true;
															  }
															  var time = a.auction_data.last_bid.Date != null
																		 && Math.Abs(b.bid_value - a.auction_data.last_bid.highest_bid) < 0.001
																		 && b.user_name == a.auction_data.last_bid.highest_bidder
																			 ? a.auction_data.last_bid.Date
																			 : (DateTime?)null;
															  var bid = auction.Bids.FirstOrDefault(ab => Math.Abs(ab.Value - b.bid_value) < 0.001)
																		?? context.Bids.Add(new Bid
																							 {
																								 Auction = auction,
																								 Player = player,
																								 Value = b.bid_value,
																								 Time = time
																							 });
															  bid.Time = time;
														  });
									 });
				}
			}
			finally
			{
				context.SaveChanges();
				if (disposeContext)
				{
					context.Dispose();
				}
			}
		}

		public virtual void Update()
		{
			var disposeContext = false;
			var context = ctx;
			if (context == null)
			{
				context = new MadBidContext();
				disposeContext = true;
			}
			try
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
										 var auction = context.Auctions.FirstOrDefault(au => au.Id == a.auction_id) ??
													   context.Auctions.Add(new Auction { Id = a.auction_id, Players = new Collection<Player>(), Bids = new Collection<Bid>() });
										 auction.BidTimeOut = a.timeout;
										 auction.Status = (AuctionStatus)(a.state % 100);
										 DateTime date;
										 if (!DateTime.TryParse(a.date_bid, out date) || auction.LastBidDate == date)
											 return;
										 auction.LastBidDate = date;
										 auction.Price = a.highest_bid;
										 var player = auction.Players.FirstOrDefault(p => p.Name == a.highest_bidder)
													  ?? context.Players.FirstOrDefault(p => p.Name == a.highest_bidder)
													  ?? context.Players.Add(new Player { Name = a.highest_bidder });
										 if (player.Name == SessionStore.CurrentUser)
										 {
											 auction.CurrentUserAuction = true;
										 }
										 if (auction.Bids.FirstOrDefault(ab => Math.Abs(ab.Value - a.highest_bid) < 0.001) == null)
											 context.Bids.Add(new Bid
																  {
																	  Auction = auction,
																	  Player = player,
																	  Value = a.highest_bid,
																  });
									 });
				}

			}
			finally
			{
				context.SaveChanges();
				if (disposeContext)
				{
					context.Dispose();
				}
			}
		}
	}
}