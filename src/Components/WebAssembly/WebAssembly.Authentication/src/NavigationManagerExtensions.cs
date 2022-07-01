// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Extensions for <see cref="NavigationManager"/>.
/// </summary>
public static class NavigationManagerExtensions
{
    internal const string LogoutNavigationState = "Logout";

    /// <summary>
    /// Initiates a logout operation by navigating to the log out endpoint.
    /// </summary>
    /// <remarks>
    /// The navigation includes stated that is added to the browser history entry to
    /// prevent logout operations performed from different contexts.
    /// </remarks>
    /// <param name="manager">The <see cref="NavigationManager"/>.</param>
    /// <param name="logoutPath">The path to navigate too.</param>
    public static void NavigateToLogout(this NavigationManager manager, string logoutPath)
    {
        manager.NavigateTo(logoutPath, new NavigationOptions
        {
            State = LogoutNavigationState
        });
    }
}
