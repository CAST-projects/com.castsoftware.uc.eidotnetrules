using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestEnsureProperArgumentsToEvents {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.EnsureProperArgumentsToEvents_Source;

         var checker = CastDotNetExtensionChecker<EnsureProperArgumentsToEvents>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
            .AddExpected(62, 16)
            .AddExpected(13, 16)
            .AddExpected(29, 12)
            .AddExpected(37, 16)
            .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
