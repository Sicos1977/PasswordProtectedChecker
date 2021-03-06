﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PasswordProtectedChecker;
using System.IO;

namespace PasswordProtectedTester
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void DocWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.doc");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void DocxWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.docx");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void OdpWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.odp");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void OdsWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.ods");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void OdtWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.odt");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void PdfWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.pdf");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void PdfWithoutPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\NotPasswordProtected.pdf");
            Assert.IsFalse(checkerResult.Protected);
        }

        [TestMethod]
        public void PptWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.ppt");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void PptxWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.pptx");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void XlsWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.xls");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void XlsxWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.xlsx");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void ZipWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.zip");
            Assert.IsTrue(checkerResult.Trail == "PasswordProtected.zip -> test.txt");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void ZipWithPasswordProtectedZipEntry()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtectedZipEntry.zip");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void MsgFileWithDocxWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.msg");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void MsgFileWithMsgWithDocxWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\MsgFileWithMsgWithDocxWithPassword.msg");
            Assert.IsTrue(checkerResult.Trail == "MsgFileWithMsgWithDocxWithPassword.msg -> An MSG file with a password protected Word document.msg -> PasswordProtected.docx");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void MsgFileWithEmlWithDocxWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\MsgFileWithEmlWithDocxWithPassword.msg");
            Assert.IsTrue(checkerResult.Trail == "MsgFileWithEmlWithDocxWithPassword.msg -> PasswordProtected.eml -> PasswordProtected.docx");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void ZipWithMsgFileWithEmlWithDocxWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\ZipWithMsgFileWithEmlWithDocxWithPassword.zip");
            Assert.IsTrue(checkerResult.Trail == "ZipWithMsgFileWithEmlWithDocxWithPassword.zip -> MsgFileWithEmlWithDocxWithPassword.msg -> PasswordProtected.eml -> PasswordProtected.docx");
            Assert.IsTrue(checkerResult.Protected);
        }

        [TestMethod]
        public void EmlFileWithDocxWithPassword()
        {
            var checkerResult = new Checker().IsFileProtected(GetCurrentDir() + "TestFiles\\PasswordProtected.eml");
            Assert.IsTrue(checkerResult.Protected);
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