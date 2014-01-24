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
						ResetDeltas();
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

		public void ResetDeltas()
		{
			Auctions.Values.ToList().ForEach(a => a.ResetDeltas());
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
					.Deserialize<Results>(client.DownloadString("http://uk.madbid.com/json/auction/update/"))
					.response
					.items
					.Where(a => a.highest_bid > 0.001)
					.ToList()
					.ForEach(a =>
								 {
									 var auction = Auctions.GetOrAdd(a.auction_id, () => new Auction { Id = a.auction_id });
									 auction.BidTime = a.timeout;
									 auction.Status = (AuctionStatus)(a.state % 100);
									 auction.Price = a.highest_bid;
									 DateTime date;
									 if (!DateTime.TryParse(a.date_bid, out date) || auction.LastBidDate == date)
										 return;
									 auction.LastBidDate = date;
									 var player = auction.Players.GetOrAdd(a.highest_bidder, () => new Player { Name = a.highest_bidder });
									 player.Count++;
									 player.Delta++;
								 });
			}

		}
	}
}