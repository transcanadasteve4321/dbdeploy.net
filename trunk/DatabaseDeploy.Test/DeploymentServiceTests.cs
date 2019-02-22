// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="DeploymentServiceTests.cs" company="Database Deploy 2">
//    Copyright (c) 2015 Database Deploy 2.  This code is licensed under the Microsoft Public License (MS-PL).  http://www.opensource.org/licenses/MS-PL.
//  </copyright>
//   <summary>
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.IO;
using System.Linq;
using DatabaseDeploy.Test.Database;
using DatabaseDeploy.Test.Utilities;

namespace DatabaseDeploy.Test
{
    using System.Collections.Generic;

    using DatabaseDeploy.Core;
    using DatabaseDeploy.Core.Configuration;
    using DatabaseDeploy.Core.Database;
    using DatabaseDeploy.Core.Database.DatabaseInstances;
    using DatabaseDeploy.Core.FileManagement;
    using DatabaseDeploy.Core.IoC;
    using DatabaseDeploy.Core.ScriptGeneration;
    using DatabaseDeploy.Core.Utilities;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <summary>
    ///     Tests the deployment Service.
    /// </summary>
    [TestClass]
    public class DeploymentServiceTests : TestFixtureBase
    {
        /// <summary>
        ///     Tests the BuildDeploymentScript method, including getting changes that need to be made.
        /// </summary>
        [TestMethod]
        public void ThatBuildScriptDeploymentWorks()
        {
            Mock<IDatabaseService> databaseServiceMock = new Mock<IDatabaseService>(MockBehavior.Strict);
            Mock<IConfigurationService> configurationServiceMock = new Mock<IConfigurationService>(MockBehavior.Strict);
            Mock<IScriptService> scriptServiceMock = new Mock<IScriptService>(MockBehavior.Strict);
            Mock<IFileService> fileServiceMock = new Mock<IFileService>(MockBehavior.Strict);
            Mock<IScriptMessageFormatter> scriptMessageFormatterMock =
                new Mock<IScriptMessageFormatter>(MockBehavior.Strict);

            IDictionary<decimal, IScriptFile> availableScripts = this.GetScripts();
            IDictionary<decimal, IChangeLog> changeLogs = this.GetChangeLogs();

            string changeScript = "Change Script";

            // FileService Setup
            fileServiceMock.Setup(file => file.CleanupPastRuns()).Verifiable();
            fileServiceMock.Setup(file => file.GetScriptFiles()).Returns(availableScripts).Verifiable();
            fileServiceMock.Setup(file => file.WriteChangeScript(It.Is<string>(s => s == changeScript))).Verifiable();
            fileServiceMock.Setup(file => file.WriteUndoScript(It.Is<string>(s => s == changeScript))).Verifiable();
            fileServiceMock.Setup(file => file.WriteScriptList(It.IsAny<Dictionary<decimal, IScriptFile>>()))
                .Verifiable();

            // Database Service Setup
            databaseServiceMock.Setup(db => db.GetAppliedChanges()).Returns(changeLogs).Verifiable();

            // Configuration Service Setup
            configurationServiceMock.Setup(config => config.LastChangeToApply).Returns(1000).Verifiable();
            configurationServiceMock.Setup(config => config.OutputFile).Returns("File Location").Verifiable();
            configurationServiceMock.Setup(config => config.UndoOutputFile).Returns("File Location").Verifiable();
            configurationServiceMock.Setup(config => config.ScriptListFile)
                .Returns("ScriptListFileLocation")
                .Verifiable();

            // Script Formatter setup
            scriptMessageFormatterMock.Setup(fm => fm.FormatCollection(It.IsAny<ICollection<decimal>>()))
                .Returns("String Formatted.")
                .Verifiable();

            // Script Service Setup
            scriptServiceMock.Setup(script => script.BuildChangeScript(It.IsAny<IDictionary<decimal, IScriptFile>>(), 5))
                .Returns(changeScript)
                .Verifiable();
            scriptServiceMock.Setup(script => script.BuildUndoScript(It.IsAny<IDictionary<decimal, IScriptFile>>()))
                .Returns(changeScript)
                .Verifiable();

            configurationServiceMock.Setup(c => c.DatabaseService).Returns(databaseServiceMock.Object);

            DeploymentService deploymentService = new DeploymentService(
                configurationServiceMock.Object,
                scriptServiceMock.Object,
                fileServiceMock.Object,
                scriptMessageFormatterMock.Object);

            deploymentService.BuildDeploymentScript();

            // File Verifies
            fileServiceMock.Verify(file => file.CleanupPastRuns(), Times.Exactly(1));
            fileServiceMock.Verify(file => file.GetScriptFiles(), Times.Exactly(1));
            fileServiceMock.Verify(
                file => file.WriteChangeScript(It.Is<string>(s => s == changeScript)),
                Times.Exactly(1));
            fileServiceMock.Verify(
                file => file.WriteUndoScript(It.Is<string>(s => s == changeScript)),
                Times.Exactly(1));
            fileServiceMock.Verify(
                file => file.WriteScriptList(It.IsAny<Dictionary<decimal, IScriptFile>>()),
                Times.Exactly(1));

            // Database Verifies
            databaseServiceMock.Verify(db => db.GetAppliedChanges(), Times.Exactly(1));

            // Configuration Service Verifies
            configurationServiceMock.Verify(config => config.LastChangeToApply, Times.AtLeastOnce());
            configurationServiceMock.Verify(config => config.OutputFile, Times.AtLeastOnce());
            configurationServiceMock.Verify(config => config.UndoOutputFile, Times.AtLeastOnce());
            configurationServiceMock.Verify(config => config.ScriptListFile, Times.AtLeastOnce());

            // Script Formamter Verifies.
            scriptMessageFormatterMock.Verify(
                fm => fm.FormatCollection(It.IsAny<ICollection<decimal>>()),
                Times.AtLeastOnce());

            // Script Service Verifies
            scriptServiceMock.Verify(
                script => script.BuildChangeScript(It.IsAny<IDictionary<decimal, IScriptFile>>(), 5),
                Times.Exactly(1));
            scriptServiceMock.Verify(
                script => script.BuildUndoScript(It.IsAny<IDictionary<decimal, IScriptFile>>()),
                Times.Exactly(1));
        }

        /// <summary>
        ///     Tests the BuildDeploymentScript method, including getting changes that need to be made.
        /// </summary>
        [TestMethod]
        public void ThatLastChangesToApplyDetected()
        {
            Mock<IDatabaseService> databaseServiceMock = new Mock<IDatabaseService>(MockBehavior.Strict);
            Mock<IConfigurationService> configurationServiceMock = new Mock<IConfigurationService>(MockBehavior.Strict);
            Mock<IScriptService> scriptServiceMock = new Mock<IScriptService>(MockBehavior.Strict);
            Mock<IFileService> fileServiceMock = new Mock<IFileService>(MockBehavior.Strict);
            Mock<IScriptMessageFormatter> scriptMessageFormatterMock =
                new Mock<IScriptMessageFormatter>(MockBehavior.Strict);

            IDictionary<decimal, IScriptFile> availableScripts = this.GetScripts();
            IDictionary<decimal, IChangeLog> changeLogs = this.GetChangeLogs();

            string changeScript = "Change Script";

            // FileService Setup
            fileServiceMock.Setup(file => file.CleanupPastRuns()).Verifiable();
            fileServiceMock.Setup(file => file.GetScriptFiles()).Returns(availableScripts).Verifiable();
            fileServiceMock.Setup(file => file.WriteChangeScript(It.Is<string>(s => s == changeScript))).Verifiable();
            fileServiceMock.Setup(file => file.WriteUndoScript(It.Is<string>(s => s == changeScript))).Verifiable();
            fileServiceMock.Setup(file => file.WriteScriptList(It.IsAny<Dictionary<decimal, IScriptFile>>()))
                .Verifiable();

            // Database Service Setup
            databaseServiceMock.Setup(db => db.GetAppliedChanges()).Returns(changeLogs).Verifiable();

            // Configuration Service Setup
            configurationServiceMock.Setup(config => config.LastChangeToApply).Returns(7).Verifiable();
            configurationServiceMock.Setup(config => config.OutputFile).Returns("File Location").Verifiable();
            configurationServiceMock.Setup(config => config.UndoOutputFile).Returns("File Location").Verifiable();
            configurationServiceMock.Setup(config => config.ScriptListFile)
                .Returns("ScriptListFileLocation")
                .Verifiable();

            // Script Formatter setup
            scriptMessageFormatterMock.Setup(fm => fm.FormatCollection(It.IsAny<ICollection<decimal>>()))
                .Returns("String Formatted.")
                .Verifiable();

            // Script Service Setup
            int passedCount = 0;
            scriptServiceMock.Setup(script => script.BuildChangeScript(It.IsAny<IDictionary<decimal, IScriptFile>>(), 5))
                .Callback<IDictionary<decimal, IScriptFile>, decimal>((files, currentDbVersion) => passedCount = files.Count)
                .Returns(changeScript)
                .Verifiable();
            scriptServiceMock.Setup(script => script.BuildUndoScript(It.IsAny<IDictionary<decimal, IScriptFile>>()))
                .Callback<IDictionary<decimal, IScriptFile>>(files => passedCount = files.Count)
                .Returns(changeScript)
                .Verifiable();

            configurationServiceMock.Setup(c => c.DatabaseService).Returns(databaseServiceMock.Object);

            DeploymentService deploymentService = new DeploymentService(
                configurationServiceMock.Object,
                scriptServiceMock.Object,
                fileServiceMock.Object,
                scriptMessageFormatterMock.Object);

            deploymentService.BuildDeploymentScript();

            // File Verifies
            fileServiceMock.Verify(file => file.CleanupPastRuns(), Times.Exactly(1));
            fileServiceMock.Verify(file => file.GetScriptFiles(), Times.Exactly(1));
            fileServiceMock.Verify(
                file => file.WriteChangeScript(It.Is<string>(s => s == changeScript)),
                Times.Exactly(1));
            fileServiceMock.Verify(
                file => file.WriteScriptList(It.IsAny<Dictionary<decimal, IScriptFile>>()),
                Times.Exactly(1));

            // Database Verifies
            databaseServiceMock.Verify(db => db.GetAppliedChanges(), Times.Exactly(1));

            // Configuration Service Verifies
            configurationServiceMock.Verify(config => config.LastChangeToApply, Times.AtLeastOnce());
            configurationServiceMock.Verify(config => config.OutputFile, Times.AtLeastOnce());
            configurationServiceMock.Verify(config => config.UndoOutputFile, Times.AtLeastOnce());
            configurationServiceMock.Verify(config => config.ScriptListFile, Times.AtLeastOnce());

            // Script Formamter Verifies.
            scriptMessageFormatterMock.Verify(
                fm => fm.FormatCollection(It.IsAny<ICollection<decimal>>()),
                Times.AtLeastOnce());

            // Script Service Verifies
            scriptServiceMock.Verify(
                script => script.BuildChangeScript(It.IsAny<IDictionary<decimal, IScriptFile>>(), 5),
                Times.Exactly(1));
            scriptServiceMock.Verify(
                script => script.BuildUndoScript(It.IsAny<IDictionary<decimal, IScriptFile>>()),
                Times.Exactly(1));
            Assert.AreEqual(passedCount, 2);
        }

        /// <summary>
        ///     Tests the BuildDeploymentScript method, including getting changes that need to be made.
        /// </summary>
        [TestMethod]
        public void ThatNoChangesDetected()
        {
            Mock<IDatabaseService> databaseServiceMock = new Mock<IDatabaseService>(MockBehavior.Strict);
            Mock<IConfigurationService> configurationServiceMock = new Mock<IConfigurationService>(MockBehavior.Strict);
            Mock<IScriptService> scriptServiceMock = new Mock<IScriptService>(MockBehavior.Strict);
            Mock<IFileService> fileServiceMock = new Mock<IFileService>(MockBehavior.Strict);
            Mock<IScriptMessageFormatter> scriptMessageFormatterMock =
                new Mock<IScriptMessageFormatter>(MockBehavior.Strict);

            IDictionary<decimal, IScriptFile> availableScripts = this.GetScripts();
            IDictionary<decimal, IChangeLog> changeLogs = this.GetChangeLogsAll();

            string changeScript = "Change Script";

            // FileService Setup
            fileServiceMock.Setup(file => file.CleanupPastRuns()).Verifiable();
            fileServiceMock.Setup(file => file.GetScriptFiles()).Returns(availableScripts).Verifiable();
            fileServiceMock.Setup(file => file.WriteChangeScript(It.Is<string>(s => s == changeScript))).Verifiable();
            fileServiceMock.Setup(file => file.WriteScriptList(It.IsAny<Dictionary<decimal, IScriptFile>>()))
                .Verifiable();

            // Database Service Setup
            databaseServiceMock.Setup(db => db.GetAppliedChanges()).Returns(changeLogs).Verifiable();

            // Configuration Service Setup
            configurationServiceMock.Setup(config => config.LastChangeToApply).Returns(1000).Verifiable();
            configurationServiceMock.Setup(config => config.OutputFile).Returns("File Location").Verifiable();
            configurationServiceMock.Setup(config => config.UndoOutputFile).Returns("File Location").Verifiable();
            configurationServiceMock.Setup(config => config.ScriptListFile)
                .Returns("ScriptListFileLocation")
                .Verifiable();

            // Script Formatter setup
            scriptMessageFormatterMock.Setup(fm => fm.FormatCollection(It.IsAny<ICollection<decimal>>()))
                .Returns("String Formatted.")
                .Verifiable();

            // Script Service Setup
            scriptServiceMock.Setup(script => script.BuildChangeScript(It.IsAny<IDictionary<decimal, IScriptFile>>(), 0))
                .Returns(changeScript)
                .Verifiable();
            scriptServiceMock.Setup(script => script.BuildUndoScript(It.IsAny<IDictionary<decimal, IScriptFile>>()))
                .Returns(changeScript)
                .Verifiable();

            configurationServiceMock.Setup(c => c.DatabaseService).Returns(databaseServiceMock.Object);

            DeploymentService deploymentService = new DeploymentService(
                configurationServiceMock.Object,
                scriptServiceMock.Object,
                fileServiceMock.Object,
                scriptMessageFormatterMock.Object);

            deploymentService.BuildDeploymentScript();

            // File Verifies
            fileServiceMock.Verify(file => file.CleanupPastRuns(), Times.Exactly(1));
            fileServiceMock.Verify(file => file.GetScriptFiles(), Times.Exactly(1));
            fileServiceMock.Verify(
                file => file.WriteChangeScript(It.Is<string>(s => s == changeScript)),
                Times.Exactly(0));
            fileServiceMock.Verify(
                file => file.WriteScriptList(It.IsAny<Dictionary<decimal, IScriptFile>>()),
                Times.Exactly(1));

            // Database Verifies
            databaseServiceMock.Verify(db => db.GetAppliedChanges(), Times.Exactly(1));

            // Configuration Service Verifies
            configurationServiceMock.Verify(config => config.LastChangeToApply, Times.Never());
            configurationServiceMock.Verify(config => config.OutputFile, Times.Never());
            configurationServiceMock.Verify(config => config.UndoOutputFile, Times.Never());
            configurationServiceMock.Verify(config => config.ScriptListFile, Times.AtLeastOnce());

            // Script Formamter Verifies.
            scriptMessageFormatterMock.Verify(
                fm => fm.FormatCollection(It.IsAny<ICollection<decimal>>()),
                Times.AtLeastOnce());

            // Script Service Verifies
            scriptServiceMock.Verify(
                script => script.BuildChangeScript(It.IsAny<IDictionary<decimal, IScriptFile>>(), 0),
                Times.Never());
            scriptServiceMock.Verify(
                script => script.BuildUndoScript(It.IsAny<IDictionary<decimal, IScriptFile>>()),
                Times.Never());
        }

        /// <summary>
        ///     Tests the BuildDeploymentScript method, including getting changes that need to be made.
        /// </summary>
        [TestMethod]
        public void ThatNoScriptsDoesntFail()
        {
            Mock<IDatabaseService> databaseServiceMock = new Mock<IDatabaseService>(MockBehavior.Strict);
            Mock<IConfigurationService> configurationServiceMock = new Mock<IConfigurationService>(MockBehavior.Strict);
            Mock<IScriptService> scriptServiceMock = new Mock<IScriptService>(MockBehavior.Strict);
            Mock<IFileService> fileServiceMock = new Mock<IFileService>(MockBehavior.Strict);
            Mock<IScriptMessageFormatter> scriptMessageFormatterMock =
                new Mock<IScriptMessageFormatter>(MockBehavior.Strict);

            IDictionary<decimal, IScriptFile> availableScripts = new Dictionary<decimal, IScriptFile>();
            IDictionary<decimal, IChangeLog> changeLogs = new Dictionary<decimal, IChangeLog>();

            string changeScript = "Change Script";

            // FileService Setup
            fileServiceMock.Setup(file => file.CleanupPastRuns()).Verifiable();
            fileServiceMock.Setup(file => file.GetScriptFiles()).Returns(availableScripts).Verifiable();
            fileServiceMock.Setup(file => file.WriteChangeScript(It.Is<string>(s => s == changeScript))).Verifiable();
            fileServiceMock.Setup(file => file.WriteScriptList(It.IsAny<Dictionary<decimal, IScriptFile>>()))
                .Verifiable();

            // Database Service Setup
            databaseServiceMock.Setup(db => db.GetAppliedChanges()).Returns(changeLogs).Verifiable();

            // Configuration Service Setup
            configurationServiceMock.Setup(config => config.LastChangeToApply).Returns(1000).Verifiable();
            configurationServiceMock.Setup(config => config.OutputFile).Returns("File Location").Verifiable();
            configurationServiceMock.Setup(config => config.ScriptListFile)
                .Returns("ScriptListFileLocation")
                .Verifiable();
            configurationServiceMock.Setup(config => config.RootDirectory).Returns("Root Directory").Verifiable();

            // Script Formatter setup
            scriptMessageFormatterMock.Setup(fm => fm.FormatCollection(It.IsAny<ICollection<decimal>>()))
                .Returns("String Formatted.")
                .Verifiable();

            // Script Service Setup
            scriptServiceMock.Setup(script => script.BuildChangeScript(It.IsAny<IDictionary<decimal, IScriptFile>>(), 0))
                .Returns(changeScript)
                .Verifiable();

            configurationServiceMock.Setup(c => c.DatabaseService).Returns(databaseServiceMock.Object);

            DeploymentService deploymentService = new DeploymentService(
                configurationServiceMock.Object,
                scriptServiceMock.Object,
                fileServiceMock.Object,
                scriptMessageFormatterMock.Object);

            deploymentService.BuildDeploymentScript();

            // File Verifies
            fileServiceMock.Verify(file => file.CleanupPastRuns(), Times.Exactly(1));
            fileServiceMock.Verify(file => file.GetScriptFiles(), Times.Exactly(1));
            fileServiceMock.Verify(file => file.WriteChangeScript(It.Is<string>(s => s == changeScript)), Times.Never());
            fileServiceMock.Verify(
                file => file.WriteScriptList(It.IsAny<Dictionary<decimal, IScriptFile>>()),
                Times.Never());

            // Database Verifies
            databaseServiceMock.Verify(db => db.GetAppliedChanges(), Times.Exactly(0));

            // Configuration Service Verifies
            configurationServiceMock.Verify(config => config.LastChangeToApply, Times.Never());
            configurationServiceMock.Verify(config => config.OutputFile, Times.Never());
            configurationServiceMock.Verify(config => config.ScriptListFile, Times.Never());
            configurationServiceMock.Verify(config => config.RootDirectory, Times.AtLeastOnce());

            // Script Formamter Verifies.
            scriptMessageFormatterMock.Verify(
                fm => fm.FormatCollection(It.IsAny<ICollection<decimal>>()),
                Times.Never());

            // Script Service Verifies
            scriptServiceMock.Verify(
                script => script.BuildChangeScript(It.IsAny<IDictionary<decimal, IScriptFile>>(), 0),
                Times.Never());
        }

        /// <summary>
        ///     Gets a list of changelogs for testing.
        /// </summary>
        /// <returns>A list of changelogs for testing.</returns>
        private IDictionary<decimal, IChangeLog> GetChangeLogs()
        {
            IDictionary<decimal, IChangeLog> changeLogs = new Dictionary<decimal, IChangeLog>();

            changeLogs.Add(1, new ChangeLog { Description = "1", ChangeNumber = 1 });
            changeLogs.Add(2, new ChangeLog { Description = "2", ChangeNumber = 2 });
            changeLogs.Add(3, new ChangeLog { Description = "3", ChangeNumber = 3 });
            changeLogs.Add(4, new ChangeLog { Description = "4", ChangeNumber = 4 });
            changeLogs.Add(5, new ChangeLog { Description = "5", ChangeNumber = 5 });

            return changeLogs;
        }

        /// <summary>
        ///     Gets a list of changelogs for testing.
        /// </summary>
        /// <returns>A list of changelogs for testing.</returns>
        private IDictionary<decimal, IChangeLog> GetChangeLogsAll()
        {
            IDictionary<decimal, IChangeLog> changeLogs = new Dictionary<decimal, IChangeLog>();

            changeLogs.Add(1, new ChangeLog { Description = "1", ChangeNumber = 1 });
            changeLogs.Add(2, new ChangeLog { Description = "2", ChangeNumber = 2 });
            changeLogs.Add(3, new ChangeLog { Description = "3", ChangeNumber = 3 });
            changeLogs.Add(4, new ChangeLog { Description = "4", ChangeNumber = 4 });
            changeLogs.Add(5, new ChangeLog { Description = "5", ChangeNumber = 5 });
            changeLogs.Add(6, new ChangeLog { Description = "6", ChangeNumber = 6 });
            changeLogs.Add(7, new ChangeLog { Description = "7", ChangeNumber = 7 });
            changeLogs.Add(8, new ChangeLog { Description = "8", ChangeNumber = 8 });
            changeLogs.Add(9, new ChangeLog { Description = "9", ChangeNumber = 9 });
            changeLogs.Add(10, new ChangeLog { Description = "10", ChangeNumber = 10 });

            return changeLogs;
        }

        /// <summary>
        ///     Gets a list of scripts for processing.
        /// </summary>
        /// <returns>A list of scripts.</returns>
        private IDictionary<decimal, IScriptFile> GetScripts()
        {
            IDictionary<decimal, IScriptFile> scripts = new Dictionary<decimal, IScriptFile>();
            scripts.Add(1, new ScriptFile { Description = "1", FileName = "FileName1.sql", Id = 1 });
            scripts.Add(2, new ScriptFile { Description = "2", FileName = "FileName2.sql", Id = 2 });
            scripts.Add(3, new ScriptFile { Description = "3", FileName = "FileName3.sql", Id = 3 });
            scripts.Add(4, new ScriptFile { Description = "4", FileName = "FileName4.sql", Id = 4 });
            scripts.Add(5, new ScriptFile { Description = "5", FileName = "FileName5.sql", Id = 5 });
            scripts.Add(6, new ScriptFile { Description = "6", FileName = "FileName6.sql", Id = 6 });
            scripts.Add(7, new ScriptFile { Description = "7", FileName = "FileName7.sql", Id = 7 });
            scripts.Add(8, new ScriptFile { Description = "8", FileName = "FileName8.sql", Id = 8 });
            scripts.Add(9, new ScriptFile { Description = "9", FileName = "FileName9.sql", Id = 9 });
            scripts.Add(10, new ScriptFile { Description = "10", FileName = "FileName10.sql", Id = 10 });

            return scripts;
        }
    }
}