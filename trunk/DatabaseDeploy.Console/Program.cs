// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="Program.cs" company="Database Deploy 2">
//    Copyright (c) 2015 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DatabaseDeploy.Console
{
    using System;
    using System.ComponentModel;
    using System.Configuration;

    using CommandLine;

    using DatabaseDeploy.Core;
    using DatabaseDeploy.Core.Configuration;
    using DatabaseDeploy.Core.Database;

    using log4net;

    using Microsoft.Practices.Unity;

    using Container = DatabaseDeploy.Core.IoC.Container;

    /// <summary>
    ///     The program.
    /// </summary>
    internal class Program
    {
        /// <summary>
        ///     Creates the default logger
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        /// <summary>
        ///     Gets or sets the deployment service to use. This is injected by unity and should not be set.
        /// </summary>
        /// <value>The configuration service.</value>
        [Dependency]
        public static IConfigurationService ConfigurationService { get; set; }

        /// <summary>
        ///     Gets or sets the deployment service to use. This is injected by unity and should not be set.
        /// </summary>
        /// <value>The deployment service.</value>
        [Dependency]
        public static IDeploymentService DeploymentService { get; set; }

        /// <summary>
        ///     Gets a setting from app settings.
        /// </summary>
        /// <typeparam name="T">The type of setting we're retrieving</typeparam>
        /// <param name="setting">The setting to retrieve</param>
        /// <returns>A setting for the requested value or the default setting.</returns>
        private static T GetSetting<T>(string setting)
        {
            T result = default(T);
            string value = ConfigurationManager.AppSettings[setting];
            if (value != null)
            {
                result = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(value);
            }

            return result;
        }

        /// <summary>
        ///     The main entry point into the application.
        /// </summary>
        /// <param name="args">The arguments for this run</param>
        /// <exception cref="System.ArgumentException"></exception>
        private static void Main(string[] args)
        {
            var exitCode = 0;

            try
            {
                ConfigurationService = Container.UnityContainer.Resolve<IConfigurationService>();
                DeploymentService = Container.UnityContainer.Resolve<IDeploymentService>();

                ConfigurationService.ConnectionString = GetSetting<string>("ConnectionString");

                switch (GetSetting<string>("DatabaseType"))
                {
                    case "mssql":
                        ConfigurationService.DatabaseManagementSystem = DatabaseTypesEnum.SqlServer;
                        break;
                    case "ora":
                        ConfigurationService.DatabaseManagementSystem = DatabaseTypesEnum.Oracle;
                        break;
                    case "mysql":
                        ConfigurationService.DatabaseManagementSystem = DatabaseTypesEnum.MySql;
                        break;
                    default:
                        string message =
                            string.Format(
                                "An invalid database type of {0} was specified.  Only \"mssql\", \"ora\", and \"mysql\" are supported.",
                                GetSetting<string>("DatabaseType"));
                        Log.Fatal(message);
                        throw new ArgumentException(message);
                }

                ConfigurationService.LastChangeToApply = GetSetting<int>("LastChangeToApply");
                ConfigurationService.OutputFile = GetSetting<string>("OutputFile");
                ConfigurationService.Recursive = GetSetting<bool>("Recursive");
                ConfigurationService.RootDirectory = GetSetting<string>("RootDirectory");
                ConfigurationService.SearchPattern = GetSetting<string>("SearchPattern");
                ConfigurationService.FileNamePattern = GetSetting<string>("FileNamePattern");
                ConfigurationService.UndoOutputFile = GetSetting<string>("UndoFile");
                ConfigurationService.Schema = GetSetting<string>("Schema");
                ConfigurationService.ChangeLog = GetSetting<string>("ChangeLog");

                ParseCommandLine(args);

                DeploymentService.BuildDeploymentScript();
            }
            catch (Exception ex)
            {
                exitCode = 1;
                Log.Error(ex);
            }
            Environment.Exit(exitCode);
        }

        /// <summary>
        ///     Parses the command line parameters
        /// </summary>
        /// <param name="args">The arguments passed on the command line.</param>
        private static void ParseCommandLine(string[] args)
        {
            if (args.Length > 0)
            {
                ParserResult<Options> result = Parser.Default.ParseArguments<Options>(args);

                Options options = null;

                result.MapResult(
                    options1 =>
                        {
                            options = options1;
                            return options;
                        },
                    errors => { return null; });

                if (options == null)
                {
                    Log.Info("No command line options found.");
                }

                if (options != null)
                {
                    if (options.ConnectionString != null)
                    {
                        ConfigurationService.ConnectionString = options.ConnectionString;
                        Log.InfoFormat("Updated connection string to {0}", ConfigurationService.ConnectionString);
                    }

                    switch (options.DatabaseManagementSystem)
                    {
                        case "mssql":
                            ConfigurationService.DatabaseManagementSystem = DatabaseTypesEnum.SqlServer;
                            break;
                        case "ora":
                            ConfigurationService.DatabaseManagementSystem = DatabaseTypesEnum.Oracle;
                            break;
                        case "mysql":
                            ConfigurationService.DatabaseManagementSystem = DatabaseTypesEnum.MySql;
                            break;
                    }

                    ConfigurationService.LastChangeToApply = options.LastChangeToApply != default(int)
                                                                 ? options.LastChangeToApply
                                                                 : ConfigurationService.LastChangeToApply;

                    ConfigurationService.OutputFile = string.IsNullOrEmpty(options.OutputFile)
                                                          ? ConfigurationService.OutputFile
                                                          : options.OutputFile;

                    ConfigurationService.Recursive = options.Recursive != default(bool)
                                                         ? options.Recursive
                                                         : ConfigurationService.Recursive;

                    if (options.RootDirectory != null)
                    {
                        ConfigurationService.RootDirectory = options.RootDirectory;
                        Log.InfoFormat("Updated root directory to {0}", ConfigurationService.RootDirectory);
                    }

                    ConfigurationService.DatabaseScriptPath = string.IsNullOrEmpty(options.DatabaseScriptPath)
                                         ? ConfigurationService.DatabaseScriptPath
                                         : options.DatabaseScriptPath;

                    ConfigurationService.SearchPattern = string.IsNullOrEmpty(options.SearchPattern)
                                                             ? ConfigurationService.SearchPattern
                                                             : options.SearchPattern;

                    ConfigurationService.FileNamePattern = string.IsNullOrEmpty(options.FileNamePattern)
                                                               ? ConfigurationService.FileNamePattern
                                                               : options.FileNamePattern;

                    ConfigurationService.UndoOutputFile = string.IsNullOrEmpty(options.UndoOutputFile)
                                                              ? ConfigurationService.UndoOutputFile
                                                              : options.UndoOutputFile;

                    ConfigurationService.Schema = string.IsNullOrEmpty(options.Schema)
                                                      ? ConfigurationService.Schema
                                                      : options.Schema;

                    ConfigurationService.ChangeLog = string.IsNullOrEmpty(options.ChangeLog)
                                                         ? ConfigurationService.ChangeLog
                                                         : options.ChangeLog;
                }
            }
        }
    }
}