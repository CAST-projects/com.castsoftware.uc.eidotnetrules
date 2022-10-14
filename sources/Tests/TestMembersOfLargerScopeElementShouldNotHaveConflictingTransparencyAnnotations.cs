using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest
{
   [TestFixture]
   class TestMembersOfLargerScopeElementShouldNotHaveConflictingTransparencyAnnotations
   {
      [Test]
      public void Test()
      {
         var testSrc = UnitTests.Properties.SourcesToTest.MembersOfLargerScopeElementShouldNotHaveConflictingTransparencyAnnotations_Source;

         var checker = CastDotNetExtensionChecker<MembersOfLargerScopeElementShouldNotHaveConflictingTransparencyAnnotations>.CreateInstance();
         Assert.IsTrue(checker != null);

         checker
             .Apply(testSrc);

         //checker
         //    .AddExpected(13, 7)
         //    .AddExpected(19, 10)
         //    .AddExpected(25, 7)
         //    .AddExpected(31, 10)
         //    .AddExpected(36, 10)
         //    .AddExpected(39, 13)
         //    .AddExpected(45, 13)
         //    .Validate();

         //Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
