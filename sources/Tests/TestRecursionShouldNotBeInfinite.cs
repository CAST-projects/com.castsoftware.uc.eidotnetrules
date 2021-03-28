using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;
using System.Threading.Tasks;

namespace UnitTests.UnitTest
{
   [TestFixture]
   class TestRecursionShouldNotBeInfinite
   {

      [Test]
      public void Test() {

         /// [TODO#9] load the source code to test from resources
         // note: a source file should have been added to the resources using the Resource file "SourcesToTest.resx"
         var testSrc = UnitTests.Properties.SourcesToTest.RecursionShouldNotBeInfinite_Source;

         /// [TODO#10] create the checker object, parametrized with the type of the QR to test
         var checker = CastDotNetExtensionChecker<RecursionShouldNotBeInfinite>.CreateInstance();
         Assert.IsTrue(checker != null);


         /// [TODO#11] setup the expected bookmarks
         /// [TODO#12] launch the processing on the given source code
         checker
            //.AddSource(@"C:\Sources\tools.cs")
            //.AddAssemblyRef(@"C:\packages\assembly.dll")
             .Apply(testSrc);

         checker
             .AddExpected(11, 6)
             .AddExpected(22, 6)
             .AddExpected(31, 6)
             .AddExpected(38, 6)
             .AddExpected(46, 6)
             .AddExpected(100, 6)
            .AddExpected(114, 6)
            .AddExpected(132, 6)
            .AddExpected(154, 6)
            .AddExpected(173, 6)
            .AddExpected(192, 6)
             .AddExpected(310, 6)
             .AddExpected(303, 6)
             .AddExpected(296, 6)
             .AddExpected(316, 6)
             .AddExpected(323, 6)
             .AddExpected(382, 6)
             .AddExpected(399, 6)
             .AddExpected(414, 6)
             .AddExpected(450, 6)
             .AddExpected(455, 6)
             .AddExpected(472, 6)
             .AddExpected(529, 6)
             .AddExpected(502, 9)
             .Validate();

         try {
            Assert.IsTrue(checker.IsValid(), checker.getStatus());
         } catch (Exception e) {
#if DEBUG
            Console.Write(e.StackTrace);
            Console.ReadLine();
#endif 
            throw;
         }
         

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
