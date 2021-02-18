using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources {
   public class EnsureProperArgumentsToEvents_Source {

      public class Klass1 {
         public event EventHandler foo;

         protected virtual void OnTfoo(EventArgs e) {
            foo?.Invoke(null, e); // Violation
         }
      }

      public class Klass2 {
         public static event EventHandler foo;

         protected virtual void OnTfoo(EventArgs e) {
            foo?.Invoke(null, e); //No violation
         }
      }

      public class Klass3 {
         public event EventHandler foo;

         protected virtual void OnTfoo(EventArgs e) {
            foo(null, e); // Violation
         }
      }

      public class Klass4 {
         public event EventHandler foo;

         protected virtual void OnTfoo(EventArgs e) {
            foo?.Invoke(this, null); // Violation
         }
      }

      public class Klass5 {
         public event EventHandler foo;

         protected virtual void OnTfoo(EventArgs e) {
            object sender = null;
            foo?.Invoke(sender, e); // Violation
         }
      }

      public class Klass6 {
         public static event EventHandler foo;

         protected virtual void OnTfoo(EventArgs e) {
            foo.Invoke(null, e); //No violation
         }
      }

      public class Klass7 {
         public event EventHandler<ThresholdReachedEventArgs> foo;

         protected virtual void OnTfoo(ThresholdReachedEventArgs e) {
            foo?.Invoke(null, e); // Violation
         }
      }

      public class ThresholdReachedEventArgs : EventArgs {
         public int Threshold { get; set; }
         public DateTime TimeReached { get; set; }
      }

   }
}
