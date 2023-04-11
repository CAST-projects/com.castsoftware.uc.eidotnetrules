using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CastDotNetExtension;


namespace UnitTests.UnitTest
{
    [TestFixture]
    public class TestAvoidUnsafeObjectBinding
    {
        [Test]
        public void TestAvoidUnsafeObjectBinding1()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidUnsafeObjectBinding_Source;

            var checker = CastDotNetExtensionChecker<AvoidUnsafeObjectBinding>.CreateInstance();
            Assert.IsTrue(checker != null);

            var pathPackagesUnitTests = Path.Combine(UnitTestHelper.GetExecutingDirectory().FullName, "PackagesUnitTests");

            checker
                .AddAssemblyRef(Path.Combine(pathPackagesUnitTests, "System.Web.Mvc.dll"))
                .AddAssemblyRef(Path.Combine(pathPackagesUnitTests, "Microsoft.AspNetCore.Mvc.ViewFeatures.dll"))
                .AddAssemblyRef(Path.Combine(pathPackagesUnitTests, "Microsoft.AspNetCore.Mvc.Core.dll"))
                .AddAssemblyRef(Path.Combine(pathPackagesUnitTests, "EntityFramework.dll"))
                .AddAssemblyRef(Path.Combine(pathPackagesUnitTests, "Microsoft.EntityFrameworkCore.dll"))
                .Apply(testSrc);

            checker
                .AddExpected(34, 16)
                .AddExpected(59, 26)
                .AddExpected(93, 16)
                .AddExpected(107, 16)
                .Validate();
            Console.WriteLine(checker.GetStatus());
            Assert.IsTrue(checker.IsValid(), checker.GetStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
