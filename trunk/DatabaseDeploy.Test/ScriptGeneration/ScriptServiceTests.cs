// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ScriptServiceTests.cs" company="Database Deploy 2">
//    Copyright (c) 2015 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace DatabaseDeploy.Test.ScriptGeneration
{
    using System;
    using System.Collections.Generic;

    using ApprovalTests;

    using DatabaseDeploy.Core.Configuration;
    using DatabaseDeploy.Core.Database.DatabaseInstances;
    using DatabaseDeploy.Core.FileManagement;
    using DatabaseDeploy.Core.ScriptGeneration;
    using DatabaseDeploy.Core.Utilities;
    using DatabaseDeploy.Test.Utilities;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     Tests the Script service
    /// </summary>
    [TestClass]
    public class ScriptServiceTests : TestFixtureBase
    {
        private CultureInfo originalCulture;

        [TestInitialize]
        public void TestInitialize()
        {
            this.originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TimeProvider.ResetToDefault();
            EnvironmentProvider.ResetToDefault();
            CultureInfo.CurrentCulture = this.originalCulture;
        }

        /// <summary>
        ///     Ensures that building a change script behaves as expected.
        /// </summary>
        [TestMethod]
        public void ThatBuildChangeScriptWorks()
        {
            DateTime dateTime = new DateTime(2014, 09, 17, 17, 42, 55);
            TimeProvider.Current = new MockTimeProvider(dateTime);
            MockEnvironmentProvider mockEnvironmentProvider = new MockEnvironmentProvider();
            mockEnvironmentProvider.SetUserName("userName");
            EnvironmentProvider.Current = mockEnvironmentProvider;
            string changeScriptHeader =
                "Change Script Header -- CurrentVersion = $(CurrentVersion);Current DateTime = $(CurrentDateTime);Current User = $(CurrentUser)";
            string scriptHeader =
                "Script Header -- Script Name = $(ScriptName);Script Id = $(ScriptId);Script Description = $(ScriptDescription)";
            string scriptFooter = "Script Footer -- String Name = $(ScriptName);Script Id = $(ScriptId)";
            string changeScriptFooter = "Change Script Footer -- Current DateTime = $(CurrentDateTime)";
            string undoToken = "--//@Undo";

            ScriptService constructorTest = new ScriptService();

            Mock<IDatabaseService> databaseServiceMock = new Mock<IDatabaseService>(MockBehavior.Strict);
            Mock<IFileService> fileServiceMock = new Mock<IFileService>(MockBehavior.Strict);

            // Database service mock setup
            databaseServiceMock.Setup(d => d.GetScriptFromFile(DatabaseScriptEnum.ChangeScriptHeader))
                .Returns(changeScriptHeader);
            databaseServiceMock.Setup(d => d.GetScriptFromFile(DatabaseScriptEnum.ScriptHeader)).Returns(scriptHeader);
            databaseServiceMock.Setup(d => d.GetScriptFromFile(DatabaseScriptEnum.ScriptFooter)).Returns(scriptFooter);
            databaseServiceMock.Setup(d => d.GetScriptFromFile(DatabaseScriptEnum.ChangeScriptFooter))
                .Returns(changeScriptFooter);
            databaseServiceMock.Setup(d => d.GetScriptFromFile(DatabaseScriptEnum.UndoToken)).Returns(undoToken);

            // file Service mock setup
            fileServiceMock.Setup(f => f.GetFileContents(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(
                    (string s, bool b) =>
                        {
                            ScriptFile file = new ScriptFile();
                            file.Parse(fileServiceMock.Object, s);
                            if (file.Id % 2 == 0)
                            {
                                return file.Description + undoToken + "Undo text.";
                            }
                            return file.Description;
                        });

            IDictionary<decimal, IScriptFile> changes = this.GetChanges();
            Mock<IConfigurationService> configurationService = new Mock<IConfigurationService>();
            configurationService.Setup(c => c.DatabaseService).Returns(databaseServiceMock.Object);

            IScriptService scriptService = new ScriptService(
                fileServiceMock.Object,
                new TokenReplacer(new ConfigurationService()),
                configurationService.Object);

            string result = scriptService.BuildChangeScript(changes, 0);
            Approvals.Verify(result);
        }

        /// <summary>
        ///     Ensures that build undo script functionality works.
        /// </summary>
        [TestMethod]
        public void ThatBuildUndoScriptWorks()
        {
            DateTime dateTime = new DateTime(2014, 09, 17, 17, 42, 55);
            TimeProvider.Current = new MockTimeProvider(dateTime);
            MockEnvironmentProvider mockEnvironmentProvider = new MockEnvironmentProvider();
            mockEnvironmentProvider.SetUserName("userName");
            EnvironmentProvider.Current = mockEnvironmentProvider;

            string undoScriptHeader =
                "Undo Script Header -- CurrentVersion = $(CurrentVersion);Current DateTime = $(CurrentDateTime);Current User = $(CurrentUser)";
            string undoHeader =
                "Script Header -- Script Name = $(ScriptName);Script Id = $(ScriptId);Script Description = $(ScriptDescription)";
            string undoFooter = "Script Footer -- String Name = $(ScriptName);Script Id = $(ScriptId)";
            string undoScriptFooter = "Undo Script Footer -- Current DateTime = $(CurrentDateTime)";
            string undoToken = "--//@Undo";

            ScriptService constructorTest = new ScriptService();

            Mock<IDatabaseService> databaseServiceMock = new Mock<IDatabaseService>(MockBehavior.Strict);
            Mock<IFileService> fileServiceMock = new Mock<IFileService>(MockBehavior.Strict);

            // Database service mock setup
            databaseServiceMock.Setup(d => d.GetScriptFromFile(DatabaseScriptEnum.UndoScriptHeader))
                .Returns(undoScriptHeader);
            databaseServiceMock.Setup(d => d.GetScriptFromFile(DatabaseScriptEnum.UndoHeader)).Returns(undoHeader);
            databaseServiceMock.Setup(d => d.GetScriptFromFile(DatabaseScriptEnum.UndoFooter)).Returns(undoFooter);
            databaseServiceMock.Setup(d => d.GetScriptFromFile(DatabaseScriptEnum.UndoScriptFooter))
                .Returns(undoScriptFooter);
            databaseServiceMock.Setup(d => d.GetScriptFromFile(DatabaseScriptEnum.UndoToken)).Returns(undoToken);

            Mock<IFileService> fsMock2 = new Mock<IFileService>(MockBehavior.Strict);
            fsMock2.Setup(f => f.GetFileContents(It.IsAny<string>(), It.IsAny<bool>())).Returns(string.Empty);

            // file Service mock setup
            fileServiceMock.Setup(f => f.GetFileContents(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(
                    (string s, bool b) =>
                        {
                            ScriptFile file = new ScriptFile();
                            file.Parse(fsMock2.Object, s);
                            if (file.Id % 2 == 0)
                            {
                                return file.Description + undoToken + "Undo text.";
                            }
                            return file.Description;
                        });

            IDictionary<decimal, IScriptFile> changes = this.GetChanges();
            Mock<IConfigurationService> configurationService = new Mock<IConfigurationService>();
            configurationService.Setup(c => c.DatabaseService).Returns(databaseServiceMock.Object);

            IScriptService scriptService = new ScriptService(
                fileServiceMock.Object,
                new TokenReplacer(new ConfigurationService()),
                configurationService.Object);

            string result = scriptService.BuildUndoScript(changes);
            Approvals.Verify(result);
        }

        /// <summary>
        ///     Adds a single change to the dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary to add changes to</param>
        /// <param name="change">The change number ot add.</param>
        private void AddChange(IDictionary<decimal, IScriptFile> dictionary, int change)
        {
            Mock<IFileService> fileServiceMock = new Mock<IFileService>();
            fileServiceMock.Setup(f => f.GetFileContents(It.IsAny<string>(), It.IsAny<bool>())).Returns(string.Empty);
            ScriptFile script = new ScriptFile();
            script.Parse(fileServiceMock.Object, string.Format("{0} {0}.sql", change));
            dictionary.Add(change, script);
        }

        /// <summary>
        ///     Gets a set of changes for testing.
        /// </summary>
        /// <returns>A dictionary with the changes to test.</returns>
        private IDictionary<decimal, IScriptFile> GetChanges()
        {
            Dictionary<decimal, IScriptFile> changes = new Dictionary<decimal, IScriptFile>();
            this.AddChange(changes, 1);
            this.AddChange(changes, 2);
            this.AddChange(changes, 3);

            return changes;
        }
    }
}