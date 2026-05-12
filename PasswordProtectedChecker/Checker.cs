//
// Checker.cs
//
// Author: Kees van Spelde <sicos2002@hotmail.com>
//
// Copyright (c) 2018 - 2026 Kees van Spelde (www.magic-sessions.com)
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
using System.IO.Compression;
using MsgReader.Mime;
using MsgReader.Outlook;
using OpenMcdf;
using PasswordProtectedChecker.Exceptions;
using PasswordProtectedChecker.Helpers;
using Storage = MsgReader.Outlook.Storage;

namespace PasswordProtectedChecker;

/// <summary>
///     A class with methods to check if a file is password protected
/// </summary>
public class Checker
{
    #region IsFileProtected
    /// <summary>
    ///     Returns <c>true</c> when the given <paramref name="fileName" /> is password protected
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    /// <exception cref="PPCFileIsCorrupt">Raised when the file is corrupt</exception>
    public Result IsFileProtected(string fileName)
    {
        using var fileStream = new FileInfo(fileName).Open(FileMode.Open);
        return IsStreamProtected(fileStream, fileName);
    }
    #endregion

    #region IsStreamProtected
    /// <summary>
    ///     Returns <c>true</c> when the given file in the <paramref name="fileStream" /> is password protected
    /// </summary>
    /// <param name="fileStream">The file stream</param>
    /// <param name="fileNameOrExtension">
    ///     The filename or extension for the file that is inside the stream. When set to <c>null</c>
    ///     the method tries to autodetect the type of file that is inside the file stream
    /// </param>
    /// <returns></returns>
    /// <exception cref="PPCFileIsCorrupt">Raised when the file is corrupt</exception>
    /// <exception cref="PPCInvalidFile">Raised when the program could not detect what kind of file the stream is</exception>
    public Result IsStreamProtected(Stream fileStream, string fileNameOrExtension = null)
    {
        return IsStreamProtected(fileStream, fileNameOrExtension, null);
    }

    /// <summary>
    ///     Returns <c>true</c> when the given file in the <paramref name="fileStream" /> is password protected
    /// </summary>
    /// <param name="fileStream">The file stream</param>
    /// <param name="fileNameOrExtension">
    ///     The filename or extension for the file that is inside the stream. When set to <c>null</c>
    ///     the method tries to autodetect the type of file that is inside the file stream
    /// </param>
    /// <param name="checkerResult"></param>
    /// <returns></returns>
    /// <exception cref="PPCFileIsCorrupt">Raised when the file is corrupt</exception>
    /// <exception cref="PPCInvalidFile">Raised when the program could not detect what kind of file the stream is</exception>
    private Result IsStreamProtected(Stream fileStream,
        string fileNameOrExtension,
        Result checkerResult)
    {
        string extension;

        if (string.IsNullOrWhiteSpace(fileNameOrExtension))
        {
            if (fileStream.Length < 100)
                throw new PPCStreamToShort();

            using var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            var fileTypeFileInfo = FileTypeSelector.GetFileTypeFileInfo(memoryStream.ToArray());

            if (fileTypeFileInfo.MagicBytes == null)
                throw new PPCInvalidFile("Could not autodetect the file type, use the extension parameter to set the file type");

            extension = fileTypeFileInfo.Extension;
        }
        else
        {
            extension = Path.GetExtension(fileNameOrExtension);
        }

        extension = extension?.ToUpperInvariant();
        var root = false;

        if (checkerResult == null)
        {
            root = true;
            checkerResult = new Result();
            checkerResult.AddParentFile(!string.IsNullOrEmpty(fileNameOrExtension) ? fileNameOrExtension : extension);
        }
        else
            checkerResult.AddChildFile(!string.IsNullOrEmpty(fileNameOrExtension) ? fileNameOrExtension : extension);

        switch (extension)
        {
            case ".DOC":
            case ".DOT":
            case ".DOCM":
            case ".DOCX":
            case ".DOTM":
                checkerResult.Protected = IsWordPasswordProtected(fileStream);
                break;

            case ".ODT":
                checkerResult.Protected = IsOpenDocumentFormatPasswordProtected(fileStream);
                break;

            case ".XLS":
            case ".XLT":
            case ".XLW":
            case ".XLSB":
            case ".XLSM":
            case ".XLSX":
            case ".XLTM":
            case ".XLTX":
                checkerResult.Protected = IsExcelPasswordProtected(fileStream);
                break;

            case ".ODS":
                checkerResult.Protected = IsOpenDocumentFormatPasswordProtected(fileStream);
                break;

            case ".POT":
            case ".PPT":
            case ".PPS":
            case ".POTM":
            case ".POTX":
            case ".PPSM":
            case ".PPSX":
            case ".PPTM":
            case ".PPTX":
                checkerResult.Protected = IsPowerPointPasswordProtected(fileStream);
                break;

            case ".ODP":
                checkerResult.Protected = IsOpenDocumentFormatPasswordProtected(fileStream);
                break;

            case ".ZIP":
                
                if (!root)
                {
                    var childCheckerResult = new Result {ParentResult = checkerResult};
                    childCheckerResult.AddParentFile(!string.IsNullOrEmpty(fileNameOrExtension) ? fileNameOrExtension : extension);
                    checkerResult = childCheckerResult;
                }
                
                checkerResult = IsZipPasswordProtected(fileStream, checkerResult);
                break;

            case ".MSG":

                if (!root)
                {
                    var childCheckerResult = new Result {ParentResult = checkerResult};
                    childCheckerResult.AddParentFile(!string.IsNullOrEmpty(fileNameOrExtension) ? fileNameOrExtension : extension);
                    checkerResult = childCheckerResult;
                }

                checkerResult = IsMsgPasswordProtected(fileStream, checkerResult);
                break;

            case ".EML":
                
                if (!root)
                {
                    var childCheckerResult = new Result {ParentResult = checkerResult};
                    childCheckerResult.AddParentFile(!string.IsNullOrEmpty(fileNameOrExtension) ? fileNameOrExtension : extension);
                    checkerResult = childCheckerResult;
                }
                
                checkerResult = IsEmlPasswordProtected(fileStream, checkerResult);
                break;
        }

        return checkerResult;
    }
    #endregion

    #region IsWordPasswordProtected
    /// <summary>
    ///     Returns <c>true</c> when the Word file is password protected
    /// </summary>
    /// <param name="fileStream">A stream to the file</param>
    /// <returns></returns>
    /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
    private bool IsWordPasswordProtected(Stream fileStream)
    {
        try
        {
            using var compoundFile = RootStorage.Open(fileStream, StorageModeFlags.LeaveOpen);
            if (compoundFile.TryOpenStream("EncryptedPackage", out _)) return true;

            if (!compoundFile.TryOpenStream("WordDocument", out var wordDocumentStream))
                return false;

            using var binaryReader = new BinaryReader(wordDocumentStream);
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
        catch (ArgumentNullException argumentNullException)
        {
            throw new PPCFileIsCorrupt("The file stream is corrupt", argumentNullException);
        }
        catch (ArgumentOutOfRangeException argumentOutOfRangeException)
        {
            throw new PPCFileIsCorrupt("The file stream is corrupt", argumentOutOfRangeException);
        }
        catch (ArgumentException argumentException)
        {
            throw new PPCFileIsCorrupt("The file stream is corrupt", argumentException);
        }
        catch (Exception)
        {
            // It seems the file is just a normal Microsoft Office 2007 and up Open XML file
            return false;
        }
    }
    #endregion

    #region IsExcelPasswordProtected
    /// <summary>
    ///     Returns <c>true</c> when the Excel file is password protected
    /// </summary>
    /// <param name="fileStream">A stream to the file</param>
    /// <returns></returns>
    /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
    private bool IsExcelPasswordProtected(Stream fileStream)
    {
        try
        {
            using var compoundFile = RootStorage.Open(fileStream, StorageModeFlags.LeaveOpen);
            if (compoundFile.TryOpenStream("EncryptedPackage", out _)) return true;

            if(!compoundFile.TryOpenStream("WorkBook", out var workBookStream))
                compoundFile.TryOpenStream("Book", out workBookStream);

            if (workBookStream == null) return false;

            using var binaryReader = new BinaryReader(workBookStream);
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
        catch (ArgumentNullException argumentNullException)
        {
            throw new PPCFileIsCorrupt("The file stream is corrupt", argumentNullException);
        }
        catch (ArgumentOutOfRangeException argumentOutOfRangeException)
        {
            throw new PPCFileIsCorrupt("The file stream is corrupt", argumentOutOfRangeException);
        }
        catch (ArgumentException argumentException)
        {
            throw new PPCFileIsCorrupt("The file stream is corrupt", argumentException);
        }
        catch (Exception)
        {
            // It seems the file is just a normal Microsoft Office 2007 and up Open XML file
            return false;
        }
    }
    #endregion

    #region IsPowerPointPasswordProtected
    /// <summary>
    ///     Returns <c>true</c> when the binary PowerPoint file is password protected
    /// </summary>
    /// <param name="fileStream">A stream to the file</param>
    /// <returns></returns>
    /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
    private bool IsPowerPointPasswordProtected(Stream fileStream)
    {
        try
        {
            using var compoundFile = RootStorage.Open(fileStream, StorageModeFlags.LeaveOpen);
            if (compoundFile.TryOpenStream("EncryptedPackage", out _)) return true;
            if (!compoundFile.TryOpenStream("Current User", out var currentUserStream))
                return false;

            using var binaryReader = new BinaryReader(currentUserStream);
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
        catch (ArgumentNullException argumentNullException)
        {
            throw new PPCFileIsCorrupt("The file stream is corrupt", argumentNullException);
        }
        catch (ArgumentOutOfRangeException argumentOutOfRangeException)
        {
            throw new PPCFileIsCorrupt("The file stream is corrupt", argumentOutOfRangeException);
        }
        catch (ArgumentException argumentException)
        {
            throw new PPCFileIsCorrupt("The file stream is corrupt", argumentException);
        }
        catch (Exception)
        {
            // It seems the file is just a normal Microsoft Office 2007 and up Open XML file
            return false;
        }
    }
    #endregion

    #region IsOpenDocumentFormatPasswordProtected
    /// <summary>
    /// Returns true when the <paramref name="fileStream" /> is password protected
    /// </summary>
    /// <param name="fileStream">A stream to the file</param>
    /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
    private static bool IsOpenDocumentFormatPasswordProtected(Stream fileStream)
    {
        try
        {
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
            var entry = archive.GetEntry("META-INF/manifest.xml");
            if (entry == null) return false;

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var manifest = reader.ReadToEnd();
                
            return manifest.Contains("encryption-data", StringComparison.OrdinalIgnoreCase);
        }
        catch (InvalidDataException exception)
        {
            // .NET gooit specifiek deze exception als het geen geldig zip-bestand is
            throw new PPCFileIsCorrupt("The file stream is corrupt", exception);
        }
        catch (Exception exception)
        {
            throw new PPCFileIsCorrupt("An error occurred while reading the file stream", exception);
        }
    }
    #endregion

    #region IsZipPasswordProtected
    /// <summary>
    ///     Returns <c>true</c> when the ZIP file is password protected
    /// </summary>
    /// <param name="fileStream">A stream to the file</param>
    /// <param name="checkerResult"><see cref="Result"/></param>
    /// <returns></returns>
    /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
    private Result IsZipPasswordProtected(Stream fileStream, Result checkerResult)
    {
        try
        {
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
            foreach (var entry in archive.Entries)
            {
                try
                {
                    using var entryStream = entry.Open();
                    if (string.IsNullOrEmpty(entry.Name)) continue; // Sla mappen over

                    using var memoryStream = new MemoryStream();
                    entryStream.CopyTo(memoryStream);
                    memoryStream.Position = 0;

                    checkerResult = IsStreamProtected(memoryStream, entry.FullName, checkerResult);
                    if (checkerResult.Protected) return checkerResult;
                }
                catch (InvalidDataException)
                {
                    // .NET gooit vaak deze exception als de header aangeeft dat er encryptie is
                    // die de standaard ZipArchive niet ondersteunt (zoals wachtwoorden).
                    checkerResult.Protected = true;
                    checkerResult.AddChildFile(entry.FullName);
                    return checkerResult;
                }
            }

            return checkerResult;
        }
        catch (InvalidDataException ex)
        {
            throw new PPCFileIsCorrupt("The file stream is corrupt or not a valid zip", ex);
        }
        catch (Exception ex)
        {
            throw new PPCFileIsCorrupt("An unexpected error occurred", ex);
        }
    }
    #endregion

    #region IsMsgPasswordProtected
    /// <summary>
    ///     Returns <c>true</c> when one or more attachments in the MSG file are password protected
    /// </summary>
    /// <param name="fileStream">A stream to the file</param>
    /// <param name="checkerResult"><see cref="Result"/></param>
    /// <returns></returns>
    /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
    private Result IsMsgPasswordProtected(Stream fileStream, Result checkerResult)
    {
        try
        {
            using var message = new Storage.Message(fileStream, FileAccess.Read, true);
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

                    foreach (var attachment in message.Attachments)
                    {
                        switch (attachment)
                        {
                            case Storage.Attachment { Data: null }:
                                continue;

                            case Storage.Attachment attach:
                            {
                                using var memoryStream = new MemoryStream(attach.Data);
                                checkerResult = IsStreamProtected(memoryStream, attach.FileName, checkerResult);

                                break;
                            }

                            case Storage.Message msg:
                            {
                                using var memoryStream = new MemoryStream();
                                msg.Save(memoryStream);
                                checkerResult = IsStreamProtected(memoryStream, msg.FileName, checkerResult);

                                break;
                            }
                        }

                        if (checkerResult.Protected) return checkerResult;
                    }

                    break;
            }

            return checkerResult;
        }
        catch (Exception exception)
        {
            throw new PPCFileIsCorrupt("The file stream is corrupt", exception);
        }
    }
    #endregion

    #region IsEmlPasswordProtected
    /// <summary>
    ///     Returns <c>true</c> when one or more attachments in the EML file are password protected
    /// </summary>
    /// <param name="fileStream">A stream to the file</param>
    /// <param name="checkerResult"><see cref="Result"/></param>
    /// <returns></returns>
    /// <exception cref="PPCFileIsCorrupt">Raised when the file stream is corrupt</exception>
    private Result IsEmlPasswordProtected(Stream fileStream, Result checkerResult)
    {
        try
        {
            var stream = fileStream;
            var message = Message.Load(stream);
            if (message.Attachments == null) 
                return checkerResult;

            foreach (var attachment in message.Attachments)
            {
                using var memoryStream = new MemoryStream(attachment.Body);
                checkerResult = IsStreamProtected(memoryStream, attachment.FileName, checkerResult);
                if (!checkerResult.Protected) continue;
                return checkerResult;
            }

            return checkerResult;
        }
        catch (Exception exception)
        {
            throw new PPCFileIsCorrupt("The file stream is corrupt", exception);
        }
    }
    #endregion
}