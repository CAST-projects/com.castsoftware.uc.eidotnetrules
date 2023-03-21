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
    public class TestAvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage
    {
        [Test]
        public void TestAvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage1()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage_Source;

            var checker = CastDotNetExtensionChecker<AvoidStaticVariableModificationInMethodsForClassInheritingFromSystemWebUIPage>.CreateInstance();
            Assert.IsTrue(checker != null);

            checker
                .AddAssemblyRef(@"TestAssemblies\System.Web.dll")
                .Apply(testSrc);

            checker
                .AddExpected(19, 16)
                .Validate();
            Console.WriteLine(checker.getStatus());
            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
