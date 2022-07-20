using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;

namespace UnitTests.UnitTest.Sources
{

   public class MembersOfLargerScopeElementShouldNotHaveConflictingTransparencyAnnotations_Source
   {

      [SecurityCritical]
      public class CriticalClass
      {
         // CA2136 violation - this method is not really safe critical, since the larger scoped type annotation
         // has precidence over the smaller scoped method annotation.  This can be fixed by removing the
         // SecuritySafeCritical attribute on this method
         [SecuritySafeCritical] //Violation
         public void SafeCriticalMethodKO()
         {
         }
      }

      [SecurityCritical]
      public class OuterCriticalClass
      {
         // CA2136 violation - this method is not really safe critical, since the larger scoped type annotation
         // has precidence over the smaller scoped method annotation.  This can be fixed by removing the
         // SecuritySafeCritical attribute on this method
         [SecuritySafeCritical] //Violation
         public class PublicSafeCriticalClassKO
         {
         }

         [SecurityCritical]
         public class InnerCriticalClass
         {
            [SecuritySafeCritical] //Violation
            private int _innerClassSafeCriticalMemberKO;
         }

         public class InnerNormalClassWiithKOMember
         {
            [SecuritySafeCritical] //Violation
            private int _innerClassSafeCriticalMemberKO;
         }

         public class InnerNormalClass
         {
            private int _innerClassMember;
         }
      }


      public class NormalClass
      {
         public class PublicNormalClass
         {
         }

         
         public class InnerNormalClass
         {
            private int _innerClassMember;
         }
      }      
   }
}
