// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ScriptMessageFormatter.cs" company="Database Deploy 2">
//    Copyright (c) 2015 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace DatabaseDeploy.Core.Utilities
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    ///     Takes an IDictionary and makes a pretty script message
    /// </summary>
    public class ScriptMessageFormatter : IScriptMessageFormatter
    {
        /// <summary>
        ///     Formats a collection of values into a pretty string.
        ///     If the values are all integers, the string will be very pretty.
        ///     If the values have decimals, the string will be a straight concatentaion with commas.
        /// </summary>
        /// <param name="values">The values to format</param>
        /// <returns>A string containing the pretty values (for example "1 to 5, 10 to 15, 20, 40, 60" for integers, "1.1, 1.2, 1.7" for decimals)</returns>
        public string FormatCollection(ICollection<decimal> values)
        {
            if (values == null || !values.Any())
            {
                return "No scripts found.";
            }

            return this.AllValuesAreIntegers(values)
                ? this.FormatCollection(values.Select(Convert.ToInt32).ToArray())
                : string.Join(",", values);
        }

        private string FormatCollection(ICollection<int> values)
        {
            string result;

            if (values != null && values.Any())
            {
                StringBuilder textString = new StringBuilder();
                int lastNumber = -1;
                int rangeStart = -1;

                IOrderedEnumerable<int> orderedValues = values.OrderBy(x => x);

                foreach (int value in orderedValues)
                {
                    if (lastNumber == -1)
                    {
                        lastNumber = value;
                        rangeStart = value;
                    }
                    else if (value == lastNumber + 1)
                    {
                        lastNumber = value;
                    }
                    else
                    {
                        this.AppendRange(textString, lastNumber, rangeStart);
                        lastNumber = value;
                        rangeStart = value;
                    }
                }

                this.AppendRange(textString, lastNumber, rangeStart);
                result = textString.ToString().Trim().TrimEnd(',');
            }
            else
            {
                result = "No scripts found.";
            }

            return result;
        }

        private bool AllValuesAreIntegers(ICollection<decimal> values)
        {
            return values.All(this.IsInteger);
        }

        private bool IsInteger(decimal value)
        {
            return value == Math.Truncate(value);
        }

        /// <summary>
        ///     Appends a range of values to the stringbuilder
        /// </summary>
        /// <param name="textString">The stringbuilder</param>
        /// <param name="lastNumber">The last number that was found.</param>
        /// <param name="rangeStart">The start of the range.</param>
        private void AppendRange(StringBuilder textString, int lastNumber, int rangeStart)
        {
            if (lastNumber == rangeStart)
            {
                textString.Append(lastNumber);
                textString.Append(", ");
            }
            else if (rangeStart + 1 == lastNumber)
            {
                textString.Append(rangeStart);
                textString.Append(", ");
                textString.Append(lastNumber);
                textString.Append(", ");
            }
            else
            {
                textString.Append(rangeStart);
                textString.Append(" to ");
                textString.Append(lastNumber);
                textString.Append(", ");
            }
        }
    }
}