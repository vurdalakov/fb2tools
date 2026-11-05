namespace Vurdalakov
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    internal static class XmlExtensions
    {
        public static XElement GetElement(this XContainer container, String elementName) => container.Descendants().FirstOrDefault(x => elementName == x.Name.LocalName);

        public static XElement GetElement(this XContainer container, String elementName1, String elementName2) => container.GetElement(elementName1)?.GetElement(elementName2);

        public static String GetValue(this XContainer container, String elementName) => container.GetElement(elementName)?.Value.Trim();

        public static String GetValue(this XContainer container, String elementName1, String elementName2) => container.GetElement(elementName1)?.GetValue(elementName2);

        public static String GetInnerXml(this XContainer container, String elementName)
        {
            var element = container.GetElement(elementName);
            return null == element ? null : String.Concat(element.Nodes().Select(n => n.ToString()));
        }

        public static XElement GetElementWithAtribute(this XContainer container, String elementName, String attributeName, String attributeValue)
            => container.Descendants().FirstOrDefault(x => (elementName == x.Name.LocalName) && ((String)x.Attribute(attributeName) == attributeValue));

        public static String GetAttributeValue(this XElement element, String attributeName, String defaultAttributeValue = null) => element.Attribute(attributeName)?.Value ?? defaultAttributeValue;

        public static Encoding GetXmlEncoding(this Stream stream)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // read BOM
            using var binaryReader = new BinaryReader(stream, Encoding.ASCII, true); // leave open
            var bom = binaryReader.ReadUInt32();
            stream.Position = 0;

            // detect BOM
            if (0x00BFBBEF == (bom & 0x00FFFFFF))
            {
                return Encoding.UTF8;
            }
            if (0x00FEFF == (bom & 0x0000FFFF))
            {
                return Encoding.Unicode; // UTF-16 LE
            }
            if (0x0000FFFE == (bom & 0x0000FFFF))
            {
                return Encoding.BigEndianUnicode;
            }
            if (bom == 0x0000FEFF)
            {
                return new UTF32Encoding(true, true); // UTF-32 BE
            }
            if (0x0000FFFE == bom)
            {
                return Encoding.UTF32; // UTF-32 LE
            }

            // read encoding from XML declaration
            var buffer = new Byte[256];
            binaryReader.Read(buffer, 0, buffer.Length);
            stream.Position = 0;

            var start = Encoding.ASCII.GetString(buffer, 0, buffer.Length);

            var match = Regex.Match(start, @"<\?xml.*encoding=['""]([\w\-\d]+)['""]", RegexOptions.IgnoreCase);
            if (match.Success && (2 == match.Groups.Count))
            {
                try
                {
                    return Encoding.GetEncoding(match.Groups[1].Value);
                }
                catch
                {
                }
            }

            // default fallback
            return Encoding.UTF8;
        }
    }
}