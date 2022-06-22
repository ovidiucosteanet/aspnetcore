// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;

[ExportAspNetCoreEmbeddedLanguageDocumentHighlighter(name: "Route", language: LanguageNames.CSharp)]
internal class RoutePatternHighlighter : IAspNetCoreEmbeddedLanguageDocumentHighlighter
{
    // Regex example:
    // new System.Text.RegularExpressions.Regex(@"(?:a(?<digit>[0-5])|b(?<digit>[4-7]))c\k<digit>");

    public ImmutableArray<AspNetCoreDocumentHighlights> GetDocumentHighlights(
        SemanticModel semanticModel, SyntaxToken token, int position, CancellationToken cancellationToken)
    {
        var virtualChars = AspNetCoreCSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
        var tree = RoutePatternParser.TryParse(virtualChars);
        if (tree == null)
        {
            return ImmutableArray<AspNetCoreDocumentHighlights>.Empty;
        }

        return GetHighlights(tree, semanticModel, token, position, cancellationToken);
    }

    private static ImmutableArray<AspNetCoreDocumentHighlights> GetHighlights(
        RoutePatternTree tree, SemanticModel semanticModel, SyntaxToken syntaxToken, int position, CancellationToken cancellationToken)
    {
        var virtualChar = tree.Text.Find(position);
        if (virtualChar == null)
        {
            return ImmutableArray<AspNetCoreDocumentHighlights>.Empty;
        }

        var node = FindParameterNode(tree.Root, virtualChar.Value);
        if (node == null)
        {
            return ImmutableArray<AspNetCoreDocumentHighlights>.Empty;
        }

        var highlightSpans = new List<AspNetCoreHighlightSpan>();
        highlightSpans.Add(new AspNetCoreHighlightSpan(node.GetSpan(), AspNetCoreHighlightSpanKind.Reference));

        var (method, _) = EndpointMethodDetector.FindEndpointMethod(syntaxToken, semanticModel, cancellationToken);
        if (method != null)
        {
            var resolvedParameterSymbols = new List<ISymbol>();
            var childSymbols = method switch
            {
                ITypeSymbol typeSymbol => typeSymbol.GetMembers().OfType<IPropertySymbol>().ToImmutableArray().As<ISymbol>(),
                IMethodSymbol methodSymbol => methodSymbol.Parameters.As<ISymbol>(),
                _ => throw new InvalidOperationException("Unexpected symbol type: " + method)
            };

            var parameterName = node.ParameterNameToken.Value.ToString();
            var matchingParameter = childSymbols.FirstOrDefault(s => s.Name == parameterName)
                ?? childSymbols.FirstOrDefault(s => string.Equals(s.Name, parameterName, StringComparison.OrdinalIgnoreCase));

            if (matchingParameter != null)
            {
                foreach (var item in matchingParameter.DeclaringSyntaxReferences)
                {
                    var syntaxNode = item.GetSyntax(cancellationToken);
                    if (syntaxNode is ParameterSyntax parameterSyntax)
                    {
                        highlightSpans.Add(new AspNetCoreHighlightSpan(parameterSyntax.Identifier.Span, AspNetCoreHighlightSpanKind.Definition));
                    }
                }

                foreach (var item in method.DeclaringSyntaxReferences)
                {
                    var methodSyntax = item.GetSyntax(cancellationToken);

                    var parameterReferences = methodSyntax
                        .DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(i => i.Identifier.Text == matchingParameter.Name)
                        .Where(i => SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(i).Symbol, matchingParameter));
                    foreach (var reference in parameterReferences)
                    {
                        highlightSpans.Add(new AspNetCoreHighlightSpan(reference.Identifier.Span, AspNetCoreHighlightSpanKind.Definition));
                    }
                }
            }
        }

        return ImmutableArray.Create(new AspNetCoreDocumentHighlights(highlightSpans.ToImmutableArray()));
    }

    private static RoutePatternNameParameterPartNode? FindParameterNode(RoutePatternNode node, AspNetCoreVirtualChar ch)
        => FindNode<RoutePatternNameParameterPartNode>(node, ch, (parameter, c) => parameter.ParameterNameToken.VirtualChars.Contains(c));

    private static TNode? FindNode<TNode>(RoutePatternNode node, AspNetCoreVirtualChar ch, Func<TNode, AspNetCoreVirtualChar, bool> predicate)
        where TNode : RoutePatternNode
    {
        if (node is TNode nodeMatch && predicate(nodeMatch, ch))
        {
            return nodeMatch;
        }

        foreach (var child in node)
        {
            if (child.IsNode)
            {
                var result = FindNode(child.Node, ch, predicate);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }
}
