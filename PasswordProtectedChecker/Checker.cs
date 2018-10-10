using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using OpenMcdf;
using PasswordProtectedChecker.Exceptions;

namespace PasswordProtectedChecker
{
    /// <summary>
    /// A class with methods to check if a file is password protected
    /// </summary>
    public class Checker
    {
        #region IsFileProtected
        /// <summary>
        /// Returns <c>true</c> when the given <paramref name="fileName"/> is password protected
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool IsFileProtected(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            extension = extension?.ToUpperInvariant();

            switch (extension)
            {
                case ".DOC":
                case ".DOT":
                case ".DOCM":
                case ".DOCX":
                case ".DOTM":
                    return IsWordPasswordProtected(fileName);

                case ".ODT":
                    return OpenDocumentFormatIsPasswordProtected(fileName);

                case ".XLS":
                case ".XLT":
                case ".XLW":
                case ".XLSB":
                case ".XLSM":
                case ".XLSX":
                case ".XLTM":
                case ".XLTX":
                    return IsExcellPasswordProtected(fileName);

                case ".ODS":
                    if (OpenDocumentFormatIsPasswordProtected(fileName))
                        return true;

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
                    return IsPowerPointPasswordProtected(fileName);

                case ".ODP":
                    return OpenDocumentFormatIsPasswordProtected(fileName);
            }

            return false;
        }
        #endregion

        #region IsWordPasswordProtected
        /// <summary>
        /// Returns <c>true</c> when the Word file is password protected
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="PPCFileIsCorrupt">Raised when the file is corrupt</exception>
        public static bool IsWordPasswordProtected(string fileName)
        {
            try
            {
                using (var compoundFile = new CompoundFile(fileName))
                {
                    if (compoundFile.RootStorage.TryGetStream("EncryptedPackage") != null) return true;

                    var stream = compoundFile.RootStorage.TryGetStream("WordDocument");

                    if (stream == null)
                        throw new PPCFileIsCorrupt($"Could not find the WordDocument stream in the file '{fileName}'");

                    var bytes = stream.GetData();
                    using (var memoryStream = new MemoryStream(bytes))
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
            catch (CFCorruptedFileException)
            {
                throw new PPCFileIsCorrupt($"The file '{Path.GetFileName(fileName)}' is corrupt");
            }
            catch (CFFileFormatException)
            {
                // It seems the file is just a normal Microsoft Office 2007 and up Open XML file
                return false;
            }
        }
        #endregion

        #region IsExcellPasswordProtected
        /// <summary>
        /// Returns <c>true</c> when the Excel file is password protected
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="PPCFileIsCorrupt">Raised when the file is corrupt</exception>
        public static bool IsExcellPasswordProtected(string fileName)
        {
            try
            {
                using (var compoundFile = new CompoundFile(fileName))
                {
                    if (compoundFile.RootStorage.TryGetStream("EncryptedPackage") != null) return true;

                    var stream = compoundFile.RootStorage.TryGetStream("WorkBook");
                    if (stream == null)
                        compoundFile.RootStorage.TryGetStream("Book");

                    if (stream == null)
                        throw new PPCFileIsCorrupt($"Could not find the WorkBook or Book stream in the file '{fileName}'");

                    var bytes = stream.GetData();
                    using (var memoryStream = new MemoryStream(bytes))
                    using (var binaryReader = new BinaryReader(memoryStream))
                    {
                        // Get the record type, at the beginning of the stream this should always be the BOF
                        var recordType = binaryReader.ReadUInt16();

                        // Something seems to be wrong, we would expect a BOF but for some reason it isn't so stop it
                        if (recordType != 0x809)
                            throw new PPCFileIsCorrupt($"The file '{fileName}' is corrupt");

                        var recordLength = binaryReader.ReadUInt16();
                        binaryReader.BaseStream.Position += recordLength;

                        // Search after the BOF for the FilePass record, this starts with 2F hex
                        recordType = binaryReader.ReadUInt16();
                        return recordType == 0x2F;
                    }
                }
            }
            catch (CFCorruptedFileException)
            {
                throw new PPCFileIsCorrupt($"The file '{Path.GetFileName(fileName)}' is corrupt");
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
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static bool IsPowerPointPasswordProtected(string fileName)
        {
            try
            {
                using (var compoundFile = new CompoundFile(fileName))
                {
                    if (compoundFile.RootStorage.TryGetStream("EncryptedPackage") != null) return true;
                    var stream = compoundFile.RootStorage.TryGetStream("Current User");
                    if (stream == null) return false;

                    using (var memoryStream = new MemoryStream(stream.GetData()))
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
            catch (CFCorruptedFileException)
            {
                throw new PPCFileIsCorrupt($"The file '{Path.GetFileName(fileName)}' is corrupt");
            }
            catch (CFFileFormatException)
            {
                // It seems the file is just a normal Microsoft Office 2007 and up Open XML file
                return false;
            }
        }
        #endregion

        #region OpenDocumentFormatIsPasswordProtected
        /// <summary>
        ///     Returns true when the <paramref name="inputFile" /> is password protected
        /// </summary>
        /// <param name="inputFile">The OpenDocument format file</param>
        public bool OpenDocumentFormatIsPasswordProtected(string inputFile)
        {
            var zipFile = new ZipFile(inputFile);

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
        #endregion
    }
}
