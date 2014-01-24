using System;
using System.Collections;
using System.Collections.Generic;

namespace MadBidFetcher.Extensions
{
	public static class DictionaryExtensions
	{
		public static T2 GetOrAdd<T1, T2>(this Dictionary<T1, T2> dictionary, T1 key, Func<T2> addFunction)
		{
			T2 value;
			if (dictionary.TryGetValue(key, out value))
				return value;
			lock (((ICollection) dictionary).SyncRoot)
			{
				if (dictionary.TryGetValue(key, out value))
					return value;
				return dictionary[key] = addFunction();
			}
		}
	}
}