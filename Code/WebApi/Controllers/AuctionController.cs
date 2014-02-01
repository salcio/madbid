using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using MadBidFetcher.Model;
using MadBidFetcher.Services;

namespace WebApi.Controllers
{
	public class AuctionController : Controller
	{
		public ActionResult Index()
		{
			return View(Updater.Instance.Auctions.Values
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

		//public ActionResult Populate()
		//{
		//	if (Request.HttpMethod == "GET")
		//	return View();
		//	Updater.Instance.
		//}

		public ActionResult Show(int id)
		{
			Auction auction;
			if (!Updater.Instance.Auctions.TryGetValue(id, out auction))
				return RedirectToAction("Index");
			var model = new AuctionModel
			{
				Auction = auction,
			};
			return View(model);
		}

		public ActionResult Similar(int id)
		{
			Auction auction;
			if (!Updater.Instance.Auctions.TryGetValue(id, out auction))
				return RedirectToAction("Index");

			return View("Index", Updater.Instance.Auctions
				.Values
				.Where(a => a.ProductId == auction.ProductId)
				.OrderByDescending(a=>a.Id==id)
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

		public ActionResult Image(int id, int index)
		{
			Auction auction;
			if (!Updater.Instance.Auctions.TryGetValue(id, out auction) || auction.Images == null || auction.Images.Length == 0)
				return new EmptyResult();
			if (auction.Images[index].StartsWith("http"))
				return Redirect(auction.Images[index]);
			var fileLastModified = System.IO.File.GetLastWriteTimeUtc(auction.Images[index]);
			fileLastModified = new DateTime(fileLastModified.Year, fileLastModified.Month, fileLastModified.Day, fileLastModified.Hour, fileLastModified.Minute, fileLastModified.Second);
			if (Request.Headers["If-Modified-Since"] != null)
			{
				var modifiedSince = DateTime.Parse(Request.Headers["If-Modified-Since"]);

				if (modifiedSince.ToUniversalTime() >= fileLastModified)
				{
					Response.StatusCode = 304;
					Response.StatusDescription = "Not Modified";
					Response.SuppressContent = true;
					return new ContentResult { Content = string.Empty, ContentType = "image/jpg" };
				}
			}
			Response.Cache.SetLastModified(fileLastModified);
			Response.Cache.SetExpires(DateTime.Now.AddDays(2));
			Response.Cache.SetCacheability(HttpCacheability.Public);
			Response.Cache.SetMaxAge(new TimeSpan(2, 2, 2, 2));
			Response.Cache.SetSlidingExpiration(true);
			return File(auction.Images[index], "image/jpg", null);
		}

		public ActionResult TogglePin(int id)
		{
			Auction auction;
			if (Updater.Instance.Auctions.TryGetValue(id, out auction))
			{
				auction.Pinned = !auction.Pinned;
			}
			return Redirect(Request.UrlReferrer.AbsoluteUri);
		}

		public ActionResult RefreshAll()
		{
			lock (Updater.Instance.Auctions)
			{
				foreach (var value in Updater.Instance.Auctions.Values)
				{
					Updater.Instance.Refresh(value.Id);
				}
				return Redirect(Request.UrlReferrer.AbsoluteUri);
			}
		}

		public ActionResult Refresh(int id)
		{
			Auction auction;
			if (Updater.Instance.Auctions.TryGetValue(id, out auction))
			{
				Updater.Instance.Refresh(id);
			}
			return Redirect(Request.UrlReferrer.AbsoluteUri);
		}
	}
}
