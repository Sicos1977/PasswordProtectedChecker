/*
  Copyright 2006-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using System.IO;
using PasswordProtectedChecker.Pdf.Interfaces;
using text = System.Text;

namespace PasswordProtectedChecker.Pdf
{
    /**
      <summary>Generic stream.</summary>
    */
    public sealed class Stream : IInputStream
    {
        private System.IO.Stream _stream;

        public Stream(System.IO.Stream stream)
        {
            _stream = stream;
        }

        public ByteOrderEnum ByteOrder { get; set; } = ByteOrderEnum.BigEndian;

        public override int GetHashCode()
        {
            return _stream.GetHashCode();
        }

        public long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public void Read(byte[] data)
        {
            _stream.Read(data, 0, data.Length);
        }

        public void Read(
            byte[] data,
            int offset,
            int count)
        {
            _stream.Read(data, offset, count);
        }

        public int ReadByte()
        {
            return _stream.ReadByte();
        }

        public int ReadInt()
        {
            var data = new byte[sizeof(int)];
            Read(data);
            return ConvertUtils.ByteArrayToInt(data, 0, ByteOrder);
        }

        public int ReadInt(int length)
        {
            var data = new byte[length];
            Read(data);
            return ConvertUtils.ByteArrayToNumber(data, 0, length, ByteOrder);
        }

        public string ReadLine()
        {
            var buffer = new text.StringBuilder();
            while (true)
            {
                var c = _stream.ReadByte();
                if (c == -1)
                    if (buffer.Length == 0)
                        return null;
                    else
                        break;
                if (c == '\r'
                    || c == '\n')
                    break;

                buffer.Append((char) c);
            }

            return buffer.ToString();
        }

        public short ReadShort()
        {
            var data = new byte[sizeof(short)];
            Read(data);
            return (short) ConvertUtils.ByteArrayToNumber(data, 0, data.Length, ByteOrder);
        }

        public string ReadString(int length)
        {
            var buffer = new text.StringBuilder();
            int c;

            while (length-- > 0)
            {
                c = _stream.ReadByte();
                if (c == -1)
                    break;

                buffer.Append((char) c);
            }

            return buffer.ToString();
        }

        public sbyte ReadSignedByte()
        {
            throw new NotImplementedException();
        }

        public ushort ReadUnsignedShort()
        {
            var data = new byte[sizeof(ushort)];
            Read(data);
            return (ushort) ConvertUtils.ByteArrayToNumber(data, 0, data.Length, ByteOrder);
        }

        public void Seek(long offset)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
        }

        public void Skip(long offset)
        {
            _stream.Seek(offset, SeekOrigin.Current);
        }

        #region IDataWrapper
        public byte[] ToByteArray()
        {
            var data = new byte[_stream.Length];
            {
                _stream.Position = 0;
                _stream.Read(data, 0, data.Length);
            }
            return data;
        }
        #endregion

        public long Length => _stream.Length;

        public void Dispose()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}