// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ScriptFile.cs" company="Database Deploy 2">
//    Copyright (c) 2015 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DatabaseDeploy.Core.ScriptGeneration
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    using DatabaseDeploy.Core.FileManagement;

    using log4net;

    /// <summary>
    ///     Represents a script in the system.
    /// </summary>
    public class ScriptFile : IScriptFile
    {
        /// <summary>
        ///     Creates the default logger
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScriptFile));

        /// <summary>
        ///     The regex file name
        ///     !! WARNING !! This initial value is NOT the actual regular expression used!
        ///     The user is allowed to override it in their options, in Console's app.config, or the default value in ConfigurationService.fileNamePattern.
        /// </summary>
        private static Regex regexFileName = new Regex(@"(\d+)(\s+)?(.+)?");

        /// <summary>
        ///     Pattern used to parse the file names
        /// </summary>
        /// <value>The file name pattern.</value>
        public static string FileNamePattern
        {
            get
            {
                return regexFileName.ToString();
            }

            set
            {
                regexFileName = new Regex(value);
            }
        }

        /// <summary>
        ///     Gets or sets the contents of the script
        /// </summary>
        /// <value>The contents.</value>
        public string Contents { get; set; }

        /// <summary>
        ///     Gets or sets the description for this file.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        ///     Gets or sets the file info for this script
        /// </summary>
        /// <value>The file information.</value>
        public FileInfo FileInfo { get; set; }

        /// <summary>
        ///     Gets or sets the full file name for this script
        /// </summary>
        /// <value>The name of the file.</value>
        public string FileName { get; set; }

        /// <summary>
        ///     Gets or sets the script Id. These must be unique
        /// </summary>
        /// <value>The identifier.</value>
        public decimal Id { get; set; }

        /// <summary>
        ///     Parses a file info for the information needed in the script
        /// </summary>
        /// <param name="fileService">Service to deal with files</param>
        /// <param name="filePath">The file info to parse</param>
        public void Parse(IFileService fileService, string filePath)
        {
            this.FileName = filePath.Trim();
            this.FileInfo = new FileInfo(this.FileName);
            this.GetIdAndDescription();
            this.ReadContents(fileService, filePath);
        }

        /// <summary>
        ///     Gets the Id and description from the file name,
        ///     using the regular expression specified in <see cref="FileNamePattern"/>.
        /// </summary>
        /// <exception cref="System.Exception"></exception>
        private void GetIdAndDescription()
        {
            Match m = regexFileName.Match(Path.GetFileNameWithoutExtension(this.FileInfo.Name));
            bool success = m.Success && m.Groups.Count > 1;
            decimal id = 0;
            if (success)
            {
                success = decimal.TryParse(m.Groups[1].Value, out id);
            }

            if (!success)
            {
                string message = string.Format("The file {0} is formed incorrectly.  The file must match this format: {1}", this.FileInfo.FullName, FileNamePattern);
                Log.Error(message);
                throw new Exception(message);
            }

            this.Id = id;
            if (m.Groups.Count > 3)
            {
                int finalGroup = m.Groups.Count - 1;
                this.Description = m.Groups[finalGroup].Value;
            }
        }

        /// <summary>
        ///     Reads the contents.
        /// </summary>
        /// <param name="fileService">The file service.</param>
        /// <param name="filePath">The file path.</param>
        private void ReadContents(IFileService fileService, string filePath)
        {
            string fileContents = fileService.GetFileContents(filePath, false);
            this.Contents = fileContents.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }
    }
}