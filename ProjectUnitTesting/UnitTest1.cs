using StatusApp;


namespace ProjectUnitTesting
{
    public class UnitTest1
    {
        DeploymentMethods deploymentMethods = new DeploymentMethods();

        [Fact]
        public void GetBackupPath_ValidInput_ReturnsCorrectPath()
        {
            string backupFolderPath = "C:\\Backups";

            DateTime backupStamp = new DateTime(2025, 02, 19, 09, 00, 00);

            string result = deploymentMethods.GetBackupPath(backupFolderPath, backupStamp);

            Assert.Equal($"C:\\Backups\\{StatusApp.DeploymentMethods.BackupFolderName}_2025-02-19_09-00-00", result);

        }

        [Fact]
        public void GetBackupPath_NullFolderPath_ThrowsException()
        {
            //could be due to wrong path in configFile
            string backupFolderPath = null;

            DateTime backupStamp = new DateTime(2025, 02, 19, 09, 00, 00);

            Assert.Throws<ArgumentNullException>(() => deploymentMethods.GetBackupPath(backupFolderPath, backupStamp));
        }

        [Fact]
        public void CopyDirectory_Should_Copy_All_Files_And_Folders()
        {
            string originDir = Path.Combine(Path.GetTempPath(), "originDir");//copy from path
            string targetDir = Path.Combine(Path.GetTempPath(), "targetDir");//copy to path

            Directory.CreateDirectory(originDir);//create the copy from directory
            File.WriteAllText(Path.Combine(originDir, "testFile.txt"), "Hello World");//create a txt file in the copy from dir with hello world
            string subDir = Path.Combine(originDir, "subDir");//copy from subDirectory
            Directory.CreateDirectory(subDir);//create the copy from subDir
            File.WriteAllText(Path.Combine(subDir, "subFile.txt"), "Nested File");//create a file in the copy from subDir 

            //Call the method 
            var result = deploymentMethods.CopyDirectory(originDir, targetDir, true);
            int folderCount = result.Item1;
            int fileCount = result.Item2;

            //Check result
            Assert.True(Directory.Exists(targetDir));
            Assert.True(File.Exists(Path.Combine(targetDir, "testFile.txt")));
            Assert.True(Directory.Exists(Path.Combine(targetDir, "subDir")));
            Assert.True(File.Exists(Path.Combine(targetDir, "subDir", "subFile.txt")));
            Assert.Equal(2, folderCount);
            Assert.Equal(2, fileCount);

            //Delete dirs and files for reTesting Correctly
            Directory.Delete(targetDir, true);
        }

        [Fact]
        public void CopyDirectory_Should_Not_Duplicate_Existing()
        {
            string originDir = Path.Combine(Path.GetTempPath(), "originDir");
            string targetDir = Path.Combine(Path.GetTempPath(), "targetDir2");
            Directory.CreateDirectory(originDir);
            File.WriteAllText(Path.Combine(originDir, "testFile.txt"), "Hello World");
            Directory.CreateDirectory(targetDir);
            File.WriteAllText(Path.Combine(targetDir, "testFile.txt"), "Hello World");

            var result = deploymentMethods.CopyDirectory(originDir, targetDir, true);
            int folderCount = result.Item1, fileCount = result.Item2;

            Assert.Equal(0, folderCount);
            Assert.Equal(0, fileCount);
        }

        [Fact]
        public void BackupFiles_Should_Backup_Folder()
        {
            string sourceDir = Path.Combine(Path.GetTempPath(), "sourceDir3");
            string destDir = Path.Combine(Path.GetTempPath(), "destDir3");
            string item = "testFolder";
            string backupLogFile = "log.txt";
            Directory.CreateDirectory(Path.Combine(sourceDir, item));

            var result = deploymentMethods.BackupFiles(sourceDir, destDir, item, backupLogFile, true);

            int backupFolderCount = result.Item1;

            Assert.True(Directory.Exists(Path.Combine(destDir, item)));
            Assert.Equal(1, backupFolderCount);

            Directory.Delete(Path.Combine(destDir, item));
        }

        [Fact]
        public void BackupFiles_Should_Backup_File()
        {
            string sourceDir = Path.Combine(Path.GetTempPath(), "sourceDir3");
            string destDir = Path.Combine(Path.GetTempPath(), "destDir3");
            string item = "testFile.txt";
            string backupLogFile = "log.txt";
            Directory.CreateDirectory(sourceDir);
            File.WriteAllText(Path.Combine(sourceDir, item), "Hello");

            var result = deploymentMethods.BackupFiles(sourceDir, destDir, item, backupLogFile, true);
            int backupFileCount = result.Item2; ;

            Assert.True(File.Exists(Path.Combine(destDir, item)));
            Assert.Equal(1, backupFileCount);

            File.Delete(Path.Combine(destDir, item));

        }

        [Fact]
        public void BackupSource_Should_Move_All_Files_And_Folders()
        {
            string sourceFolder = Path.Combine(Path.GetTempPath(), "sourceFolder");
            string destinationFolder = Path.Combine(Path.GetTempPath(), "destinationFolder");

            Directory.CreateDirectory(sourceFolder);
            Directory.CreateDirectory(destinationFolder);
            File.WriteAllText(Path.Combine(sourceFolder, "file2.txt"), "Test File");
            string subFolder = Path.Combine(sourceFolder, "subFolder");
            Directory.CreateDirectory(subFolder);
            File.WriteAllText(Path.Combine(subFolder, "file3.txt"), "Test Sub File");

            deploymentMethods.BackupSource(sourceFolder, destinationFolder);

            Assert.True(!Directory.Exists(Path.Combine(sourceFolder, "subFolder")));

            Assert.True(File.Exists(Path.Combine(destinationFolder, "file2.txt")));
            Assert.True(Directory.Exists(Path.Combine(destinationFolder, "subFolder")));
            Assert.True(File.Exists(Path.Combine(destinationFolder, "subFolder", "file3.txt")));

            Directory.Delete(sourceFolder, true);
            Directory.Delete(destinationFolder, true);
        }

        [Fact]
        public void RollBack_Should_Replace_Common_Files()
        {
            string backupFolder = Path.Combine(Path.GetTempPath(), "rollbackBackupFolder");
            string sourceDir = Path.Combine(Path.GetTempPath(), "rollbackSourceDir");
            string destDir = Path.Combine(Path.GetTempPath(), "rollbackDestDir");
            Directory.CreateDirectory(backupFolder);
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destDir);
            List<string> commonFiles = new List<string> { "file1.txt", "file2.txt" };
            File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "test1");
            File.WriteAllText(Path.Combine(sourceDir, "file2.txt"), "test2");
            File.WriteAllText(Path.Combine(destDir, "file1.txt"), "old");
            File.WriteAllText(Path.Combine(destDir, "file2.txt"), "old");

            deploymentMethods.RollBackItems(backupFolder, sourceDir, destDir, commonFiles);

            string expectedContentFile1 = File.ReadAllText(Path.Combine(sourceDir, "file1.txt"));
            string expectedContentFile2 = File.ReadAllText(Path.Combine(sourceDir, "file2.txt"));

            string actualContentFile1 = File.ReadAllText(Path.Combine(destDir, "file1.txt"));
            string actualContentFile2 = File.ReadAllText(Path.Combine(destDir, "file2.txt"));

            Assert.Equal(actualContentFile1, expectedContentFile1);
            Assert.Equal(actualContentFile2, expectedContentFile2);

            Directory.Delete(sourceDir, true);
            Directory.Delete(destDir, true);
            Directory.Delete(backupFolder, true);

        }

        [Fact]
        public void DeleteItems_DeletsFilesAndFolders_Correctly()
        {
            var itemsToDelete = new List<string>
            {
                Path.GetTempFileName(),
                Path.Combine(Path.GetTempPath(),Path.GetRandomFileName())
            };

            Directory.CreateDirectory(itemsToDelete[1]);

            var result = deploymentMethods.DeleteItems(itemsToDelete);
            int deletedFolders = result.Item1;
            int deletedFiles = result.Item2;

            Assert.Equal(1, deletedFiles);
            Assert.Equal(1, deletedFolders);
            Assert.True(!File.Exists(itemsToDelete[0]));
            Assert.True(!Directory.Exists(itemsToDelete[1]));

        }

        [Fact]
        public void GetUnwantedBackups_Should_Sort_And_Return()
        {
            string backupFolder = Path.Combine(Path.GetTempPath(), "BackupFolderTest");

            Directory.CreateDirectory(backupFolder);

            var backups = new List<string>
            {
                Path.Combine(backupFolder,"backup1"),
                Path.Combine(backupFolder,"backup2"),
                Path.Combine(backupFolder,"backup3"),
                Path.Combine(backupFolder,"backup4"),
                Path.Combine(backupFolder,"backup5")
            };

            int keepBackupsCount = 3;

            var result = deploymentMethods.GetUnwantedBackups(backups, keepBackupsCount);

            List<string> backupsToDelete = result.Item1;
            int backupsToDeleteCount = result.Item2;

            var expectedBackupsToDelete = new List<string>
            {
                Path.Combine(backupFolder,"backup4"),
                Path.Combine(backupFolder,"backup5"),
            };

            Assert.Equal(2, backupsToDeleteCount);
            Assert.Equal(expectedBackupsToDelete, backupsToDelete);

            Directory.Delete(backupFolder, true);
        }

    }
}