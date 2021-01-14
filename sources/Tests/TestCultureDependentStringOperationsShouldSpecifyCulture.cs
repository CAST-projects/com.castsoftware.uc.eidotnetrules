using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestCultureDependentStringOperationsShouldSpecifyCulture {
      [Test]
      public void Test() {
         /// [TODO#9] load the source code to test from resources
         // note: a source file should have been added to the resources using the Resource file "SourcesToTest.resx"
         var testSrc = UnitTests.Properties.SourcesToTest.CultureDependentStringOperationsShouldSpecifyCulture_Source;

         /// [TODO#10] create the checker object, parametrized with the type of the QR to test
         var checker = CastDotNetExtensionChecker<CultureDependentStringOperationsShouldSpecifyCulture>.CreateInstance();
         Assert.IsTrue(checker != null);


         /// [TODO#11] setup the expected bookmarks
         /// [TODO#12] launch the processing on the given source code
         checker
            //.AddSource(@"C:\Sources\tools.cs")
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
         .AddExpected(14, 23)
         .AddExpected(10, 23)
         .AddExpected(19, 21)
         .AddExpected(18, 21)
         .AddExpected(23, 25)
         .AddExpected(28, 23)
         .AddExpected(24, 25)
         .AddExpected(29, 23)
         .AddExpected(35, 22)
             .Validate();

         /// [TODO#13] Check the results
         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
