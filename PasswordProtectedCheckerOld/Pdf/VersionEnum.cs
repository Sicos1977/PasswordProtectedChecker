/*
  Copyright 2010 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Clown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

namespace PasswordProtectedChecker.Pdf
{
    /**
      <summary>Managed PDF version number [PDF:1.6:H.1].</summary>
    */
    public enum VersionEnum
    {
        /**
          <summary>Version 1.0 (1993, Acrobat 1).</summary>
        */
        Pdf10,

        /**
          <summary>Version 1.1 (1996, Acrobat 2).</summary>
        */
        Pdf11,

        /**
          <summary>Version 1.2 (1996, Acrobat 3).</summary>
        */
        Pdf12,

        /**
          <summary>Version 1.3 (2000, Acrobat 4).</summary>
        */
        Pdf13,

        /**
          <summary>Version 1.4 (2001, Acrobat 5).</summary>
        */
        Pdf14,

        /**
          <summary>Version 1.5 (2003, Acrobat 6).</summary>
        */
        Pdf15,

        /**
          <summary>Version 1.6 (2004, Acrobat 7).</summary>
        */
        Pdf16,

        /**
          <summary>Version 1.7 (2006, Acrobat 8).</summary>
        */
        Pdf17
    }

    internal static class VersionEnumExtension
    {
        public static Version GetVersion(
            this VersionEnum value
        )
        {
            var versionSuffix = value.ToString().Substring(value.ToString().Length - 2);
            return Version.Get(versionSuffix[0] + "." + versionSuffix[1]);
        }
    }
}