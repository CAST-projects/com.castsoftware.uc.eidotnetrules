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
    class TestAvoidPersistSecurityInfoInConnectionString
    {
        [Test]
        public void Test()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidPersistSecurityInfoInConnectionString_Source;

            var checker = CastDotNetExtensionChecker<AvoidPersistSecurityInfoInConnectionString>.CreateInstance();
            Assert.IsTrue(checker != null);


            checker
                //.AddAssemblyRef(@"C:\packages\assembly.dll")
                .Apply(testSrc);

            checker
               .AddExpected(18, 12)
               .AddExpected(20, 12)
               .AddExpected(24, 12)
               .AddExpected(25, 12)
                .Validate();
            Console.WriteLine(checker.getStatus());
            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
