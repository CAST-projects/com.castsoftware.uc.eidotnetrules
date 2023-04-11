using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestAvoidRecursiveTypeInheritance {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.AvoidRecursiveTypeInheritance_Source;

         var checker = CastDotNetExtensionChecker<AvoidRecursiveTypeInheritance>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
               .AddExpected(12, 12)
               .AddExpected(17, 12)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
