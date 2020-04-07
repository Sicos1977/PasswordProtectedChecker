/*
  Copyright 2011-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using System.Globalization;
using System.Text;
using PasswordProtectedChecker.Pdf.Interfaces;

namespace PasswordProtectedChecker.Pdf
{
    /**
      <summary>PostScript (non-procedural subset) parser [PS].</summary>
    */
    public class PostScriptParser
        : IDisposable
    {
        #region types
        public enum TokenTypeEnum // [PS:3.3].
        {
            Keyword,
            Boolean,
            Integer,
            Real,
            Literal,
            Hex,
            Name,
            Comment,
            ArrayBegin,
            ArrayEnd,
            DictionaryBegin,
            DictionaryEnd,
            Null
        }
        #endregion

        #region static
        #region fields
        private static readonly NumberFormatInfo StandardNumberFormatInfo = NumberFormatInfo.InvariantInfo;
        #endregion

        #region interface
        #region private
        private static int GetHex(
            int c
        )
        {
            if (c >= '0' && c <= '9')
                return c - '0';
            if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;
            if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;
            return -1;
        }

        /**
        <summary>Evaluate whether a character is a delimiter.</summary>
      */
        private static bool IsDelimiter(
            int c
        )
        {
            return c == Symbol.OpenRoundBracket
                   || c == Symbol.CloseRoundBracket
                   || c == Symbol.OpenAngleBracket
                   || c == Symbol.CloseAngleBracket
                   || c == Symbol.OpenSquareBracket
                   || c == Symbol.CloseSquareBracket
                   || c == Symbol.Slash
                   || c == Symbol.Percent;
        }

        /**
        <summary>Evaluate whether a character is an EOL marker.</summary>
      */
        private static bool IsEOL(
            int c
        )
        {
            return c == 10 || c == 13;
        }

        /**
        <summary>Evaluate whether a character is a white-space.</summary>
      */
        private static bool IsWhitespace(
            int c
        )
        {
            return c == 32 || IsEOL(c) || c == 0 || c == 9 || c == 12;
        }
        #endregion
        #endregion
        #endregion

        #region dynamic
        #region fields
        #endregion

        #region constructors
        public PostScriptParser(IInputStream stream)
        {
            Stream = stream;
        }

        public PostScriptParser(
            byte[] data
        )
        {
            Stream = new Buffer(data);
        }
        #endregion

        #region interface
        #region public
        public override int GetHashCode(
        )
        {
            return Stream.GetHashCode();
        }

        /**
        <summary>Gets a token after moving to the given offset.</summary>
        <param name="offset">Number of tokens to skip before reaching the intended one.</param>
        <seealso cref="Token"/>
      */
        public object GetToken(
            int offset
        )
        {
            MoveNext(offset);
            return Token;
        }

        public long Length => Stream.Length;

        /**
        <summary>Moves the pointer to the next token.</summary>
        <param name="offset">Number of tokens to skip before reaching the intended one.</param>
      */
        public bool MoveNext(
            int offset
        )
        {
            for (
                var index = 0;
                index < offset;
                index++
            )
                if (!MoveNext())
                    return false;
            return true;
        }

        /**
        <summary>Moves the pointer to the next token.</summary>
        <remarks>To properly parse the current token, the pointer MUST be just before its starting
        (leading whitespaces are ignored). When this method terminates, the pointer IS
        at the last byte of the current token.</remarks>
        <returns>Whether a new token was found.</returns>
      */
        public virtual bool MoveNext(
        )
        {
            StringBuilder buffer = null;
            Token = null;
            var c = 0;

            // Skip leading white-space characters.
            do
            {
                c = Stream.ReadByte();
                if (c == -1)
                    return false;
            } while (IsWhitespace(c)); // Keep goin' till there's a white-space character...

            // Which character is it?
            switch (c)
            {
                case Symbol.Slash: // Name.
                {
                    TokenType = TokenTypeEnum.Name;

                    /*
                      NOTE: As name objects are simple symbols uniquely defined by sequences of characters,
                      the bytes making up the name are never treated as text, so here they are just
                      passed through without unescaping.
                    */
                    buffer = new StringBuilder();
                    while (true)
                    {
                        c = Stream.ReadByte();
                        if (c == -1)
                            break; // NOOP.
                        if (IsDelimiter(c) || IsWhitespace(c))
                            break;

                        buffer.Append((char) c);
                    }

                    if (c > -1) Stream.Skip(-1);
                }
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '.':
                case '-':
                case '+': // Number.
                {
                    if (c == '.')
                        TokenType = TokenTypeEnum.Real;
                    else // Digit or signum.
                        TokenType = TokenTypeEnum.Integer;

                    // Building the number...
                    buffer = new StringBuilder();
                    while (true)
                    {
                        buffer.Append((char) c);
                        c = Stream.ReadByte();
                        if (c == -1)
                            break; // NOOP.
                        if (c == '.')
                            TokenType = TokenTypeEnum.Real;
                        else if (c < '0' || c > '9')
                            break;
                    }

                    if (c > -1) Stream.Skip(-1);
                }
                    break;
                case Symbol.OpenSquareBracket: // Array (begin).
                    TokenType = TokenTypeEnum.ArrayBegin;
                    break;
                case Symbol.CloseSquareBracket: // Array (end).
                    TokenType = TokenTypeEnum.ArrayEnd;
                    break;
                case Symbol.OpenAngleBracket: // Dictionary (begin) | Hexadecimal string.
                {
                    c = Stream.ReadByte();
                    if (c == -1)
                        throw new ParseException("Unexpected EOF (isolated opening angle-bracket character).");
                    // Is it a dictionary (2nd angle bracket)?
                    if (c == Symbol.OpenAngleBracket)
                    {
                        TokenType = TokenTypeEnum.DictionaryBegin;
                        break;
                    }

                    // Hexadecimal string (single angle bracket).
                    TokenType = TokenTypeEnum.Hex;

                    buffer = new StringBuilder();
                    while (c != Symbol.CloseAngleBracket) // NOT string end.
                    {
                        if (!IsWhitespace(c)) buffer.Append((char) c);

                        c = Stream.ReadByte();
                        if (c == -1)
                            throw new ParseException("Unexpected EOF (malformed hex string).");
                    }
                }
                    break;
                case Symbol.CloseAngleBracket: // Dictionary (end).
                {
                    c = Stream.ReadByte();
                    if (c != Symbol.CloseAngleBracket)
                        throw new ParseException("Malformed dictionary.", Stream.Position);

                    TokenType = TokenTypeEnum.DictionaryEnd;
                }
                    break;
                case Symbol.OpenRoundBracket: // Literal string.
                {
                    TokenType = TokenTypeEnum.Literal;

                    buffer = new StringBuilder();
                    var level = 0;
                    while (true)
                    {
                        c = Stream.ReadByte();
                        if (c == -1) break;

                        if (c == Symbol.OpenRoundBracket)
                        {
                            level++;
                        }
                        else if (c == Symbol.CloseRoundBracket)
                        {
                            level--;
                        }
                        else if (c == '\\')
                        {
                            var lineBreak = false;
                            c = Stream.ReadByte();
                            switch (c)
                            {
                                case 'n':
                                    c = Symbol.LineFeed;
                                    break;
                                case 'r':
                                    c = Symbol.CarriageReturn;
                                    break;
                                case 't':
                                    c = '\t';
                                    break;
                                case 'b':
                                    c = '\b';
                                    break;
                                case 'f':
                                    c = '\f';
                                    break;
                                case Symbol.OpenRoundBracket:
                                case Symbol.CloseRoundBracket:
                                case '\\':
                                    break;
                                case Symbol.CarriageReturn:
                                    lineBreak = true;
                                    c = Stream.ReadByte();
                                    if (c != Symbol.LineFeed)
                                        Stream.Skip(-1);
                                    break;
                                case Symbol.LineFeed:
                                    lineBreak = true;
                                    break;
                                default:
                                {
                                    // Is it outside the octal encoding?
                                    if (c < '0' || c > '7')
                                        break;

                                    // Octal.
                                    var octal = c - '0';
                                    c = Stream.ReadByte();
                                    // Octal end?
                                    if (c < '0' || c > '7')
                                    {
                                        c = octal;
                                        Stream.Skip(-1);
                                        break;
                                    }

                                    octal = (octal << 3) + c - '0';
                                    c = Stream.ReadByte();
                                    // Octal end?
                                    if (c < '0' || c > '7')
                                    {
                                        c = octal;
                                        Stream.Skip(-1);
                                        break;
                                    }

                                    octal = (octal << 3) + c - '0';
                                    c = octal & 0xff;
                                    break;
                                }
                            }

                            if (lineBreak)
                                continue;
                            if (c == -1)
                                break;
                        }
                        else if (c == Symbol.CarriageReturn)
                        {
                            c = Stream.ReadByte();
                            if (c == -1) break;

                            if (c != Symbol.LineFeed)
                            {
                                c = Symbol.LineFeed;
                                Stream.Skip(-1);
                            }
                        }

                        if (level == -1)
                            break;

                        buffer.Append((char) c);
                    }

                    if (c == -1)
                        throw new ParseException("Malformed literal string.");
                }
                    break;
                case Symbol.Percent: // Comment.
                {
                    TokenType = TokenTypeEnum.Comment;

                    buffer = new StringBuilder();
                    while (true)
                    {
                        c = Stream.ReadByte();
                        if (c == -1
                            || IsEOL(c))
                            break;

                        buffer.Append((char) c);
                    }
                }
                    break;
                default: // Keyword.
                {
                    TokenType = TokenTypeEnum.Keyword;

                    buffer = new StringBuilder();
                    do
                    {
                        buffer.Append((char) c);
                        c = Stream.ReadByte();
                        if (c == -1)
                            break;
                    } while (!IsDelimiter(c) && !IsWhitespace(c));

                    if (c > -1) Stream.Skip(-1);
                }
                    break;
            }

            if (buffer != null)
                switch (TokenType)
                {
                    case TokenTypeEnum.Keyword:
                    {
                        Token = buffer.ToString();
                        switch ((string) Token)
                        {
                            case Keyword.False:
                            case Keyword.True: // Boolean.
                                TokenType = TokenTypeEnum.Boolean;
                                Token = bool.Parse((string) Token);
                                break;
                            case Keyword.Null: // Null.
                                TokenType = TokenTypeEnum.Null;
                                Token = null;
                                break;
                        }
                    }
                        break;
                    case TokenTypeEnum.Name:
                    case TokenTypeEnum.Literal:
                    case TokenTypeEnum.Hex:
                    case TokenTypeEnum.Comment:
                        Token = buffer.ToString();
                        break;
                    case TokenTypeEnum.Integer:
                        Token = int.Parse(
                            buffer.ToString(),
                            NumberStyles.Integer,
                            StandardNumberFormatInfo
                        );
                        break;
                    case TokenTypeEnum.Real:
                        Token = double.Parse(
                            buffer.ToString(),
                            NumberStyles.Float,
                            StandardNumberFormatInfo
                        );
                        break;
                }
            return true;
        }

        public long Position => Stream.Position;

        /**
        <summary>Moves the pointer to the given absolute byte position.</summary>
      */
        public void Seek(
            long offset
        )
        {
            Stream.Seek(offset);
        }

        /**
        <summary>Moves the pointer to the given relative byte position.</summary>
      */
        public void Skip(
            long offset
        )
        {
            Stream.Skip(offset);
        }

        /**
        <summary>Moves the pointer before the next non-EOL character after the current position.</summary>
        <returns>Whether the stream can be further read.</returns>
      */
        public bool SkipEOL(
        )
        {
            int c;
            do
            {
                c = Stream.ReadByte();
                if (c == -1)
                    return false;
            } while (IsEOL(c)); // Keeps going till there's an EOL character.

            Stream.Skip(-1); // Moves back to the first non-EOL character position.
            return true;
        }

        /**
        <summary>Moves the pointer before the next non-whitespace character after the current position.</summary>
        <returns>Whether the stream can be further read.</returns>
      */
        public bool SkipWhitespace(
        )
        {
            int c;
            do
            {
                c = Stream.ReadByte();
                if (c == -1)
                    return false;
            } while (IsWhitespace(c)); // Keeps going till there's a whitespace character.

            Stream.Skip(-1); // Moves back to the first non-whitespace character position.
            return true;
        }

        public IInputStream Stream { get; private set; }

        /**
        <summary>Gets the currently-parsed token.</summary>
      */
        public object Token { get; protected set; }

        /**
        <summary>Gets the currently-parsed token type.</summary>
      */
        public TokenTypeEnum TokenType { get; protected set; }

        #region IDisposable
        public void Dispose(
        )
        {
            if (Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }

            GC.SuppressFinalize(this);
        }
        #endregion
        #endregion
        #endregion
        #endregion
    }
}