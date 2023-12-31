﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CastDotNetExtension.Utils {
   using AttributeArgumentType = Dictionary<string, SyntaxNode>;

   internal class TypeAttributes {

      public enum AttributeType {
         None,
         Export,
         PartCreationPolicy,
         Serializable,
         FileIOPermissionAttribute
      }
      public interface ITypeAttribute {

         AttributeType Name { get; }
         //For later: Dictionary<string, SyntaxNode> Arguments { get; }
      }

      public abstract class TypeAttribute : ITypeAttribute {
         public AttributeType Name { get; private set; }
         public Dictionary<string, SyntaxNode> Arguments { get; private set; }

         protected TypeAttribute(AttributeType type, Dictionary<string, SyntaxNode> arguments) {
            Name = type;
            Arguments = arguments;
         }
      }

      public class Export : TypeAttribute {
         public Export(Dictionary<string, SyntaxNode> arguments) :
            base(AttributeType.Export, arguments) {
         }
      }

      public class PartCreationPolicy : TypeAttribute {
         public PartCreationPolicy(Dictionary<string, SyntaxNode> arguments) :
            base(AttributeType.PartCreationPolicy, arguments) {
         }
      }

      public class Serializable : TypeAttribute
      {
         public Serializable(Dictionary<string, SyntaxNode> arguments) :
            base(AttributeType.Serializable, arguments)
         {
         }
      }

      public class FileIOPermissionAttribute : TypeAttribute
      {
         public FileIOPermissionAttribute(Dictionary<string, SyntaxNode> arguments) :
            base(AttributeType.FileIOPermissionAttribute, arguments)
         {
         }
      }

      public class TypeAttributesAll {
         public string ForType { get; private set; }

         public SyntaxNode SyntaxNode { get; private set; }

         public IList<ITypeAttribute> Attributes { get; private set; }

         internal TypeAttributesAll(string forType, SyntaxNode syntaxNode, IList<ITypeAttribute> attributes) {
            ForType = forType;
            SyntaxNode = syntaxNode;
            Attributes = attributes;
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
         var declarationSyntax = context.Node as TypeDeclarationSyntax;

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
            var syntax = syntaxRef.GetSyntax();
            if (syntax is TypeDeclarationSyntax) {
               var typeDeclarationSyntax = syntax as TypeDeclarationSyntax;
               attributes = Get(typeDeclarationSyntax, attributes, attrs2Search);
            }
         }

         return attributes;
      }

      public static IList<ITypeAttribute> Get(TypeDeclarationSyntax typeDeclaration,
         IList<ITypeAttribute> attributes, AttributeType[] attrs2Search = null) {

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
                  attributes = Get(syntax.AttributeLists, attributes, attrs2Search, declarationSyntax);
               }
            }
         }
         return attributes;
      }

      public static IList<ITypeAttribute> Get(SyntaxList<AttributeListSyntax> attributeLists,
         IList<ITypeAttribute> attributes, AttributeType[] attrs2Search = null, object declarationSyntax = null) {

            if (null != attributeLists && attributeLists.Any()) {
               bool all = null == attrs2Search;
               foreach (AttributeListSyntax attrListSynt in attributeLists) {
                  foreach (AttributeSyntax attribute in attrListSynt.Attributes) {
                     ITypeAttribute iTypeAttribute = null;
                     if (all || HasAttributeType(attrs2Search, attribute.Name.ToString()))
                     {
                        switch (attribute.Name.ToString())
                        {
                           case "Export":
                              iTypeAttribute = CreateCSharpExport(attribute, attrListSynt, declarationSyntax as TypeDeclarationSyntax);
                              break;
                           case "PartCreationPolicy":
                              iTypeAttribute = CreateCSharpPartCreationPolicy(attribute, attrListSynt);
                              break;
                           case "Serializable":
                              iTypeAttribute = CreateSerializable(attribute, attrListSynt);
                              break;
                           case "FileIOPermissionAttribute":
                              iTypeAttribute = CreateFileIOPermissionAttribute(attribute, attrListSynt);
                              break;
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


      public static PartCreationPolicy CreateCSharpPartCreationPolicy(AttributeSyntax attribute,
         AttributeListSyntax attrListSynt) {

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

      public static Export CreateCSharpExport(AttributeSyntax attribute,
         AttributeListSyntax attrListSynt,
         TypeDeclarationSyntax typeDeclaration) {

         Dictionary<AttributeListSyntax, List<string>> contractTypes =
            new Dictionary<AttributeListSyntax, List<string>>();

         if (attribute.ArgumentList.Arguments.Any()) {
            contractTypes[attrListSynt] = new List<string>();
            foreach (var argument in attribute.ArgumentList.Arguments) {
               var typeOfExpressionSyntax = argument.Expression as TypeOfExpressionSyntax;
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
