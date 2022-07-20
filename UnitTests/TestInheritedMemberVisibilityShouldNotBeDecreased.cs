using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestInheritedMemberVisibilityShouldNotBeDecreased {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.InheritedMemberVisibilityShouldNotBeDecreased_Source;

         var checker = CastDotNetExtensionChecker<InheritedMemberVisibilityShouldNotBeDecreased>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
            .AddExpected(29, 22)
            .AddExpected(16, 21)
            .AddExpected(27, 22)
            .AddExpected(17, 21)
            .AddExpected(28, 22)
            .AddExpected(18, 21)
            .AddExpected(31, 24)
            .AddExpected(21, 23)
            .AddExpected(26, 24)
            .AddExpected(11, 21)
            .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
