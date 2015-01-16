// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ScriptService.cs" company="Database Deploy 2">
//    Copyright (c) 2015 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DatabaseDeploy.Core.ScriptGeneration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using DatabaseDeploy.Core.Configuration;
    using DatabaseDeploy.Core.Database.DatabaseInstances;
    using DatabaseDeploy.Core.FileManagement;
    using DatabaseDeploy.Core.Utilities;

    /// <summary>
    ///     Used to generate scripts for dbdeploy
    /// </summary>
    public class ScriptService : IScriptService
    {
        /// <summary>
        ///     The configuration service to use for processing.
        /// </summary>
        private readonly IConfigurationService configurationService;

        /// <summary>
        ///     The file service to use for file stuff.
        /// </summary>
        private readonly IFileService fileService;

        /// <summary>
        ///     The token replacer to use for processing.
        /// </summary>
        private readonly ITokenReplacer tokenReplacer;

        /// <summary>
        ///     Represents the token to use for undo files.
        /// </summary>
        private string undoToken = string.Empty;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ScriptService" /> class.
        /// </summary>
        public ScriptService()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ScriptService" /> class.
        /// </summary>
        /// <param name="fileService">the File service to use for file requests.</param>
        /// <param name="tokenReplacer">The token replacer to use for script processing.</param>
        /// <param name="configurationService">The configuration service to use for processing.</param>
        public ScriptService(
            IFileService fileService,
            ITokenReplacer tokenReplacer,
            IConfigurationService configurationService)
        {
            this.fileService = fileService;
            this.tokenReplacer = tokenReplacer;
            this.configurationService = configurationService;
        }

        /// <summary>
        ///     The build change script.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <param name="currentDbVersion"></param>
        /// <returns>A string containing the contents of the provided change files</returns>
        public string BuildChangeScript(IDictionary<decimal, IScriptFile> changes, decimal currentDbVersion)
        {
            StringBuilder changeScript = new StringBuilder();

            this.undoToken = this.configurationService.DatabaseService.GetScriptFromFile(DatabaseScriptEnum.UndoToken);

            this.tokenReplacer.CurrentVersion = currentDbVersion;

            this.AppendScript(DatabaseScriptEnum.ChangeScriptHeader, changeScript);

            decimal[] sortedKeys = changes.Keys.OrderBy(k => k).ToArray();

            foreach (decimal key in sortedKeys)
            {
                IScriptFile scriptFile = changes[key];
                this.tokenReplacer.Script = scriptFile;
                this.AppendScript(DatabaseScriptEnum.ScriptHeader, changeScript);
                this.AppendScriptBody(changeScript, scriptFile.Contents, false);
                this.AppendScript(DatabaseScriptEnum.ScriptFooter, changeScript);
            }

            this.AppendScript(DatabaseScriptEnum.ChangeScriptFooter, changeScript);

            return changeScript.ToString();
        }

        /// <summary>
        ///     The build undo script.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <returns>A string containing the contents of the undo portion of the scripts.</returns>
        public string BuildUndoScript(IDictionary<decimal, IScriptFile> changes)
        {
            StringBuilder undoScript = new StringBuilder();

            this.undoToken = this.configurationService.DatabaseService.GetScriptFromFile(DatabaseScriptEnum.UndoToken);

            this.tokenReplacer.CurrentVersion = changes.Keys.Max();

            this.AppendScript(DatabaseScriptEnum.UndoScriptHeader, undoScript);

            decimal[] sortedKeys = changes.Keys.OrderByDescending(k => k).ToArray();

            foreach (decimal key in sortedKeys)
            {
                string scriptContents = this.fileService.GetFileContents(changes[key].FileName, false);

                if (scriptContents.Contains(this.undoToken))
                {
                    this.tokenReplacer.Script = changes[key];
                    this.AppendScript(DatabaseScriptEnum.UndoHeader, undoScript);
                    this.AppendScriptBody(undoScript, scriptContents, true);
                    this.AppendScript(DatabaseScriptEnum.UndoFooter, undoScript);
                }
            }

            this.AppendScript(DatabaseScriptEnum.UndoScriptFooter, undoScript);

            return undoScript.ToString();
        }

        /// <summary>
        ///     Appends a script to the contents of the current building script.
        /// </summary>
        /// <param name="scriptToUse">The script to use for the append.</param>
        /// <param name="changeScript">The overall change script</param>
        private void AppendScript(string scriptToUse, StringBuilder changeScript)
        {
            string scriptContents = this.configurationService.DatabaseService.GetScriptFromFile(scriptToUse);

            scriptContents = this.tokenReplacer.Replace(scriptContents);

            changeScript.AppendLine(scriptContents);
        }

        /// <summary>
        ///     Appends a script to the output file
        /// </summary>
        /// <param name="changeScript">The overall change script</param>
        /// <param name="scriptContents">The contents of the script file.</param>
        /// <param name="undo">True if an undo script is being generated, false otherwise.</param>
        private void AppendScriptBody(StringBuilder changeScript, string scriptContents, bool undo)
        {
            string scriptPortion;
            string undoPortion = string.Empty;

            int tokenLocation = scriptContents.IndexOf(this.undoToken, StringComparison.Ordinal);

            if (tokenLocation == -1)
            {
                scriptPortion = scriptContents;
            }
            else
            {
                scriptPortion = scriptContents.Substring(0, tokenLocation);
                undoPortion = scriptContents.Substring(tokenLocation + this.undoToken.Length);
            }

            if (undo)
            {
                changeScript.AppendLine(undoPortion);
            }
            else
            {
                changeScript.AppendLine(scriptPortion);
            }
        }
    }
}