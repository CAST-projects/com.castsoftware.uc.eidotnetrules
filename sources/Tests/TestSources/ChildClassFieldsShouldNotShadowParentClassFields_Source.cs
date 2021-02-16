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

    public abstract class PropertyCases
    {
       public abstract bool AbstractBool { get; set; }
       public virtual bool VirtualBool { get; set; }
       public virtual bool VirtualBoolNewedInDerived { get; set; }
       public bool SimplePropertyNewedInDerived { get; set; }
       public bool SimplePropertyHiddenInDerived { get; set; }
    }

    public class PropertyCasesDerived : PropertyCases
    {
       public override bool AbstractBool { get; set; }
       public override bool VirtualBool { get; set; }
       public new bool VirtualBoolNewedInDerived { get; set; }
       public new bool SimplePropertyNewedInDerived { get; set; }
       public bool SimplePropertyHiddenInDerived { get; set; }
    }

   }
}
