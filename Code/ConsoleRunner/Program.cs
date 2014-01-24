using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MadBidFetcher.Services;

namespace ConsoleRunner
{
	class Program
	{
		static void Main(string[] args)
		{
			var updater = Updater.Initialize(@"c:\data.dat");
				updater.UpdateLoop(100);
		}
	}
}
