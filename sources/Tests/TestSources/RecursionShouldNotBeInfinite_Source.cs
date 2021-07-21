using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources
{
   public class RecursionShouldNotBeInfinite_Source
   {
      #region RecursiveIfElse
      void RecursiveIfElseKO1(int i)
      {
         if (i >= 0) {
            RecursiveIfElseKO1(i--);
         } else if (10 == i) {
            RecursiveIfElseKO1(i += 2);
         } else {
            RecursiveIfElseKO1(--i);
         }
      }

      void RecursiveIfElseKO2(int i)
      {
         if (i >= 0) {
            RecursiveIfElseKO2(i--);
         } else {
            RecursiveIfElseKO2(i += 2);
         }
      }

      void RecursiveIfElseKO3(int i)
      {
         if (true) {
            RecursiveIfElseKO3(i--);
         }
      }

      int RecursiveIfElseKO4(int i)
      {
         if (0 != RecursiveIfElseKO4(i--)) {
            --i;
         }
         return i;
      }

      void RecursiveIfElseKO5(int i)
      {
         if (i >= 0) {
            RecursiveIfElseKO5(i--);
         } else 
            RecursiveIfElseKO5(i += 2);

      }

      void RecursiveIfElseOK1(int i)
      {
         if (i >= 0) {
            RecursiveIfElseOK1(i--);
         } else if (10 == i) {
            RecursiveIfElseOK1(i += 2);
         }
      }

      void RecursiveIfElseOK2(int i)
      {
         if (i >= 0) {
            RecursiveIfElseOK2(i--);
         }
      }

      void RecursiveIfElseOK3(int i)
      {
         if (true) {
            
         } else {
            RecursiveIfElseOK3(i--);
         }
      }

      int RecursiveIfElseOK4(int i)
      {
         if (0 != i--) {
            --i;
         }
         return i;
      }

      void RecursiveIfElseOK5(int i)
      {
         if (i >= 0) {
            
         } else
            RecursiveIfElseOK5(i += 2);

      }

      #endregion

      #region RecursiveTernary
      int RecursiveTernaryKO1(int i)
      {
         i = (i > 0) ? RecursiveTernaryKO1(++i) : RecursiveTernaryKO1(--i);
         return i;
      }

      int RecursiveTernaryOK1(int i)
      {
         i = (i > 0) ? RecursiveTernaryOK1(++i) : --i;
         return i;
      }
      #endregion

      #region RecursiveSwitch
      void RecursiveSwitchKO1(int i)
      {
         switch (i) {
            case 1:
               RecursiveSwitchKO1(10);
               break;
            case 2:
               RecursiveSwitchKO1(20);
               break;
            case 3:
               RecursiveSwitchKO1(30);
               break;
            default:
               RecursiveSwitchKO1(100);
               break;
         }
      }

      void RecursiveSwitchKO2(int i)
      {
         switch (i) {
            case 1: {
                  RecursiveSwitchKO2(10);
                  break;
               }
            case 2: {
                  RecursiveSwitchKO2(20);
                  break;
               }
            case 3: {
                  RecursiveSwitchKO2(30);
                  break;
               }
            default: {
                  RecursiveSwitchKO2(100);
                  break;
               }
         }
      }

      int RecursiveSwitchKO3(int i)
      {
         switch (i) {
            case 1:
               i = RecursiveSwitchKO3(10);
               break;
            case 2:
               i = RecursiveSwitchKO3(20);
               break;
            case 3:
               i = RecursiveSwitchKO3(30);
               break;
            default:
               i = RecursiveSwitchKO3(100);
               break;
         }
         return i;
      }

      void RecursiveSwitchKO4(int i)
      {
         switch (i) {
            case 1: {
                  RecursiveSwitchKO4(10);
                  break;
               }
            case 2: {
                  RecursiveSwitchKO4(20);
                  break;
               }
            case 3:
            default: {
                  RecursiveSwitchKO4(30);
                  break;
               }
         }
      }
      
      int RecursiveSwitchKO5(int i)
      {
         switch (RecursiveSwitchKO5(i)) {
            case 1:
               i = 10;
               break;
            case 2:
               i = 20;
               break;
            case 3:
               i = 30;
               break;
            default:
               i = 100;
               break;
         }
         return i;
      }

      void RecursiveSwitchOK1(int i)
      {
         switch (i) {
            case 1:
               RecursiveSwitchOK1(10);
               break;
            case 2:
               RecursiveSwitchOK1(20);
               break;
            case 3:
               RecursiveSwitchOK1(30);
               break;
         }
      }

      void RecursiveSwitchOK2(int i)
      {
         switch (i) {
            case 1: {
                  RecursiveSwitchOK2(10);
                  break;
               }
            case 2: {
                  RecursiveSwitchOK2(20);
                  break;
               }
            case 3: {
                  RecursiveSwitchOK2(30);
                  break;
               }
         }
      }

      int RecursiveSwitchOK3(int i)
      {
         switch (i) {
            case 1:
               i = RecursiveSwitchOK3(10);
               break;
            case 2:
               i = RecursiveSwitchOK3(20);
               break;
            case 3:
               i = RecursiveSwitchOK3(30);
               break;
         }
         return i;
      }

      void RecursiveSwitchOK4(int i)
      {
         switch (i) {
            case 1: {
                  RecursiveSwitchOK4(10);
                  break;
               }
            case 2:
            case 3: {
                  RecursiveSwitchOK4(30);
                  break;
               }
         }
      }

      int RecursiveSwitchOK5(int i)
      {
         switch (i) {
            case 1:
               i = RecursiveSwitchOK5(i);
               break;
            case 2:
               i = 20;
               break;
            case 3:
               i = 30;
               break;
            default:
               i = 100;
               break;
         }
         return i;
      }
      #endregion

      #region RecursiveLoop
      void RecursiveLoopKO1(int i)
      {
         while (true) {
            RecursiveLoopKO1(i += 10);
         }
      }

      void RecursiveLoopKO2(int i)
      {
         do {
            RecursiveLoopKO2(i += 10);
         } while (100 != i);
      }

      void RecursiveLoopKO3(int i)
      {
         while (true) 
            RecursiveLoopKO3(i += 10);
      }

      void RecursiveLoopKO4(int i)
      {
         do
            RecursiveLoopKO4(i += 10);
         while (100 != i);
      }

      int RecursiveLoopKO5(int i)
      {
         while (1000 < RecursiveLoopKO5(i)) {
            i++;
         }
         return i;
      }

      void RecursiveLoopOK1(int i)
      {
         while (true) {
            if (i < 10) {
               break;
            }
            RecursiveLoopOK1(i += 10);
         }
      }

      void RecursiveLoopOK2(int i)
      {
         do {
            if (i < 100) {
               continue;
            }
            RecursiveLoopOK2(i += 10);
         } while (100 != i);
      }

      void RecursiveLoopOK3(int i)
      {
         while (false)
            RecursiveLoopOK3(i += 10);
      }

      int RecursiveLoopOK4(int i)
      {
         while (1000 < i) {
            i++;
            if (i < 10000) {
               i = RecursiveLoopOK4(i);
            }
         }
         return i;
      }

      void RecursiveLoopOK5(int i)
      {
         do {
            if (i < 1000) {
               continue;
            }
            RecursiveLoopOK5(i += 10);
         }  while (100 != i);
      }

      
      #endregion

      #region RecursiveTryCatchFinallyThrow
      void RecursiveTryCatchFinallyThrowKO1(int i = 0)
      {
         try {
            if (0 > i) {
               throw new InvalidCastException();
            }
            RecursiveTryCatchFinallyThrowKO1(i -= 2);

         } catch (InvalidCastException) {
            RecursiveTryCatchFinallyThrowKO1(0);
         } catch (Exception) {
            //BlockTry(i += 3);
         } finally {
            //BlockTry(i += 2);
         }
      }

      void RecursiveTryCatchFinallyThrowKO2(int i = 0)
      {
         try {
            if (0 > i) {
               throw new InvalidCastException();
            }
            RecursiveTryCatchFinallyThrowKO2(i -= 2);

         } catch (Exception) {
            RecursiveTryCatchFinallyThrowKO2(i -= 32);
         } finally {
            //BlockTry(i += 2);
         }
      }

      void RecursiveTryCatchFinallyThrowKO3(int i = 0)
      {
         try {
            if (0 > i) {
               throw new InvalidCastException();
            }
            RecursiveTryCatchFinallyThrowKO3(i -= 2);

         } catch (Exception) {
            
         } finally {
            RecursiveTryCatchFinallyThrowKO3(i -= 32);
         }
      }

      void RecursiveTryCatchFinallyThrowOK1(int i = 0)
      {
         try {
            if (0 > i) {
               throw new InvalidOperationException();
            }
            RecursiveTryCatchFinallyThrowOK1(i -= 2);

         } catch (InvalidCastException) {
            RecursiveTryCatchFinallyThrowOK1(0);
         } catch (Exception) {
            //BlockTry(i += 3);
         } finally {
            //BlockTry(i += 2);
         }
      }

      
      #endregion

      #region RecurseiveReturn
      int RecurseiveReturnKO1(int i)
      {
         return RecurseiveReturnKO1(i++);
      }

      int RecurseiveReturnKO2(int i)
      {
         return (i > 0) ? RecurseiveReturnKO2(i++) : RecurseiveReturnKO2(i--);
      }

      int RecurseiveReturnOK1(int i)
      {
         if (i > 1000) {
            return ++i;
         }
         return RecurseiveReturnOK1(i++);
      }


      #endregion

      #region RecursiveGoTo
      void RecursiveGoToKO1(int i)
      {
         if (i > 100) {
            if (i > 1000) {
               goto end;
            }
            i++;
         }
      end:
         RecursiveGoToKO1(i);
      }
      #endregion

      public partial class APartialClass
      {
         partial void PartialMethodDefinitionElsewhere();
         partial void PartialMethodNoDefinition();
         void NormalMethod(int i = 0)
         {
            if (i < 0) {
               PartialMethodNoDefinition();
               NormalMethod();
            } else {
      
            }
         }
      }

      public partial class APartialClass
      {
         partial void PartialMethodDefinitionElsewhere()
         {
            try {
               NormalMethod();
               PartialMethodDefinitionElsewhere();
            } catch {
               PartialMethodDefinitionElsewhere();
            }
         }
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

      #region RecurseiveReturn2

      public class Klass {}

      public class Category
      {
          public Klass ParentId;
      }

      internal List<Category> RecurseiveReturnKO3(IEnumerable<string> keywords)
      {
          return RecurseiveReturnKO3(keywords).Where(c => c.ParentId != null).ToList();
      }

      #endregion


      #region RecursiveProperty
      private bool _prop;
      public bool recProp
      {
          get
          {
              return _prop;
          }
          set
          {
              this.recProp = value;
          }
      }

      public bool recProp2
      {
          get
          {
              return this.recProp2;
          }
          set
          {
              _prop = value;
          }
      }
     

      #endregion

     

   }
}
