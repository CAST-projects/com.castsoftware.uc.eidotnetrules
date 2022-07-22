using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CastDotNetExtension;
using System.IO;
using System.Reflection;

namespace UnitTests.UnitTest
{
    [TestFixture]
    class TestAvoidCreatingNewInstanceOfSharedInstance
    {

        public static DirectoryInfo GetExecutingDirectory()
        {
            var location = new Uri(Assembly.GetCallingAssembly().GetName().CodeBase);
            var path = Uri.UnescapeDataString(location.AbsolutePath);
            return new FileInfo(path).Directory;
        }

        [Test]
        public void Test_operations()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidCreatingNewInstanceOfSharedInstance_Source;

            var checker = CastDotNetExtensionChecker<AvoidCreatingNewInstanceOfSharedInstance>.CreateInstance()
               ;

            var pathPackagesUnitTests = Path.Combine(GetExecutingDirectory().FullName, "PackagesUnitTests");

            // 3 assemblies copied from @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\" folder
            checker = checker
                .AddAssemblyRef(Path.Combine(pathPackagesUnitTests, "System.dll"))
                .AddAssemblyRef(Path.Combine(pathPackagesUnitTests, "Facades", "System.ComponentModel.TypeConverter.dll"))
                .AddAssemblyRef(Path.Combine(pathPackagesUnitTests, "System.ComponentModel.Composition.dll"))
             ;

            Assert.IsTrue(checker != null);

            checker
                .Apply(testSrc);

            checker
               .AddExpected(112, 20)
               .AddExpected(90, 25)
               .AddExpected(83, 31)
               .AddExpected(77, 22)
               .Validate();

            Assert.IsTrue(checker.IsValid(), checker.GetStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }

    }
}
