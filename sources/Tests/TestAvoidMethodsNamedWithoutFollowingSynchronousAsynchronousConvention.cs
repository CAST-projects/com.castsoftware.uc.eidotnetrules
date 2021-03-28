using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestAvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention_Source;

         var checker = CastDotNetExtensionChecker<AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
             .AddExpected(36, 15)
             .AddExpected(12, 16)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
