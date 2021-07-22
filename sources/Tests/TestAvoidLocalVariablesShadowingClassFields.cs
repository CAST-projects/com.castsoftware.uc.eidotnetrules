using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestAvoidLocalVariablesShadowingClassFields {
      [Test]
      public void Test() {

         var testSrc = UnitTests.Properties.SourcesToTest.AvoidLocalVariablesShadowingClassFields_Source;

         var checker = CastDotNetExtensionChecker<AvoidLocalVariablesShadowingClassFields>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
             .Apply(testSrc);

         checker
            .AddExpected(12, 16)
            .AddExpected(17, 19)
            .AddExpected(36, 16)
            .AddExpected(40, 16)
            .AddExpected(54, 19)
            .AddExpected(62, 19)
            .AddExpected(69, 19)
            .AddExpected(9, 21)
            .AddExpected(28, 23)
            .AddExpected(48, 16)
            .AddExpected(57, 23)
            .AddExpected(67, 24)
            .AddExpected(80, 16)
            .AddExpected(81, 19)
            .AddExpected(82, 25)
            .AddExpected(89, 19)
            .AddExpected(90, 22)
            .AddExpected(92, 19)
            .Validate();

         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
