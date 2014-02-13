using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.UI;
using MadBidFetcher;
using MadBidFetcher.Extensions;
using MadBidFetcher.Model;
using MadBidFetcher.Services;
using Auction = MadBidFetcher.Model.Auction;
using Bid = MadBidFetcher.Bid;

namespace WebApi.Controllers
{
	public class AuctionController : Controller
	{
		public ActionResult Seed()
		{
			var creditAuction = new Regex(@"^\d{1,3} MadBid Credits");
			using (var ctx = new MadBidContext())
			{
				Updater.Initialize(HostingEnvironment.ApplicationPhysicalPath + "App_Data\\");
				var dbPlayers = ctx.Players.ToDictionary(p => p.Name, p => p);
				ctx.Players.AddRange(Updater.Instance.Players
											.Union(Updater.Instance.Auctions.SelectMany(a => a.Value.Players.Keys))
											.Where(p => !dbPlayers.ContainsKey(p))
											.Select(p => new MadBidFetcher.Player { Name = p }));
				ctx.SaveChanges();
				dbPlayers = ctx.Players.ToDictionary(p => p.Name, p => p);
				foreach (var a in Updater.Instance.Auctions.Where(a=>!creditAuction.Match(a.Value.Title).Success || a.Value.CurrentUserAuction))
				{
					var auction = ctx.Auctions
									 .FirstOrDefault(ca => ca.Id == a.Value.Id);
					if (auction == null)
					{
						auction = MadBidFetcher.Auction.FromModel(a.Value);
						auction.Bids = new Collection<Bid>();
						auction.Players = new Collection<MadBidFetcher.Player>();
						ctx.Auctions.Add(auction);
					}
					else
					{

						if (ctx.Entry(auction).Collection(e => e.Players).Query().Count() == a.Value.Players.Count
							&& ctx.Entry(auction).Collection(e => e.Bids).Query().Count() == a.Value.Bids.Count)
							continue;
						ctx.Entry(auction).Collection(e => e.Players).Load();
						ctx.Entry(auction).Collection(e => e.Bids).Load();
					}

					if (auction.Players.Count != a.Value.Players.Count)
					{
						if (auction.Players.Count > 0)
						{
							a.Value.Players.Values
							 .Distinct()
							 .Where(p => auction.Players.All(ap => ap.Name != p.Name))
							 .Select(p => dbPlayers[p.Name])
							 .ToList()
							 .ForEach(auction.Players.Add);
						}
						else
						{
							a.Value.Players.Values
							 .Distinct()
							 .Select(p => dbPlayers[p.Name])
							 .ToList()
							 .ForEach(auction.Players.Add);
						}
					}
						if (auction.Bids.Count != a.Value.Bids.Count)
							if (auction.Bids.Count > 0)
								ctx.Bids.AddRange(a.Value.Bids
													  .Where(b => auction.Bids.All(ab => Math.Abs(ab.Value - b.Value) > 0.001))
													  .Select(b => Bid.FromModel(b, auction)));
							else
								ctx.Bids.AddRange(a.Value.Bids
													  .Select(b => Bid.FromModel(b, auction)));
					ctx.SaveChanges();
				}
			}
			return new EmptyResult();
		}

		public ActionResult Index()
		{
			using (var ctx = new MadBidContext())
			{
				return View(ctx.Auctions
								.Where(a =>
									   a.Status != AuctionStatus.Closed
									   || a.LastBidDate.HasValue && a.LastBidDate.Value.AddDays(1) > DateTime.Now
									   && (!a.Title.Contains("MadBid Credits") || a.LastBidDate.Value.AddHours(1) > DateTime.Now))
								.OrderByDescending(a => a.Status == AuctionStatus.Running)
								.ThenByDescending(a => a.Pinned)
								.ThenByDescending(a => a.CurrentUserAuction)
								.ThenByDescending(a => a.Status)
					//.ThenByDescending(a => a.ActivePlayers[Auction.ActivaPlayersTimes[0]].Count)
					//.ThenByDescending(a => a.ActivePlayers[Auction.ActivaPlayersTimes[1]].Count)
								.ThenBy(a => a.BidTimeOut)
								.ThenBy(a => a.Title.Contains("MadBid Credits"))
								.ToList());
			}
		}

		//public ActionResult Populate()
		//{
		//	if (Request.HttpMethod == "GET")
		//	return View();
		//	Updater.Instance.
		//}

		public ActionResult Show(int id)
		{
			using (var ctx = new MadBidContext())
			{
				var auction = ctx.Auctions.FirstOrDefault(a => a.Id == id);
				var model = new AuctionModel
								{
									Auction = auction,
								};
				return View(model);
			}
		}

		public ActionResult Similar(int id)
		{
			using (var ctx = new MadBidContext())
			{
				var auction = ctx.Auctions.FirstOrDefault(a => a.Id == id);

				return View("Index", ctx.Auctions
					.Where(a => a.ProductId == auction.ProductId)
					.OrderByDescending(a => a.Id == id)
					.ThenByDescending(a => a.Status == AuctionStatus.Running)
					.ThenByDescending(a => a.Pinned)
					.ThenByDescending(a => a.CurrentUserAuction)
					.ThenByDescending(a => a.Status)
					//.ThenByDescending(a => a.ActivePlayers[Auction.ActivaPlayersTimes[0]].Count)
					//.ThenByDescending(a => a.ActivePlayers[Auction.ActivaPlayersTimes[1]].Count)
					.ThenBy(a => a.BidTimeOut)
					.ThenBy(a => a.Title.Contains("MadBid Credits"))
					.ToList());
			}
		}

		public ActionResult Image(int id, int index)
		{
			using (var ctx = new MadBidContext())
			{
				var auction = ctx.Auctions.FirstOrDefault(a => a.Id == id);
				if (auction == null)
					return new EmptyResult();
				if (auction.Images[index].StartsWith("http"))
					return Redirect(auction.Images[index]);
				var fileLastModified = System.IO.File.GetLastWriteTimeUtc(auction.Images[index]);
				fileLastModified = new DateTime(fileLastModified.Year, fileLastModified.Month, fileLastModified.Day,
				                                fileLastModified.Hour, fileLastModified.Minute, fileLastModified.Second);
				if (Request.Headers["If-Modified-Since"] != null)
				{
					var modifiedSince = DateTime.Parse(Request.Headers["If-Modified-Since"]);

					if (modifiedSince.ToUniversalTime() >= fileLastModified)
					{
						Response.StatusCode = 304;
						Response.StatusDescription = "Not Modified";
						Response.SuppressContent = true;
						return new ContentResult {Content = string.Empty, ContentType = "image/jpg"};
					}
				}
				Response.Cache.SetLastModified(fileLastModified);
				Response.Cache.SetExpires(DateTime.Now.AddDays(2));
				Response.Cache.SetCacheability(HttpCacheability.Public);
				Response.Cache.SetMaxAge(new TimeSpan(2, 2, 2, 2));
				Response.Cache.SetSlidingExpiration(true);
				return File(auction.Images[index], "image/jpg", null);
			}
		}

		public ActionResult TogglePin(int id)
		{
			using (var ctx = new MadBidContext())
			{
				var auction = ctx.Auctions.FirstOrDefault(a => a.Id == id);
				if (auction != null)
				{
					auction.Pinned = !auction.Pinned;
					ctx.SaveChanges();
				}
			}
			return Redirect(Request.UrlReferrer.AbsoluteUri);
		}

		public ActionResult RefreshAll()
		{
			using (var ctx = new MadBidContext())
			{
				foreach (var value in ctx.Auctions.Where(a => a.Status != AuctionStatus.Closed))
				{
					DbUpdater.Instance.Refresh(value.Id);
				}
				return Redirect(Request.UrlReferrer.AbsoluteUri);
			}
		}

		public ActionResult Refresh(int id)
		{
			using (var ctx = new MadBidContext())
			{
				var auction = ctx.Auctions.FirstOrDefault(a => a.Id == id);
				if (auction != null)
				{
					DbUpdater.Instance.Refresh(id);
				}
				return Redirect(Request.UrlReferrer.AbsoluteUri);
			}
		}
	}
}
