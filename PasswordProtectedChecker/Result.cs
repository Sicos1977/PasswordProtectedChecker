using System.Collections.Generic;
using System.IO;

namespace PasswordProtectedChecker
{
    /// <summary>
    ///     A placeholder to return the result of a check with a breadcrumb trail about the
    ///     file that has a password when the result is <c>true</c>
    /// </summary>
    public sealed class Result
    {
        #region Properties
        /// <summary>
        ///     Returns the result
        /// </summary>
        public bool Protected { get; internal set; }

        /// <summary>
        ///     The file parent
        /// </summary>
        public string Parent { get; private set; }

        /// <summary>
        ///     The children files
        /// </summary>
        public List<string> Children { get; }

        /// <summary>
        ///     The <see cref="Result"/> parent
        /// </summary>
        public Result ParentResult { get; internal set; }

        /// <summary>
        ///     Returns the trail to the password protected file
        /// </summary>
        public string Trail
        {
            get
            {
                var result = Parent + " -> " + Children[Children.Count - 1];

                var parentCheckerResult = ParentResult;

                while (parentCheckerResult != null)
                {
                    result = parentCheckerResult.Parent + " -> " + result;
                    parentCheckerResult = parentCheckerResult.ParentResult;
                }

                return result;
            }
        }
        #endregion
        
        #region Constructor
        internal Result()
        {
            Children = new List<string>();
        }
        #endregion

        #region AddParentFile
        /// <summary>
        ///     Adds a file to the trail
        /// </summary>
        /// <param name="file"></param>
        internal void AddParentFile(string file)
        {
            Parent = Path.GetFileName(file);
        }
        #endregion

        #region AddChildFile
        /// <summary>
        ///     Adds a file to the trail
        /// </summary>
        /// <param name="file"></param>
        internal void AddChildFile(string file)
        {
            Children.Add(Path.GetFileName(file));
        }
        #endregion
    }
}