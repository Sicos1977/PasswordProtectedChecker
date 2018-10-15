/*
  Copyright 2011 Stefano Chizzolini. http://www.pdfclown.org

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
using org.pdfclown.objects;
using PasswordProtectedChecker.Pdf.Interfaces;

namespace PasswordProtectedChecker.Pdf
{
    /**
      <summary>PDF file parser [PDF:1.7:3.2,3.4].</summary>
    */
    public sealed class FileParser : BaseParser
    {
        #region static
        #region fields
        private static readonly int EOFMarkerChunkSize = 1024; // [PDF:1.6:H.3.18].
        #endregion
        #endregion

        #region types
        public struct Reference
        {
            public readonly int GenerationNumber;
            public readonly int ObjectNumber;

            internal Reference(
                int objectNumber,
                int generationNumber
            )
            {
                ObjectNumber = objectNumber;
                GenerationNumber = generationNumber;
            }
        }
        #endregion

        #region dynamic
        #region fields
        private readonly File file;
        #endregion

        #region constructors
        internal FileParser(
            IInputStream stream,
            File file
        ) : base(stream)
        {
            this.file = file;
        }
        #endregion

        #region interface
        #region public
        public override bool MoveNext(
        )
        {
            var moved = base.MoveNext();
            if (moved)
                switch (TokenType)
                {
                    case TokenTypeEnum.Integer:
                    {
                        /*
                    NOTE: We need to verify whether indirect reference pattern is applicable:
                    ref :=  { int int 'R' }
                  */
                        var stream = Stream;
                        var baseOffset = stream.Position; // Backs up the recovery position.

                        // 1. Object number.
                        var objectNumber = (int) Token;
                        // 2. Generation number.
                        base.MoveNext();
                        if (TokenType == TokenTypeEnum.Integer)
                        {
                            var generationNumber = (int) Token;
                            // 3. Reference keyword.
                            base.MoveNext();
                            if (TokenType == TokenTypeEnum.Keyword
                                && Token.Equals(Keyword.Reference))
                                Token = new Reference(objectNumber, generationNumber);
                        }

                        if (!(Token is Reference))
                        {
                            // Rollback!
                            stream.Seek(baseOffset);
                            Token = objectNumber;
                            TokenType = TokenTypeEnum.Integer;
                        }
                    }
                        break;
                }
            return moved;
        }

        public override PdfDataObject ParsePdfObject(
        )
        {
            switch (TokenType)
            {
                case TokenTypeEnum.Keyword:
                    if (Token is Reference)
                        return new PdfReference(
                            (Reference) Token,
                            file
                        );
                    break;
            }

            var pdfObject = base.ParsePdfObject();
            if (pdfObject is PdfDictionary)
            {
                var stream = Stream;
                var oldOffset = (int) stream.Position;
                MoveNext();
                // Is this dictionary the header of a stream object [PDF:1.6:3.2.7]?
                if (TokenType == TokenTypeEnum.Keyword
                    && Token.Equals(Keyword.BeginStream))
                {
                    var streamHeader = (PdfDictionary) pdfObject;

                    // Keep track of current position!
                    /*
                      NOTE: Indirect reference resolution is an outbound call which affects the stream pointer position,
                      so we need to recover our current position after it returns.
                    */
                    var position = stream.Position;
                    // Get the stream length!
                    var length = ((PdfInteger) streamHeader.Resolve(PdfName.Length)).IntValue;
                    // Move to the stream data beginning!
                    stream.Seek(position);
                    SkipEOL();

                    // Copy the stream data to the instance!
                    var data = new byte[length];
                    stream.Read(data);

                    MoveNext(); // Postcondition (last token should be 'endstream' keyword).

                    object streamType = streamHeader[PdfName.Type];
                    if (PdfName.ObjStm.Equals(streamType)) // Object stream [PDF:1.6:3.4.6].
                        return new ObjectStream(
                            streamHeader,
                            new Buffer(data)
                        );
                    if (PdfName.XRef.Equals(streamType)) // Cross-reference stream [PDF:1.6:3.4.7].
                        return new XRefStream(
                            streamHeader,
                            new Buffer(data)
                        );
                    return new PdfStream(
                        streamHeader,
                        new Buffer(data)
                    );
                }

                stream.Seek(oldOffset);
            }

            return pdfObject;
        }

        /**
        <summary>Retrieves the PDF version of the file [PDF:1.6:3.4.1].</summary>
      */
        public string RetrieveVersion(
        )
        {
            var stream = Stream;
            stream.Seek(0);
            var header = stream.ReadString(10);
            if (!header.StartsWith(Keyword.BOF))
                throw new ParseException("PDF header not found.", stream.Position);

            return header.Substring(Keyword.BOF.Length, 3);
        }

        /**
        <summary>Retrieves the starting position of the last xref-table section [PDF:1.6:3.4.4].</summary>
      */
        public long RetrieveXRefOffset(
        )
        {
            var stream = Stream;
            var streamLength = stream.Length;
            var chunkSize = (int) Math.Min(streamLength, EOFMarkerChunkSize);

            // Move back before 'startxref' keyword!
            var position = streamLength - chunkSize;
            stream.Seek(position);

            // Get 'startxref' keyword position!
            var index = stream.ReadString(chunkSize).LastIndexOf(Keyword.StartXRef);
            if (index < 0)
                throw new ParseException("'" + Keyword.StartXRef + "' keyword not found.", stream.Position);

            // Go past the startxref keyword!
            stream.Seek(position + index);
            MoveNext();

            // Go to the xref offset!
            MoveNext();
            if (TokenType != TokenTypeEnum.Integer)
                throw new ParseException("'" + Keyword.StartXRef + "' value invalid.", stream.Position);

            return (int) Token;
        }
        #endregion
        #endregion
        #endregion
    }
}