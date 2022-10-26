using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CastDotNetExtension;
using CastDotNetExtension.Utils;


namespace UnitTests.UnitTest
{
    [TestFixture]
    class TestAvoidUsingXmlTextReaderWithoutRestrictionOfXMLExternalEntityReference
    {
        [Test]
        public void TestNetFramework472()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidUsingXmlTextReaderWithoutRestrictionOfXMLExternalEntityReference_Source;

            var checker = CastDotNetExtensionChecker<AvoidUsingXmlTextReaderWithoutRestrictionOfXMLExternalEntityReference>.CreateInstance();
            Assert.IsTrue(checker != null);

            // Set current Net Framework
            FrameworkVersion.currentNetFrameworkKind = NetFrameworkKind.NetFramework;
            FrameworkVersion.currentNetFrameworkVersion = new Version("4.7.2");
            checker
                //.AddAssemblyRef(@"C:\packages\assembly.dll")
                .Apply(testSrc);

            checker
                .AddExpected(43, 35)
                .Validate();
            Console.WriteLine(checker.getStatus());
            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }

        [Test]
        public void TestNetFramework451()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidUsingXmlTextReaderWithoutRestrictionOfXMLExternalEntityReference_Source;

            var checker = CastDotNetExtensionChecker<AvoidUsingXmlTextReaderWithoutRestrictionOfXMLExternalEntityReference>.CreateInstance();
            Assert.IsTrue(checker != null);

            // Set current Net Framework
            FrameworkVersion.currentNetFrameworkKind = NetFrameworkKind.NetFramework;
            FrameworkVersion.currentNetFrameworkVersion = new Version("4.5.1");
            checker
                //.AddAssemblyRef(@"C:\packages\assembly.dll")
                .Apply(testSrc);

            checker
                .AddExpected(14, 35)
                .AddExpected(43, 35)
                .Validate();
            Console.WriteLine(checker.getStatus());
            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }

        [Test]
        public void TestNetFramework35()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidUsingXmlTextReaderWithoutRestrictionOfXMLExternalEntityReference_Source;

            var checker = CastDotNetExtensionChecker<AvoidUsingXmlTextReaderWithoutRestrictionOfXMLExternalEntityReference>.CreateInstance();
            Assert.IsTrue(checker != null);

            // Set current Net Framework
            FrameworkVersion.currentNetFrameworkKind = NetFrameworkKind.NetFramework;
            FrameworkVersion.currentNetFrameworkVersion = new Version("3.5");
            checker
                //.AddAssemblyRef(@"C:\packages\assembly.dll")
                .Apply(testSrc);

            checker
                .AddExpected(14, 35)
                .AddExpected(43, 35)
                .Validate();
            Console.WriteLine(checker.getStatus());
            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
