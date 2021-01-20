using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources {
   public class ConditionalStructuresShouldNotHaveIdenticalBranches_Source {
      private bool ReturnTrue() {
         return true;
      }

      private bool ReturnFalse(int i = 0) {
         return false;
      }

      private void TestSwitchBranchesKO(int i = 0) {

         switch (i) {
            case 1:
               i++;
               break;
            case 2:
            default:
               i++;
               break;
         }

         switch (i) {
            case 1:
            case 4: {
                  i++;
                  break;
               }
            case 2:
               i++; ; ;
               break;

            case 3: {
                  i++;
                  break;
               }
            default:
               i++;
               break;
         }

         switch (i) {
            case 1: {
                  i++;
                  break;
               }
            case 2: {
                  i++; ; ;
                  break;
               }
            case 3: {
                  i++;
                  break;
               }
            default:
               i++;
               break;
         }


         switch (i) {
            case 1:
               i++;
               break;
            case 2:
               i++; ; ;
               break;
            case 3:
               i++;
               break;
            default:
               i++;
               break;
         }

         switch (i) {
            case 1:
               i++;
               break;
            default:
               i++;
               break;
         }


         switch (i) {
            case 1:
               i++;
               break;
            case 2:
               i++;
               break;
            default:
               i++;
               break;
         }

         switch (i) {
            case 2:
            default:
               i++;
               break;
            case 1:
               i++;
               break;
         }


         switch (i) {
            case 1:
               i++;
               break;
            case 2:
               //differs only by comment
               i++;
               break;
            default:
               /*default*/
               i++;
               break;
         }

      }


      private void TestSwitchBranchesOK(int i = 0) {

         switch (i) {
            case 1:
               i++;
               break;
            case 2:
               i++; ; ;
               break;
         }

         switch (i) {
            case 1:
               i++;
               break;
            case 2:
               i++; ; ;
               break;
            case 3:
               i++;
               break;
         }

         switch (i) {
            case 1:
            case 4: {
                  i++;
                  break;
               }
            case 2:
               i++; ; ;
               break;

            case 3: {
                  i++;
                  i++;
                  break;
               }
         }

         switch (i) {
            case 1: {
                  i++;
                  break;
               }
            case 2:
               i++; ; ;
               break;

            case 3: {
                  i++;
                  i++;
                  break;
               }
         }


         switch (i) {
            case 1: {
                  i++;
                  break;
               }
            case 2: {
                  i++; ; ;
                  break;
               }
            case 3: {
                  ++i;
                  break;
               }
         }


         switch (i) {
            case 1:
               i++;
               break;
            case 2:
               i++; ; ;
               break;
            case 3:
               i--;
               break;
         }

         switch (i) {
            default:
               i++;
               break;
         }

         switch (i) {
            case 1:
               i++;
               break;
         }

         switch (i) {
            case 1:
               i++;
               break;
            case 2:
               i--;
               break;
         }

         switch (i) {
            case 1:
               i++;
               break;
            case 2:
               ++i;
               break;
         }

      }


      private void TestIfBranchesKO() {
         int i1 = 1;
         var x = false;

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

         if (true == x)
            ReturnTrue();
         else {
            // differs only by comment
            ReturnTrue();
         }

      }

      private void TestIfBranchesOK() {
         int i1 = 1;
         var x = false;

         if (true == x) {
            ReturnTrue();
         }

         if (true == x)
            ReturnFalse(1);
         else {
            ReturnFalse();
         }

         if (true == x) {
            ReturnTrue();
         }
         else if (1 == i1) {
            ReturnFalse();
         }
         else {
            ReturnTrue();
         }


         if (true == x) {
            ReturnTrue();
         }
         else
            ReturnFalse();


         if (true == x) {
            ReturnTrue();
         }

         if (true == x) {
            ReturnFalse();
         }
         else {
            ReturnTrue();
         }

         if (true == x)
            ReturnFalse();
         else
            ReturnTrue();

         if (true == x)
            ReturnFalse();
         else {
            ReturnTrue();
         }

         if (true == x)
            ReturnFalse();
         else {
            ReturnTrue(); ; ; ;
         }

         if (true == x)
            ReturnFalse(1);
         else {
            ReturnTrue(); ; ; ;
         }

         if (true == x)
            ReturnFalse(1);
         else {
            //comment
            ReturnTrue(); ; ; ;
         }

      }

      void TestTernaryOperator(int i = 0) {
         i = 0 == i ? ++i : --i;
         i = 0 == i ? ++i : ++i;
      }

   }
}
