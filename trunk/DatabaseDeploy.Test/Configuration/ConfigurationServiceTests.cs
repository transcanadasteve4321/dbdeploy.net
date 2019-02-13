// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ConfigurationServiceTests.cs" company="Database Deploy 2">
//    Copyright (c) 2015 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DatabaseDeploy.Test.Configuration
{
    using System.IO;

    using DatabaseDeploy.Core.Configuration;
    using DatabaseDeploy.Core.Database;
    using DatabaseDeploy.Core.Database.DatabaseInstances;
    using DatabaseDeploy.Core.Database.DatabaseInstances.MySql;
    using DatabaseDeploy.Core.Database.DatabaseInstances.Oracle;
    using DatabaseDeploy.Core.Database.DatabaseInstances.SqlServer;
    using DatabaseDeploy.Core.IoC;
    using DatabaseDeploy.Core.Utilities;

    using Microsoft.Practices.Unity;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     Tests the configuration service
    /// </summary>
    [TestClass]
    public class ConfigurationServiceTests : TestFixtureBase
    {
        /// <summary>
        ///     Ensures that a value of 0 will apply all changes.
        /// </summary>
        [TestMethod]
        public void That0EqualsIntMaxValueForLastChangeToApply()
        {
            IConfigurationService config = new ConfigurationService();
            config.LastChangeToApply = 0;

            Assert.AreEqual(config.LastChangeToApply, int.MaxValue);

            config.LastChangeToApply = -155;
            Assert.AreEqual(config.LastChangeToApply, int.MaxValue);

            config.LastChangeToApply = 500;
            Assert.AreEqual(config.LastChangeToApply, 500);
        }

        /// <summary>
        ///     Ensures that a basic config object has the appropriate defaults.
        /// </summary>
        [TestMethod]
        public void ThatConfigHasDefaults()
        {
            IConfigurationService config = new ConfigurationService();

            Assert.AreEqual(config.LastChangeToApply, int.MaxValue);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(config.OutputFile.Trim()));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(config.RootDirectory.Trim()));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(config.ScriptListFile.Trim()));
            Assert.AreEqual(config.SearchPattern, "*.sql");
            Assert.IsTrue(!string.IsNullOrWhiteSpace(config.UndoOutputFile.Trim()));
        }

        /// <summary>
        ///     Tests that database management setup updates the config for databases
        /// </summary>
        [TestMethod]
        public void ThatDatabaseManagementSetUpdatesConfig()
        {
            IConfigurationService config = new ConfigurationService();

            config.DatabaseManagementSystem = DatabaseTypesEnum.MySql;
            IDatabaseService db = Container.UnityContainer.Resolve<IDatabaseService>();

            Assert.IsTrue(db is MySqlDatabaseService);
            Assert.IsTrue(config.DatabaseScriptPath.Contains("mysql"));

            config.DatabaseManagementSystem = DatabaseTypesEnum.SqlServer;
            db = Container.UnityContainer.Resolve<IDatabaseService>();

            Assert.IsTrue(db is SqlServerDatabaseService);
            Assert.IsTrue(config.DatabaseScriptPath.Contains("mssql"));

            config.DatabaseManagementSystem = DatabaseTypesEnum.Oracle;
            db = Container.UnityContainer.Resolve<IDatabaseService>();

            Assert.IsTrue(db is OracleDatabaseService);
            Assert.IsTrue(config.DatabaseScriptPath.Contains("ora"));
        }

        /// <summary>
        ///     Tests to ensure a default database setup is in place.
        /// </summary>
        [TestMethod]
        public void ThatDefaultDatabaseSetupExists()
        {
            ConfigurationService config = new ConfigurationService();
            config.SetupDatabaseType();
            IDatabaseService db = Container.UnityContainer.Resolve<IDatabaseService>();

            Assert.IsTrue(db is SqlServerDatabaseService);
            Assert.IsTrue(config.DatabaseScriptPath.Contains("mssql"));
        }

        /// <summary>
        ///     Ensures that if the defaults of values are passed or null is passed, the settings aren't altered.
        /// </summary>
        [TestMethod]
        public void ThatEmptyValuesDontChangeSettings()
        {
            IConfigurationService config = new ConfigurationService();
            config.OutputFile = "My Value";
            config.OutputFile = string.Empty;
            Assert.IsTrue(!string.IsNullOrWhiteSpace(config.OutputFile));

            config.RootDirectory = "My Value";
            config.RootDirectory = string.Empty;
            Assert.IsTrue(!string.IsNullOrWhiteSpace(config.RootDirectory));

            config.ScriptListFile = "My Value";
            config.ScriptListFile = string.Empty;
            Assert.IsTrue(!string.IsNullOrWhiteSpace(config.ScriptListFile));

            config.SearchPattern = "My Value";
            config.SearchPattern = string.Empty;
            Assert.IsTrue(!string.IsNullOrWhiteSpace(config.SearchPattern));

            config.UndoOutputFile = "My Value";
            config.UndoOutputFile = string.Empty;
            Assert.IsTrue(!string.IsNullOrWhiteSpace(config.UndoOutputFile));
        }

        /// <summary>
        ///     Tests that a relative path for a root directory resolves to the correct physical path.
        /// </summary>
        [TestMethod]
        public void ThatRelativeRootDirectoryResolves()
        {
            IConfigurationService config = new ConfigurationService();
            config.RootDirectory = "TestDirectory";

            Assert.AreEqual(
                config.RootDirectory,
                Path.Combine(EnvironmentProvider.Current.CurrentDirectory, "TestDirectory"));
        }

        /// <summary>
        ///     Ensures that a rooted root directory doesn't get changed into a relative root directory.
        /// </summary>
        [TestMethod]
        public void ThatRootedRootDirectoryIsntRelocated()
        {
            IConfigurationService config = new ConfigurationService();
            config.RootDirectory = @"c:\MyDirectory";

            Assert.AreEqual(config.RootDirectory, @"c:\MyDirectory");
        }

        /// <summary>
        ///     Ensures that the location of the script list file will be in the root directory.
        /// </summary>
        [TestMethod]
        public void ThatScriptListFileIsInRootDirectory()
        {
            IConfigurationService config = new ConfigurationService();
            string directory = Path.GetDirectoryName(config.ScriptListFile);
            Assert.AreEqual(directory, config.RootDirectory);
        }

        /// <summary>
        ///     Ensures that tostring actually returns the details fo rthe config.  Note that we're not doing a deep test here
        ///     since the value is low.
        /// </summary>
        [TestMethod]
        public void ThatToStringReturnsSettings()
        {
            IConfigurationService config = ConfigurationServiceWith("Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;");
            string result = config.ToString();
            Assert.AreNotEqual(result, typeof(ConfigurationService).FullName);
            Assert.AreNotEqual(result, typeof(IConfigurationService).FullName);
        }

        /// <summary>
        /// Ensure that connection strings with passwords are not shown.
        /// </summary>
        [TestMethod]
        public void ThatToStringCensorsConnectionStringsWithPasswords()
        {
            string connectionStringWithPassword = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";
            IConfigurationService config = ConfigurationServiceWith(connectionStringWithPassword);
            string result = config.ToString();
            Assert.IsFalse(result.Contains(connectionStringWithPassword));
            StringAssert.Contains(result, "********************");
        }

        private static ConfigurationService ConfigurationServiceWith(string connectionString)
        {
            return new ConfigurationService
            {
                ConnectionString = connectionString
            };
        }
    }
}