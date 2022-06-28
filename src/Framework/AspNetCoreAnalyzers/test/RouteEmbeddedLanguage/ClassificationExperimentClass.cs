// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

internal class ClassificationExperimentClass
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="param"></param>
    public static void Test(string param)
    {
        Regex.IsMatch("", "rege(x(es)?|xps?)");
        Regex.IsMatch("", @"[2-9]|[12]\d|3[0-6]");
        Regex.IsMatch("", "mi.....ft");
        Regex.IsMatch("", @"\d{5}(-\d{4})?");
        Regex.IsMatch("", @"(?<name>group)");
        Regex.IsMatch("", @"$^($(?# comment ))^");

        // {, } = blue
        // [, ] = blue
        // ?, *, **, :, (, ) = pink
        // constraint = green
        // default value = default
        // constraint argument = default
        Helper.MapRoute("foo/{*path:regex:alpha:int}"); // catch all
        Helper.MapRoute("foo/{**path}"); // catch all without escape
        Helper.MapRoute("{controller}/{action}/{id?}"); // optional segment
        Helper.MapRoute("files/{filename}.{ext?}"); // optional segment after .
        Helper.MapRoute("{controller=Home}/{**action=Index}"); // default values
        Helper.MapRoute("[controller]/[action]"); // token replacement
        Helper.MapRoute("a{b}c{d}"); // complex segment
        Helper.MapRoute("{id:int}"); // route constraint
        Helper.MapRoute("{username:minlength(4)}"); // route constraint with argument
        Helper.MapRoute("{filename:length(8,16)}"); // route constraint with multiple arguments
        Helper.MapRoute("{ssn:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)}"); // route constraint with string argument
        Helper.MapRoute("{id:int:min(1)}"); // multiple route constraints
        EndpointRouteBuilderExtensions.MapGet(endpoints: null,
            "{id}/{cancellationToken:min(5):int:}",
            handler: (string id, CancellationToken cancellationToken) => ""); // multiple route constraints
        EndpointRouteBuilderExtensions.MapGet(endpoints: null,
            "{id}/{cancellationToken:min(5):int:}",
            handler: ([AsParameters] PageData page) => "");
        EndpointRouteBuilderExtensions.MapGet(endpoints: null, "{id}", requestDelegate: context => Task.CompletedTask);

        IEndpointRouteBuilder b = null!;
        var g = b.MapGroup("{sdf}");
        g.MapGet("{p}", (int p) => "");
        g.MapControllerRoute("Default", "{pie}");
        g.MapMethods("{pie}", new string[] { "" }, () => string.Empty);

        Helper.MapRoute("/users/{userId}/books/{bookId?}",
            (int userId, int bookId) => $"The user id is {userId} and book id is {bookId}");
    }
}

public class PageData
{
    public int PageNumber { get; set; }
    public int PageIndex { get; set; }
}

public static class Helper
{
    public static void MapRoute([StringSyntax("Route")] string pattern)
    {
    }
    public static void MapRoute([StringSyntax("Route")] string pattern, Delegate d)
    {
    }
}

public class TestController
{
    [HttpGet("foo/{path:int}/{id}")]
    public object GetProduct(int id)
    {
        return "";
    }
}
