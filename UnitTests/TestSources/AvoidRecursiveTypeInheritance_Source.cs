using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources {
   public class AvoidRecursiveTypeInheritance_Source {

      class C1<T> {
      }

      class C2KO<S> : C1<C2KO<C1<S>>> // Noncompliant
      {
         public int x = 101;
      }

      class C3KO<S> : C1<C3KO<C3KO<S>>> // Noncompliant
      {
         public int x = 101;
      }

      class C4OK<S> : C1<C1<C1<S>>> 
      {
         public int x = 101;
      }

      class C5OK<T> : C1<C5OK<T>> {

      }

   }
}
