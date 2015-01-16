// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DeploymentService.cs" company="Database Deploy 2">
//    Copyright (c) 2015 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DatabaseDeploy.Core
{
    using System.Collections.Generic;
    using System.Linq;

    using DatabaseDeploy.Core.Configuration;
    using DatabaseDeploy.Core.Database;
    using DatabaseDeploy.Core.Database.DatabaseInstances;
    using DatabaseDeploy.Core.FileManagement;
    using DatabaseDeploy.Core.IoC;
    using DatabaseDeploy.Core.ScriptGeneration;
    using DatabaseDeploy.Core.Utilities;

    using log4net;

    /// <summary>
    ///     Manages the overall database deployment.
    /// </summary>
    public class DeploymentService : IDeploymentService
    {
        /// <summary>
        ///     Creates the default logger
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(DeploymentService));

        /// <summary>
        ///     The configuration service to use for the build
        /// </summary>
        private readonly IConfigurationService configurationService;

        /// <summary>
        ///     The file service to use for the build
        /// </summary>
        private readonly IFileService fileService;

        /// <summary>
        ///     The formatter for formatting script concats
        /// </summary>
        private readonly IScriptMessageFormatter scriptMessageFormatter;

        /// <summary>
        ///     The script service to use for the build
        /// </summary>
        private readonly IScriptService scriptService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DeploymentService" /> class.
        /// </summary>
        /// <param name="configurationService">The configuration Service.</param>
        /// <param name="scriptService">The script Service.</param>
        /// <param name="fileService">The file Service.</param>
        /// <param name="scriptMessageFormatter">The script formatter for messages</param>
        public DeploymentService(IConfigurationService configurationService, IScriptService scriptService, IFileService fileService, IScriptMessageFormatter scriptMessageFormatter)
        {
            this.configurationService = configurationService;
            this.scriptService = scriptService;
            this.fileService = fileService;
            this.scriptMessageFormatter = scriptMessageFormatter;
        }

        /// <summary>
        ///     Gets the current configuration service that is being used.
        /// </summary>
        /// <value>The configuration service.</value>
        public IConfigurationService ConfigurationService
        {
            get
            {
                return this.configurationService;
            }
        }

        /// <summary>
        ///     Builds the deployment scripts based on the information that is known at the time.
        /// </summary>
        public void BuildDeploymentScript()
        {
            Log.InfoIfEnabled("Starting dbdeploy run with settings:\n{0}", this.configurationService);

            Log.InfoIfEnabled("Cleaning up past runs.");

            this.fileService.CleanupPastRuns();

            Log.InfoIfEnabled("Getting available script files.");

            IDictionary<decimal, IScriptFile> scripts = this.fileService.GetScriptFiles();

            if (scripts.Any())
            {
                Log.InfoIfEnabled("Writing found file list into {0}", this.configurationService.ScriptListFile);
                this.fileService.WriteScriptList(scripts);
                Log.InfoIfEnabled("Found scripts {0}.", this.scriptMessageFormatter.FormatCollection(scripts.Keys));

                Log.InfoIfEnabled("Getting applied changes.");
                IDictionary<decimal, IChangeLog> changes = this.configurationService.DatabaseService.GetAppliedChanges();

                decimal currentDbVersion = changes.Keys.Max();

                Log.InfoIfEnabled("Found scripts {0}.", this.scriptMessageFormatter.FormatCollection(changes.Keys));

                Log.InfoIfEnabled("Getting scripts to apply.");
                IDictionary<decimal, IScriptFile> scriptsToApply = this.GetScriptsToApply(scripts, changes);

                if (scriptsToApply.Any())
                {
                    Log.InfoIfEnabled("Scripts {0} need to be applied.", this.scriptMessageFormatter.FormatCollection(scriptsToApply.Keys));

                    Log.InfoIfEnabled("Building change script.");
                    string changeScript = this.scriptService.BuildChangeScript(scriptsToApply, currentDbVersion);

                    Log.InfoIfEnabled("Building undo script for scripts {0}.", this.scriptMessageFormatter.FormatCollection(changes.Keys));

                    string undoScript = this.scriptService.BuildUndoScript(scriptsToApply);
                    Log.InfoIfEnabled("Writing change script to {0}", this.configurationService.OutputFile);

                    this.fileService.WriteChangeScript(changeScript);

                    Log.InfoIfEnabled("Writing undo change script to {0}", this.configurationService.UndoOutputFile);

                    this.fileService.WriteUndoScript(undoScript);
                }
                else
                {
                    Log.InfoIfEnabled("No changes need to be applied.  Skipping script generation.");
                }
            }
            else
            {
                Log.InfoIfEnabled("No scripts found at {0}.  Skipping script generation.", this.configurationService.RootDirectory);
            }

            Log.InfoIfEnabled("Deployment generation complete.");
        }

        /// <summary>
        ///     Compares the applied scripts to the available scripts and determines which scripts to apply.
        /// </summary>
        /// <param name="availableScripts">The scripts that are available to be applied to the database.</param>
        /// <param name="appliedChanges">The scripts that have already been applied to the database</param>
        /// <returns>A dictionary containing the scripts that need to be applied to the current database instance</returns>
        private IDictionary<decimal, IScriptFile> GetScriptsToApply(IDictionary<decimal, IScriptFile> availableScripts, IDictionary<decimal, IChangeLog> appliedChanges)
        {
            IDictionary<decimal, IScriptFile> scriptsToApply = new Dictionary<decimal, IScriptFile>();
            decimal[] sortedKeys = availableScripts.Keys.OrderBy(k => k).ToArray();

            foreach (decimal key in
                sortedKeys.Where(key => !appliedChanges.ContainsKey(key) && !scriptsToApply.ContainsKey(key)))
            {
                if (key > this.configurationService.LastChangeToApply)
                {
                    Log.InfoIfEnabled("LastChangeToApply == {0}.  Skipping remaining scripts.", this.configurationService.LastChangeToApply);

                    break;
                }

                scriptsToApply.Add(key, availableScripts[key]);
            }

            return scriptsToApply;
        }
    }
}