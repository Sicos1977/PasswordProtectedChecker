using System;
using System.IO;

namespace PasswordProtectedChecker.Helpers;

/// <summary>
///    Helper class for file related operations
/// </summary>
internal static class FileManager
{
    #region Consts
    /// <summary>
    ///     The max path length in Windows
    /// </summary>
    private const int MaxPath = 248;
    #endregion

    #region GetTempDirectory
    /// <summary>
    ///     Returns a temp directory
    /// </summary>
    /// <param name="tempPath">
    ///     The path where to create the temp directory or <c>null</c>
    ///     to use the default temp path
    /// </param>
    /// <returns></returns>
    public static DirectoryInfo GetTempDirectory(string tempPath)
    {
        var temp = !string.IsNullOrEmpty(tempPath) ? tempPath : Path.GetTempPath();
        var tempDirectory = new DirectoryInfo(Path.Combine(temp, Guid.NewGuid().ToString()));
        tempDirectory.Create();
        return tempDirectory;
    }
    #endregion

    #region CheckForBackSlash
    /// <summary>
    ///     Check if there is a backslash at the end of the string and if not add it
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public static string CheckForBackSlash(string line)
    {
        if (line[line.Length - 1] == Path.DirectorySeparatorChar ||
            line[line.Length - 1] == Path.AltDirectorySeparatorChar)
            return line;

        return line + Path.DirectorySeparatorChar;
    }
    #endregion

    #region ValidateLongFileName
    /// <summary>
    ///     Validates the length of <paramref name="fileName" />, when this is longer then <see cref="MaxPath" /> chars it will
    ///     be truncated.
    /// </summary>
    /// <param name="fileName">The filename with path</param>
    /// <param name="extraTruncateSize">Optional extra truncate size, when not used the filename is truncated until it fits</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    ///     Raised when no path or file name is given in the <paramref name="fileName" />
    /// </exception>
    /// <exception cref="PathTooLongException">
    ///     Raised when it is not possible to truncate the <paramref name="fileName" />
    /// </exception>
    public static string ValidateLongFileName(string fileName, int extraTruncateSize = -1)
    {
        var fileNameWithoutExtension = GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
            throw new ArgumentException(@"No file name is given, e.g. c:\temp\temp.txt", nameof(fileName));

        var extension = GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(extension))
            extension = string.Empty;

        var path = GetDirectoryName(fileName);

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(@"No path is given, e.g. c:\temp\temp.txt", nameof(fileName));

        path = CheckForBackSlash(path);

        if (fileName.Length <= MaxPath)
            return fileName;

        var maxFileNameLength = MaxPath - path.Length - extension.Length;
        if (extraTruncateSize != -1)
            maxFileNameLength -= extraTruncateSize;

        if (maxFileNameLength < 1)
            throw new PathTooLongException($"Unable the truncate the fileName '{fileName}', current size '{fileName.Length}'");

        return path + fileNameWithoutExtension.Substring(0, maxFileNameLength) + extension;
    }
    #endregion

    #region GetExtension
    /// <summary>
    ///     Returns the extension of the specified <paramref name="path" /> string
    /// </summary>
    /// <param name="path">The path of the file</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Raised when no path is given</exception>
    public static string GetExtension(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path");

        var pathParts = path.Split(Path.DirectorySeparatorChar);
        var fileName = pathParts[pathParts.Length - 1];

        var index = fileName.LastIndexOf(".", StringComparison.Ordinal);

        return index == -1
            ? string.Empty
            : fileName.Substring(fileName.LastIndexOf(".", StringComparison.Ordinal), fileName.Length - index);
    }
    #endregion

    #region GetFileNameWithoutExtension
    /// <summary>
    ///     Returns the file name of the specified <paramref name="path" /> string without the extension
    /// </summary>
    /// <param name="path">The path of the file</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string GetFileNameWithoutExtension(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("No path given", nameof(path));

        var pathParts = path.Split(Path.DirectorySeparatorChar);
        var fileName = pathParts[pathParts.Length - 1];
        return !fileName.Contains(".")
            ? fileName
            : fileName.Substring(0, fileName.LastIndexOf(".", StringComparison.Ordinal));
    }
    #endregion

    #region GetDirectoryName
    /// <summary>
    ///     Returns the directory information for the specified <paramref name="path" /> string
    /// </summary>
    /// <param name="path">The path of a file or directory</param>
    /// <returns></returns>
    public static string GetDirectoryName(string path)
    {
        //GetDirectoryName('C:\MyDir\MySubDir\myfile.ext') returns 'C:\MyDir\MySubDir'
        //GetDirectoryName('C:\MyDir\MySubDir') returns 'C:\MyDir'
        //GetDirectoryName('C:\MyDir\') returns 'C:\MyDir'
        //GetDirectoryName('C:\MyDir') returns 'C:\'
        //GetDirectoryName('C:\') returns ''

        var pathParts = path.Split(Path.DirectorySeparatorChar);

        if (pathParts.Length <= 1)
            return string.Empty;

        var result = pathParts[0];

        for (var i = 1; i < pathParts.Length - 1; i++)
            result += Path.DirectorySeparatorChar + pathParts[i];

        return result;
    }
    #endregion
}