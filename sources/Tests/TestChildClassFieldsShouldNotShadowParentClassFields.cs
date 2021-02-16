﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestChildClassFieldsShouldNotShadowParentClassFields {
      [Test]
      public void Test() {
         /// [TODO#9] load the source code to test from resources
         // note: a source file should have been added to the resources using the Resource file "SourcesToTest.resx"
         var testSrc = UnitTests.Properties.SourcesToTest.ChildClassFieldsShouldNotShadowParentClassFields_Source;

         /// [TODO#10] create the checker object, parametrized with the type of the QR to test
         var checker = CastDotNetExtensionChecker<ChildClassFieldsShouldNotShadowParentClassFields>.CreateInstance();
         Assert.IsTrue(checker != null);


         /// [TODO#11] setup the expected bookmarks
         /// [TODO#12] launch the processing on the given source code
         checker
            //.AddSource(@"C:\Sources\tools.cs")
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
             .AddExpected(15, 22)
             .AddExpected(10, 23)
             .AddExpected(16, 28)
             .AddExpected(11, 23)
             .AddExpected(29,19)
             .AddExpected(40,19)
             .AddExpected(31, 19)
             .AddExpected(42, 19)
             .AddExpected(30, 19)
             .AddExpected(41, 19)
             .AddExpected(43, 22)
             .AddExpected(49, 21)
             .Validate();

         /// [TODO#13] Check the results
         Assert.IsTrue(checker.IsValid(), checker.getStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
