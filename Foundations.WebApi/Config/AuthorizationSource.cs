// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AuthorizationSource.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    /// <summary>
    ///     Set of all possible authorization sources.
    /// </summary>
    public enum AuthorizationSource
    {
        Header = 0,
        Form,
        QueryString
    }
}
