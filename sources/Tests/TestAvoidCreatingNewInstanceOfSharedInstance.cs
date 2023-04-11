using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest {
   [TestFixture]
   class TestAvoidCreatingNewInstanceOfSharedInstance {

      [Test]
      public void Test_operations() {
         var testSrc = UnitTests.Properties.SourcesToTest.AvoidCreatingNewInstanceOfSharedInstance_Source;
         
         var checker = CastDotNetExtensionChecker<AvoidCreatingNewInstanceOfSharedInstance>.CreateInstance()
            ;

         checker = checker
            .AddAssemblyRef(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.dll")
            .AddAssemblyRef(@"c:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades\System.ComponentModel.TypeConverter.dll")
            .AddAssemblyRef(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.ComponentModel.Composition.dll")
         ;

         Assert.IsTrue(checker != null);

         checker
             .Apply(testSrc);

         checker
            .AddExpected(112,20)
            .AddExpected(90,25)
            .AddExpected(83,31)
            .AddExpected(77,22)            
            .Validate();

         Assert.IsTrue(checker.IsValid(), checker.GetStatus());

         //Assert.IsFalse(checker.ResultsMissing.Any());
         //Assert.IsFalse(checker.ResultsUnexpected.Any());

      }

   }
}
