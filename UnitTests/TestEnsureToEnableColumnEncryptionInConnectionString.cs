﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CastDotNetExtension;
using System.IO;

namespace UnitTests.UnitTest
{
    [TestFixture]
    public class TestEnsureToEnableColumnEncryptionInConnectionString
    {
        [Test]
        public void TestEnsureToEnableColumnEncryptionInConnectionString1()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.EnsureToEnableColumnEncryptionInConnectionString_Source;

            var checker = CastDotNetExtensionChecker<EnsureToEnableColumnEncryptionInConnectionString>.CreateInstance();
            Assert.IsTrue(checker != null);

            var pathPackagesUnitTests = Path.Combine(UnitTestHelper.GetExecutingDirectory().FullName, "PackagesUnitTests");

            checker
                .AddAssemblyRef(Path.Combine(pathPackagesUnitTests, "System.Data.dll"))
                .Apply(testSrc);

            checker
                .AddExpected(17, 39)
                .AddExpected(23, 39)
                .Validate();
            Console.WriteLine(checker.GetStatus());
            Assert.IsTrue(checker.IsValid(), checker.GetStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
