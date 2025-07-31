using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RolePlaying.Data
{
	public static class XmlHelper
	{
		public static XElement GetAssetElementFromXML(string filePath)
		{
			XDocument doc;
			using (var stream = TitleContainer.OpenStream($"Content/{filePath}.xml"))
			{
				doc = XDocument.Load(stream);
			}
			return doc.Root.Element("Asset");
		}
	}
}
