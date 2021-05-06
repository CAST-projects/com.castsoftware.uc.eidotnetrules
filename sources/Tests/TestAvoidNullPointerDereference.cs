using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using CastDotNetExtension;

namespace UnitTests.UnitTest
{
    [TestFixture]
    class TestAvoidNullPointerDereference
    {
        [Test]
        public void TestMethod1()
        {

            var testSrc = UnitTests.Properties.SourcesToTest.AvoidNullPointerDereference_Source;

            var checker = CastDotNetExtensionChecker<AvoidNullPointerDereference>.CreateInstance();
            Assert.IsTrue(checker != null);

            checker
                .Apply(testSrc);

            checker
                .AddExpected(19, 16)
                .AddExpected(47, 16)
                .AddExpected(67, 16)
                .AddExpected(102, 16)
                .AddExpected(122, 16)
                .AddExpected(155, 12)
                .AddExpected(176, 20)
                .AddExpected(187, 12)
                .AddExpected(198, 12)
                .AddExpected(209, 12)
                .AddExpected(221, 15)
                .AddExpected(260, 16)
                .AddExpected(274, 16)
                .AddExpected(295, 12)
                .AddExpected(307, 15)
                .AddExpected(309, 16)
                .AddExpected(310, 16)
                .AddExpected(314, 16)
                .Validate();

            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());
        }
    }
}
