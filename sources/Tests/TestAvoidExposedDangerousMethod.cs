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
    public class TestAvoidExposedDangerousMethod
    {
        [Test]
        public void TestAvoidExposedDangerousMethod1()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidExposedDangerousMethod_Source;

            var checker = CastDotNetExtensionChecker<AvoidExposedDangerousMethod>.CreateInstance();
            Assert.IsTrue(checker != null);

            checker
                .AddAssemblyRef(@"TestAssemblies\System.Data.dll")
                .Apply(testSrc);

            checker
                .AddExpected(23, 50)
                .AddExpected(31, 39)
                .AddExpected(37, 50)
                .AddExpected(43, 50)
                .AddExpected(52, 50)
                .AddExpected(62, 50)
                .AddExpected(68, 46)
                .Validate();
            Console.WriteLine(checker.GetStatus());
            Assert.IsTrue(checker.IsValid(), checker.GetStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
