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
    public class TestEnsureToRequireAntiforgeryTokenForPostPutPatchAndDeleteMethods
    {
        [Test]
        public void TestEnsureToRequireAntiforgeryTokenForPostPutPatchAndDeleteMethods1()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.EnsureToRequireAntiforgeryTokenForPostPutPatchAndDeleteMethods_Source;

            var checker = CastDotNetExtensionChecker<EnsureToRequireAntiforgeryTokenForPostPutPatchAndDeleteMethods>.CreateInstance();
            Assert.IsTrue(checker != null);

            checker
                .AddAssemblyRef(@"TestAssemblies\System.Web.dll")
                .AddAssemblyRef(@"TestAssemblies\System.Web.Mvc.dll")
                .AddAssemblyRef(@"TestAssemblies\Microsoft.AspNetCore.Mvc.ViewFeatures.dll")
                .AddAssemblyRef(@"TestAssemblies\Microsoft.AspNetCore.Mvc.Core.dll")
                .Apply(testSrc);

            checker
                .AddExpected(20, 20)
                .AddExpected(31, 20)
                .Validate();
            Console.WriteLine(checker.GetStatus());
            Assert.IsTrue(checker.IsValid(), checker.GetStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
