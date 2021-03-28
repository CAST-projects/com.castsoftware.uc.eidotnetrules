using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestAvoidCreatingExceptionWithoutThrowingThem {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.AvoidCreatingExceptionWithoutThrowingThem_Source;

         var checker = CastDotNetExtensionChecker<AvoidCreatingExceptionWithoutThrowingThem>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
            .AddExpected(11,37)
            .AddExpected(110,24)
            .AddExpected(48,24)
            .AddExpected(31,13)
            .AddExpected(37,19)
            .AddExpected(29,9)
            .AddExpected(115, 22)
            .AddExpected(141,21)
            .AddExpected(140,26)
            .Validate();

         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
