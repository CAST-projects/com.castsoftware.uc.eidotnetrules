using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestClassesImplementingIEquatableTShouldBeSealed {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.ClassesImplementingIEquatableTShouldBeSealed_Source;

         var checker = CastDotNetExtensionChecker<ClassesImplementingIEquatableTShouldBeSealed>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
             .AddExpected(15, 6)
             .AddExpected(63, 6)
             .AddExpected(79, 6)
             .AddExpected(91, 6)
             .AddExpected(91, 6)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
