using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.UnitTest.Sources {
   public class ClassesImplementingIEquatableTShouldBeSealed_Source {

      public class UnSealedEqualsVirtual : IEquatable<int> {
         public virtual bool Equals(int i) {
            return false;
         }
      }

      public class UnSealedDerviedFromUnSealedEqualsVirtual : UnSealedEqualsVirtual {
         public override bool Equals(int i) {
            return false;
         }
      }

      public class UnSealedNoEqualsDerviedFromUnSealedEqualsVirtual : UnSealedEqualsVirtual {
      }

      public sealed class SealedDerviedFromUnSealedEqualsVirtual : UnSealedEqualsVirtual {
         public override bool Equals(int i) {
            return false;
         }
      }


      protected abstract class AbstractUnSealedEqualsAbstract : IEquatable<int> {
         public abstract bool Equals(int i);
      }

      protected class UnsealedDerivedFromAbstractUnSealedEqualsAbstract : AbstractUnSealedEqualsAbstract {
         public override bool Equals(int i) {
            return true;
         }
      }

      protected sealed class SealedDerivedFromAbstractUnSealedEqualsAbstract : AbstractUnSealedEqualsAbstract {
         public override bool Equals(int i) {
            return true;
         }
      }

      protected abstract class AbstractUnSealedEqualsVirtual : IEquatable<int> {
         public virtual bool Equals(int i) {
            return true;
         }
      }

      public interface ISomeInterface {
         void aMethod();
      }

      public class SomeClass : ISomeInterface {
         public void aMethod() {

         }
      }

      public class EquatableNotSealed : IEquatable<int> {
         public bool Equals(int other) {
            return false;
         }
      }

      public sealed class EquatableSealed : IEquatable<int> {
         public bool Equals(int other) {
            return false;
         }
      }

      protected interface IMyEquatable<T> : IEquatable<T> {

      }

      protected class MyEquatableNotSealed : IMyEquatable<int> {
         public bool Equals(int other) {
            return false;
         }
      }

      protected sealed class MyEquatableSealed : IMyEquatable<int> {
         public bool Equals(int other) {
            return false;
         }
      }

      protected class UnSealedMultipleEquatable : IEquatable<int>, IEquatable<float> {
         public bool Equals(int other) {
            return false;
         }
         public bool Equals(float other) {
            return false;
         }

      }

      protected sealed class SealedMultipleEquatable : IEquatable<int>, IEquatable<float> {
         public bool Equals(int other) {
            return false;
         }
         public bool Equals(float other) {
            return false;
         }
      }

      private class PrivateUnSealedMultipleEquatable : IEquatable<int>, IEquatable<float> {
         public bool Equals(int other) {
            return false;
         }
         public bool Equals(float other) {
            return false;
         }

      }

      internal class InternalUnSealedMultipleEquatable : IEquatable<int>, IEquatable<float> {
         public bool Equals(int other) {
            return false;
         }
         public bool Equals(float other) {
            return false;
         }

      }

      class DefaultUnSealedMultipleEquatable : IEquatable<int>, IEquatable<float> {
         public bool Equals(int other) {
            return false;
         }
         public bool Equals(float other) {
            return false;
         }

      }


   }
}
