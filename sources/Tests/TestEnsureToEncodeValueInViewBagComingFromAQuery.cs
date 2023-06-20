using System;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest
{
    [TestFixture]
    public class TestEnsureToEncodeValueInViewBagComingFromAQuery
    {
        [Test]
    public void TestEnsureToEncodeValueInViewBagComingFromAQuery1()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.EnsureToEncodeValueInViewBagComingFromAQuery_Source;

            var checker = CastDotNetExtensionChecker<EnsureToEncodeValueInViewBagComingFromAQuery>.CreateInstance();
    Assert.IsTrue(checker != null);

            checker
                .AddAssemblyRef(@"TestAssemblies\System.Web.dll")
                .AddAssemblyRef(@"TestAssemblies\System.Web.Mvc.dll")
                .AddAssemblyRef(@"TestAssemblies\System.Data.dll")
                //.AddAssemblyRef(@"TestAssemblies\Microsoft.AspNetCore.Mvc.ViewFeatures.dll")
                //.AddAssemblyRef(@"TestAssemblies\Microsoft.AspNetCore.Mvc.Core.dll")
                .Apply(testSrc);

            checker
                .AddExpected(25, 20)
                .AddExpected(42, 20)
                .AddExpected(58, 20)
                .Validate();
            Console.WriteLine(checker.GetStatus());
            Assert.IsTrue(checker.IsValid(), checker.GetStatus());
        }
    }
}
