using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MadBidFetcher.Extensions
{
	public static class ObjectExtensions
	{
		public static string ToXmlString<T>(this T input)
		{
			using (var writer = new StringWriter())
			{
				input.ToXml(writer);
				return writer.ToString();
			}
		}

		public static void ToXml<T>(this T objectToSerialize, Stream stream)
		{
			new XmlSerializer(typeof(T)).Serialize(stream, objectToSerialize);
		}

		public static void ToXml<T>(this T objectToSerialize, StringWriter writer)
		{
			new XmlSerializer(typeof(T)).Serialize(writer, objectToSerialize);
		}

		public static string SerializeToXml<T>(this T value)
		{
			if (value == null)
			{
				return null;
			}

			var serializer = new XmlSerializer(typeof(T));

			var settings = new XmlWriterSettings
				               {
					               Encoding = new UnicodeEncoding(false, false),
					               Indent = false,
					               OmitXmlDeclaration = false
				               };

			using (var textWriter = new StringWriter())
			{
				using (var xmlWriter = XmlWriter.Create(textWriter, settings))
				{
					serializer.Serialize(xmlWriter, value);
				}
				return textWriter.ToString();
			}
		}

		public static void SerializeToXmlFile<T>(this T @object, string file)
		{
			var xml = @object.SerializeToXml();
			File.WriteAllText(file, xml, Encoding.Unicode);
		}

		public static T DeserializeFromXmlFile<T>(string file)
		{
			return DeserializeFromXml<T>(File.ReadAllText(file));
		}


		public static T DeserializeFromXml<T>(string xml)
		{

			if (string.IsNullOrEmpty(xml))
			{
				return default(T);
			}

			var serializer = new XmlSerializer(typeof(T));

			var settings = new XmlReaderSettings();
			// No settings need modifying here

			using (var textReader = new StringReader(xml))
			{
				using (var xmlReader = XmlReader.Create(textReader, settings))
				{
					return (T)serializer.Deserialize(xmlReader);
				}
			}
		}
		public static void SerializeObject<T>(this T objectToSerialize, string filename)
		{
			using (var stream = File.Open(filename, FileMode.Create))
			{
				var bFormatter = new BinaryFormatter();
				bFormatter.Serialize(stream, objectToSerialize);
			}
		}

		public static T DeSerializeObject<T>(string filename)
		{
			using (Stream stream = File.Open(filename, FileMode.Open))
			{
				var bFormatter = new BinaryFormatter();
				return (T)bFormatter.Deserialize(stream);
			}
		}
	}
}