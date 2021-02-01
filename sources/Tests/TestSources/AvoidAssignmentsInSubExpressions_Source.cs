using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources
{
   public class AvoidAssignmentsInSubExpressions_Source
   {
      class Test
      {
         public Test(String str) {

         }
         public void AMethod(String str) {

         }

         public void BMethod(String str1, String str2) {

         }
      }

      public void AssignmentsInSubExpressionsKO() {
         String str = "abc";
         String result;
         if (string.IsNullOrEmpty(result = str.Substring(0, 1)))  {// Noncompliant

         }

         int i;
         switch (i = 0) {
            case 1:
               Console.WriteLine(i);
               break;
            default:
               Console.WriteLine("default");
               break;
         }

         Test test = new Test(str = "def");
         test.AMethod(str = "xyz");
         test.BMethod(str = "abc", result = "def");

      }

      public void AssignmentsInSubExpressionsOK() {
         String str = "abc";
         String result = str.Substring(0, 1);
         bool b = string.IsNullOrEmpty(result);
         if (b) {// Noncompliant

         }

         int i = 0;
         switch (i) {
            case 1:
               Console.WriteLine(i);
               break;
            default:
               Console.WriteLine("default");
               break;
         }

         Test test = new Test(str);
         test.AMethod("xyz");
         test.BMethod(str , "def");

      }
   }
}
