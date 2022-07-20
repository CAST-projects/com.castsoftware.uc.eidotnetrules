using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest
{
   [TestFixture]
   class TestAvoidAssignmentsInSubExpressions
   {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.AvoidAssignmentsInSubExpressions_Source;

         var checker = CastDotNetExtensionChecker<AvoidAssignmentsInSubExpressions>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
            .AddExpected (27,34)
            .AddExpected (27,34)
            .AddExpected (32,17)
            .AddExpected (41,30)
            .AddExpected (42,22)
            .AddExpected (43,22)
            .AddExpected (43,35)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
