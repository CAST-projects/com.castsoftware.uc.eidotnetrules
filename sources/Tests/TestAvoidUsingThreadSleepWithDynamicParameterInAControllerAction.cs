﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest
{
    [TestFixture]
    public class TestAvoidUsingThreadSleepWithDynamicParameterInAControllerAction
    {
        [Test]
        public void TestAvoidUsingThreadSleepWithDynamicParameterInAControllerAction1()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidUsingThreadSleepWithDynamicParameterInAControllerAction_Source;

            var checker = CastDotNetExtensionChecker<AvoidUsingThreadSleepWithDynamicParameterInAControllerAction>.CreateInstance();
            Assert.IsTrue(checker != null);

            checker
                .AddAssemblyRef(@"TestAssemblies\System.Web.Mvc.dll")
                .AddAssemblyRef(@"TestAssemblies\Microsoft.AspNetCore.Mvc.ViewFeatures.dll")
                .AddAssemblyRef(@"TestAssemblies\Microsoft.AspNetCore.Mvc.Core.dll")
                .Apply(testSrc);

            checker
                .AddExpected(20, 12)
                .AddExpected(26, 12)
                .AddExpected(38, 16)
                .Validate();
            Console.WriteLine(checker.getStatus());
            Assert.IsTrue(checker.IsValid(), checker.getStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}