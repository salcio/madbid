namespace MadBidFetcher.Model.MadBitl
{
	public class AuctionRefresh
	{
		public AuctionData auction_data { get; set; }
		public int auction_id { get; set; }
		public object buynow_data { get; set; }
		public int[] categories { get; set; }
		public string description { get; set; }
		public string description_summary { get; set; }
		public string[] images { get; set; }
		public int intl_auction_id { get; set; }
		public int pool_id { get; set; }
		public int product_id { get; set; }
		public int prototype_id { get; set; }
		public float rrp { get; set; }
		public object segments { get; set; }
		public float shipping_costs { get; set; }
		public string sms_keyword { get; set; }
		public string title { get; set; }
		public int type { get; set; }
	}
}