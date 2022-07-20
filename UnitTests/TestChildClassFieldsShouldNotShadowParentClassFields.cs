using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestChildClassFieldsShouldNotShadowParentClassFields {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.ChildClassFieldsShouldNotShadowParentClassFields_Source;

         var checker = CastDotNetExtensionChecker<ChildClassFieldsShouldNotShadowParentClassFields>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
             .Apply(testSrc);

         checker
             .AddExpected(15, 22)
             .AddExpected(10, 23)
             .AddExpected(16, 28)
             .AddExpected(11, 23)
             .AddExpected(29,19)
             .AddExpected(40,19)
             .AddExpected(31, 19)
             .AddExpected(42, 19)
             .AddExpected(30, 19)
             .AddExpected(41, 19)
             .AddExpected(43, 22)
             .AddExpected(49, 21)

             .AddExpected(57, 21)
             .AddExpected(86, 26)
             .AddExpected(59, 18)
             .AddExpected(88, 23)
             .AddExpected(61, 18)
             .AddExpected(90, 24)
             .AddExpected(64, 21)
             .AddExpected(93, 26)
             .AddExpected(66, 18)
             .AddExpected(95, 23)
             .AddExpected(68, 18)
             .AddExpected(97, 24)

             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
