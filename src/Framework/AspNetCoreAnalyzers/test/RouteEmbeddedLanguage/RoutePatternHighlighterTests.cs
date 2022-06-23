// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

public partial class RoutePatternHighlighterTests
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new RoutePatternAnalyzer());

    [Fact]
    public async Task AfterLiteral_NoHighlight()
    {
        // Arrange & Act & Assert
        await TestHighlightingAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""hi$$"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");
    }

    [Fact]
    public async Task AfterParameterStart_NoHighlight()
    {
        // Arrange & Act & Assert
        await TestHighlightingAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{$$"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");
    }

    [Fact]
    public async Task BeforeParameterName_CompleteParameter_HighlightName()
    {
        // Arrange & Act & Assert
        await TestHighlightingAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{$$[|hi|]}"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");
    }

    [Fact]
    public async Task BeforeParameterName_ParameterWithConstraint_HighlightName()
    {
        // Arrange & Act & Assert
        await TestHighlightingAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{$$[|hi|]:int"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");
    }

    [Fact]
    public async Task MiddleParameterName_CompleteParameter_HighlightName()
    {
        // Arrange & Act & Assert
        await TestHighlightingAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{[|h$$i|]}"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");
    }

    [Fact]
    public async Task MiddleConstraint_ParameterWithConstraint_NoHighlight()
    {
        // Arrange & Act & Assert
        await TestHighlightingAsync(@"
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main()
    {
        M(@""{hi:i$$nt"");
    }

    static void M([StringSyntax(""Route"")] string p)
    {
    }
}
");
    }

    [Fact]
    public async Task InParameterName_MatchingDelegate_NoHighlight()
    {
        // Arrange & Act & Assert
        await TestHighlightingAsync(@"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{$$[|id|]}"", ExecuteGet);
    }

    static string ExecuteGet(string [|id|])
    {
        return $""{[|id|]}"";
    }
}
");
    }

    [Fact]
    public async Task InParameterName_MatchingAction_HighlightParameter()
    {
        // Arrange & Act & Assert
        await TestHighlightingAsync(@"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

class Program
{
    static void Main()
    {
    }
}

public class TestController
{
    [HttpGet(@""{$$[|id|]}"")]
    public object TestAction(int [|id|])
    {
        return null;
    }
}
");
    }

    [Fact]
    public async Task InParameterName_MatchingDelegateWithConflictingIdentifer_DontHighlightConflict()
    {
        // Arrange & Act & Assert
        await TestHighlightingAsync(@"
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main()
    {
        EndpointRouteBuilderExtensions.MapGet(null, @""{$$[|id|]}"", ExecuteGet);
    }

    static string ExecuteGet(string [|id|])
    {
        [|id|] = TestEnum.id.ToString();
        return $""{[|id|]}"";
    }

    enum TestEnum
    {
        id;
    }
}
");
    }

    private async Task TestHighlightingAsync(string source)
    {
        MarkupTestFile.GetPositionAndSpans(source, out var output, out int cursorPosition, out var spans);

        var tempSpans = spans.ToList();
        var highlightSpans = await Runner.GetHighlightingAsync(cursorPosition, output);
        foreach (var span in highlightSpans)
        {
            if (!tempSpans.Remove(span.TextSpan))
            {
                throw new Exception($"Couldn't find {span.TextSpan} in highlight results.");
            }
        }

        if (tempSpans.Count > 0)
        {
            throw new Exception($"Unmatched highlight spans in document: {string.Join(", ", tempSpans)}");
        }
    }
}
