using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CastDotNetExtension;
using System.IO;

namespace UnitTests.UnitTest
{
    [TestFixture]
    public class TestEnsureToAbandonSessionPreviousBeforeModifyingCurrentSession
    {
        [Test]
        public void Test()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.EnsureToAbandonSessionPreviousBeforeModifyingCurrentSession_Source;

            var checker = CastDotNetExtensionChecker<EnsureToAbandonSessionPreviousBeforeModifyingCurrentSession>.CreateInstance();
            Assert.IsTrue(checker != null);

            var pathPackagesUnitTests = Path.Combine(UnitTestHelper.GetExecutingDirectory().FullName, "PackagesUnitTests");

            checker
                .AddAssemblyRef(Path.Combine(pathPackagesUnitTests, "System.Web.dll"))
                .Apply(testSrc);

            checker
                .AddExpected(14, 12)
                .Validate();
            Console.WriteLine(checker.GetStatus());
            Assert.IsTrue(checker.IsValid(), checker.GetStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
