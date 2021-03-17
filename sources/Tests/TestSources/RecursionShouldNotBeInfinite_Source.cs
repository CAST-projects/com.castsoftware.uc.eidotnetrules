using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources
{
   public class RecursionShouldNotBeInfinite_Source
   {

      public partial class APartialClass
      {
         partial void PartialMethodDefinitionElsewhere();
         partial void PartialMethodNoDefinition();
         void NormalMethod()
         {
            PartialMethodNoDefinition();
            NormalMethod();
         }
      }

      public partial class APartialClass
      {
         partial void PartialMethodDefinitionElsewhere()
         {
            PartialMethodDefinitionElsewhere();
         }
      }


      public event EventHandler foo;

      public void Callfoo()
      {
         foo.Invoke(null, null);
      }

      public override string ToString()
      {
         return base.ToString();
      }

      void OverloadedMethod(int i)
      {
         OverloadedMethod(i, 1);
      }

      void OverloadedMethod(int i, int k)
      {
         OverloadedMethod(i);
      }

      void RecursiveMethod(int i)
      {
         RecursiveMethod(i);
         NonRecursiveMethod(i);
      }

      void NonRecursiveMethod(int i)
      {
         RecursiveMethod(i);
         //NonRecursiveMethod(i);
      }

      private abstract class Base
      {
         public abstract void AnAbstractMethod();
      }

      private class Derived : Base
      {
         public override void AnAbstractMethod()
         {
            Console.WriteLine("AnAbstractMethod");
         }
      }

      private void CallAnAbstractMethod(Base baseObj)
      {
         baseObj.AnAbstractMethod();
      }

      private void CallCallAnAbstractMethod()
      {
         CallAnAbstractMethod(new Derived());
      }
   }
}
