using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestInterfaceInstancesShouldNotBeCastToConcreteTypes {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.InterfaceInstancesShouldNotBeCastToConcreteTypes_Source;

         var checker = CastDotNetExtensionChecker<InterfaceInstancesShouldNotBeCastToConcreteTypes>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
             .AddExpected(36, 24)
             .AddExpected(37, 24)
             .AddExpected(40, 28)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
