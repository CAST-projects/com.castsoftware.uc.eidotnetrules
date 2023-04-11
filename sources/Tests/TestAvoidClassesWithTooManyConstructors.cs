using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using CastDotNetExtension;

namespace UnitTests.UnitTest
{
    [TestFixture]
    class TestAvoidClassesWithTooManyConstructors
    {
        [Test]
        public void TestMethod1()
        {

            var testSrc = UnitTests.Properties.SourcesToTest.AvoidClassesWithTooManyConstructors_QualUatExample;

            var checker = CastDotNetExtensionChecker<AvoidClassesWithTooManyConstructorsAnalyzer>.CreateInstance();
            Assert.IsTrue(checker != null);

            checker
                .Apply(testSrc);

            checker
                .AddExpected(10, 8)
                .AddExpected(15, 8)
                .AddExpected(20, 8)
                .AddExpected(25, 8)
                .AddExpected(30, 8)
                .Validate();

            Assert.IsTrue(checker.IsValid(), checker.GetStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());
        }
    }
}

