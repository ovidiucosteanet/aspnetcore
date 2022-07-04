// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

// REVIEW: Should this be public API?
internal class ControllerRouteHandlerInvocationContext : RouteHandlerInvocationContext
{
    public ControllerRouteHandlerInvocationContext(HttpContext httpContext, ObjectMethodExecutor executor, object controller, object?[]? arguments)
    {
        HttpContext = httpContext;
        Executor = executor;
        Controller = controller;
        Arguments = arguments ?? Array.Empty<object?>();
    }

    public object Controller { get; }

    internal ObjectMethodExecutor Executor { get; }

    public override HttpContext HttpContext { get; }

    public override IList<object?> Arguments { get; }

    public override T GetArgument<T>(int index)
    {
        return (T)Arguments[index]!;
    }
}
