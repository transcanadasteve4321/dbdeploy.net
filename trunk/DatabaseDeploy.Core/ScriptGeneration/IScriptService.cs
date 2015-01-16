// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IScriptService.cs" company="Database Deploy 2">
//    Copyright (c) 2015 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DatabaseDeploy.Core.ScriptGeneration
{
    using System.Collections.Generic;

    /// <summary>
    ///     Represents a service for building scripts.
    /// </summary>
    public interface IScriptService
    {
        /// <summary>
        ///     Builds a change script from the given configuration.
        /// </summary>
        /// <param name="changes">The changes that are to be included.</param>
        /// <param name="currentDbVersion"></param>
        /// <returns>A string containing the script to be run.</returns>
        string BuildChangeScript(IDictionary<decimal, IScriptFile> changes, decimal currentDbVersion);

        /// <summary>
        ///     Builds an undo script from the changes given. This is not currently used.
        /// </summary>
        /// <param name="changes">The changes that are to be used to build an undo script.</param>
        /// <returns>A string containing the undo script.</returns>
        string BuildUndoScript(IDictionary<decimal, IScriptFile> changes);
    }
}