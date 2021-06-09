using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests
{
    [TestFixture]
    class TestAvoidHavingSameImplementationInAllBranchesOfConditionalStructure
    {

        [Test]
        public void Test()
        {

            var testSrc = UnitTests.Properties.SourcesToTest.AvoidHavingSameImplementationInAllBranchesOfConditionalStructure_Source;

            var checker = CastDotNetExtensionChecker<AvoidHavingSameImplementationInAllBranchesOfConditionalStructure>.CreateInstance();
            Assert.IsTrue(checker != null);


            checker
                .Apply(testSrc);

            checker
               .AddExpected(16, 12)
               .AddExpected(41, 12)
               .AddExpected(87, 12)
               .AddExpected(145, 12)
               .Validate();

            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
