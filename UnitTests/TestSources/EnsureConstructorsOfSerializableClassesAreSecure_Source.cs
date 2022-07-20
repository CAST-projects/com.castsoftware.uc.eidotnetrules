using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace UnitTests.UnitTest.Sources
{

   [Serializable]
   class SerializedKO1 : ISerializable
   {

      [FileIOPermissionAttribute(SecurityAction.Demand, Unrestricted = true)]
      SerializedKO1() {
      }

      protected SerializedKO1(SerializationInfo info, StreamingContext context) { // Noncompliant
      }

      public void GetObjectData(SerializationInfo info, StreamingContext context) {

      }
   }

   [Serializable]
   class SerializedOK1 : ISerializable
   {

      SerializedOK1() {
      }

      protected SerializedOK1(SerializationInfo info, StreamingContext context) { 
      }

      public void GetObjectData(SerializationInfo info, StreamingContext context) {

      }
   }

   [Serializable]
   class SerializedOK2 : ISerializable
   {

      SerializedOK2() {
      }

      [FileIOPermissionAttribute(SecurityAction.Demand, Unrestricted = true)]
      protected SerializedOK2(SerializationInfo info, StreamingContext context) { 
      }

      
      public void GetObjectData(SerializationInfo info, StreamingContext context) {

      }
   }

   [Serializable]
   class SerializedOK3 : ISerializable
   {

      [FileIOPermissionAttribute(SecurityAction.Demand, Unrestricted = true)]
      SerializedOK3() {
      }

      [FileIOPermissionAttribute(SecurityAction.Demand, Unrestricted = true)]
      protected SerializedOK3(SerializationInfo info, StreamingContext context) { 
      }


      public void GetObjectData(SerializationInfo info, StreamingContext context) {

      }
   }

   class SerializedOK4 
   {
      SerializedOK4() {
      }
   }



}
