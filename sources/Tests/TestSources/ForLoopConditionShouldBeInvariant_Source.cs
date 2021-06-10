using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources
{
   public class ForLoopConditionShouldBeInvariant_Source
   {

      void FuncI(int i) {
         ++i;
      }

      void ForLoopConditionInvariantOK() {
         for (int i = 0; i < 10; ++i) {
            FuncI(i);
         }

         for (int i = 0; i < 10; ++i) {
            Console.WriteLine(i);
         }

         int j = 0;
         for (; j < 10; ++j) {
            Console.WriteLine(j);
         }

         int limit = 10;
         for (; j < limit; ++j) {
            Console.WriteLine(limit);
         }
      }

      int getI() {
         return 10;
      }

      void setIref(ref int i) {
         i = 10;
      }

      void setIout(out int i) {
         i = 11;
      }

      void ForLoopConditionInvariantKO() {

         for (int i = 0; i < 10; ++i) {
            if (i == 1) {
               setIref(ref i);
            }
            Console.WriteLine(i);
         }

         for (int i = 0; i < 10; ++i) {
            if (i == 1) {
               setIout(out i);
            }
            Console.WriteLine(i);
         }


         for (int i = 0; i < 10; ++i) {
            if (i == 1) {
               i++;
            }
            Console.WriteLine(i);
         }

         for (int i = 0; i < 10; ++i) {
            if (i == 1) {
               --i;
            }
            Console.WriteLine(i);
         }



         for (int i = 0; i < 10; ++i) {
            if (i == 1) {
               i = getI();
            }
            Console.WriteLine(i);
         }

         int j = 0;
         for (; j < 10; ++j) {
            if (j == 1) {
               j += 2;
            }
            Console.WriteLine(j);
         }

         for (int i = 0; i < 10; ++i) {
            if (i == 1) {
               i = i + 1;
            }
            Console.WriteLine(i);
         }

         for (int i = 0; i < 10; ++i) {
            if (i == 1) {
               i = 3;
            }
            Console.WriteLine(i);
         }

         int limit = 10;
         for (; j < limit; ++j) {
            if (0 == limit) {
               limit++;
            }
            Console.WriteLine(limit);
         }
      }

      int getVal()
      {
          return 10;
      }

      int stopValue { get; set; }

      public class Klass
      {
          public int stopValue { get; set; }
      }
      void ForLoopConditionVariantKO()
      {
          for (int i = 0; i < getVal(); ++i) 
          {
            Console.WriteLine(i);
          }

          int j = 0;
          for (; j < getVal(); ++j)
          {
              Console.WriteLine(i);
          }

          stopValue = 10;
          for (int i = 0; i < stopValue; ++i)
          {
              Console.WriteLine(i);
          }

          var klass = new Klass();
          klass.stopValue = 10;
          for (int i = 0; i < klass.stopValue; ++i)
          {
              Console.WriteLine(i);
          }

      }
   }
}
