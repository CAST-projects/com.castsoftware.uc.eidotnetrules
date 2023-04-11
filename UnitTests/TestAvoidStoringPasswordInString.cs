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
    public class TestAvoidStoringPasswordInString
    {
        [Test]
        public void TestAvoidStoringPasswordInString1()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidStoringPasswordInString_Source;

            var checker = CastDotNetExtensionChecker<AvoidStoringPasswordInString>.CreateInstance();
            Assert.IsTrue(checker != null);


            checker
                //.AddAssemblyRef(@"C:\packages\assembly.dll")
                .Apply(testSrc);

            checker
               .AddExpected(10, 8)
               .AddExpected(12, 23)
               .AddExpected(14, 32)
               .AddExpected(16, 16)
               .AddExpected(23, 16)
                .Validate();
            Console.WriteLine(checker.getStatus());
            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
