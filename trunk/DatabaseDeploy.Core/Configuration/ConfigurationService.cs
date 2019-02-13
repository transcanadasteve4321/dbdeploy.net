// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ConfigurationService.cs" company="Database Deploy 2">
//    Copyright (c) 2015 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DatabaseDeploy.Core.Configuration
{
    using System.IO;
    using System.Text;

    using DatabaseDeploy.Core.Database;
    using DatabaseDeploy.Core.Database.DatabaseInstances;
    using DatabaseDeploy.Core.Database.DatabaseInstances.MySql;
    using DatabaseDeploy.Core.Database.DatabaseInstances.Oracle;
    using DatabaseDeploy.Core.Database.DatabaseInstances.SqlServer;
    using DatabaseDeploy.Core.IoC;
    using DatabaseDeploy.Core.Utilities;

    using Microsoft.Practices.Unity;

    /// <summary>
    /// Provides a service for managing configuration values.
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        /// <summary>
        /// The default database path
        /// </summary>
        private const string DefaultDatabasePath = @"Database\Scripts";

        /// <summary>
        /// The default output filename
        /// </summary>
        private const string DefaultOutputFile = "DbDeploy.sql";

        /// <summary>
        /// The default file to contain the list of scripts.
        /// </summary>
        private const string DefaultScriptListFile = "ScriptList.txt";

        /// <summary>
        /// The default undo filename
        /// </summary>
        private const string DefaultUndoFile = "DbDeployUndo.sql";

        /// <summary>
        /// The changelog table.
        /// </summary>
        private string changeLog = "changelog";

        /// <summary>
        /// The database management type
        /// </summary>
        private DatabaseTypesEnum databaseManagementSystem = DatabaseTypesEnum.SqlServer;

        /// <summary>
        /// The pattern to use for parsing the name of the script file
        /// </summary>
        private string fileNamePattern = @"((\d*\.)?\d+)(\s+)?(.+)?";

        /// <summary>
        /// The last change to apply in the database.
        /// </summary>
        private int lastChangeToApply = int.MaxValue;

        /// <summary>
        /// The output file to use for this run.
        /// </summary>
        private string outputFile = DefaultOutputFile;

        /// <summary>
        /// The root directory to use for this run.
        /// </summary>
        private string rootDirectory;

        /// <summary>
        /// The schema
        /// </summary>
        private string schema = "dbo";

        /// <summary>
        /// Contains the name of the script file where scripts should be written.
        /// </summary>
        private string scriptListFile = DefaultScriptListFile;

        /// <summary>
        /// The search pattern to use for finding script files
        /// </summary>
        private string searchPattern = "*.sql";

        /// <summary>
        /// The undo output file.
        /// </summary>
        private string undoOutputFile = DefaultUndoFile;

        /// <summary>
        /// Gets or sets the name of the change log table.
        /// </summary>
        /// <value>The change log.</value>
        public string ChangeLog
        {
            get
            {
                return this.changeLog;
            }

            set
            {
                if (value != null && value.Trim() != string.Empty)
                {
                    this.changeLog = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a connection string to be used by the system
        /// </summary>
        /// <value>The connection string.</value>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the type of DBMS to use.
        /// </summary>
        /// <value>The database management system.</value>
        public DatabaseTypesEnum DatabaseManagementSystem
        {
            get
            {
                return this.databaseManagementSystem;
            }

            set
            {
                this.databaseManagementSystem = value;
                this.SetupDatabaseType();
            }
        }

        /// <summary>
        /// Gets or sets the database script path
        /// </summary>
        /// <value>The database script path.</value>
        public string DatabaseScriptPath { get; set; }

        /// <summary>
        /// Gets the database service that should be used for database operations.
        /// </summary>
        /// <value>The database service.</value>
        public IDatabaseService DatabaseService
        {
            get
            {
                return (IDatabaseService)Container.UnityContainer.Resolve(typeof(IDatabaseService));
            }
        }

        /// <summary>
        /// Gets or sets the pattern to use for parsing the name of the script file
        /// </summary>
        /// <value>The file name pattern.</value>
        public string FileNamePattern
        {
            get
            {
                return this.fileNamePattern;
            }

            set
            {
                if (value != null && value.Trim() != string.Empty)
                {
                    this.fileNamePattern = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the last change that should be applied to the database.
        /// </summary>
        /// <value>The last change to apply.</value>
        /// <remarks>Set to 0 or max value (the default) to apply all changes. Any other positive number will stop applying changes at
        /// that level.</remarks>
        public int LastChangeToApply
        {
            get
            {
                return this.lastChangeToApply;
            }

            set
            {
                if (value <= 0)
                {
                    this.lastChangeToApply = int.MaxValue;
                }
                else
                {
                    this.lastChangeToApply = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the directory and file name that will be used for writing out the change script
        /// </summary>
        /// <value>The output file.</value>
        public string OutputFile
        {
            get
            {
                return this.outputFile;
            }

            set
            {
                if (value != null && value.Trim() != string.Empty)
                {
                    this.outputFile = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the script search should be recursive
        /// </summary>
        /// <value><c>true</c> if recursive; otherwise, <c>false</c>.</value>
        public bool Recursive { get; set; }

        /// <summary>
        /// Gets or sets the root directory for processing
        /// </summary>
        /// <value>The root directory.</value>
        public string RootDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(this.rootDirectory) || this.rootDirectory.Trim() == string.Empty)
                {
                    this.rootDirectory = EnvironmentProvider.Current.CurrentDirectory;
                }

                string result;

                if (Path.IsPathRooted(this.rootDirectory))
                {
                    result = this.rootDirectory;
                }
                else
                {
                    result = Path.Combine(EnvironmentProvider.Current.CurrentDirectory, this.rootDirectory);
                }

                return result;
            }

            set
            {
                if (value != null && value.Trim() != string.Empty)
                {
                    this.rootDirectory = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the schema to use as a prefix for the change log table.
        /// </summary>
        /// <value>The schema.</value>
        public string Schema
        {
            get
            {
                return this.schema;
            }

            set
            {
                if (value != null && value.Trim() != string.Empty)
                {
                    this.schema = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the file where the list of found scripts should be written.  If no path is provided, a
        /// relative path is assumed and the file will be in the RootDirectory directory.
        /// </summary>
        /// <value>The script list file.</value>
        public string ScriptListFile
        {
            get
            {
                return Path.Combine(this.RootDirectory, this.scriptListFile);
            }

            set
            {
                if (value != null && value.Trim() != string.Empty)
                {
                    this.scriptListFile = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the search pattern to use for finding script files
        /// </summary>
        /// <value>The search pattern.</value>
        public string SearchPattern
        {
            get
            {
                return this.searchPattern;
            }

            set
            {
                if (value != null && value.Trim() != string.Empty)
                {
                    this.searchPattern = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the directory and file name that will be used for writing out the undo change script
        /// </summary>
        /// <value>The undo output file.</value>
        public string UndoOutputFile
        {
            get
            {
                return this.undoOutputFile;
            }

            set
            {
                if (value != null && value.Trim() != string.Empty)
                {
                    this.undoOutputFile = value;
                }
            }
        }

        /// <summary>
        /// Sets up the container to have the appropriate mappings for the appropriate database types.
        /// </summary>
        public void SetupDatabaseType()
        {
            string rootPath = Path.Combine(EnvironmentProvider.Current.ExecutingAssemblyDirectory, DefaultDatabasePath);

            switch (this.DatabaseManagementSystem)
            {
                case DatabaseTypesEnum.MySql:
                    Container.UnityContainer.RegisterType(typeof(IDatabaseService), typeof(MySqlDatabaseService));
                    this.DatabaseScriptPath = Path.Combine(rootPath, "mysql");
                    break;
                case DatabaseTypesEnum.Oracle:
                    Container.UnityContainer.RegisterType(typeof(IDatabaseService), typeof(OracleDatabaseService));
                    this.DatabaseScriptPath = Path.Combine(rootPath, "ora");
                    break;
                case DatabaseTypesEnum.SqlServer:
                    Container.UnityContainer.RegisterType(typeof(IDatabaseService), typeof(SqlServerDatabaseService));
                    this.DatabaseScriptPath = Path.Combine(rootPath, "mssql");
                    break;
            }
        }

        /// <summary>
        /// Overrides tostring to get a descriptive list of all settings.
        /// </summary>
        /// <returns>A string containing a list of all settings.</returns>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("ConnectionString: {0}", this.CensoredConnectionString()));
            builder.AppendLine(string.Format("DatabaseManagementSystem: {0}", this.DatabaseManagementSystem));
            builder.AppendLine(string.Format("LastChangeToApply: {0}", this.LastChangeToApply));
            builder.AppendLine(string.Format("OutputFile: {0}", this.OutputFile));
            builder.AppendLine(string.Format("Recursive: {0}", this.Recursive));
            builder.AppendLine(string.Format("RootDirectory: {0}", this.RootDirectory));
            builder.AppendLine(string.Format("ScriptListFile: {0}", this.ScriptListFile));
            builder.AppendLine(string.Format("SearchPattern: {0}", this.SearchPattern));
            builder.AppendLine(string.Format("FileNamePattern: {0}", this.FileNamePattern));
            builder.AppendLine(string.Format("UndoOutputFile: {0}", this.UndoOutputFile));
            ////builder.AppendLine(string.Format("UseTransactions: {0}", this.UseTransactions));

            string result = builder.ToString();

            return result;
        }

        private string CensoredConnectionString()
        {
            return this.ConnectionString.ToLower().Contains("password") ? "********************" : this.ConnectionString;
        }

        /////// <summary>
        ///////   Gets or sets a value indicating whether or not transactions should be used for each script
        /////// </summary>
        /////// <remarks>
        ///////   In my opinion, this option should not be used. Instead, put a transaction in the actual script file itself, as needed.
        /////// </remarks>

        ////public bool UseTransactions { get; set; }
    }
}