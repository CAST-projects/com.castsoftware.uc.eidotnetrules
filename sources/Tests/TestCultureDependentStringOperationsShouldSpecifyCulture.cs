using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestCultureDependentStringOperationsShouldSpecifyCulture {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.CultureDependentStringOperationsShouldSpecifyCulture_Source;

         var checker = CastDotNetExtensionChecker<CultureDependentStringOperationsShouldSpecifyCulture>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
         .AddExpected(14, 23)
         .AddExpected(10, 23)
         .AddExpected(19, 21)
         .AddExpected(18, 21)
         .AddExpected(23, 25)
         .AddExpected(28, 23)
         .AddExpected(24, 25)
         .AddExpected(29, 23)
         .AddExpected(35, 22)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
