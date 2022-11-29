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
    public class TestEnsureToAbandonSessionPreviousBeforeModifyingCurrentSession
    {
        [Test]
        public void Test()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.EnsureToAbandonSessionPreviousBeforeModifyingCurrentSession_Source;

            var checker = CastDotNetExtensionChecker<EnsureToAbandonSessionPreviousBeforeModifyingCurrentSession>.CreateInstance();
            Assert.IsTrue(checker != null);


            checker
                .AddAssemblyRef(@"TestAssemblies\System.Web.dll")
                .Apply(testSrc);

            checker
                .AddExpected(14, 12)
                .Validate();
            Console.WriteLine(checker.getStatus());
            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
