using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CastDotNetExtension;


namespace UnitTests.UnitTest
{
    [TestFixture]
    public class TestAvoidUnsafeObjectBinding
    {
        [Test]
        public void Test()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidUnsafeObjectBinding_Source;

            var checker = CastDotNetExtensionChecker<AvoidUnsafeObjectBinding>.CreateInstance();
            Assert.IsTrue(checker != null);

            checker
                .AddAssemblyRef(@"..\..\..\Tests\TestAssemblies\System.Web.Mvc.dll")
                .AddAssemblyRef(@"..\..\..\Tests\TestAssemblies\Microsoft.AspNetCore.Mvc.ViewFeatures.dll")
                .AddAssemblyRef(@"..\..\..\Tests\TestAssemblies\Microsoft.AspNetCore.Mvc.Core.dll")
                .AddAssemblyRef(@"..\..\..\Tests\TestAssemblies\EntityFramework.dll")
                .AddAssemblyRef(@"..\..\..\Tests\TestAssemblies\Microsoft.EntityFrameworkCore.dll")
                .Apply(testSrc);

            checker
                //.AddExpected(28, 8)
                //.AddExpected(45, 8)
                //.AddExpected(85, 8)
                //.AddExpected(99, 8)
                .Validate();
            Console.WriteLine(checker.getStatus());
            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
