using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest
{
   [TestFixture]
   class TestForLoopConditionShouldBeInvariant
   {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.ForLoopConditionShouldBeInvariant_Source;

         var checker = CastDotNetExtensionChecker<ForLoopConditionShouldBeInvariant>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
            .AddExpected(51, 23)
            .AddExpected(58, 23)
            .AddExpected(66, 15)
            .AddExpected(73, 15)
            .AddExpected(82, 15)
            .AddExpected(90, 15)
            .AddExpected(97, 15)
            .AddExpected(104, 15)
            .AddExpected(112, 15)
            .AddExpected(131, 26)
            .AddExpected(137, 17)
            .AddExpected(143, 26)
            .AddExpected(150, 26)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
