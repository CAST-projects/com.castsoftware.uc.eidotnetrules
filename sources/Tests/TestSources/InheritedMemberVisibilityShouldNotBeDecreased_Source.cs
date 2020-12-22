using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources {
   public class InheritedMemberVisibilityShouldNotBeDecreased_Source {

      public class Base {
         public virtual void VirtualMethod() { }
         public void BaseMethod(int x) { }
      }

      public class Foo  : Base {
         public override void VirtualMethod() { }
         public void SomeMethod(int count) { }
         public void SomeMethodOut(int count, out int x) { x = 0; }
         public void SomeMethodRef(int count, ref int x) { x = 0; }
         protected void ProtectedMethod() { }
         private void PrivateMethod() { }
         internal void InternalMethod() { }
         internal void InternalMethod2() { }
         public void NewMethod() { }
      }
      public class Bar : Foo {
         protected void BaseMethod(int x) { } // Noncompliant
         private void SomeMethodOut(int count, out int y) { y = 0; } // Noncompliant
         private void SomeMethodRef(int count, ref int y) { y = 0; } // Noncompliant
         private void SomeMethod(int count) { } // Noncompliant
         public void ProtectedMethod() { } // OK
         protected void InternalMethod() { } // Noncompliant
         protected void PrivateMethod() { } // OK
         internal void InternalMethod2() { } // OK
         public new void NewMethod() { } // OK
      }


   }
}
