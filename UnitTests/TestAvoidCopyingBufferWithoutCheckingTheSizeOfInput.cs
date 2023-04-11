using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using CastDotNetExtension;

namespace UnitTests.UnitTest
{
    [TestFixture]
    public class TestAvoidCopyingBufferWithoutCheckingTheSizeOfInput
    {
        [Test]
        public void TestAvoidCopyingBufferWithoutCheckingTheSizeOfInput1()
        {
            var testSrc = UnitTests.Properties.SourcesToTest.AvoidCopyingBufferWithoutCheckingTheSizeOfInput_Source;

            var checker = CastDotNetExtensionChecker<AvoidCopyingBufferWithoutCheckingTheSizeOfInput>.CreateInstance();
            Assert.IsTrue(checker != null);


            checker
                //.AddAssemblyRef(@"C:\packages\assembly.dll")
                .Apply(testSrc);

            checker
                .AddExpected(22, 12)
                .Validate();
            Console.WriteLine(checker.GetStatus());
            Assert.IsTrue(checker.IsValid(), checker.GetStatus());

            //Assert.IsFalse(checker.ResultsMissing.Any());
            //Assert.IsFalse(checker.ResultsUnexpected.Any());

        }
    }
}
