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
             .AddExpected(14, 6)
             .AddExpected(30, 9)
             .AddExpected(37, 9)
             .AddExpected(55, 6)
             .AddExpected(61, 6)
             .AddExpected(71, 9)
             .AddExpected(86, 9)
             .AddExpected(94, 13)
             .AddExpected(121, 6)
             .AddExpected(160, 7)
             .AddExpected(173, 7)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
