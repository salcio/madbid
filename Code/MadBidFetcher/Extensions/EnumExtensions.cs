using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace MadBidFetcher.Extensions
{
	public static class EnumExtensions
	{
		public static T[] ToEnumArray<T>(this T enumValue)
			where T : IComparable, IFormattable, IConvertible
		{
			var enumValues = new List<T>();
			foreach (T val in Enum.GetValues(typeof(T)))
			{
				var left = (int)(object)val;
				var right = (int)(object)enumValue;
				if ((left & right) == left)
				{
					enumValues.Add(val);
				}
			}
			return enumValues.ToArray();
		}

		public static bool Matches<T>(this T enumValue, T value)
			where T : IComparable, IFormattable, IConvertible
		{
			return (int)(object)enumValue == ((int)(object)enumValue & (int)(object)value);
		}

		public static string[] Descriptions<T>(this T value)
			where T : IComparable, IFormattable, IConvertible
		{
			return value.ToEnumArray().Select(v => v.Description()).ToArray();
		}

		public static string[] Descriptions<T>()
			where T : IComparable, IFormattable, IConvertible
		{
			return Enum.GetValues(typeof(T)).Cast<T>().Select(v => v.Description()).ToArray();
		}

		public static string Description<T>(this T value)
			where T : IComparable, IFormattable, IConvertible
		{
			var stringEnumValue = value.ToString(CultureInfo.InvariantCulture);
			var fi = value.GetType().GetField(stringEnumValue);
			if (fi == null)
				return stringEnumValue;
			var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

			return attributes.Any() ? attributes[0].Description : stringEnumValue;
		}

		public static T GetValue<T>(this string description, T defaultValue)
			where T : IComparable, IFormattable, IConvertible
		{
			var value = Enum.GetValues(typeof (T)).Cast<T>().FirstOrDefault(v => v.Description() == description);
			return value != null ? value: defaultValue;
		}
	}
}