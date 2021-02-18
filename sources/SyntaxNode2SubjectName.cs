using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CastDotNetExtension.Utils {
   internal class SyntaxNode2SubjectName {



      private static readonly HashSet<SyntaxKind> Kinds = new HashSet<SyntaxKind> {
                  SyntaxKind.ConstructorDeclaration,
                  SyntaxKind.MethodDeclaration,
                  SyntaxKind.ClassDeclaration,
                  SyntaxKind.PropertyDeclaration,
                  SyntaxKind.FieldDeclaration,
                  SyntaxKind.VariableDeclaration,
                  SyntaxKind.SimpleLambdaExpression,
                  SyntaxKind.ParenthesizedLambdaExpression,
                  SyntaxKind.CompilationUnit,
                  SyntaxKind.SimpleAssignmentExpression,
               };



      private SyntaxNode2SubjectName()
      {

      }

      private static SyntaxNode GetParentNode(SyntaxNode node, Func<SyntaxNode, bool> typeHandler = null) {
         if (null != node) {
            SyntaxNode parent = node;
            do {
               parent = parent.Parent;

               if (null != parent && null != typeHandler && !typeHandler(parent)) {
                  break;
               }

            } while (null != parent && !Kinds.Contains(parent.Kind()));

            return parent;
         }

         return null;
      }

      public static string GetCSharp(SyntaxNode node, Func<SyntaxNode, bool> typeHandler = null) {

         string name = null;
         node = GetParentNode(node, typeHandler);
         if (null != node) {
            var kind = node.Kind();
            switch (kind) {
               case SyntaxKind.FieldDeclaration: {
                     var fieldDeclarationSyntax = node as Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax;
                     if (null != fieldDeclarationSyntax) {
                        var varNode = fieldDeclarationSyntax.Declaration.Variables.FirstOrDefault();
                        if (null != varNode) {
                           name = varNode.Identifier.ToString();
                        }
                     }
                     break;
                  }
               case SyntaxKind.VariableDeclaration: {
                     var variableDeclarationSyntax = node as Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclarationSyntax;
                     var varNode = variableDeclarationSyntax.Variables.FirstOrDefault();
                     if (null != varNode) {
                        name = varNode.Identifier.ToString();
                     }
                     break;
                  }
               case SyntaxKind.SimpleAssignmentExpression: {
                     var assignmentExpr = node as Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax;
                     name = assignmentExpr.Left.ToString();
                     break;
                  }
               case SyntaxKind.MethodDeclaration:
               case SyntaxKind.ClassDeclaration:
               case SyntaxKind.PropertyDeclaration: {
                     var type = node.GetType();
                     var prop = type.GetProperty("Identifier");
                     var propval = prop.GetMethod.Invoke(node, null);
                     name = propval.ToString();
                     break;
                  }
               case SyntaxKind.ParenthesizedLambdaExpression:
               case SyntaxKind.SimpleLambdaExpression: {
                     name = "<lambda>";
                     break;
                  }

            }
         }
         return name;
      }


      public static string Get(SyntaxNode node, Func<SyntaxNode, bool> typeHandler = null) {
         return GetCSharp(node, typeHandler);
      }
   }
}
