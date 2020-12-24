using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources {
   public class EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull_Source {
      class Result {

      }

      class KO {
         public Result[] GetResultsReturnArray() {
            return null; // Noncompliant
         }

         public IEnumerable<Result> GetResultsReturnIEnumerable() {
            return null; // Noncompliant
         }

         //public IEnumerable<Result> GetResults() => null; // Noncompliant

         public IEnumerable<Result> ResultsPropertyReturnsIEnumerable {
            get {
               return null; // Noncompliant
            }
         }

         public Result[] ResultsPropertyReturnsArray {
            get {
               return null; // Noncompliant
            }
         }


         //public IEnumerable<Result> Results => null; // Noncompliant
      }

      class OK {
         public Result[] GetResultsReturnArray() {
            return new Result[0]; // Noncompliant
         }

         public IEnumerable<Result> GetResultsReturnIEnumerable() {
            return Enumerable.Empty<Result>(); // Noncompliant
         }

         //public IEnumerable<Result> GetResults() => null; // Noncompliant

         public IEnumerable<Result> ResultsPropertyReturnsIEnumerable {
            get {
               return Enumerable.Empty<Result>(); // Noncompliant
            }
         }

         public Result[] ResultsPropertyReturnsArray {
            get {
               return new Result[0]; // Noncompliant
            }
         }


         //public IEnumerable<Result> Results => null; // Noncompliant
      }

   }
}
