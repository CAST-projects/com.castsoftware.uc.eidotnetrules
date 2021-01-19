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
         /// [TODO#9] load the source code to test from resources
         // note: a source file should have been added to the resources using the Resource file "SourcesToTest.resx"
         var testSrc = UnitTests.Properties.SourcesToTest.UseLogicalORInsteadOfBitwiseORInBooleanContext_Source;

         /// [TODO#10] create the checker object, parametrized with the type of the QR to test
         var checker = CastDotNetExtensionChecker<UseLogicalORInsteadOfBitwiseORInBooleanContext>.CreateInstance();
         Assert.IsTrue(checker != null);


         /// [TODO#11] setup the expected bookmarks
         /// [TODO#12] launch the processing on the given source code
         checker
            //.AddSource(@"C:\Sources\tools.cs")
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
            .AddExpected(28, 17)
            .AddExpected(29, 13)
            .AddExpected(39, 13)
            .Validate();

         /// [TODO#13] Check the results
         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
