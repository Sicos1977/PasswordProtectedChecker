﻿//
// Checker.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2018 Magic-Sessions. (www.magic-sessions.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NON INFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.IO;
using iTextSharp.text.pdf;
using ICSharpCode.SharpZipLib.Zip;
using MsgReader.Outlook;
using OpenMcdf;
using PasswordProtectedChecker.Exceptions;
using PasswordProtectedChecker.Helpers;

namespace PasswordProtectedChecker
{
    /// <summary>
    /// A class with methods to check if a file is password protected
    /// </summary>
    public class Checker
    {
        #region Fields
        private readonly string _tempPath;
        #endregion

        #region Properties
        /// <summary>
        /// Makes a temp directory and returns it as an <see cref="DirectoryInfo"/> object
        /// </summary>
        private DirectoryInfo TempDirectory
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_tempPath) && Directory.Exists(_tempPath))
                    return FileManager.GetTempDirectory(_tempPath);

                return FileManager.GetTempDirectory(null);
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates this object and sets it's needed properties
        /// </summary>
        /// <param name="tempPath">When set then temporary files will be created at this location instead
        /// of the default Windows temp folder</param>
        public Checker(string tempPath = null)
        {
            _tempPath = tempPath;
        }
        #endregion

        #region IsStreamProtected
        /// <summary>
        /// Returns <c>true</c> when the given file in the <paramref name="fileStream"/> is password protected
        /// </summary>
        /// <param name="fileStream">The file stream</param>
        /// <param name="fileNameOrExtension">The filename or extension for the file that is inside the stream. When set to <c>null</c>
        /// the method tries to autodetect the type of file that is inside the file stream</param>
        /// <returns></returns>
        /// <exception cref="PPCFileIsCorrupt">Raised when the file is corrupt</exception>
        /// <exception cref="PPCInvalidFile">Raised when the program could not detect what kind of file the stream is</exception>
        public bool IsStreamProtected(Stream fileStream, string fileNameOrExtension = null)
        {
            string extension;

            if (string.IsNullOrWhiteSpace(fileNameOrExtension))
            {
                if (fileStream.Length < 100)
                    throw new PPCStreamToShort();

                using (var memoryStream = new MemoryStream())
                {
                    fileStream.CopyTo(memoryStream);
                    var fileTypeFileInfo = FileTypeSelector.GetFileTypeFileInfo(memoryStream.ToArray());

                    if (fileTypeFileInfo.MagicBytes == null)
                        throw new PPCInvalidFile(
                            "Could not autodetect the file type, use the extension parameter to set the file type");

                    extension = fileTypeFileInfo.Extension;                }
            }
            else
                extension = Path.GetExtension(fileNameOrExtension);

            extension = extension?.ToUpperInvariant();

            switch (extension)
            {
                case ".DOC":
                case ".DOT":
                case ".DOCM":
                case ".DOCX":
                case ".DOTM":
                    return IsWordPasswordProtected(fileStream);

                case ".ODT":
                    return IsOpenDocumentFormatPasswordProtected(fileStream);

                case ".XLS":
                case ".XLT":
                case ".XLW":
                case ".XLSB":
                case ".XLSM":
                case ".XLSX":
                case ".XLTM":
                case ".XLTX":
                    return IsExcelPasswordProtected(fileStream);

                case ".ODS":
                    return IsOpenDocumentFormatPasswordProtected(fileStream);

                case ".POT":
                case ".PPT":
                case ".PPS":
                case ".POTM":
                case ".POTX":
                case ".PPSM":
                case ".PPSX":
                case ".PPTM":
                case ".PPTX":
                    return IsPowerPointPasswordProtected(fileStream);

                case ".ODP":
                    return IsOpenDocumentFormatPasswordProtected(fileStream);

                case ".PDF":
                    return IsPdfPasswordProtected(fileStream);

                case ".ZIP":
                    return IsZipPasswordProtected(fileStream);

                case ".MSG":
                    return IsMsgPasswordProtected(fileStream);
                
                case ".EML":
                    return IsEmlPasswordProtected(fileStream);
            }

            return false;
        }
        #endregion

        #region IsFileProtected
        /// <summary>
        /// Returns <c>true</c> when the given <paramref name="fileName"/> is password protected
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="PPCFileIsCorrupt">Raised when the file is corrupt</exception>
        public bool IsFileProtected(string fileName)
        {
            using (var fileStream = new FileInfo(fileName).Open(FileMode.Open))
                return IsStreamProtected(fileStream, fileName);
        }
        #endregion

        #region IsWordPasswordProtected
        /// <summary>
        /// Returns <c>true</c> when the Word file is password protected
        /// </summary>
        /// <param name="fileStream">A stream to the file</param>
        /// <returns></returns>
        /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
        private bool IsWordPasswordProtected(Stream fileStream)
        {
            try
            {
                using (var compoundFile = new CompoundFile(fileStream))
                {
                    if (compoundFile.RootStorage.TryGetStream("EncryptedPackage") != null) return true;

                    var wordDocumentStream = compoundFile.RootStorage.TryGetStream("WordDocument");

                    if (wordDocumentStream == null)
                        return false;

                    using (var memoryStream = new MemoryStream(wordDocumentStream.GetData()))
                    using (var binaryReader = new BinaryReader(memoryStream))
                    {
                        //http://msdn.microsoft.com/en-us/library/dd944620%28v=office.12%29.aspx
                        // The bit that shows if the file is encrypted is in the 11th and 12th byte so we 
                        // need to skip the first 10 bytes
                        binaryReader.ReadBytes(10);

                        // Now we read the 2 bytes that we need
                        var pnNext = binaryReader.ReadUInt16();
                        //(value & mask) == mask)

                        // The bit that tells us if the file is encrypted
                        return (pnNext & 0x0100) == 0x0100;
                    }
                }
            }
            catch (CFCorruptedFileException cfCorruptedFileException)
            {
                throw new PPCFileIsCorrupt("The file stream is corrupt", cfCorruptedFileException);
            }
            catch (CFFileFormatException)
            {
                // It seems the file is just a normal Microsoft Office 2007 and up Open XML file
                return false;
            }
        }
        #endregion

        #region IsExcelPasswordProtected
        /// <summary>
        /// Returns <c>true</c> when the Excel file is password protected
        /// </summary>
        /// <param name="fileStream">A stream to the file</param>
        /// <returns></returns>
        /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
        private bool IsExcelPasswordProtected(Stream fileStream)
        {
            try
            {
                using (var compoundFile = new CompoundFile(fileStream))
                {
                    if (compoundFile.RootStorage.TryGetStream("EncryptedPackage") != null) return true;

                    var workBookStream = compoundFile.RootStorage.TryGetStream("WorkBook");
                    if (workBookStream == null)
                        compoundFile.RootStorage.TryGetStream("Book");

                    if (workBookStream == null) return false;

                    using (var memoryStream = new MemoryStream(workBookStream.GetData()))
                    using (var binaryReader = new BinaryReader(memoryStream))
                    {
                        // Get the record type, at the beginning of the stream this should always be the BOF
                        var recordType = binaryReader.ReadUInt16();

                        // Something seems to be wrong, we would expect a BOF but for some reason it isn't so stop it
                        if (recordType != 0x809)
                            throw new PPCFileIsCorrupt("The fileStream is corrupt expected recordType 0x809");

                        var recordLength = binaryReader.ReadUInt16();
                        binaryReader.BaseStream.Position += recordLength;

                        // Search after the BOF for the FilePass record, this starts with 2F hex
                        recordType = binaryReader.ReadUInt16();
                        return recordType == 0x2F;
                    }
                }
            }
            catch (CFCorruptedFileException cfCorruptedFileException)
            {
                throw new PPCFileIsCorrupt("The file stream is corrupt", cfCorruptedFileException);
            }
            catch (CFFileFormatException)
            {
                // It seems the file is just a normal Microsoft Office 2007 and up Open XML file
                return false;
            }
        }
        #endregion

        #region IsPowerPointPasswordProtected
        /// <summary>
        /// Returns <c>true</c> when the binary PowerPoint file is password protected
        /// </summary>
        /// <param name="fileStream">A stream to the file</param>
        /// <returns></returns>
        /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
        private bool IsPowerPointPasswordProtected(Stream fileStream)
        {
            try
            {
                using (var compoundFile = new CompoundFile(fileStream))
                {
                    if (compoundFile.RootStorage.TryGetStream("EncryptedPackage") != null) return true;
                    var currentUserStream = compoundFile.RootStorage.TryGetStream("Current User");
                    if (currentUserStream == null) return false;

                    using (var memoryStream = new MemoryStream(currentUserStream.GetData()))
                    using (var binaryReader = new BinaryReader(memoryStream))
                    {
                        var verAndInstance = binaryReader.ReadUInt16();
                        // ReSharper disable UnusedVariable
                        // We need to read these fields to get to the correct location in the Current User stream
                        var version = verAndInstance & 0x000FU; // first 4 bit of field verAndInstance
                        var instance = (verAndInstance & 0xFFF0U) >> 4; // last 12 bit of field verAndInstance
                        var typeCode = binaryReader.ReadUInt16();
                        var size = binaryReader.ReadUInt32();
                        var size1 = binaryReader.ReadUInt32();
                        // ReSharper restore UnusedVariable
                        var headerToken = binaryReader.ReadUInt32();

                        switch (headerToken)
                        {
                            // Not encrypted
                            case 0xE391C05F:
                                return false;

                            // Encrypted
                            case 0xF3D1C4DF:
                                return true;

                            default:
                                return false;
                        }
                    }
                }
            }
            catch (CFCorruptedFileException cfCorruptedFileException)
            {
                throw new PPCFileIsCorrupt("The file stream is corrupt", cfCorruptedFileException);
            }
            catch (CFFileFormatException)
            {
                // It seems the file is just a normal Microsoft Office 2007 and up Open XML file
                return false;
            }
        }
        #endregion

        #region IsOpenDocumentFormatPasswordProtected
        /// <summary>
        ///     Returns true when the <paramref name="fileStream" /> is password protected
        /// </summary>
        /// <param name="fileStream">A stream to the file</param>
        /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
        private bool IsOpenDocumentFormatPasswordProtected(Stream fileStream)
        {
            try
            {
                var zipFile = new ZipFile(fileStream);

                // Check if the file is password protected
                var manifestEntry = zipFile.FindEntry("META-INF/manifest.xml", true);
                if (manifestEntry == -1) return false;
                using (var manifestEntryStream = zipFile.GetInputStream(manifestEntry))
                using (var manifestEntryMemoryStream = new MemoryStream())
                {
                    manifestEntryStream.CopyTo(manifestEntryMemoryStream);
                    manifestEntryMemoryStream.Position = 0;
                    using (var streamReader = new StreamReader(manifestEntryMemoryStream))
                    {
                        var manifest = streamReader.ReadToEnd();
                        if (manifest.ToUpperInvariant().Contains("ENCRYPTION-DATA"))
                            return true;
                    }
                }

                return false;
            }
            catch (Exception exception)
            {
                throw new PPCFileIsCorrupt("The file stream is corrupt", exception);
            }
        }
        #endregion

        #region IsPowerPointPasswordProtected
        /// <summary>
        /// Returns <c>true</c> when the PDF file is password protected
        /// </summary>
        /// <param name="fileStream">A stream to the file</param>
        /// <returns></returns>
        /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
        private bool IsPdfPasswordProtected(Stream fileStream)
        {
            try
            {
                var reader = new PdfReader(fileStream);
                return reader.IsEncrypted();
            }
            catch (BadPasswordException)
            {
                return true;
            }
            catch (BadPdfFormatException badPdfFormatException)
            {
                throw new PPCFileIsCorrupt("The file stream is corrupt", badPdfFormatException);
            }
        }
        #endregion

        #region IsZipPasswordProtected
        /// <summary>
        /// Returns <c>true</c> when the ZIP file is password protected
        /// </summary>
        /// <param name="fileStream">A stream to the file</param>
        /// <returns></returns>
        /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
        private bool IsZipPasswordProtected(Stream fileStream)
        {
            try
            {
                using (var zip = new ZipFile(fileStream))
                {
                    // First check the zip entries for passwords
                    foreach (ZipEntry zipEntry in zip)
                    {
                        if (zipEntry.IsCrypted)
                            return true;
                    }

                    foreach (ZipEntry zipEntry in zip)
                    {
                        if (!zipEntry.IsFile) continue;
                        using (var zipStream = zip.GetInputStream(zipEntry))
                        {
                            var result = IsStreamProtected(zipStream, zipEntry.Name);
                            if (result) return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception exception)
            {
                throw new PPCFileIsCorrupt("The file stream is corrupt", exception);
            }
        }
        #endregion

        #region IsMsgPasswordProtected
        /// <summary>
        /// Returns <c>true</c> when one or more attachments in the MSG file are password protected
        /// </summary>
        /// <param name="fileStream">A stream to the file</param>
        /// <returns></returns>
        /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
        private bool IsMsgPasswordProtected(Stream fileStream)
        {
            try
            {
                using (var message = new Storage.Message(fileStream))
                {
                    switch (message.Type)
                    {
                        case MessageType.Email:
                        case MessageType.EmailSms:
                        case MessageType.EmailNonDeliveryReport:
                        case MessageType.EmailDeliveryReport:
                        case MessageType.EmailDelayedDeliveryReport:
                        case MessageType.EmailReadReceipt:
                        case MessageType.EmailNonReadReceipt:
                        case MessageType.EmailEncryptedAndMaybeSigned:
                        case MessageType.EmailEncryptedAndMaybeSignedNonDelivery:
                        case MessageType.EmailEncryptedAndMaybeSignedDelivery:
                        case MessageType.EmailClearSignedReadReceipt:
                        case MessageType.EmailClearSignedNonDelivery:
                        case MessageType.EmailClearSignedDelivery:
                        case MessageType.EmailBmaStub:
                        case MessageType.CiscoUnityVoiceMessage:
                        case MessageType.EmailClearSigned:
                        case MessageType.RightFaxAdv:
                        case MessageType.SkypeForBusinessMissedMessage:
                        case MessageType.SkypeForBusinessConversation:
                            DirectoryInfo tempDirectory = null;

                            try
                            {
                                tempDirectory = TempDirectory;
                                foreach (var attachment in message.Attachments)
                                {
                                    var result = false;

                                    switch (attachment)
                                    {
                                        case Storage.Attachment attach when attach.Data == null:
                                            continue;
                                        case Storage.Attachment attach:
                                        {
                                            var attachmentFileName =
                                                FileManager.FileExistsMakeNew(Path.Combine(tempDirectory.FullName,
                                                    attach.FileName));
                                            File.WriteAllBytes(attachmentFileName, attach.Data);
                                            result = IsFileProtected(attachmentFileName);
                                            break;
                                        }
                                        case Storage.Message msg:
                                        {
                                            var attachmentFileName =
                                                FileManager.FileExistsMakeNew(Path.Combine(tempDirectory.FullName,
                                                    msg.FileName));
                                            msg.Save(attachmentFileName);
                                            result = IsFileProtected(attachmentFileName);
                                            break;
                                        }
                                    }

                                    if (result) return true;
                                }

                                return false;
                            }
                            finally
                            {
                                if (tempDirectory != null && tempDirectory.Exists)
                                    tempDirectory.Delete(true);
                            }
                    }
                    
                    return false;
                }
            }
            catch (Exception exception)
            {
                throw new PPCFileIsCorrupt("The file stream is corrupt", exception);
            }
        }
        #endregion

        #region IsEmlPasswordProtected
        /// <summary>
        /// Returns <c>true</c> when one or more attachments in the EML file are password protected
        /// </summary>
        /// <param name="fileStream">A stream to the file</param>
        /// <returns></returns>
        /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
        private bool IsEmlPasswordProtected(Stream fileStream)
        {
            try
            {
                using (var stream = fileStream)
                {
                    var message = MsgReader.Mime.Message.Load(stream);
                    if (message.Attachments == null) return false;
                    DirectoryInfo tempDirectory = null;

                    try
                    {
                        tempDirectory = TempDirectory;
                        foreach (var attachment in message.Attachments)
                        {
                            var attachmentFileName =
                                FileManager.FileExistsMakeNew(Path.Combine(tempDirectory.FullName,
                                    attachment.FileName));
                            var fileInfo = new FileInfo(FileManager.FileExistsMakeNew(attachmentFileName));
                            File.WriteAllBytes(fileInfo.FullName, attachment.Body);
                            var result = IsFileProtected(attachmentFileName);
                            if (result) return true;
                        }

                        return false;
                    }
                    finally
                    {
                        if (tempDirectory != null && tempDirectory.Exists)
                            tempDirectory.Delete(true);
                    }
                }
            }
            catch (Exception exception)
            {
                throw new PPCFileIsCorrupt("The file stream is corrupt", exception);
            }
        }
        #endregion
    }
}
