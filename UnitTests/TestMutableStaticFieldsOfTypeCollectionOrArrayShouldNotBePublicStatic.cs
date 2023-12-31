﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestMutableStaticFieldsOfTypeCollectionOrArrayShouldNotBePublicStatic {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.MutableStaticFieldsOfTypeCollectionOrArrayShouldNotBePublicStatic_Source;

         var checker = CastDotNetExtensionChecker<MutableStaticFieldsOfTypeCollectionOrArrayShouldNotBePublicStatic>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
               .AddExpected(12, 32)
               .AddExpected(13, 36)
               .AddExpected(32, 56)
               .AddExpected(35, 42)
               .AddExpected(40, 32)
               .AddExpected(41, 36)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
