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
            int iii;
         } catch (Exception e) {
            throw;
         }

      }

      private Exception _nullReferenceExceptionInitedLaterAndNotThrown = null;

      void someOtherMethod() {
         _nullReferenceExceptionInitedLaterAndNotThrown  = new NullReferenceException();
      }

      void ThrowExpression(int groupId, object group)
      {
         var argException = new ArgumentException("Group {groupId} does not exist" , "no param");
         //var groupSomething = group ?? throw new ArgumentException("Group {groupId} does not exist" , "no param");
         var groupSomething = group ?? throw argException;
      }

   }
}
