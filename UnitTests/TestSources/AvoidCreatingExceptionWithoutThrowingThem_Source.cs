using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;


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

         var groupSomething = group ?? throw argException;
      }

      void ThrowExpression(int groupId, object group)
      {
         var groupSomething = group ?? throw new ArgumentException("Group {groupId} does not exist" , "no param");
      }

      void OtherTypesOfCreations()
      {
         var arrayCreation = new int[] { 1, 2, 3 };
         dynamic d = new AvoidCreatingExceptionWithoutThrowingThem_Source();
      }

      class A
      {
         public A(dynamic d)
         {

         }
      }

      class B { }

      public static void BuildDynamicExpando(string className,
          Dictionary<string, object> fields)
      {
         dynamic obj = new B();
         A a = new A(obj);

         var anon = new { AnInt = 0, AFloat = 0.1f };
      }

      private void CreateTypeParamTypeObj<T>(string arg) where T : new()
      {
         var t = new T();
      }

      private void UseCreateTypeParamTypeObj<T>(string arg) where T : new()
      {
         CreateTypeParamTypeObj<Exception>("arg");
      }

      private object PropInPlace { get; set; } = new object();
      private object PropInCtor { get; set; } 
      AvoidCreatingExceptionWithoutThrowingThem_Source() {
         PropInCtor = new object();
      }
   }

   public class SomeOtherClass
   {
      private Exception _privateException = new System.Exception();
   }

   public class Test {
        private static void logFile(Object oInformation){
            Exception err = new IOException();
            int errorCount = 0;
            StringBuilder sbPath = new StringBuilder();
            sbPath.Append(((ArrayList) oInformation)[1].ToString());
            sbPath.Append("/Exceptions/");
            sbPath.Append(((ArrayList) oInformation)[5].ToString());
            sbPath.Append("/");
            
            if(Directory.Exists(sbPath.ToString()) == false){
                Directory.CreateDirectory(sbPath.ToString());
            }

            sbPath.Append(((ArrayList) oInformation)[6].ToString());
            sbPath.Append(".htm");
            while((err is IOException) == true && errorCount < 100){
                try{
                    File.AppendAllText(sbPath.ToString(), ((ArrayList) oInformation)[7].ToString());
                    err = new Exception();
                }    
                catch(IOException error){
                    err = error;
                }
                errorCount++;
            }
        }
         AnotherException _anotherException = new AnotherException();
         MyException _anException = new MyException();
      }

      interface IMyInterface { }

      class MyException : Exception, IMyInterface
      {

      }

      interface IAnotherInterface { }

      class AnotherException : MyException, IAnotherInterface
      {

      }




}
