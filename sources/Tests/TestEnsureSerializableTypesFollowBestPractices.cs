using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest
{
   [TestFixture]
   class TestEnsureSerializableTypesFollowBestPractices
   {
      [Test]
      public void Test()
      {
         var testSrc = UnitTests.Properties.SourcesToTest.EnsureSerializableTypesFollowBestPractices_Source;

         var checker = CastDotNetExtensionChecker<EnsureSerializableTypesFollowBestPractices>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
             .Apply(testSrc);

         checker
             .AddExpected(11, 6)
             .AddExpected(27, 9)
             .AddExpected(34, 9)
             .AddExpected(52, 6)
             .AddExpected(58, 6)
             .AddExpected(68, 9)
             .AddExpected(83, 9)
             .AddExpected(91, 13)
             .AddExpected(118, 6)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
