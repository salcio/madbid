using System;

namespace MadBidFetcher.Model.MadBitl
{
	public class Reference
	{
		public string image_base { get; set; }

		public int sms_cost { get; set; }
		public int sms_msisdn { get; set; }
		public DateTime timestamp { get; set; }
	}
}