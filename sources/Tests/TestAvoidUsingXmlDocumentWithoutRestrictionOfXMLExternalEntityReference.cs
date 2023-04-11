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
    class TestAvoidUsingXmlDocumentWithoutRestrictionOfXMLExternalEntityReference
    {
        [Test]
        public void TestNetFramework472()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidUsingXmlDocumentWithoutRestrictionOfXMLExternalEntityReference_Source;

            var checker = CastDotNetExtensionChecker<AvoidUsingXmlDocumentWithoutRestrictionOfXMLExternalEntityReference>.CreateInstance();
            Assert.IsTrue(checker != null);

            // Set current Net Framework
            FrameworkVersion.currentNetFrameworkKind = NetFrameworkKind.NetFramework;
            FrameworkVersion.currentNetFrameworkVersion = new Version("4.7.2");
            checker
                //.AddAssemblyRef(@"C:\packages\assembly.dll")
                .Apply(testSrc);

            checker
               .AddExpected(28, 33)
                .Validate();
            Console.WriteLine(checker.GetStatus());
            Assert.IsTrue(checker.IsValid(), checker.GetStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }

        [Test]
        public void TestNetFramework451()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidUsingXmlDocumentWithoutRestrictionOfXMLExternalEntityReference_Source;

            var checker = CastDotNetExtensionChecker<AvoidUsingXmlDocumentWithoutRestrictionOfXMLExternalEntityReference>.CreateInstance();
            Assert.IsTrue(checker != null);

            // Set current Net Framework
            FrameworkVersion.currentNetFrameworkKind = NetFrameworkKind.NetFramework;
            FrameworkVersion.currentNetFrameworkVersion = new Version("4.5.1");
            checker
                //.AddAssemblyRef(@"C:\packages\assembly.dll")
                .Apply(testSrc);

            checker
               .AddExpected(14, 33)
               .AddExpected(28, 33)
                .Validate();
            Console.WriteLine(checker.GetStatus());
            Assert.IsTrue(checker.IsValid(), checker.GetStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
