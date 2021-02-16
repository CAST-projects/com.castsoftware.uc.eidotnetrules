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
       public abstract bool AbstractBoolOK { get; set; }
       public virtual bool VirtualBoolOK { get; set; }
       public virtual bool VirtualBoolNewedInDerivedOK { get; set; }
       public bool SimplePropertyNewedInDerivedOK { get; set; }
       public bool SimplePropertyHiddenInDerivedKO { get; set; }
       public bool PropertyHiddenByPlainFieldKO { get; set; }
       public bool fieldHiddenByPropertyKO = false;
    }

    public class PropertyCasesDerived : PropertyCases
    {
       public override bool AbstractBoolOK { get; set; }
       public override bool VirtualBoolOK { get; set; }
       public new bool VirtualBoolNewedInDerivedOK { get; set; }
       public new bool SimplePropertyNewedInDerivedOK { get; set; }
       public bool SimplePropertyHiddenInDerivedKO { get; set; }
       public bool PropertyHiddenByPlainFieldKO = false;
       public bool FieldHiddenByPropertyKO { get; set; }
       protected bool _propertyValue = false;
       public bool AProp { get { return _propertyValue; } }
    }

    public class PropertyCasesDerived2 : PropertyCasesDerived
    {
       protected int _propertyValue = 0;
    }

   }
}
