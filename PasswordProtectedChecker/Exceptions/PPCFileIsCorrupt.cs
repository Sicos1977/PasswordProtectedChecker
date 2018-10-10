using System;

namespace PasswordProtectedChecker.Exceptions
{
    /// <summary>
    ///     Raised when the Microsoft Office file is corrupt
    /// </summary>
    public class PPCFileIsCorrupt : Exception
    {
        internal PPCFileIsCorrupt() {}

        internal PPCFileIsCorrupt(string message) : base(message) {}

        internal PPCFileIsCorrupt(string message, Exception inner) : base(message, inner) {}
    }
}