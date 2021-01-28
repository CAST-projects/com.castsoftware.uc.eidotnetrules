using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources
{
   public class MergeAdjacentTryBlocksWithIdenticalCatchFinallyStatements_Source
   {
      void DoFirstThing() { }

      void DoSecondThing() { }

      void DoThirdThing() { }

      void DoFourthThing() { }

      void DoThingInCatch() { }

      void TryIdenticalCatchKO() {
         try {
            DoFirstThing();

            try {
               DoSecondThing();
            } catch (Exception x) {
               DoThingInCatch();
            }

            try {
               DoThirdThing();
            } catch (Exception x) {
               DoThingInCatch();
            }

         } catch (Exception e) {
            DoThingInCatch();
         }

         DoSecondThing();

         try {
            DoThirdThing();
         } catch (Exception e) {
            DoThingInCatch();
         }

         try {
            DoSecondThing();
         } catch (Exception e) {
            DoThingInCatch();
         }


         try {
            DoFourthThing();
         } catch (ArgumentException e) {
            Console.WriteLine("in catch");
         }

         try {
            DoFirstThing();
         } catch (ArgumentException e) {
            Console.WriteLine("in catch");
         }
      }

      void TryIdenticalFinallyKO() {
         try {
            DoFirstThing();

            try {
               DoSecondThing();
            } finally {

            }
         } finally {
            DoThingInCatch();
         }

         DoSecondThing();

         try {
            DoThirdThing();
         } finally {
            DoThingInCatch();
         }

         try {
            DoSecondThing();
         } finally {
            DoThingInCatch();
         }


         try {
            DoFourthThing();
         } finally {
            Console.WriteLine("in finally");
         }

         try {
            DoFirstThing();
         } finally {
            Console.WriteLine("in finally");
         }
      }

      void TryIdenticalCatchFinallyKO() {
         try {
            DoFirstThing();

            try {
               DoSecondThing();
            } finally {

            }
         } catch (Exception e) {
            DoThingInCatch();
         } finally {
            DoThingInCatch();
         }

         DoSecondThing();

         try {
            DoThirdThing();
         } catch (Exception e) {
            DoThingInCatch();
         } finally {
            DoThingInCatch();
         }

         try {
            DoSecondThing();
         } catch (Exception e) {
            DoThingInCatch();
         } finally {
            DoThingInCatch();
         }


         try {
            DoFourthThing();
         } catch (ArgumentException e) {
            DoThingInCatch();
         } finally {
            Console.WriteLine("in finally");
         }

         try {
            DoFirstThing();
         } catch (ArgumentException e) {
            DoThingInCatch();
         } finally {
            Console.WriteLine("in finally");
         }
      }

      void TryIdenticalCatchFinallyOK() {
         try {
            DoFirstThing();

            try {
               DoSecondThing();
            } finally {

            }
         } finally {
            DoThingInCatch();
         }

         DoSecondThing();

         try {
            DoThirdThing();
         } catch (Exception e) {
            DoThingInCatch();
         }

         try {
            DoSecondThing();
         } finally {
            DoThingInCatch();
         }


         try {
            DoFourthThing();
         } catch (ArgumentException e) {
            DoThingInCatch();
         } finally {
            Console.WriteLine("in finallyyyy");
         }

         try {
            DoFirstThing();
         } catch (ArgumentException e) {
            DoThingInCatch();
         } finally {
            Console.WriteLine("in finally");
         }
      }

   }
}
