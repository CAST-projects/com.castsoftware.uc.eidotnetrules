using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace UnitTests.TestSources
{
    public class AvoidUsingXmlDocumentWithoutRestrictionOfXMLExternalEntityReference_Source
    {
        public void func()
        {
            // .NET Framework < 4.5.2
            XmlDocument parser = new XmlDocument(); // VIOLATION: XmlDocument is not safe by default
            parser.LoadXml("xxe.xml");
        }

        public void func2()
        {
            // .Net Framework 4.5.1
            XmlDocument parser = new XmlDocument();
            parser.XmlResolver = null; // FIXED: XmlResolver has been set to null
            parser.LoadXml("xxe.xml");
        }

        public void func3()
        {
            XmlDocument parser = new XmlDocument(); // .Net Framework 4.7.2
            parser.XmlResolver = new XmlUrlResolver(); // VIOLATION: XmlDocument.XmlResolver configured with XmlUrlResolver 
            parser.LoadXml("xxe.xml");
        }
    }
}
