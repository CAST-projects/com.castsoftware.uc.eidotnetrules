using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources {
   public class InterfaceInstancesShouldNotBeCastToConcreteTypes_Source {
      interface BaseInterface {
         void BaseInterfaceMethod();
      }

      interface DerivedInterface : BaseInterface {
         void DerivedInterfaceMethod();
      }

      abstract class AbstractClass : DerivedInterface {
         public abstract void BaseInterfaceMethod();
         public abstract void DerivedInterfaceMethod();
      }

      class Clazz : AbstractClass {
         public override void BaseInterfaceMethod() { }
         public override void DerivedInterfaceMethod() { }
      }

      struct AStruct : DerivedInterface {
         public void BaseInterfaceMethod() { }
         public void DerivedInterfaceMethod() { }
      }

      void TestCast() {
         BaseInterface baseInterface = new Clazz();
         AbstractClass abstractClass1 = (AbstractClass)baseInterface;
         AbstractClass abstractClass2 = baseInterface as AbstractClass;

         Clazz clazz1 = (Clazz)baseInterface;
         Clazz clazz2 = baseInterface as Clazz;

         BaseInterface baseInterfaceStruct = new AStruct();
         AStruct aStruct1 = (AStruct)baseInterfaceStruct;
      }
   }
}
