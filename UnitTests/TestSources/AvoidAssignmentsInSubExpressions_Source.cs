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

      public static void Get(Func<int, string, bool> typeHandler = null)
      {
         typeHandler(0, "astring");
      }

      public static void UseGet()
      {
         string strFromGet;

         Get(delegate(int i, string str) {
            strFromGet = str;
            return true;
         });

      }


      public class Cat
      {
          // Auto-implemented properties.
          public int Age { get; set; }
          public string Name { get; set; }

          public Cat()
          {
          }

          public Cat(string name)
          {
              this.Name = name;
          }

          public static List<Cat> CreateCatList(Cat cat)
          {
              return new List<Cat>() { cat };
          }
      }

    public void TestCat(Cat cat) {}

    public void AssignmentInInitializerOK1()
      {
          Cat cat = new Cat { Age = 10, Name = "Fluffy" };
          Cat sameCat = new Cat("Fluffy") { Age = 10 };
          this.TestCat(new Cat() 
            { 
                Age = 10, 
                Name = "Fluffy" 
            });
          List<Cat> lCat = new List<Cat>();
          lCat.Add(new Cat("Fluffy") 
          { 
              Age = 10 
          });
      }

    public static IEnumerable<Cat> AssignmentInInitializerOK2(int id)
    {
        return Cat.CreateCatList(new Cat()
            {
                Age = 10,
                Name = "Fluffy"
            });
    }

   }
}
