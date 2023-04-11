using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CastDotNetExtension;
using System.IO;
using System.Reflection;

namespace UnitTests.UnitTest
{
    [TestFixture]
    public class TestAvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage
    {
        [Test]
        public void TestAvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage1()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage_Source;

            var checker = CastDotNetExtensionChecker<AvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage>.CreateInstance();
            Assert.IsTrue(checker != null);

            var pathPackagesUnitTests = Path.Combine(UnitTestHelper.GetExecutingDirectory().FullName, "PackagesUnitTests");

            checker
                .AddAssemblyRef(Path.Combine(pathPackagesUnitTests, "System.Web.dll"))
                .Apply(testSrc);

            checker
                .AddExpected(19, 16)
                .Validate();
            Console.WriteLine(checker.GetStatus());
            Assert.IsTrue(checker.IsValid(), checker.GetStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
