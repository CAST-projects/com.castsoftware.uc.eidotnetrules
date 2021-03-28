using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest
{
   [TestFixture]
   class TestMergeAdjacentTryBlocksWithIdenticalCatchFinallyStatements
   {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.MergeAdjacentTryBlocksWithIdenticalCatchFinallyStatements_Source;

         var checker = CastDotNetExtensionChecker<MergeAdjacentTryBlocksWithIdenticalCatchFinallyStatements>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
         .AddExpected(24, 12)
         .AddExpected(21, 9)
         .AddExpected(55, 9)
         .AddExpected(110, 9)
         .AddExpected(143, 9)
         .AddExpected(69, 9)
         .AddExpected(96, 9)
         .Validate();

         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
