using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestConditionalStructuresShouldNotHaveIdenticalBranches {
      [Test]
      public void Test() {
         /// [TODO#9] load the source code to test from resources
         // note: a source file should have been added to the resources using the Resource file "SourcesToTest.resx"
         var testSrc = UnitTests.Properties.SourcesToTest.ConditionalStructuresShouldNotHaveIdenticalBranches_Source;

         /// [TODO#10] create the checker object, parametrized with the type of the QR to test
         var checker = CastDotNetExtensionChecker<ConditionalStructuresShouldNotHaveIdenticalBranches>.CreateInstance();
         Assert.IsTrue(checker != null);


         /// [TODO#11] setup the expected bookmarks
         /// [TODO#12] launch the processing on the given source code
         checker
            //.AddSource(@"C:\Sources\tools.cs")
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
            .AddExpected(18, 9)
            .AddExpected(28, 9)
            .AddExpected(47, 9)
            .AddExpected(66, 9)
            .AddExpected(81, 9)
            .AddExpected(91, 9)
            .AddExpected(103, 9)
            .AddExpected(114, 9)
            .AddExpected(253, 9)
            .AddExpected(264, 9)
            .AddExpected(271, 9)
            .AddExpected(278, 9)
            .AddExpected(283, 9)
            .AddExpected(289, 9)
            .AddExpected(295, 9)
            .AddExpected(381, 13)
            .AddExpected(390, 24)
            .AddExpected(409, 12)
            .AddExpected(450,9)
            .AddExpected(456,9)
            .AddExpected(461,9)
            .AddExpected(468,9)
            .Validate();

         /// [TODO#13] Check the results
         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
