using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using Verse;
using UnityEngine;

namespace SmashTools.Xml
{
	public static class XmlExporter
	{
		private static XmlWriter writer;
		private static FileStream stream;
		private static string filePath;

		public static void StartDocument(string filePath)
		{
			XmlExporter.filePath = filePath;
			stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
			writer = XmlWriter.Create(stream, new XmlWriterSettings()
			{
				Indent = true,
				IndentChars = "\t",
				NewLineChars = "\n",
				Encoding = Encoding.UTF8,
			});
			writer.WriteStartDocument();
		}

		public static void Export()
		{
			Application.OpenURL(filePath);
		}

		public static void Close()
		{
			writer.WriteEndDocument();
			writer.Flush();
			writer.Close();
			writer.Dispose();

			stream.Close();
			stream.Dispose();

			stream = null;
			writer = null;
			filePath = string.Empty;
		}

		public static void OpenNode(string name, params (string name, string value)[] attributes)
		{
			try
			{
				writer.WriteStartElement(name);
				if (!attributes.NullOrEmpty())
				{
					foreach ((string name, string value) attribute in attributes)
					{
						if (attribute.name.NullOrEmpty())
						{
							continue;
						}
						writer.WriteAttributeString(attribute.name, attribute.value);
					}
				}
			}
			catch (Exception ex)
			{
				Close();
				throw ex;
			}
		}

		public static void CloseNode(bool emptyText = false)
		{
			try
			{
				if (emptyText)
				{
					writer.WriteFullEndElement();
				}
				else
				{
					writer.WriteEndElement();
				}
			}
			catch (Exception ex)
			{
				Close();
				throw ex;
			}
		}

		public static void WriteObject<T>(string localName, T value)
		{
			try
			{
				writer.WriteElementString(localName, value.ToString());
			}
			catch (Exception ex)
			{
				Close();
				throw ex;
			}
		}

		public static void WriteElement(string localName, string value)
		{
			try
			{
				writer.WriteElementString(localName, value);
			}
			catch (Exception ex)
			{
				Close();
				throw ex;
			}
		}

		public static void WriteElement(string localName, IXmlExport value)
		{
			try
			{
				OpenNode(localName);
				{
					value.Export();
				}
				CloseNode();
			}
			catch (Exception ex)
			{
				Close();
				throw ex;
			}
		}

		public static void WriteString(string text)
		{
			try
			{
				writer.WriteString(text);
			}
			catch (Exception ex)
			{
				Close();
				throw ex;
			}
		}

		public static void WriteList<T>(string localName, IList<T> value) where T : IXmlExport
		{
			try
			{
				OpenNode(localName);
				{
					foreach (T item in value)
					{
						OpenNode("li");
						{
							item.Export();
						}
						CloseNode();
					}
				}
				CloseNode();
			}
			catch (Exception ex)
			{
				Close();
				throw ex;
			}
		}

		public static void WriteList<T>(string localName, IList<T> value, Action<T> itemWriter)
		{
			try
			{
				OpenNode(localName);
				{
					foreach (T item in value)
					{
						OpenNode("li");
						{
							itemWriter(item);
						}
						CloseNode();
					}
				}
				CloseNode();
			}
			catch (Exception ex)
			{
				Close();
				throw ex;
			}
		}
	}
}
