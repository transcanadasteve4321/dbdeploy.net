// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="LogUtility.cs" company="Database Deploy 2">
//    Copyright (c) 2015 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DatabaseDeploy.Core.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using log4net;

    /// <summary>
    ///     Represents a class for assisting with logging statements.
    /// </summary>
    /// <remarks>
    ///     Throughout this class, logging is NOT done as recommended in other classes to avoid potential recursion from
    ///     calling
    ///     this static class over and over.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public static class LogUtility
    {
        /// <summary>
        ///     Limits the maximum logging depth.  Basically, if you're deeper than this, you're probably recursing, so don't go
        ///     any deeper
        /// </summary>
        private const int MaxDepth = 50;

        /// <summary>
        ///     Creates the default logger
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(LogUtility));

        /// <summary>
        ///     A counter for preventing recursive depths in logging.
        /// </summary>
        private static int currentDepth;

        /// <summary>
        ///     Gets the context using a method base and the args passed to it.  Ideal for PostSharp
        /// </summary>
        /// <param name="currentMethod">The current method.</param>
        /// <param name="passedValues">The values passed.</param>
        /// <returns>a string containing the formatted text for the method</returns>
        public static string GetContext(MethodBase currentMethod, object[] passedValues)
        {
            ParameterInfo[] methodParameters = currentMethod.GetParameters();

            string result = BuildLoggingStatement(currentMethod, methodParameters, passedValues);

            return result;
        }

        /// <summary>
        ///     Gets the context for the current method call and translates it into a debug logging statement.
        /// </summary>
        /// <param name="passedValues">The parameters of the call.</param>
        /// <returns>A string containing the context string.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the number of parameters passed does not match the number of parameters
        ///     defined on the method.
        /// </exception>
        /// <remarks>
        ///     The parameters of the call are not available to reflection, so they must be passed in.  If the number of
        ///     parameters passed to GetContext doesn't match the number expected, and exception is thrown.
        /// </remarks>
        public static string GetContext(params object[] passedValues)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetContext called.");
            }

            string result = "Unable to build context.";

            currentDepth++;

            if (currentDepth > MaxDepth)
            {
                result = string.Format("Max logging depth of {0} reached.", MaxDepth);
            }
            else
            {
                try
                {
                    MethodBase currentMethod = GetCurrentMethod();

                    if (currentMethod != null)
                    {
                        ParameterInfo[] methodParameters = currentMethod.GetParameters();

                        ValidateParameters(currentMethod, methodParameters, passedValues);

                        result = BuildLoggingStatement(currentMethod, methodParameters, passedValues);
                    }
                }
                catch (ArgumentException argumentException)
                {
                    Log.Error(argumentException);
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to build logging statements.", ex);
                    result = string.Format("Unable to build logging statements.  Error = {0}", ex.Message);
                }
            }

            currentDepth--;

            if (Log.IsDebugEnabled)
            {
                Log.Debug(string.Format("GetContext finished.  Result={0}", result));
            }

            return result;
        }

        /// <summary>
        ///     Gets the value for an object.
        /// </summary>
        /// <param name="value">The value to get a string for.</param>
        /// <returns>A string representation of the object.</returns>
        public static string GetObjectValue(object value)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetObjectValue(object) called.");
            }

            return GetObjectValue(value, new Dictionary<object, string>(), new List<object>());
        }

        /// <summary>
        ///     Gets the result string message for result calls.
        /// </summary>
        /// <returns>A string containing the method get result string and the result value.</returns>
        public static string GetResult()
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetResult called.");
            }

            return GetResult(null);
        }

        /// <summary>
        ///     Gets the result string message for result calls.
        /// </summary>
        /// <param name="result">The result value to be added to the string.</param>
        /// <returns>A string containing the method get result string and the result value.</returns>
        public static string GetResult(object result)
        {
            MethodBase currentMethod = GetCurrentMethod();

            return GetResult(currentMethod, result);
        }

        /// <summary>
        ///     Gets the result string message for result calls.
        /// </summary>
        /// <param name="currentMethod">The current method to get a result for.</param>
        /// <param name="result">The result value to be added to the string.</param>
        /// <returns>A string containing the method get result string and the result value.</returns>
        public static string GetResult(MethodBase currentMethod, object result)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetResult(object) called.");
            }

            StringBuilder logResult = new StringBuilder();

            try
            {
                if (currentMethod != null)
                {
                    GetMethodDeclaration(logResult, currentMethod, currentMethod.GetParameters());
                    logResult.Append(" Completed.");
                    if (result != null)
                    {
                        logResult.Append("  Result=~|");
                        logResult.Append(GetObjectValue(result));
                        logResult.Append("|~");
                    }
                }
                else
                {
                    logResult.Append(
                        "Unable to get the method.  Check for LogUtility ERROR level messages to find out why.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unable to build logging results.", ex);
                logResult.AppendFormat("Unable to build logging results.  Error = {0}", ex.Message);
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug(string.Format("GetResult Completed.  Result={0}", logResult));
            }

            return logResult.ToString();
        }

        /// <summary>
        ///     Builds a logging statement from the given parameters and passed values.
        /// </summary>
        /// <param name="currentMethod">The current method that we're logging.</param>
        /// <param name="methodParameters">The parameters from the method</param>
        /// <param name="passedValues">The values that have been passed for logging.</param>
        /// <returns>A string containing the built up logging statement.</returns>
        private static string BuildLoggingStatement(
            MethodBase currentMethod,
            ParameterInfo[] methodParameters,
            object[] passedValues)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("BuildLoggingStatement Called.");
            }

            string result = string.Empty;
            StringBuilder loggingStatement = new StringBuilder();

            GetMethodDeclaration(loggingStatement, currentMethod, methodParameters);

            if (methodParameters.Count() > 0)
            {
                loggingStatement.Append(" --> ");
            }

            GetMethodParameterValues(loggingStatement, methodParameters, passedValues);

            result = loggingStatement.ToString();

            if (Log.IsDebugEnabled)
            {
                Log.Debug(string.Format("BuildLoggingStatement Complete.  Result = {0}", result));
            }

            return result;
        }

        /// <summary>
        ///     Gets the current method for the calls to logutility.
        /// </summary>
        /// <returns>A method base containing the current logging method.</returns>
        private static MethodBase GetCurrentMethod()
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetCurrentMethod called.");
            }

            MethodBase result = null;

            try
            {
                StackTrace trace = new StackTrace();

                StackFrame[] frames = trace.GetFrames();
                if (frames != null)
                {
                    result =
                        frames.Select(frame => frame.GetMethod())
                            .FirstOrDefault(
                                method =>
                                method.DeclaringType != typeof(LogUtility)
                                && method.Module.Name.ToLower().StartsWith("vendrx"));
                }
            }
            catch (Exception ex)
            {
                // we're swallowing this because we can get partial trust exceptions and I'm not sure exactly waht will be thrown.
                // Need to test and figure out exactly what to catch
                Log.Error("Error getting stack trace and frame.", ex);
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetCurrentMethod Complete");
            }

            return result;
        }

        /// <summary>
        ///     Gets a list of the generic types for the method.
        /// </summary>
        /// <param name="loggingStatement">The logging statement being built</param>
        /// <param name="genericTypes">Type types ot put into the statement.</param>
        private static void GetGenericTypes(StringBuilder loggingStatement, Type[] genericTypes)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetGenericTypes called.");
            }

            loggingStatement.Append("<");
            bool first = true;
            foreach (Type type in genericTypes)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    loggingStatement.Append(", ");
                }

                loggingStatement.Append(type.Name);
            }

            loggingStatement.Append(">");

            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetGenericTypes complete.");
            }
        }

        /// <summary>
        ///     Gets a method declaration for the current method.
        /// </summary>
        /// <param name="loggingStatement">The logging statement we're building.</param>
        /// <param name="currentMethod">The current method to log.</param>
        /// <param name="methodParameters">The method paramters for the current method.</param>
        private static void GetMethodDeclaration(
            StringBuilder loggingStatement,
            MethodBase currentMethod,
            ParameterInfo[] methodParameters)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetMethodDeclaration Called");
            }

            loggingStatement.Append(currentMethod.Name);
            if (currentMethod.IsGenericMethod)
            {
                GetGenericTypes(loggingStatement, currentMethod.GetGenericArguments());
            }

            loggingStatement.Append("(");

            bool first = true;

            foreach (ParameterInfo info in methodParameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    loggingStatement.Append(", ");
                }

                if (info.GetCustomAttributes(typeof(ParamArrayAttribute), true).Count() > 0)
                {
                    loggingStatement.Append("params ");
                }

                loggingStatement.Append(info.ParameterType.Name);
                if (info.ParameterType.IsGenericType)
                {
                    GetGenericTypes(loggingStatement, info.ParameterType.GetGenericArguments());
                }

                loggingStatement.Append(" ");

                loggingStatement.Append(info.Name);
            }

            loggingStatement.Append(")");

            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetMethodDeclaration Complete");
            }
        }

        /// <summary>
        ///     Gets the values for all parameters.
        /// </summary>
        /// <param name="loggingStatement">The logging statement we're building.</param>
        /// <param name="methodParameters">The method parameters for the current method.</param>
        /// <param name="passedValues">The values that have been passed for logging.</param>
        private static void GetMethodParameterValues(
            StringBuilder loggingStatement,
            ParameterInfo[] methodParameters,
            object[] passedValues)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetMethodParameterValues called.");
            }

            int passedLocation = 0;
            bool first = true;
            foreach (ParameterInfo info in methodParameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    loggingStatement.Append(", ");
                }

                loggingStatement.Append(info.Name);
                loggingStatement.Append(" = |: ");

                if (info.GetCustomAttributes(typeof(ParamArrayAttribute), true).Count() > 0
                    && methodParameters.Count() == 1)
                {
                    loggingStatement.Append(GetObjectValue(passedValues));
                }
                else
                {
                    if (info.IsOut)
                    {
                        loggingStatement.Append("Out Parameter");
                    }
                    else
                    {
                        if (passedValues == null)
                        {
                            loggingStatement.Append(GetObjectValue("null"));
                            passedLocation++;
                        }
                        else if (passedValues.Count() > passedLocation)
                        {
                            loggingStatement.Append(GetObjectValue(passedValues[passedLocation]));
                            passedLocation++;
                        }
                        else
                        {
                            if (info.IsOptional)
                            {
                                loggingStatement.Append("Optional.  Not provided.");
                            }
                        }
                    }
                }

                loggingStatement.Append(" :|");
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetMethodParameterValues complete.");
            }
        }

        /// <summary>
        ///     Gets the value for an object.
        /// </summary>
        /// <param name="value">The value to get a string for.</param>
        /// <param name="appendedObjects">The objects that have already been parsed.</param>
        /// <param name="seenObjects">Objects that have been seen.  May or may not have string values created.</param>
        /// <returns>A string representation of the object.</returns>
        private static string GetObjectValue(
            object value,
            IDictionary<object, string> appendedObjects,
            IList<object> seenObjects)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("GetObjectValue called.");
            }

            StringBuilder builder = new StringBuilder();
            string result;

            if (seenObjects.Contains(value) && !appendedObjects.ContainsKey(value))
            {
                result = string.Format("Recursion detected for object of type {0}", value.GetType().Name);
            }
            else
            {
                if (value == null)
                {
                    result = "null";
                }
                else
                {
                    seenObjects.Add(value);

                    if (appendedObjects.ContainsKey(value))
                    {
                        result = appendedObjects[value];
                    }
                    else
                    {
                        if (IsArray(value))
                        {
                            IEnumerable array = value as IEnumerable;
                            builder.Append("{ ");

                            if (array != null)
                            {
                                bool first = true;
                                foreach (object item in array)
                                {
                                    if (first)
                                    {
                                        first = false;
                                    }
                                    else
                                    {
                                        builder.Append(", ");
                                    }

                                    builder.Append(GetObjectValue(item, appendedObjects, seenObjects));
                                }
                            }
                            else
                            {
                                builder.Append("null");
                            }

                            builder.Append(" }");
                        }
                        else
                        {
                            builder.Append(value);
                        }

                        result = builder.ToString();
                        appendedObjects.Add(value, result);
                    }
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug(string.Format("GetObjectValue complete.  Result={0}", result));
            }

            return result;
        }

        /// <summary>
        ///     Determines whether or not the value passed is an array.
        /// </summary>
        /// <param name="valueToCheck">The value to verify.</param>
        /// <returns>True of the valueToCheck is or contains an array, false otherwise.</returns>
        private static bool IsArray(object valueToCheck)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("IsArray Called");
            }

            bool result = false;

            if (valueToCheck != null && valueToCheck.GetType() != typeof(string))
            {
                if (valueToCheck is IEnumerable)
                {
                    result = true;
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug(string.Format("IsArray Complete.  Result ={0}", result));
            }

            return result;
        }

        /// <summary>
        ///     Verifies taht the parameters passed match the method's properties
        /// </summary>
        /// <param name="currentMethod">The current method we're testing for.</param>
        /// <param name="methodParameters">The parameters for the method.</param>
        /// <param name="passedValues">The values that have been passed in.</param>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        private static void ValidateParameters(
            MethodBase currentMethod,
            ParameterInfo[] methodParameters,
            object[] passedValues)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug("ValidateParameters called.");
            }

            if (methodParameters == null)
            {
                methodParameters = new ParameterInfo[0];
            }

            int outParameterCount = methodParameters.Count(info => info.IsOut);
            int paramsCount =
                methodParameters.Count(info => info.GetCustomAttributes(typeof(ParamArrayAttribute), true).Count() > 0);
            int optionalCount = methodParameters.Count(info => info.IsOptional);
            int requiredAmount = methodParameters.Count() - outParameterCount - optionalCount;

            if (paramsCount > 0 && methodParameters.Count() == 1)
            {
                // do nothing--if a method only has the params array, the passed values may be null, they may be an array of values, etc.  In other words, we'll take whatever they send because we can't detect
                // an invalid configuration.
            }
            else if (passedValues == null && methodParameters.Count() == 1 && methodParameters[0].ParameterType.IsArray)
            {
                // do nothing -- this is allowed if their is only one parameter and it's an object and it's an array.
            }
            else if (passedValues.Count() > methodParameters.Count() && methodParameters.Count() == 1
                     && methodParameters[0].ParameterType.IsArray)
            {
                // Again, do nothing.  Probably an object array.
            }
            else
            {
                // If they passed more than we can possibly have, that's bad.
                if (passedValues.Count() > methodParameters.Count())
                {
                    throw new ArgumentException(
                        string.Format(
                            "LogUtility: Method {0} has {1} parameters passed but only has {2} parameters.",
                            currentMethod.Name,
                            passedValues.Count(),
                            methodParameters.Count()));
                }

                if (passedValues.Count() < requiredAmount)
                {
                    throw new ArgumentException(
                        string.Format(
                            "LogUtility: Method {0} has {1} parameters passed but requires at least {2} parameters.",
                            currentMethod.Name,
                            passedValues.Count(),
                            methodParameters.Count()));
                }
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug("ValidateParameters complete.");
            }
        }
    }
}