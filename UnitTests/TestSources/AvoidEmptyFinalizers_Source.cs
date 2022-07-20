using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace UnitTests.UnitTest.Sources {
   public class AvoidEmptyFinalizers_Source {

      class FinalizerWithDebugFailInIfDebugKO {
         ~FinalizerWithDebugFailInIfDebugKO() {
#if DEBUG
            Debug.Fail("Failed");
#endif
         }
      }

      class FinalizerInIfDebugWithDebugFailOK {
#if DEBUG
         ~FinalizerInIfDebugWithDebugFailOK() {

            Debug.Fail("Failed");
         }
#endif
      }

#if DEBUG
      class ClassInIfDebugWithFinalizerDebugFailOK {

         ~ClassInIfDebugWithFinalizerDebugFailOK() {

            Debug.Fail("Failed");
         }
      }
#endif


      class EmptyFinalizerKO {
         ~EmptyFinalizerKO() {

         }
      }

      class FinalizerWithOnlyCommentsKO {
         ~FinalizerWithOnlyCommentsKO() {
            // finalizer
         }
      }

      class FinalizerWithDebugFailKO {
         ~FinalizerWithDebugFailKO() {
            Debug.Fail("Failed");
         }
      }

      class ClassWithFinalizerWithValidStatements {
         ~ClassWithFinalizerWithValidStatements() {
            int i = 0;
         }
      }

   }
}
