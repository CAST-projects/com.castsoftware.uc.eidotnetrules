﻿using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CastDotNetExtension.Utils {
   class SyntaxNode2SubjectName {


      private static readonly HashSet<string> CSharpTypes = new HashSet<string> {
                  "ConstructorDeclarationSyntax",
                  "MethodDeclarationSyntax",
                  "ClassDeclarationSyntax",
                  "PropertyDeclarationSyntax",
                  "FieldDeclarationSyntax",
                  "VariableDeclarationSyntax",
                  "LambdaExpressionSyntax",
                  "ParenthesizedLambdaExpressionSyntax",
                  "CompilationUnitSyntax",
                  "AssignmentExpressionSyntax",
               };


      private static SyntaxNode getParentNode(SyntaxNode node, HashSet<string> types, Func<SyntaxNode, bool> typeHandler = null) {
         if (null != node) {
            var parent = node.Parent;
            while (null != parent && !types.Contains(parent.GetType().Name)) {
               if (null != typeHandler && !typeHandler(parent)) {
                  break;
               }
               parent = parent.Parent;
            }

            if (null != typeHandler) {
               typeHandler(parent);
            }
            return parent;
         }
         return null;
      }

      public static String getCSharp(SyntaxNode node, Func<SyntaxNode, bool> typeHandler = null) {

         string name = null;
         try {

            node = getParentNode(node, CSharpTypes, typeHandler);
            if (null != node) {
               var type = node.GetType();
               switch (type.Name) {
                  case "FieldDeclarationSyntax": {
                        var fieldDeclarationSyntax = node as Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax;
                        if (null != fieldDeclarationSyntax) {
                           var varNode = fieldDeclarationSyntax.Declaration.Variables.FirstOrDefault();
                           if (null != varNode) {
                              name = varNode.Identifier.ToString();
                           }
                        }
                        break;
                     }
                  case "VariableDeclarationSyntax": {
                        var variableDeclarationSyntax = node as Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclarationSyntax;
                        var varNode = variableDeclarationSyntax.Variables.FirstOrDefault();
                        if (null != varNode) {
                           name = varNode.Identifier.ToString();
                        }
                        break;
                     }
                  case "AssignmentExpressionSyntax": {
                        var assignmentExpr = node as Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax;
                        name = assignmentExpr.Left.ToString();
                        break;
                     }
                  case "MethodDeclarationSyntax":
                  case "ClassDeclarationSyntax":
                  case "PropertyDeclarationSyntax": {
                        var prop = type.GetProperty("Identifier");
                        var propval = prop.GetMethod.Invoke(node, null);
                        name = propval.ToString();
                        break;
                     }
                  case "LambdaExpressionSyntax": {
                        var lambda = node as Microsoft.CodeAnalysis.CSharp.Syntax.LambdaExpressionSyntax;
                        name = "<lambda>";
                        break;
                     }

               }
            }
         }
         catch (Exception e) {
            System.Console.WriteLine(e.Message);
         }
         return name;
      }


      public static String get(SyntaxNode node, Func<SyntaxNode, bool> typeHandler = null) {
         return getCSharp(node, typeHandler);
      }
   }
}
