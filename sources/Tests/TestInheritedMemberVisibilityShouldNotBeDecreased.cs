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
         /// [TODO#9] load the source code to test from resources
         // note: a source file should have been added to the resources using the Resource file "SourcesToTest.resx"
         var testSrc = UnitTests.Properties.SourcesToTest.InheritedMemberVisibilityShouldNotBeDecreased_Source;

         /// [TODO#10] create the checker object, parametrized with the type of the QR to test
         var checker = CastDotNetExtensionChecker<InheritedMemberVisibilityShouldNotBeDecreased>.CreateInstance();
         Assert.IsTrue(checker != null);


         /// [TODO#11] setup the expected bookmarks
         /// [TODO#12] launch the processing on the given source code
         checker
            //.AddSource(@"C:\Sources\tools.cs")
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

         /// [TODO#13] Check the results
         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
