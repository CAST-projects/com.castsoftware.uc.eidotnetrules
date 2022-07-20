using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestUseLogicalORInsteadOfBitwiseORInBooleanContext {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.UseLogicalORInsteadOfBitwiseORInBooleanContext_Source;

         var checker = CastDotNetExtensionChecker<UseLogicalORandANDInsteadOfBitwiseORandANDInBooleanContext>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
            .AddExpected(28, 17)
            .AddExpected(29, 13)
            .AddExpected(39, 13)
            .AddExpected(93, 18)
            .AddExpected(94, 14)
            .AddExpected(95, 14)
            .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
