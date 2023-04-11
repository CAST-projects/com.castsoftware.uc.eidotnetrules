using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestAvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.AvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName_Source;

         var checker = CastDotNetExtensionChecker<AvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
            .AddExpected(14, 29)
            .AddExpected(58, 29)
            .AddExpected(36, 29)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
