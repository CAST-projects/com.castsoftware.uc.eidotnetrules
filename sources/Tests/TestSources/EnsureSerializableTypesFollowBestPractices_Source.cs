using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace UnitTests.UnitTest.Sources
{
   public class EnsureSerializableTypesFollowBestPractices_Source
   {
      private class SerializableAttributeKO : ISerializable // Violation: No Serializable attribute
      {
         protected SerializableAttributeKO(SerializationInfo info, StreamingContext context) { }
         public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { }
      }

      [Serializable]
      private class SerializableAttributeOK : ISerializable
      {
         protected SerializableAttributeOK(SerializationInfo info, StreamingContext context) { }
         public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { }
      }

      [Serializable]
      private class SerializableCtorKO1 : ISerializable
      {
         public SerializableCtorKO1(SerializationInfo info, StreamingContext context) { }
         public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { }
      }

      [Serializable]
      private class SerializableCtorKO2 : ISerializable
      {
         SerializableCtorKO2(SerializationInfo info, StreamingContext context) { }
         public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { }
      }

      [Serializable]
      private sealed class SerializableCtorOK1 : ISerializable
      {
         private SerializableCtorOK1(SerializationInfo info, StreamingContext context) { }
         public void GetObjectData(SerializationInfo info, StreamingContext context) { }
      }

      [Serializable]
      private class SerializableCtorOK2 : ISerializable
      {
         protected SerializableCtorOK2(SerializationInfo info, StreamingContext context) { }
         public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { }
      }

      [Serializable]
      private class NoSerializableCtorKO1 : ISerializable
      {
         public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { }
      }

      [Serializable]
      private class NoSerializableCtorKO2 : ISerializable
      {
         NoSerializableCtorKO2() { }
         public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { }
      }

      [Serializable]
      private sealed class SealedSerializableCtorOK1 : ISerializable
      {
         public SealedSerializableCtorOK1(SerializationInfo info, StreamingContext context) { }
         public void GetObjectData(SerializationInfo info, StreamingContext context) { }
      }

      [Serializable]
      private class SerializableOK : ISerializable
      {
         protected SerializableOK() { }
         protected SerializableOK(SerializationInfo info, StreamingContext context) { }
         public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { }
      }

      [Serializable]
      private class SerializableCtorBaseCallKO : SerializableOK, ISerializable
      {
         protected SerializableCtorBaseCallKO(SerializationInfo info, StreamingContext context) { }
         public override void GetObjectData(SerializationInfo info, StreamingContext context) { base.GetObjectData(info, context); }
      }

      [Serializable]
      private class NonSerialzedMembersKO : ISerializable
      {
         int _intMemberSerialized1;
         int _intMemberNonSerialized2;

         protected NonSerialzedMembersKO(SerializationInfo info, StreamingContext context) {
            _intMemberSerialized1 = info.GetInt32("_intMemberSerialized1");
         }
         public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("_intMemberSerialized1", _intMemberSerialized1);
         }
      }

      [Serializable]
      private class NonSerialzedMembersOK : ISerializable
      {
         int _intMemberSerialized1;
         [NonSerialized]
         int _intMemberNonSerialized2;

         protected NonSerialzedMembersOK(SerializationInfo info, StreamingContext context)
         {
            _intMemberSerialized1 = info.GetInt32("_intMemberSerialized1");
         }
         public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
         {
            info.AddValue("_intMemberSerialized1", _intMemberSerialized1);
         }
      }

      [Serializable]
      private class NoGetObjectDataKO : SerializableOK, ISerializable
      {
         int _intSerializableMember;
         protected NoGetObjectDataKO(SerializationInfo info, StreamingContext context) : base(info, context) { }
      }

      [Serializable]
      private class NoGetObjectDataOK1 : SerializableOK, ISerializable
      {
         [NonSerialized]
         int _intNonSerializableMember;
         protected NoGetObjectDataOK1(SerializationInfo info, StreamingContext context) : base(info, context) { }
      }

      [Serializable]
      private class NoGetObjectDataOK2 : SerializableOK, ISerializable
      {
         protected NoGetObjectDataOK2(SerializationInfo info, StreamingContext context) : base(info, context) { }
      }

      [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
      [Serializable]
      public /*sealed*/ class AliasRepositoryAttribute : Attribute
      {
         public AliasRepositoryAttribute(string name)
         {
            Name = name;
         }

         public string Name
         {
            get { return m_name; }
            set { m_name = value; }
         }

         private string m_name = null;
      }   
   }
}
