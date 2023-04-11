using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;


namespace UnitTests.TestSources
{
    public class AvoidUsingXPathNavigatorWithoutRestrictionOfXMLExternalEntityReference_Source
    {

        public void function()
        {
            XPathDocument doc = new XPathDocument("example.xml");
            XPathNavigator nav = doc.CreateNavigator();
            string xml = nav.InnerXml.ToString();
        }

        public void function2(string filePath)
        {
            XPathDocument doc = new XPathDocument(filePath);
            XPathNavigator nav = doc.CreateNavigator();
            string xml = nav.InnerXml.ToString();
        }

        public void function3()
        {
            XmlReader reader = XmlReader.Create("example.xml");
            XPathDocument doc = new XPathDocument(reader);
            XPathNavigator nav = doc.CreateNavigator();
            string xml = nav.InnerXml.ToString();
        }
    }
}
