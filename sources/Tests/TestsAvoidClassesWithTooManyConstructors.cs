using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;

namespace CastDotNetExtension.UnitTest
{
    [TestFixture]
    class TemplateQualityRuleTests
    {
        /// Implementation of the test using the following template
        [Test]
        public void TestMethod1()
        {

            /// [TODO#9] load the source code to test from resources
            // note: a source file should have been added to the resources using the Resource file "SourcesToTest.resx"
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidClassesWithTooManyConstructors_QualUatExample;

            /// [TODO#10] create the checker object, parametrized with the type of the QR to test
            var checker = CastDotNetExtensionChecker<AvoidClassesWithTooManyConstructorsAnalyzer>.CreateInstance();
            Assert.IsTrue(checker != null);

            /// [TODO#11] setup the expected bookmarks
            /// [TODO#12] launch the processing on the given source code
            checker
                //.AddSource(@"C:\Sources\tools.cs")
                //.AddAssemblyRef(@"C:\packages\assembly.dll")
                .Apply(testSrc);

            //checker
            //    .AddExpected(10, 8)
            //    .AddExpected(15, 8)
            //    .AddExpected(20, 8)
            //    .AddExpected(25, 8)
            //    .AddExpected(30, 8)
            //    .Validate();

            /// [TODO#13] Check the results
            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());
        }
    }
}

