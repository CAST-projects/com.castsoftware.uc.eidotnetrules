using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest
{
   [TestFixture]
   class TestEnsureConstructorsOfSerializableClassesAreSecure
   {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.EnsureConstructorsOfSerializableClassesAreSecure_Source;

         var checker = CastDotNetExtensionChecker<EnsureConstructorsOfSerializableClassesAreSecure>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
             .AddExpected(19, 16)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
