using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestEmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull_Source;

         var checker = CastDotNetExtensionChecker<EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
            .AddExpected(18, 12)
            .AddExpected(14, 12)
            .AddExpected(25, 15)
            .AddExpected(31, 15)
            .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
