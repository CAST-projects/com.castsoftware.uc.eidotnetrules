﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace UnitTests.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class SourcesToTest {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SourcesToTest() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("UnitTests.Properties.SourcesToTest", typeof(SourcesToTest).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;   
        ///using System.Threading.Tasks;
        ///
        ///namespace Sonar.Analyzers.CSharp.Common.Tests.UnitTests.Analyzers.AvoidClassesWithTooManyConstructors
        ///{
        ///    class AvoidClassesWithTooManyConstructors_QualUatExample
        ///    {
        ///        public AvoidClassesWithTooManyConstructors_QualUatExample()
        ///        {
        ///            //1er constructeur
        ///        }
        ///
        ///        public AvoidClassesWithTooManyConstructors_QualUatExample(string maChaine)
        ///        {
        ///         [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string AvoidClassesWithTooManyConstructors_QualUatExample {
            get {
                return ResourceManager.GetString("AvoidClassesWithTooManyConstructors_QualUatExample", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using System.Threading.Tasks;
        ///
        ///namespace UnitTests.UnitTest.Sources {
        ///   class AvoidLocalVariablesShadowingClassFields_Source {
        ///      class Shadow {
        ///         private int aMember;
        ///
        ///         void Shadow_aMemberKO() {
        ///            int aMember = 0;
        ///         }
        ///
        ///         void Shadow_aMemberInInnerScopeKO() {
        ///            {
        ///               int aMember = 0;
        ///            }
        ///         }
        ///
        ///         void DontShadow_aMemberOK() {
        ///             [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string AvoidLocalVariablesShadowingClassFields_Source {
            get {
                return ResourceManager.GetString("AvoidLocalVariablesShadowingClassFields_Source", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using System.Threading.Tasks;
        ///using System.Threading;
        ///
        ///
        ///
        ///namespace UnitTests.UnitTest.Sources {
        ///   public class AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention_Source {
        ///
        ///    public Task Read(byte [] buffer, int offset, int count, CancellationToken cancellationToken) 
        ///    {
        ///        Action&lt;object&gt; action = (object obj) =&gt;
        ///                        {
        ///                           Console.WriteLine(&quot;Ta [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention_Source {
            get {
                return ResourceManager.GetString("AvoidMethodsNamedWithoutFollowingSynchronousAsynchronousConvention_Source", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using System.Threading.Tasks;
        ///using System.Reflection;
        ///
        ///
        ///namespace UnitTests.UnitTest.Sources {
        ///   public class AvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName_Source {
        ///
        ///      public class KlassAssemblyLoadFrom {
        ///         public void foo() {
        ///            Assembly SampleAssembly;
        ///            SampleAssembly = Assembly.LoadFrom(&quot;c:\\Sample.Assembly.dll&quot;);
        ///            // Obtain a refe [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string AvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName_Source {
            get {
                return ResourceManager.GetString("AvoidUsing_Assembly_LoadFrom_Assembly_LoadFileAndAssembly_LoadWithPartialName_Sou" +
                        "rce", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using System.Threading.Tasks;
        ///
        ///namespace UnitTests.UnitTest.Sources {
        ///   public class ChildClassFieldsShouldNotShadowParentClassFields_Source {
        ///
        ///      class Base {
        ///         protected int ripe;
        ///         protected int flesh;
        ///      }
        ///
        ///      class Derived : Base {
        ///         private bool ripe; // Noncompliant
        ///         private static int FLESH; // Noncompliant
        ///
        ///         private bool ripened;
        ///         private static char FLESH_COL [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ChildClassFieldsShouldNotShadowParentClassFields_Source {
            get {
                return ResourceManager.GetString("ChildClassFieldsShouldNotShadowParentClassFields_Source", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using System.Threading.Tasks;
        ///
        ///namespace UnitTests.UnitTest.Sources {
        ///   public class ClassesImplementingIEquatableTShouldBeSealed_Source {
        ///
        ///      public class UnSealedEqualsVirtual : IEquatable&lt;int&gt; {
        ///         public virtual bool Equals(int i) {
        ///            return false;
        ///         }
        ///      }
        ///
        ///      public class UnSealedDerviedFromUnSealedEqualsVirtual : UnSealedEqualsVirtual {
        ///         public override bool E [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ClassesImplementingIEquatableTShouldBeSealed_Source {
            get {
                return ResourceManager.GetString("ClassesImplementingIEquatableTShouldBeSealed_Source", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using System.Threading.Tasks;
        ///using System.Globalization;
        ///
        ///namespace UnitTests.UnitTest.Sources {
        ///   public class CultureDependentStringOperationsShouldSpecifyCulture_Source {
        ///      private static readonly String str = &quot;abc&quot;;
        ///      String lowerKO = str.ToLower();
        ///      String lowerOK = str.ToLower(CultureInfo.InvariantCulture);
        ///      String lowerOKInvariant = str.ToLowerInvariant();
        ///
        ///      String upperKO = st [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string CultureDependentStringOperationsShouldSpecifyCulture_Source {
            get {
                return ResourceManager.GetString("CultureDependentStringOperationsShouldSpecifyCulture_Source", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using System.Threading.Tasks;
        ///
        ///namespace UnitTests.UnitTest.Sources {
        ///   public class EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull_Source {
        ///      class Result {
        ///
        ///      }
        ///
        ///      class KO {
        ///         public Result[] GetResultsReturnArray() {
        ///            return null; // Noncompliant
        ///         }
        ///
        ///         public IEnumerable&lt;Result&gt; GetResultsReturnIEnumerable() {
        ///            return null; // Noncomplia [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull_Source {
            get {
                return ResourceManager.GetString("EmptyArraysAndCollectionsShouldBeReturnedInsteadOfNull_Source", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using System.Threading.Tasks;
        ///
        ///namespace UnitTests.UnitTest.Sources {
        ///   public class EnsureProperArgumentsToEvents_Source {
        ///
        ///      public class Klass1 {
        ///         public event EventHandler foo;
        ///
        ///         protected virtual void OnTfoo(EventArgs e) {
        ///            foo?.Invoke(null, e); // Violation
        ///         }
        ///      }
        ///
        ///      public class Klass2 {
        ///         public static event EventHandler foo;
        ///
        ///         protect [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string EnsureProperArgumentsToEvents_Source {
            get {
                return ResourceManager.GetString("EnsureProperArgumentsToEvents_Source", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using System.Threading.Tasks;
        ///
        ///namespace UnitTests.UnitTest.Sources {
        ///   public class InheritedMemberVisibilityShouldNotBeDecreased_Source {
        ///
        ///      public class Base {
        ///         public virtual void VirtualMethod() { }
        ///         public void BaseMethod(int x) { }
        ///      }
        ///
        ///      public class Foo  : Base {
        ///         public override void VirtualMethod() { }
        ///         public void SomeMethod(int count) { }
        ///         p [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string InheritedMemberVisibilityShouldNotBeDecreased_Source {
            get {
                return ResourceManager.GetString("InheritedMemberVisibilityShouldNotBeDecreased_Source", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using System.Threading.Tasks;
        ///
        ///namespace UnitTests.UnitTest.Sources {
        ///   public class InterfaceInstancesShouldNotBeCastToConcreteTypes_Source {
        ///      interface BaseInterface {
        ///         void BaseInterfaceMethod();
        ///      }
        ///
        ///      interface DerivedInterface : BaseInterface {
        ///         void DerivedInterfaceMethod();
        ///      }
        ///
        ///      abstract class AbstractClass : DerivedInterface {
        ///         public abstract void B [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string InterfaceInstancesShouldNotBeCastToConcreteTypes_Source {
            get {
                return ResourceManager.GetString("InterfaceInstancesShouldNotBeCastToConcreteTypes_Source", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using System.Threading.Tasks;
        ///
        ///namespace UnitTests.UnitTest.Sources {
        ///   public class TrackFIXMETags_Source {
        ///      // FIXME
        ///      // FIXME too.
        ///      // NOT FIXME
        ///      /* FIXME Again */
        ///      /* Not Fixme */
        ///      /* fixme */
        ///   }
        ///}
        ///.
        /// </summary>
        internal static string TrackFIXMETags_Source {
            get {
                return ResourceManager.GetString("TrackFIXMETags_Source", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to using System;
        ///using System.Collections.Generic;
        ///using System.Linq;
        ///using System.Text;
        ///using System.Threading.Tasks;
        ///
        ///namespace UnitTests.UnitTest.Sources {
        ///   public class TrackTODOTags_Source {
        ///      //TODO this
        ///      // TODO that
        ///      // Not TODO
        ///      /* TODO this again */
        ///      /*todo THAT again*/
        ///      /* NOT TODO */
        ///   }
        ///}
        ///.
        /// </summary>
        internal static string TrackTODOTags_Source {
            get {
                return ResourceManager.GetString("TrackTODOTags_Source", resourceCulture);
            }
        }
    }
}
