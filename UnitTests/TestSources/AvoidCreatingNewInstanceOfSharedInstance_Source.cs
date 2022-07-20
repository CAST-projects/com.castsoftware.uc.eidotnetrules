using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;



namespace UnitTests.UnitTest.Sources {

   namespace Shared
   {
      interface IInterface1
      {

      }

      interface IInterface2
      {

      }

      interface IInterface3
      {

      }

      interface IInterface4
      {

      }

      interface IInterface5
      {

      }

      interface IInterface6
      {

      }

      interface IInterface7
      {

      }

      [PartCreationPolicy(CreationPolicy.Shared)]
      class AService : IInterface1, IInterface2, IInterface3, IInterface4, IInterface5, IInterface6, IInterface7
      {
         public AService() {
            System.Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
         }
      }

      class AServiceUser
      {
         private ServiceContainer _serviceContainer;
         private AService _objAService = new AService();
         private AService _objAServiceInitedLater;
         public AServiceUser() {
            _objAServiceInitedLater = new AService();
            _serviceContainer = new ServiceContainer();
            _serviceContainer.AddService(typeof(IInterface1), new AService());
            _serviceContainer.AddService(typeof(AService), createService);
            _serviceContainer.AddService(typeof(IInterface2), PropAService);
            _serviceContainer.AddService(typeof(IInterface3), _objAService);
            AService objAService = new AService();
            _serviceContainer.AddService(typeof(IInterface4), objAService);
            _serviceContainer.AddService(typeof(IInterface5), (IServiceContainer, Type) => new AService());
            AService objAServiceInitedLater = null;
            objAServiceInitedLater = new AService();
            _serviceContainer.AddService(typeof(IInterface6), objAServiceInitedLater);
            _serviceContainer.AddService(typeof(IInterface7), _objAServiceInitedLater);
            ServiceContainer _serviceContainer2 = new ServiceContainer();
            UseAService();

            AService LocalCreateAService()
            {
               return new AService();
            }

            var aService = lambdaVar;
         }

         AService lambdaVar => new AService();

         public AService PropAService {
            get { return new AService(); }
         }

         public AService PropAServiceNotUsed {
            get { return new AService(); }
         }

         private AService AServiceSet {
            get;
            set;
         }

         public object createService(IServiceContainer container, Type serviceType) {
            return new AService();
         }

         public void UseAService() {
            var aservice1 = _serviceContainer.GetService(typeof(IInterface1));
            var aservice2 = _serviceContainer.GetService(typeof(IInterface2));
            var aservice3 = _serviceContainer.GetService(typeof(IInterface3));
            var aservice4 = _serviceContainer.GetService(typeof(IInterface4));
            var aservice5 = _serviceContainer.GetService(typeof(IInterface5));
            var aservice6 = _serviceContainer.GetService(typeof(AService));
            var aservice7 = _serviceContainer.GetService(typeof(IInterface6));
            var aservice8 = _serviceContainer.GetService(typeof(IInterface7));

            var x = new AService();
            x = AServiceSet = PropAServiceNotUsed;
         }
      }

   }
   public class AvoidCreatingNewInstanceOfSharedInstance_Source
   {
   }

}
