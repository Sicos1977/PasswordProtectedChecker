/*
  Copyright 2006-2013 Stefano Chizzolini. http://www.pdfclown.org

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
using System.Collections;
using System.Collections.Generic;
using PasswordProtectedChecker.Pdf.Interfaces;
using text = System.Text;

namespace PasswordProtectedChecker.Pdf
{
    /**
      <summary>PDF array object, that is a one-dimensional collection of (possibly-heterogeneous)
      objects arranged sequentially [PDF:1.7:3.2.5].</summary>
    */
    public sealed class PdfArray: PdfDirectObject
    {
        #region static
        #region fields
        private static readonly byte[] BeginArrayChunk = Encoding.Pdf.Encode(Keyword.BeginArray);
        private static readonly byte[] EndArrayChunk = Encoding.Pdf.Encode(Keyword.EndArray);
        #endregion
        #endregion

        #region dynamic
        #region fields
        internal List<PdfDirectObject> items;
        #endregion

        #region constructors
        public PdfArray(
        ) : this(10)
        {
        }

        public PdfArray(
            int capacity
        )
        {
            items = new List<PdfDirectObject>(capacity);
        }

        public PdfArray(
            params PdfDirectObject[] items
        ) : this(items.Length)
        {
            Updateable = false;
            this.AddAll(items);
            Updateable = true;
        }

        public PdfArray(
            IList<PdfDirectObject> items
        ) : this(items.Count)
        {
            Updateable = false;
            this.AddAll(items);
            Updateable = true;
        }
        #endregion

        public PdfDirectObject Get<T>(
            int index
        ) where T : PdfDataObject, new()
        {
            return Get<T>(index, true);
        }

        /**
        <summary>Gets the value corresponding to the given index, forcing its instantiation in case
        of missing entry.</summary>
        <param name="index">Index of the item to return.</param>
        <param name="direct">Whether the item has to be instantiated directly within its container
        instead of being referenced through an indirect object.</param>
      */
        public PdfDirectObject Get<T>(
            int index,
            bool direct
        ) where T : PdfDataObject, new()
        {
            PdfDirectObject item;
            if (index == Count
                || (item = this[index]) == null
                || !item.Resolve().GetType().Equals(typeof(T)))
                try
                {
                    item = (PdfDirectObject) Include(direct
                        ? (PdfDataObject) new T()
                        : new PdfIndirectObject(File, new T(), new XRefEntry(0, 0)).Reference);
                    if (index == Count)
                        items.Add(item);
                    else if (item == null)
                        items[index] = item;
                    else
                        items.Insert(index, item);
                    item.Virtual = true;
                }
                catch (Exception e)
                {
                    throw new Exception(typeof(T).Name + " failed to instantiate.", e);
                }

            return item;
        }

        public override int GetHashCode(
        )
        {
            return items.GetHashCode();
        }

        public override PdfObject Parent { get; internal set; }

        /**
        <summary>Gets the dereferenced value corresponding to the given index.</summary>
        <remarks>This method takes care to resolve the value returned by
        <see cref="this[int]">this[int]</see>.</remarks>
        <param name="index">Index of the item to return.</param>
      */
        public PdfDataObject Resolve(
            int index
        )
        {
            return Resolve(this[index]);
        }

        /**
        <summary>Gets the dereferenced value corresponding to the given index, forcing its
        instantiation in case of missing entry.</summary>
        <remarks>This method takes care to resolve the value returned by
        <see cref="Get<T>">Get<T></see>.</remarks>
        <param name="index">Index of the item to return.</param>
      */
        public T Resolve<T>(
            int index
        ) where T : PdfDataObject, new()
        {
            return (T) Resolve(Get<T>(index));
        }

        public override PdfObject Swap(
            PdfObject other
        )
        {
            var otherArray = (PdfArray) other;
            var otherItems = otherArray.items;
            // Update the other!
            otherArray.items = items;
            otherArray.Update();
            // Update this one!
            items = otherItems;
            Update();
            return this;
        }

        public override string ToString(
        )
        {
            var buffer = new text.StringBuilder();
            {
                // Begin.
                buffer.Append("[ ");
                // Elements.
                foreach (var item in items) buffer.Append(ToString(item)).Append(" ");
                // End.
                buffer.Append("]");
            }
            return buffer.ToString();
        }

        public override bool Updateable { get; set; } = true;

        public override bool Updated { get; protected internal set; }

        public PdfDirectObject this[
            int index
        ]
        {
            get => items[index];
            set
            {
                var oldItem = items[index];
                items[index] = (PdfDirectObject) Include(value);
                Exclude(oldItem);
                Update();
            }
        }

        #region ICollection
        public void Add(
            PdfDirectObject item
        )
        {
            items.Add((PdfDirectObject) Include(item));
            Update();
        }

        public bool Contains(
            PdfDirectObject item
        )
        {
            return items.Contains(item);
        }

        public void CopyTo(
            PdfDirectObject[] items,
            int index
        )
        {
            this.items.CopyTo(items, index);
        }

        public int Count => items.Count;

        public bool IsReadOnly => false;

        public bool Remove(
            PdfDirectObject item
        )
        {
            if (!items.Remove(item))
                return false;

            Exclude(item);
            Update();
            return true;
        }

        #region IEnumerable<PdfDirectObject>
        public IEnumerator<PdfDirectObject> GetEnumerator(
        )
        {
            return items.GetEnumerator();
        }

        #region IEnumerable
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
        #endregion
        #endregion
        #endregion

        #region protected
        protected internal override bool Virtual { get; set; }
        #endregion
    }
}