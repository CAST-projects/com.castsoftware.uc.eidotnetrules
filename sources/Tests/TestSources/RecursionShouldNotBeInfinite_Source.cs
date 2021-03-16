using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources
{
   public class RecursionShouldNotBeInfinite_Source
   {

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
