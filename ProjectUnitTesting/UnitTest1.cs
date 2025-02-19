using StatusApp;
using Xunit;
using Xunit.Abstractions;

namespace ProjectUnitTesting
{
    public class UnitTest1
    {
        DeploymentMethods deploymentMethods = new DeploymentMethods();

        private readonly ITestOutputHelper _output;

        public UnitTest1(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void GetBackupPath_ValidInput_ReturnsCorrectPath()
        {
            string backupFolderPath = "C:\\Backups";

            DateTime backupStamp = new DateTime(2025, 02, 19, 09, 00, 00);

            string result = deploymentMethods.GetBackupPath(backupFolderPath, backupStamp);

            _output.WriteLine(result);

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

            int folderCount = 0, fileCount = 0;

            //Call the method 
            var result = deploymentMethods.CopyDirectory(originDir, targetDir, ref folderCount, ref fileCount, true);

            //Check result
            Assert.True(Directory.Exists(targetDir));
            Assert.True(File.Exists(Path.Combine(targetDir, "testFile.txt")));
            Assert.True(Directory.Exists(Path.Combine(targetDir, "subDir")));
            Assert.True(File.Exists(Path.Combine(targetDir, "subDir", "subFile.txt")));
            //Assert.Contains("Created", result);
            Assert.Equal(" Created 2 Folders & 2 Files", result);

            //Delete dirs and files for reTesting Correctly
            Directory.Delete(targetDir, true);

        }

        [Fact]
        public void CopyDirectory_Shoul_Not_Duplicate_Existing()
        {
            string originDir = Path.Combine(Path.GetTempPath(), "originDir");
            string targetDir = Path.Combine(Path.GetTempPath(), "targetDir2");
            Directory.CreateDirectory(originDir);
            File.WriteAllText(Path.Combine(originDir, "testFile.txt"), "Hello World");
            Directory.CreateDirectory(targetDir);
            File.WriteAllText(Path.Combine(targetDir, "testFile.txt"), "Hello World");

            int folderCount = 0, fileCount = 0;

            var result = deploymentMethods.CopyDirectory(originDir, targetDir, ref folderCount, ref fileCount, true);

            Assert.Equal(" All files are similar. No new files to create ", result);
        }
    }
}