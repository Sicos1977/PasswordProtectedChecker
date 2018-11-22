using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PasswordProtectedChecker;

namespace PasswordProtectedTester
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void DocWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.doc");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DocxWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.docx");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void OdpWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.odp");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void OdsWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.ods");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void OdtWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.odt");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void PdfWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.pdf");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void PdfWithoutPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\NotPasswordProtected.pdf");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void PptWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.ppt");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void PptxWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.pptx");
            Assert.IsTrue(result);
        }
        
        [TestMethod]
        public void XlsWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.xls");
            Assert.IsTrue(result);
        }
        
        [TestMethod]
        public void XlsxWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.xlsx");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ZipWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.zip");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ZipWithPasswordProtectedZipEntry()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtectedZipEntry.zip");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void MsgFileWithDocxWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.msg");
            Assert.IsTrue(result);
        }
        
        [TestMethod]
        public void EmlFileWithDocxWithPassword()
        {
            var result = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.eml");
            Assert.IsTrue(result);
        }

        private static string GetCurrentDir()
        {
            var directoryInfo = Directory.GetParent(Directory.GetCurrentDirectory()).Parent;
            if (directoryInfo != null)
                return directoryInfo.FullName + Path.DirectorySeparatorChar;
            throw new DirectoryNotFoundException();
        }
    }
}
