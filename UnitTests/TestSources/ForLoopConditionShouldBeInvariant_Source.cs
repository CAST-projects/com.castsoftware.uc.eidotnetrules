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

      public partial class Klass
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
              Console.WriteLine(j);
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

      public partial class Klass
      {
          public List<int> Items { get; set; }
      }

      public void ForLoopConditionAuthorizedPropertyOK1()
      {
          List<int> iter = new List<int>() { 1, 2, 3, 4 };
          for (int i = 0; i < iter.Count; ++i)
          {
              Console.WriteLine(iter[i]);
          }
          int[] array1 = new int[] { 1, 3, 4, 7, 8 };
          for (int i = 0; i < array1.Length; ++i)
          {
              Console.WriteLine(array1[i]);
          }
          var klass = new Klass();
          for (int i = 0; i < klass.Items.Count; ++i)
          {
              Console.WriteLine(klass.Items[i]);
          }

      }

       public void ForLoopConditionAuthorizedPropertyOK1(string[] ressources)
      {
           object[] Temp;
           int i;
           Temp = new object[ressources.Length];
           for(i=0; i < ressources.Length; i++)
           {
               Temp[i] = ressources[i];
           }

      }


   }
}
