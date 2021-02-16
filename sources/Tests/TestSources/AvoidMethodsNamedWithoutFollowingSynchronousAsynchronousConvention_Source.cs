using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;



namespace UnitTests.UnitTest.Sources {
   public class AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention_Source {

    public Task Read(byte [] buffer, int offset, int count, CancellationToken cancellationToken) 
    {
        Action<object> action = (object obj) =>
                        {
                           Console.WriteLine("Task={0}, obj={1}, Thread={2}",
                           Task.CurrentId, obj,
                           Thread.CurrentThread.ManagedThreadId);
                        };
        return new Task(action, "Hello");
    }// violation

    public Task ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
       Action<object> action = (object obj) => {
                          Console.WriteLine("Task={0}, obj={1}, Thread={2}",
                          Task.CurrentId, obj,
                          Thread.CurrentThread.ManagedThreadId);
                       };
       return new Task(action, "Hello");
    }// no violation

    public int Read() {
       return 0;
    }

    public int ReadAsync() {
       return 0;
    }

   }
}
