using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CastDotNetExtension.Utils {
   using AttributeArgumentType = Dictionary<String, SyntaxNode>;

   class TypeAttributes {

      public enum AttributeType {
         None,
         Export,
         PartCreationPolicy,
         Serializable,
         FileIOPermissionAttribute
      }
      public interface ITypeAttribute {

         AttributeType Name { get; }
         Dictionary<String, SyntaxNode> Arguments { get; }
      }

      public abstract class TypeAttribute : ITypeAttribute {
         public AttributeType Name {
            get { return _type; }
         }
         public Dictionary<String, SyntaxNode> Arguments {
            get { return _arguments; }
         }

         private Dictionary<String, SyntaxNode> _arguments;
         private AttributeType _type;
         public TypeAttribute(AttributeType type, Dictionary<String, SyntaxNode> arguments) {
            _type = type;
            _arguments = arguments;
         }
      }

      public class Export : TypeAttribute {
         public Export(Dictionary<String, SyntaxNode> arguments) :
            base(AttributeType.Export, arguments) {
         }
      }

      public class PartCreationPolicy : TypeAttribute {
         public PartCreationPolicy(Dictionary<String, SyntaxNode> arguments) :
            base(AttributeType.PartCreationPolicy, arguments) {
         }
      }

      public class Serializable : TypeAttribute
      {
         public Serializable(Dictionary<String, SyntaxNode> arguments) :
            base(AttributeType.Serializable, arguments)
         {
         }
      }

      public class FileIOPermissionAttribute : TypeAttribute
      {
         public FileIOPermissionAttribute(Dictionary<String, SyntaxNode> arguments) :
            base(AttributeType.FileIOPermissionAttribute, arguments)
         {
         }
      }

      public class TypeAttributesAll {
         public String ForType {
            get { return _forType; }
         }
         public SyntaxNode SyntaxNode {
            get { return _syntaxNode; }
         }
         public IList<ITypeAttribute> Attributes {
            get { return _attributes; }
         }
         private String _forType;
         private SyntaxNode _syntaxNode;
         private IList<ITypeAttribute> _attributes;
         internal TypeAttributesAll(String forType, SyntaxNode syntaxNode, IList<ITypeAttribute> attributes) {
            _forType = forType;
            _syntaxNode = syntaxNode;
            _attributes = attributes;
         }
      }

      private static bool HasAttributeType(AttributeType[] attrs2Search, string attribute) {
         if (null != attrs2Search && null != attribute) {
            foreach (var attr in attrs2Search) {
               if (attr.ToString() == attribute) {
                  return true;
               }
            }
         }
         return false;
      }

      public static TypeAttributesAll Get(SyntaxNodeAnalysisContext context, AttributeType[] attrs2Search = null) {
         IList<ITypeAttribute> attributes = new List<ITypeAttribute>();
         var declarationSyntax = context.Node as Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax;

         if (null != declarationSyntax) {
            var attrs = Get(declarationSyntax, attributes, attrs2Search);
            if (attrs.Any()) {
               var typeName = declarationSyntax.Identifier.ValueText;
               return new TypeAttributesAll(typeName, declarationSyntax, attrs);
            }

         }
         return null;
      }

      public static IList<ITypeAttribute> Get(INamedTypeSymbol namedType,
         IList<ITypeAttribute> attributes, AttributeType[] attrs2Search = null) {
       
         foreach (var syntaxRef in namedType.DeclaringSyntaxReferences) {
            var typeDeclarationSyntaxes = syntaxRef.SyntaxTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>();
            foreach (var typeDeclarationSyntax in typeDeclarationSyntaxes) {
               attributes = TypeAttributes.Get(typeDeclarationSyntax, attributes, new[] { TypeAttributes.AttributeType.Serializable });
            }
         }

         return attributes;
      }

      public static IList<ITypeAttribute> Get(Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax typeDeclaration,
         IList<ITypeAttribute> attributes, AttributeType[] attrs2Search = null) {

         bool all = null == attrs2Search;

         if (typeDeclaration != null) {
            // get the contract type for each contract
            if (typeDeclaration.AttributeLists.Any()) {
               return Get(typeDeclaration.AttributeLists, attributes, attrs2Search);
            }
         }
         return attributes;
      }

      public static IList<ITypeAttribute> Get(IMethodSymbol iMethodSymbol,
         IList<ITypeAttribute> attributes, AttributeType[] attrs2Search = null, object declarationSyntax = null) {

         if (null != iMethodSymbol) {
            foreach (var syntaxRef in iMethodSymbol.DeclaringSyntaxReferences) {
               var syntax = syntaxRef.GetSyntax() as BaseMethodDeclarationSyntax;
               if (null != syntax) {
                  attributes = Get(syntax.AttributeLists, attributes, new[] { TypeAttributes.AttributeType.FileIOPermissionAttribute });
               }
            }
         }
         return attributes;
      }

      public static IList<ITypeAttribute> Get(SyntaxList<AttributeListSyntax> attributeLists,
         IList<ITypeAttribute> attributes, AttributeType[] attrs2Search = null, object declarationSyntax = null) {

            if (null != attributeLists && attributeLists.Any()) {
               bool all = null == attrs2Search;
               foreach (Microsoft.CodeAnalysis.CSharp.Syntax.AttributeListSyntax attrListSynt in attributeLists) {
                  foreach (Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax attribute in attrListSynt.Attributes) {
                     ITypeAttribute iTypeAttribute = null;
                     if (all || HasAttributeType(attrs2Search, attribute.Name.ToString())) {
                        if ("Export" == attribute.Name.ToString()) {
                           iTypeAttribute = CreateCSharpExport(attribute, attrListSynt, declarationSyntax as TypeDeclarationSyntax);
                        } else if ("PartCreationPolicy" == attribute.Name.ToString()) {
                           iTypeAttribute = CreateCSharpPartCreationPolicy(attribute, attrListSynt);
                        } else if ("Serializable" == attribute.Name.ToString()) {
                           iTypeAttribute = CreateSerializable(attribute, attrListSynt);
                        } else if ("FileIOPermissionAttribute" == attribute.Name.ToString()) {
                           iTypeAttribute = CreateFileIOPermissionAttribute(attribute, attrListSynt);
                        }
                        if (null != iTypeAttribute) {
                           attributes.Add(iTypeAttribute);
                           if (!all) {
                              return attributes;
                           }
                        }
                     }
                  }
               }
            }
            return attributes;
      }


      public static PartCreationPolicy CreateCSharpPartCreationPolicy(Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax attribute,
         Microsoft.CodeAnalysis.CSharp.Syntax.AttributeListSyntax attrListSynt) {

         Dictionary<string, SyntaxNode> arguments = new Dictionary<string, SyntaxNode>();
         if (attribute.ArgumentList.Arguments.Any()) {
            foreach (var argument in attribute.ArgumentList.Arguments) {
               arguments[argument.ToFullString()] = argument;
            }
         }

         if (arguments.Any()) {
            return new PartCreationPolicy(arguments);
         }
         return null;
      }



      public static Serializable CreateSerializable(AttributeSyntax attribute, AttributeListSyntax attrListSynt) {

         return new Serializable(new Dictionary<string, SyntaxNode>());

      }

      public static FileIOPermissionAttribute CreateFileIOPermissionAttribute(AttributeSyntax attribute, AttributeListSyntax attrListSynt) {
         return new FileIOPermissionAttribute(new Dictionary<string, SyntaxNode>());
      }

      public static Export CreateCSharpExport(Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax attribute,
         Microsoft.CodeAnalysis.CSharp.Syntax.AttributeListSyntax attrListSynt,
         Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax typeDeclaration) {

         Dictionary<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeListSyntax, List<string>> contractTypes =
            new Dictionary<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeListSyntax, List<string>>();

         if (attribute.ArgumentList.Arguments.Any()) {
            contractTypes[attrListSynt] = new List<string>();
            foreach (var argument in attribute.ArgumentList.Arguments) {
               var typeOfExpressionSyntax = argument.Expression as Microsoft.CodeAnalysis.CSharp.Syntax.TypeOfExpressionSyntax;
               if (typeOfExpressionSyntax != null) {
                  contractTypes[attrListSynt].Add(typeOfExpressionSyntax.Type.ToString());
                  break;
               }
            }
         }

         Dictionary<string, SyntaxNode> arguments = new Dictionary<string, SyntaxNode>();
         // if there is contract type then check the match with the class declaration
         if (contractTypes.Any()) {
            Dictionary<string, SyntaxNode> comparisons = new Dictionary<string, SyntaxNode>(); // { { className, typeDeclaration } };
            if (typeDeclaration.BaseList != null && typeDeclaration.BaseList.Types != null) {
               foreach (var baseTypeSyntax in typeDeclaration.BaseList.Types) {
                  comparisons[baseTypeSyntax.Type.ToString()] = baseTypeSyntax.Type;
               }
            }
            foreach (var item in contractTypes) {
               foreach (string contractType in item.Value) {
                  SyntaxNode typeSyntax = null;
                  comparisons.TryGetValue(contractType, out typeSyntax);
                  arguments[contractType] = typeSyntax;
               }
            }
         }
         if (arguments.Any()) {
            return new Export(arguments);
         }
         return null;
      }

   }
}
