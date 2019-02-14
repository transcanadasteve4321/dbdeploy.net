// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="IScriptMessageFormatter.cs" company="Database Deploy 2">
//    Copyright (c) 2015 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DatabaseDeploy.Core.Utilities
{
    using System.Collections.Generic;

    /// <summary>
    ///     Takes an IDictionary and parses it into a pretty format
    /// </summary>
    public interface IScriptMessageFormatter
    {
        /// <summary>
        ///     Formats a collection of values into a pretty string.
        ///     If the values are all integers, the string will be very pretty.
        ///     If the values have decimals, the string will be a straight concatentaion with commas.
        /// </summary>
        /// <param name="values">The values to format</param>
        /// <returns>A string containing the pretty values (for example "1 to 5, 10 to 15, 20, 40, 60" for integers, "1.1, 1.2, 1.7" for decimals)</returns>
        string FormatCollection(ICollection<decimal> values);
    }
}