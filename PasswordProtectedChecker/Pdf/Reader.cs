/*
  Copyright 2006-2011 Stefano Chizzolini. http://www.pdfclown.org

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

using System;
using System.Collections.Generic;
using PasswordProtectedChecker.Pdf.Interfaces;

namespace PasswordProtectedChecker.Pdf
{
    /**
      <summary>PDF file reader.</summary>
    */
    public sealed class Reader : IDisposable
    {
        public FileParser Parser { get; private set; }

        internal Reader(IInputStream stream, File file)
        {
            Parser = new FileParser(stream, file);
        }

        public void Dispose(
        )
        {
            if (Parser != null)
            {
                Parser.Dispose();
                Parser = null;
            }

            GC.SuppressFinalize(this);
        }

        public override int GetHashCode(
        )
        {
            return Parser.GetHashCode();
        }

        /**
        <summary>Retrieves the file information.</summary>
      */
        public FileInfo ReadInfo(
        )
        {
            //TODO:hybrid xref table/stream
            var version = Version.Get(Parser.RetrieveVersion());
            PdfDictionary trailer = null;
            var xrefEntries = new SortedDictionary<int, XRefEntry>();
            {
                var sectionOffset = Parser.RetrieveXRefOffset();
                while (sectionOffset > -1)
                {
                    // Move to the start of the xref section!
                    Parser.Seek(sectionOffset);

                    PdfDictionary sectionTrailer;
                    if (Parser.GetToken(1).Equals(Keyword.XRef)) // XRef-table section.
                    {
                        // Looping sequentially across the subsections inside the current xref-table section...
                        while (true)
                        {
                            /*
                              NOTE: Each iteration of this block represents the scanning of one subsection.
                              We get its bounds (first and last object numbers within its range) and then collect
                              its entries.
                            */
                            // 1. First object number.
                            Parser.MoveNext();
                            if (Parser.TokenType == PostScriptParser.TokenTypeEnum.Keyword
                                && Parser.Token.Equals(Keyword.Trailer)) // XRef-table section ended.
                                break;
                            if (Parser.TokenType != PostScriptParser.TokenTypeEnum.Integer)
                                throw new ParseException(
                                    "Neither object number of the first object in this xref subsection nor end of xref section found.",
                                    Parser.Position);

                            // Get the object number of the first object in this xref-table subsection!
                            var startObjectNumber = (int) Parser.Token;

                            // 2. Last object number.
                            Parser.MoveNext();
                            if (Parser.TokenType != PostScriptParser.TokenTypeEnum.Integer)
                                throw new ParseException("Number of entries in this xref subsection not found.",
                                    Parser.Position);

                            // Get the object number of the last object in this xref-table subsection!
                            var endObjectNumber = (int) Parser.Token + startObjectNumber;

                            // 3. XRef-table subsection entries.
                            for (
                                var index = startObjectNumber;
                                index < endObjectNumber;
                                index++
                            )
                            {
                                if (xrefEntries.ContainsKey(index)) // Already-defined entry.
                                {
                                    // Skip to the next entry!
                                    Parser.MoveNext(3);
                                    continue;
                                }

                                // Get the indirect object offset!
                                var offset = (int) Parser.GetToken(1);
                                // Get the object generation number!
                                var generation = (int) Parser.GetToken(1);
                                // Get the usage tag!
                                XRefEntry.UsageEnum usage;
                                {
                                    var usageToken = (string) Parser.GetToken(1);
                                    if (usageToken.Equals(Keyword.InUseXrefEntry))
                                        usage = XRefEntry.UsageEnum.InUse;
                                    else if (usageToken.Equals(Keyword.FreeXrefEntry))
                                        usage = XRefEntry.UsageEnum.Free;
                                    else
                                        throw new ParseException("Invalid xref entry.", Parser.Position);
                                }

                                // Define entry!
                                xrefEntries[index] = new XRefEntry(
                                    index,
                                    generation,
                                    offset,
                                    usage
                                );
                            }
                        }

                        // Get the previous trailer!
                        sectionTrailer = (PdfDictionary) Parser.ParsePdfObject(1);
                    }
                    else // XRef-stream section.
                    {
                        var stream =
                            (XRefStream) Parser
                                .ParsePdfObject(3); // Gets the xref stream skipping the indirect-object header.
                        // XRef-stream subsection entries.
                        foreach (var xrefEntry in stream.Values)
                        {
                            if (xrefEntries.ContainsKey(xrefEntry.Number)) // Already-defined entry.
                                continue;

                            // Define entry!
                            xrefEntries[xrefEntry.Number] = xrefEntry;
                        }

                        // Get the previous trailer!
                        sectionTrailer = stream.Header;
                    }

                    if (trailer == null) trailer = sectionTrailer;

                    // Get the previous xref-table section's offset!
                    var prevXRefOffset = (PdfInteger) sectionTrailer[PdfName.Prev];
                    sectionOffset = prevXRefOffset?.IntValue ?? -1;
                }
            }
            return new FileInfo(version, trailer, xrefEntries);
        }

        public sealed class FileInfo
        {
            public PdfDictionary Trailer { get; }

            public Version Version { get; }

            public SortedDictionary<int, XRefEntry> XrefEntries { get; }

            internal FileInfo(
                Version version,
                PdfDictionary trailer,
                SortedDictionary<int, XRefEntry> xrefEntries
            )
            {
                Version = version;
                Trailer = trailer;
                XrefEntries = xrefEntries;
            }
        }
    }
}