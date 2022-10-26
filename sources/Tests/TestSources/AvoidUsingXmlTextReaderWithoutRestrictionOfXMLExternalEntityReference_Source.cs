using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace UnitTests.TestSources
{
    public class AvoidUsingXmlTextReaderWithoutRestrictionOfXMLExternalEntityReference_Source
    {
        public void func()
        {
            // .NET Framework < 4.5.2
            XmlTextReader reader = new XmlTextReader("xxe.xml"); // VIOLATION: XmlTextReader is not safe by default
            while (reader.Read())
            {
                Console.WriteLine(reader.Value);
            }
        }
        public void func2()
        {
            XmlTextReader reader = new XmlTextReader("xxe.xml");
            reader.ProhibitDtd = true; 
            while (reader.Read())
            {
                Console.WriteLine(reader.Value);
            }
        }

        public void func3()
        {
            XmlTextReader reader = new XmlTextReader("xxe.xml");
            reader.DtdProcessing = DtdProcessing.Prohibit;
            while (reader.Read())
            {
                Console.WriteLine(reader.Value);
            }
        }

        public void func4()
        {
            // .NET Framework < 4.5.2
            XmlTextReader reader = new XmlTextReader("xxe.xml"); // VIOLATION: XmlTextReader is not safe by default
            reader.XmlResolver = new XmlUrlResolver();
            while (reader.Read())
            {
                Console.WriteLine(reader.Value);
            }
        }
    }
}

