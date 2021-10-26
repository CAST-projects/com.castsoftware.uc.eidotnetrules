using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.DotNet.CastDotNetExtension;
using Roslyn.DotNet.Common;

namespace CastDotNetExtension 
{
   [CastRuleChecker]
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   [RuleDescription(
       Id = "EI_ChildClassFieldsShouldNotShadowParentClassFields",
       Title = "Child Class Fields Should Not Shadow Parent Class Fields",
       MessageFormat = "Child Class Fields Should Not Shadow Parent Class Fields",
       Category = "Programming Practices - OO Inheritance and Polymorphism",
       DefaultSeverity = DiagnosticSeverity.Warning,
       CastProperty = "EIDotNetQualityRules.ChildClassFieldsShouldNotShadowParentClassFields"
   )]
   public class ChildClassFieldsShouldNotShadowParentClassFields : AbstractRuleChecker 
   {
      public ChildClassFieldsShouldNotShadowParentClassFields()
            : base(ViolationCreationMode.ViolationWithAdditionalBookmarks)
        {
        }

      /// <summary>
      /// Initialize the QR with the given context and register all the syntax nodes
      /// to listen during the visit and provide a specific callback for each one
      /// </summary>
      /// <param name="context"></param>
      public override void Init(AnalysisContext context) 
      {
         context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
      }

      //private readonly object _lock = new object();

      //private void ProcessField(ISymbol field, Dictionary<string, ISymbol> fields, bool isTargetClass) {
      //   string fieldName = field.Name.ToLowerInvariant();
      //   if (isTargetClass) {
      //      fields[fieldName] = field;
      //   } else if (fields.ContainsKey(fieldName)) {
      //      var fieldSymbol = fields[fieldName];
      //      var mainPos = fieldSymbol.Locations.FirstOrDefault().GetMappedLineSpan();
      //      var additionalPos = field.Locations.FirstOrDefault().GetMappedLineSpan();
      //      AddViolation(fieldSymbol, new List<FileLinePositionSpan> { mainPos, additionalPos });
      //   }

      //}

      //private bool ProcessAssociatedProperty(IFieldSymbol fieldMaybeProp, Dictionary<string, ISymbol> fields, bool isTargetClass) {
      //   if (null != fieldMaybeProp.AssociatedSymbol && SymbolKind.Property == fieldMaybeProp.AssociatedSymbol.Kind) {
      //      var prop = fieldMaybeProp.AssociatedSymbol as IPropertySymbol;
      //      if (prop.IsAbstract || prop.IsVirtual || prop.IsOverride) {
      //         return true;
      //      }
      //      var propDeclSyntax = prop.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax();
      //      if (propDeclSyntax is PropertyDeclarationSyntax) {
      //         bool isNewed = (propDeclSyntax as PropertyDeclarationSyntax).Modifiers.
      //            FirstOrDefault(t => t.IsKind(SyntaxKind.NewKeyword)).
      //            //just silly. First will throw if not found. FirstOrDefault will return SyntaxKind.None.
      //            IsKind(SyntaxKind.NewKeyword);
      //         if (isNewed) {
      //            return true;
      //         }
      //      }
      //      ProcessField(fieldMaybeProp.AssociatedSymbol, fields, isTargetClass);
      //      return true;
      //   }
      //   return false;
      //}

      //private void AnalyzeClass(SymbolAnalysisContext context) {

      //   /*lock (_lock)*/ {
      //      try {
      //         var klazz = context.Symbol as INamedTypeSymbol;
      //         if (null != klazz && TypeKind.Class == klazz.TypeKind) {
      //            Dictionary<string, ISymbol> fields = new Dictionary<string, ISymbol>();
      //            bool isTargetClass = true;
      //            do {
      //               foreach (var field in klazz.GetMembers().OfType<IFieldSymbol>()) {
      //                  if (!ProcessAssociatedProperty(field, fields, isTargetClass) ||
      //                     !field.IsImplicitlyDeclared) {
      //                     ProcessField(field, fields, isTargetClass);
      //                  }
      //               }
                     
      //               if (!fields.Any()) {
      //                  break;
      //               }
      //               isTargetClass = false;
      //               klazz = klazz.BaseType;

      //            } while (null != klazz && !klazz.ToString().Equals("object"));
      //         }

      //      }
      //      catch (Exception e) {
      //         HashSet<string> filePaths = new HashSet<string>();
      //         foreach (var synRef in context.Symbol.DeclaringSyntaxReferences) {
      //            filePaths.Add(synRef.SyntaxTree.FilePath);
      //         }
      //         Log.Warn(" Exception while analyzing " + string.Join(",", filePaths) + ": " + context.Symbol.Locations.FirstOrDefault().GetMappedLineSpan(), e);
      //      }
      //   }


    private void AnalyzeClass(SymbolAnalysisContext context)
    {
        var classSymbol = context.Symbol as INamedTypeSymbol;
        if(classSymbol == null)
            return;
        if (classSymbol.Kind != SymbolKind.NamedType)
            return;

        var parentClassSymbol = classSymbol.BaseType;
        if (parentClassSymbol == null || parentClassSymbol.SpecialType == SpecialType.System_Object)
            return; // no shadowing possible

            
        //=== gather information ===
        // get all declared fields of 'classSymbol' (and only in this class)
        var localFields = GetDeclaredMembers(classSymbol, false, true)
            .Where(f => !WithNewKeywordModifer(f)); // exclude case with 'new' keyword

        // get all declared fields of 'parentClassSymbol' and all it's ancestors - without private fields
        var ancestorsFields = GetDeclaredMembers(parentClassSymbol, true, false)
            // remove these 2 lines if you want a deep error check - actually, we remove ancestor fields shadowing
            .GroupBy(fd => fd.Name)
            .Select(grp => grp.First());

            
        //=== comparison ===
        // extract shadowing list
        var shadowedFields = localFields
            .Join(ancestorsFields,
                f => f.Name.ToLower(), f => f.Name.ToLower(),
                (lf, af) => new {LocalField = lf, AncestorField = af});


        //=== reporting ===
        foreach (var shadowedField in shadowedFields)
        {
            // report infraction
            var location = shadowedField.LocalField.Locations.FirstOrDefault();
            if(location == null) // how can we report an unknown position ?
                continue;
            var localBookmark = location.GetMappedLineSpan();

            var bookmarks = new List<FileLinePositionSpan>() {localBookmark};
            var ancestorLocation = shadowedField.AncestorField.Locations.FirstOrDefault();
            if(ancestorLocation != null)
            {
                var ancestorBookmark = ancestorLocation.GetMappedLineSpan();
             //note : activate this block in order to add the overriden field
               
                bookmarks.Add(ancestorBookmark);
            }
            AddViolation(shadowedField.LocalField, bookmarks);
        }
    }

    private static IEnumerable<ISymbol> GetDeclaredMembers(ITypeSymbol classSymbol, bool throughAncestors, bool acceptPrivate)
    {
        //=== extract 'classSymbol' members ===
        var result = classSymbol.GetMembers()
            .Where(m => m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property)
            .Where(m =>
                !m.IsImplicitlyDeclared && // do not take field created by compiler
                m.IsDefinition &&  // must be declared in this class
                !m.IsOverride &&   // not declared as override
                (acceptPrivate || m.DeclaredAccessibility != Accessibility.Private));

        if (!throughAncestors)
            return result;


        //=== extract 'classSymbol' ancestors members ===
        var baseClassSymbol = classSymbol.BaseType;
        if (baseClassSymbol == null || baseClassSymbol.SpecialType == SpecialType.System_Object)
            return result;

        // we make recursive calls, but should not going so far as we go through inheritance type
        var ancestorsResult = GetDeclaredMembers(baseClassSymbol, true, acceptPrivate);
        result = result.Concat(ancestorsResult);

        return result;
    }

    private static bool WithNewKeywordModifer(ISymbol member)
    {
        // special case of 'new' keyword modifer accepted
        var declarationNode = GetMemberDeclarationExpression(member);
        if(declarationNode == null)
            return false;
        var localFieldModifiers = declarationNode.Modifiers;

        return localFieldModifiers != null && localFieldModifiers.Any(t => t.IsKind(SyntaxKind.NewKeyword));
    }

    private static MemberDeclarationSyntax GetMemberDeclarationExpression(ISymbol symbol)
    {
        var syntaxRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
        if(syntaxRef == null)
            return null;
        var node = syntaxRef.GetSyntax();
        if (node == null)
            return null;

        var result = node as MemberDeclarationSyntax;
        if (result != null)
            return result;

        // special for FieldDeclaration
        var variableDeclarator = node as VariableDeclaratorSyntax;
        if(variableDeclarator != null)
        {
            var variableDeclaraton = variableDeclarator.Parent as VariableDeclarationSyntax;
            if(variableDeclaraton != null)
            {
                var fieldDeclaration = variableDeclaraton.Parent as FieldDeclarationSyntax;
                if (fieldDeclaration != null)
                    return fieldDeclaration;
            }
        }
            
        return null;
    }


    }
}

