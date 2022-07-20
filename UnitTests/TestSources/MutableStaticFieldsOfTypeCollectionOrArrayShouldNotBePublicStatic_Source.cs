using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Immutable;

namespace UnitTests.UnitTest.Sources {
   public class MutableStaticFieldsOfTypeCollectionOrArrayShouldNotBePublicStatic_Source {

      public class AClass {
         public static string[] strings1KO = { "first", "second" };  // Noncompliant
         public static List<String> strings2KO = new List<String>();  // Noncompliant

         protected static string[] strings1OK = { "first", "second" };
         protected static List<String> strings2OK = new List<String>();

         protected static readonly int[] ints3OK = { 1, 2 };
         protected static readonly List<int> ints4OK = new List<int> { 1, 2 };
         protected static readonly Dictionary<int, String> dict5OK = new Dictionary<int, String> { { 1, "A" }, { 2, "B" } };

         public static ReadOnlyCollection<int> ro1OK = new ReadOnlyCollection<int>(ints4OK);
         public static ReadOnlyDictionary<int, String> ro2Ok = new ReadOnlyDictionary<int, string>(dict5OK);

         public static ImmutableArray<int> immutable1OK = ImmutableArray<int>.Empty;
         public static IImmutableDictionary<int, string> immutable2OK = dict5OK.ToImmutableDictionary();
         public static IImmutableList<int> immutable3OK = dict5OK.Keys.ToImmutableList();
         public static IImmutableSet<int> immutable5OK = dict5OK.Keys.ToImmutableHashSet();
         public static IImmutableStack<int> immutable6OK = ImmutableStack.Create<int>(ints3OK);
         public static IImmutableQueue<int> immutable7OK = ImmutableQueue.Create<int>(ints3OK);

         public static readonly Dictionary<int, String> dict3KO = new Dictionary<int, String> { { 1, "A" }, { 2, "B" } }; // Noncompliant
         public static readonly Dictionary<int, String> dict8OK = new ReadOnlyDictionary<int, String> { { 1, "A" }, { 2, "B" } };
         public static readonly List<int> ints9OK = (List<int>)immutable3OK;
         public static readonly List<int> ints4KO = ints4OK; // Noncompliant
         
      }

      public struct AStruct {
         public static string[] strings1KO = { "first", "second" };  // Noncompliant
         public static List<String> strings2KO = new List<String>();  // Noncompliant

         private static string[] strings1OK = { "first", "second" };
         private static List<String> strings2OK = new List<String>();

         private static readonly int[] ints3OK = { 1, 2 };
         private static readonly List<int> ints4OK = new List<int> { 1, 2 };
         private static readonly Dictionary<int, String> dict5OK = new Dictionary<int, String> { { 1, "A" }, { 2, "B" } };

         public static ReadOnlyCollection<int> ro1OK = new ReadOnlyCollection<int>(ints4OK);
         public static ReadOnlyDictionary<int, String> ro2Ok = new ReadOnlyDictionary<int, string>(dict5OK);

         public static ImmutableArray<int> immutable1OK = ImmutableArray<int>.Empty;
         public static IImmutableDictionary<int, string> immutable2OK = dict5OK.ToImmutableDictionary();
         public static IImmutableList<int> immutable3OK = dict5OK.Keys.ToImmutableList();
         public static IImmutableSet<int> immutable5OK = dict5OK.Keys.ToImmutableHashSet();
         public static IImmutableStack<int> immutable6OK = ImmutableStack.Create<int>(ints3OK);
         public static IImmutableQueue<int> immutable7OK = ImmutableQueue.Create<int>(ints3OK);

      }

   }
}
