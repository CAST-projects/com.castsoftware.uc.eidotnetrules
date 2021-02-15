using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources {
   public class AvoidCreatingExceptionWithoutThrowingThem_Source {

      private NullReferenceException _nullReferenceExceptionNotThrown = new NullReferenceException();
      private NullReferenceException _nullReferenceExceptionThrown = new NullReferenceException();

      private void ThrowNullReferenceException() {
         throw _nullReferenceExceptionThrown;
      }

      private void ThrowAndNot(int i = 0) {

         var toThrow = new NullReferenceException();

         if (0 == i) {
            throw new NullReferenceException();
         }
         else {
            throw toThrow;
         }

         new NullReferenceException();

         var notToThrow = new NullReferenceException();

         new Object();

         var o = new Object();

         Exception varNotToThrow = null;
         varNotToThrow = new InvalidOperationException();

         try {
            int i;
         } catch (Exception e) {
            throw;
         }

      }

      private Exception _nullReferenceExceptionInitedLaterAndNotThrown = null;

      void someOtherMethod() {
         _nullReferenceExceptionInitedLaterAndNotThrown  = new NullReferenceException();
      }

   }
}
