using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources {
   public class UseLogicalORInsteadOfBitwiseORInBooleanContext_Source {

      private bool ReturnTrue() {
         return true;
      }

      private bool ReturnFalse() {
         return false;
      }

      private int Return1() {
         return 1;
      }

      private int Return0() {
         return 0;
      }

      public void Test() {
         bool b1 = false;
         bool b2 = true;
         var x = b1 | b2;
         x = ReturnFalse() | ReturnTrue();
         b2 |= b1;

         int i1 = 0;
         int i2 = 1;
         var y = i1 | i2;
         y = Return0() | Return1();

         y = b1 ? 1 : 0 | Return1();

         x = (1 == i1) | b2;


         if (true == x) {
            ReturnTrue();
         }
         else if (1 == i1) {
            ReturnTrue();
         }
         else {
            ReturnTrue();
         }


         if (true == x) {
            ReturnTrue();
         }
         else
            ReturnTrue();


         if (true == x) {
            ReturnTrue();
         }

         if (true == x) {
            ReturnTrue();
         }
         else {
            ReturnTrue();
         }

         if (true == x) 
            ReturnTrue();
         else
            ReturnTrue();

         if (true == x)
            ReturnTrue();
         else {
            ReturnTrue();
         }

         if (true == x)
            ReturnTrue();
         else {
            ReturnTrue(); ; ; ;
         }
      }

      public void Test2()
      {
          bool b1 = false;
          bool b2 = true;
          var x = b1 & b2;
          x = ReturnFalse() & ReturnTrue();
          if (b1 & b2)
              ReturnTrue();
      }
   }
}
