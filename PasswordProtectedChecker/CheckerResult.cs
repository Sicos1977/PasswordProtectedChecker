using System.Collections.Generic;
using System.IO;

namespace PasswordProtectedChecker
{
    /// <summary>
    /// A placeholder to return the result of a check with a breadcrumb trail about the
    /// file that has a password when the result is <c>true</c>
    /// </summary>
    public class CheckerResult
    {
        #region Properties
        /// <summary>
        /// Returns the result
        /// </summary>
        public bool Result { get; internal set; }

        /// <summary>
        /// The breadcrumbs that lead to the file that is password protected
        /// <see cref="Result"/> property
        /// </summary>
        public List<string> BreadCrumbs { get; }

        /// <summary>
        /// Returns the trail to the password protected file
        /// </summary>
        public string Trail => string.Join(" --> ", BreadCrumbs);
        #endregion

        #region Constructor
        internal CheckerResult()
        {
            BreadCrumbs = new List<string>();
        }
        #endregion

        #region AddFile
        /// <summary>
        /// Adds a file to the trail
        /// </summary>
        /// <param name="file"></param>
        internal void AddFile(string file)
        {
            BreadCrumbs.Add(Path.GetFileName(file));
        }
        #endregion
    }
}
