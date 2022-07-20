using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace UnitTests.UnitTest.Sources {
   public class CultureDependentStringOperationsShouldSpecifyCulture_Source {
      private static readonly String str = "abc";
      String lowerKO = str.ToLower();
      String lowerOK = str.ToLower(CultureInfo.InvariantCulture);
      String lowerOKInvariant = str.ToLowerInvariant();

      String upperKO = str.ToUpper();
      String upperOK = str.ToUpper(CultureInfo.InvariantCulture);
      String upperOKInvariant = str.ToUpperInvariant();

      int indexKO1 = str.IndexOf("c");
      int indexKO2 = str.IndexOf("c", 0);
      int indexOK1 = str.IndexOf("c", StringComparison.CurrentCulture);
      int indexOK2 = str.IndexOf("c", 1, StringComparison.CurrentCulture);

      int lastIndexKO1 = str.LastIndexOf("c");
      int lastIndexKO2 = str.LastIndexOf("c", 0);
      int lastIndexOK1 = str.LastIndexOf("c", StringComparison.CurrentCulture);
      int lastIndexOK2 = str.LastIndexOf("c", 1, StringComparison.CurrentCulture);

      int compareKO1 = string.Compare("A", "B");
      int compareKO2 = string.Compare("A", "B", false);
      int compareOK1 = string.Compare("A", "B", true, CultureInfo.CurrentCulture);
      int compareOK2 = string.Compare("A", "B", CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);
      int compareOK3 = string.Compare("A", 0, "B", 0, 1, StringComparison.CurrentCulture);
      int compareOK4 = string.Compare("A", 0, "B", 0, 1, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);

      int compared1 = str.CompareTo("def");

   }
}
