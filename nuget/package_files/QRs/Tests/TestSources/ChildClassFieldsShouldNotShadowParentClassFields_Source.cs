using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources {
   public class ChildClassFieldsShouldNotShadowParentClassFields_Source {

      class Base {
         protected int ripe;
         protected int flesh;
      }

      class Derived : Base {
         private bool ripe; // Noncompliant
         private static int FLESH; // Noncompliant

         private bool ripened;
         private static char FLESH_COLOR;

      }

   }
}
