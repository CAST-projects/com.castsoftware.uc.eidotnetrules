using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestTrackFIXMETags {
      [Test]
      public void Test() {
         var testSrc = UnitTests.Properties.SourcesToTest.TrackFIXMETags_Source;

         var checker = CastDotNetExtensionChecker<TrackFIXMETags>.CreateInstance();
         Assert.IsTrue(checker != null);


         checker
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
             .AddExpected(8, 6)
             .AddExpected(9, 6)
             .AddExpected(11, 6)
             .AddExpected(13, 6)
             .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
