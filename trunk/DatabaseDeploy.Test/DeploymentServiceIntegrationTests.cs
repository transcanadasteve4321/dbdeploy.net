// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DeploymentServiceIntegrationTests.cs" company="Database Deploy 2">
//    Copyright (c) 2019 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Data;
using DatabaseDeploy.Core;
using DatabaseDeploy.Core.Configuration;
using DatabaseDeploy.Core.Database;
using DatabaseDeploy.Core.Database.DatabaseInstances;
using DatabaseDeploy.Core.FileManagement;
using DatabaseDeploy.Core.IoC;
using DatabaseDeploy.Core.Utilities;
using DatabaseDeploy.Test.Database;
using DatabaseDeploy.Test.FileManagement;
using DatabaseDeploy.Test.Utilities;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DatabaseDeploy.Test
{
    /// <summary>
    ///     Tests the Deployment Service with almost all of its concrete dependencies.
    ///     This is about as close to exercising DbDeploy with all the components wired up, without running the console app.
    ///     There was a bug that wasn't revealed by all the mock-based unit tests, so I started this
    ///     to mimic the way DbDeploy works when normally executed, in order to find the problem.
    /// </summary>
    [TestClass]
    public class DeploymentServiceIntegrationTests
    {
        /// <summary>
        ///     Resets global/statics that may have been customized by test scenarios.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            TimeProvider.ResetToDefault();
            EnvironmentProvider.ResetToDefault();
            Container.Reset();
        }

        /// <summary>
        ///     Tests the BuildDeploymentScript method, including getting changes that need to be made.
        /// </summary>
        [TestMethod]
        public void ThatBuildScriptDeploymentGeneratesDeployScriptContainingOnlyUnappliedChanges()
        {
            //////////////////////
            // Scenario
            //////////////////////
            // Given our SQL Server database already has a changelog table
            // and it has the following scripts already applied:
            //     | ID | Completed  | Applied By  | Description       |
            //     |  1 | 2012/01/01 | dbdeployer2 | CreateMarketTable |
            // and given that we have these script files in our project's root directory (X:\dev\projectX\db\deltas):
            //     | Filename                         | Contents                                        |
            //     | 1 CreateMarketTable.sql          | CREATE TABLE Market                             |
            //     | 2 AddDescColumnToMarketTable.sql | ALTER TABLE Market ADD COLUMN Description       |
            //     | 3 AddUniqueKeyToMarketTable.sql  | CREATE UNIQUE NONCLUSTERED INDEX UQ_Market_Name |
            // When we (dbdeployer1) ask DbDeploy to build our deployment script
            // Then our deployment script should contain only fragments from scripts 2 and 3

            //////////////////////
            // Given our SQL Server database already has a changelog table
            // and it has the following scripts already applied:
            //     | ID | Completed  | Applied By  | Description       |
            //     |  1 | 2012/01/01 | dbdeployer2 | CreateMarketTable |
            var databaseManagementSystem = DatabaseTypesEnum.SqlServer;
            var changeLogDataSet = CreateChangeLogDataset(new[]
            {
                Tuple.Create(1m, new DateTime(2012, 1, 1), "dbdeployer2", "CreateMarketTable")
            });

            // And given that we have these script files in our project's root directory (X:\dev\projectX\db\deltas):
            //     | Filename                         | Contents                                        |
            //     | 1 CreateMarketTable.sql          | CREATE TABLE Market                             |
            //     | 2 AddDescColumnToMarketTable.sql | ALTER TABLE Market ADD COLUMN Description       |
            //     | 3 AddUniqueKeyToMarketTable.sql  | CREATE UNIQUE NONCLUSTERED INDEX UQ_Market_Name |
            var ioProxy = new BogusIoProxy();
            var rootDirectory = @"X:\dev\projectX\db\deltas";
            ioProxy.AddInMemoryFile($@"{rootDirectory}\1 CreateMarketTable.sql", "CREATE TABLE Market");
            ioProxy.AddInMemoryFile($@"{rootDirectory}\2 AddDescColumnToMarketTable.sql", "ALTER TABLE Market ADD COLUMN Description");
            ioProxy.AddInMemoryFile($@"{rootDirectory}\3 AddUniqueKeyToMarketTable.sql", "CREATE UNIQUE NONCLUSTERED INDEX UQ_Market_Name");

            // When we (dbdeployer1) ask DbDeploy to build our deployment script
            var username = "dbdeployer1";
            var now = new DateTime(2019, 2, 22, 10, 19, 00);
            var deploymentService = InstantiateDeploymentService(databaseManagementSystem, changeLogDataSet, rootDirectory, ioProxy, username, now);
            deploymentService.BuildDeploymentScript();

            // Then our deployment script should contain only fragments from scripts 2 and 3
            var expectedDbDeployScript = @"-- Change Script Generated at 2/22/2019 10:19 by dbdeployer1

DECLARE @currentDatabaseVersion INTEGER, @errMsg VARCHAR(max)
SELECT @currentDatabaseVersion = MAX(change_number) FROM dbo.changelog

IF (@currentDatabaseVersion <> 1)
BEGIN
    SET @errMsg = 'Error: current database version is not 1, but ' + CONVERT(VARCHAR, @currentDatabaseVersion)
    RAISERROR (@errMsg, 18, 1)
END
GO
--------------- Fragment begins: X:\dev\projectX\db\deltas\2 AddDescColumnToMarketTable.sql ---------------
PRINT 'Executing X:\dev\projectX\db\deltas\2 AddDescColumnToMarketTable.sql'
BEGIN TRANSACTION

ALTER TABLE Market ADD COLUMN Description

INSERT INTO changelog (change_number, complete_dt, applied_by, description)
VALUES (2, GetDate(), SYSTEM_USER, 'AddDescColumnToMarketTable')

COMMIT TRANSACTION
GO

--------------- Fragment ends: X:\dev\projectX\db\deltas\2 AddDescColumnToMarketTable.sql ---------------
--------------- Fragment begins: X:\dev\projectX\db\deltas\3 AddUniqueKeyToMarketTable.sql ---------------
PRINT 'Executing X:\dev\projectX\db\deltas\3 AddUniqueKeyToMarketTable.sql'
BEGIN TRANSACTION

CREATE UNIQUE NONCLUSTERED INDEX UQ_Market_Name

INSERT INTO changelog (change_number, complete_dt, applied_by, description)
VALUES (3, GetDate(), SYSTEM_USER, 'AddUniqueKeyToMarketTable')

COMMIT TRANSACTION
GO

--------------- Fragment ends: X:\dev\projectX\db\deltas\3 AddUniqueKeyToMarketTable.sql ---------------
GO

-- Script generation completed at 2/22/2019 10:19
";
            var outputFileName = Container.UnityContainer.Resolve<IConfigurationService>().OutputFile;
            Assert.AreEqual(expectedDbDeployScript, ioProxy.ReadAllText(outputFileName));
        }

        private static IDeploymentService InstantiateDeploymentService(DatabaseTypesEnum databaseManagementSystem, DataSet changeLogDataSet, string rootDirectory, BogusIoProxy ioProxy, string username, DateTime now)
        {
            MockEnvironmentProvider mockEnvironmentProvider = new MockEnvironmentProvider();
            mockEnvironmentProvider.SetUserName(username);
            EnvironmentProvider.Current = mockEnvironmentProvider;

            TimeProvider.Current = new MockTimeProvider(now);

            var configurationService = Container.UnityContainer.Resolve<IConfigurationService>();
            configurationService.ConnectionString = "Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;";
            configurationService.DatabaseManagementSystem = databaseManagementSystem;
            configurationService.RootDirectory = rootDirectory;

            ioProxy.AddRealFilesFromDirectory(configurationService.DatabaseScriptPath);
            Container.UnityContainer.RegisterInstance(typeof(IIoProxy), ioProxy);

            var bogusDatabaseService = new BogusDatabaseMock(configurationService, Container.UnityContainer.Resolve<IFileService>(), Container.UnityContainer.Resolve<ITokenReplacer>())
            {
                DataSetToReturn = changeLogDataSet
            };
            Container.UnityContainer.RegisterInstance(typeof(IDatabaseService), bogusDatabaseService);

            return Container.UnityContainer.Resolve<IDeploymentService>();
        }

        private DataSet CreateChangeLogDataset(Tuple<decimal, DateTime, string, string>[] appliedChanges)
        {
            DataSet ds = new DataSet();
            ds.Tables.Add("changelog");
            ds.Tables["changelog"].Columns.Add("change_number", typeof(int));
            ds.Tables["changelog"].Columns.Add("complete_dt", typeof(DateTime));
            ds.Tables["changelog"].Columns.Add("applied_by", typeof(string));
            ds.Tables["changelog"].Columns.Add("description", typeof(string));

            foreach (var appliedChange in appliedChanges)
            {
                ds.Tables["changelog"].Rows.Add(appliedChange.Item1, appliedChange.Item2, appliedChange.Item3, appliedChange.Item4);
            }

            return ds;
        }
    }
}