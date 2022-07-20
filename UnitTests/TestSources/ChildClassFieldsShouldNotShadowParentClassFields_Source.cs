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

   public class TestBase
   {
       private int _m1;
       protected int _m2;
       protected int _m3;
       public int _m4;
       public int _m5;
       public int _m6;

       private int Prop1 { get; set; }
       protected int Prop2 { get; set; }
       protected int Prop3 { get; set; }
       public int Prop4 { get; set; }
       public int Prop5 { get; set; }
       public int Prop6 { get; set; }

       //private virtual int VProp1 { get; set; }
       protected virtual int VProp2 { get; set; }
       public virtual int VProp3 { get; set; }

       private void Method1() { }
       protected void Method2() { }
       public void Method3() { }

       //private virtual void VMethod1() { }
       protected virtual void VMethod2() { }
       public virtual void VMethod3() { }
   }

   public class A : TestBase
   {
       private int _m1;
       protected int /*@*/_m2;
       protected new int _m3;
       public int /*@*/_m4;
       public new int _m5;
       private int /*@*/_m6;

       private int Prop1 { get; set; }
       protected int /*@*/Prop2 { get; set; }
       protected new int Prop3 { get; set; }
       public int /*@*/Prop4 { get; set; }
       public new int Prop5 { get; set; }
       private int /*@*/Prop6 { get; set; }

       //private virtual int VProp1 { get; set; }
       protected override int VProp2 { get; set; }
       public override int VProp3 { get; set; }

       private void Method1() { }
       protected void Method2() { }
       public void Method3() { }

       //private override void VMethod1() { }
       protected override void VMethod2() { }
       public override void VMethod3() { }

   }
}
