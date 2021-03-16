using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources
{
   public class RecursionShouldNotBeInfinite_Source
   {

      public override string ToString() {
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

      void RecursiveMethod(int i) {
         RecursiveMethod(i);
         NonRecursiveMethod(i);
      }

      void NonRecursiveMethod(int i)
      {
         RecursiveMethod(i);
         //NonRecursiveMethod(i);
      }
   }
}
