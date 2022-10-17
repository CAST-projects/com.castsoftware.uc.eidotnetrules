using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest
{
    [TestFixture]
    class TestAvoidSecurityCriticalInformationExposure
    {
        [Test]
        public void Test()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidSecurityCriticalInformationExposure_Source;

            var checker = CastDotNetExtensionChecker<AvoidSecurityCriticalInformationExposure>.CreateInstance();
            Assert.IsTrue(checker != null);

            checker
                //.AddAssemblyRef(@"C:\packages\assembly.dll")
                .Apply(testSrc);

            checker
               .AddExpected(29, 12)
               .AddExpected(30, 12)
               .AddExpected(31, 12)
               .AddExpected(32, 12)
                .Validate();
            Console.WriteLine(checker.getStatus());
            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
