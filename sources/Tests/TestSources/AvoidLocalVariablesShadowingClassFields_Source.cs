using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources {
   class AvoidLocalVariablesShadowingClassFields_Source {
      class Shadow {
         private int aMember;

         void Shadow_aMemberKO() {
            int aMember = 0;
         }

         void Shadow_aMemberInInnerScopeKO() {
            {
               int aMember = 0;
            }
         }

         void DontShadow_aMemberOK() {
            {
               int aMemberX = 0;
            }
         }

         protected int aProtectedMemberOK = 0;
         protected int aProtectedMemberKO = 0;
      }


      class AnotherShadow : Shadow {
         protected int aProtectedMemberOK = 0;

         public AnotherShadow() {
            int aProtectedMemberKO = 0;
         }

         public void ShadowBase_aProtectedMemberKO() {
            int aProtectedMemberKO = 0;
         }

         public void TryShadowBasePrivateMemberOK() {
            int aMember = 0;
         }

         class InnerShadow {
            int aInnerShadowMember = 0;
            public InnerShadow() {
               int aProtectedMemberOK = 0;
            }

            void Shadow_aInnerShadowMember() {
               int aInnerShadowMember = 0;
            }

            public int aPublicInnerShadowMember = 0;
         }

         class InnerShadowAnother : InnerShadow {
            public InnerShadowAnother() {
               int aPublicInnerShadowMember = 0;
            }
         }

         struct AStruct {
            private int aStructMember;
            public void Shadow() {
               int aStructMember = 0;
            }
         }

      }
   }

    public class Klass
    {
        public Klass()
        {
            int value = 0;
            string name = "toto";
            foreach (var itr in listInt)
            {
                Console.WriteLine(itr);
            }

        }

        public int value { get; set; }
        public string name { get; set; }

        public int itr;
        public List<int> listInt = new List<int>() { 1, 2, 3, 4 }; 

    }

}
