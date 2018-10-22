using System.Collections.Generic;

namespace PasswordProtectedChecker
{
    /// <summary>
    /// A placeholder to return the result of a check with a breadcrum trail about the
    /// file that has a password when the result is <c>true</c>
    /// </summary>
    public class CheckerResult
    {
        #region Properties
        /// <summary>
        /// Returns the result
        /// </summary>
        public bool Result { get; }

        /// <summary>
        /// Returns the breadcrumb trail to the file that returns <c>true</c> in the
        /// <see cref="Result"/> property
        /// </summary>
        public List<string> BreadCrumbs => new List<string>();
        #endregion

        #region AddBreadCrumbTrail
        public void AddBreadCrumbTrail(string trail)
        {
            BreadCrumbs.Add(trail);
        }
        #endregion
    }
}
