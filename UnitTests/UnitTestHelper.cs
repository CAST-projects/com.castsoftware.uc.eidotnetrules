using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    internal static class UnitTestHelper
    {
        public static DirectoryInfo GetExecutingDirectory()
        {
            var location = new Uri(Assembly.GetCallingAssembly().GetName().CodeBase);
            var path = Uri.UnescapeDataString(location.AbsolutePath);
            return new FileInfo(path).Directory;
        }

    }
}
