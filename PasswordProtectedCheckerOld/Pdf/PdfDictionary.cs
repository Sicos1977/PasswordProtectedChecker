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
using System.Collections;
using System.Collections.Generic;
using text = System.Text;

namespace PasswordProtectedChecker.Pdf
{
    /**
      <summary>PDF dictionary object [PDF:1.6:3.2.6].</summary>
    */
    public sealed class PdfDictionary : PdfDirectObject, IDictionary<PdfName, PdfDirectObject>
    {
        internal IDictionary<PdfName, PdfDirectObject> Entries;

        public override PdfObject Parent { get; internal set; }

        public override bool Updateable { get; set; } = true;

        public override bool Updated { get; protected internal set; }

        protected internal override bool Virtual { get; set; }

        public PdfDictionary(
        )
        {
            Entries = new Dictionary<PdfName, PdfDirectObject>();
        }

        public PdfDictionary(
            int capacity
        )
        {
            Entries = new Dictionary<PdfName, PdfDirectObject>(capacity);
        }

        public void Add(
            PdfName key,
            PdfDirectObject value
        )
        {
            Entries.Add(key, (PdfDirectObject) Include(value));
            Update();
        }

        public bool ContainsKey(
            PdfName key
        )
        {
            return Entries.ContainsKey(key);
        }

        public ICollection<PdfName> Keys => Entries.Keys;

        public bool Remove(
            PdfName key
        )
        {
            var oldValue = this[key];
            if (Entries.Remove(key))
            {
                Exclude(oldValue);
                Update();
                return true;
            }

            return false;
        }

        public PdfDirectObject this[
            PdfName key
        ]
        {
            get
            {
                /*
                  NOTE: This is an intentional violation of the official .NET Framework Class
                  Library prescription (no exception is thrown anytime a key is not found --
                  a null pointer is returned instead).
                */
                PdfDirectObject value;
                Entries.TryGetValue(key, out value);
                return value;
            }
            set
            {
                if (value == null)
                {
                    Remove(key);
                }
                else
                {
                    var oldValue = this[key];
                    Entries[key] = (PdfDirectObject) Include(value);
                    Exclude(oldValue);
                    Update();
                }
            }
        }

        public bool TryGetValue(
            PdfName key,
            out PdfDirectObject value
        )
        {
            return Entries.TryGetValue(key, out value);
        }

        public ICollection<PdfDirectObject> Values => Entries.Values;

        void ICollection<KeyValuePair<PdfName, PdfDirectObject>>.Add(
            KeyValuePair<PdfName, PdfDirectObject> entry
        )
        {
            Add(entry.Key, entry.Value);
        }

        public void Clear(
        )
        {
            foreach (PdfName key in new List<PdfDirectObject>(Entries.Keys)) Remove(key);
        }

        bool ICollection<KeyValuePair<PdfName, PdfDirectObject>>.Contains(
            KeyValuePair<PdfName, PdfDirectObject> entry
        )
        {
            return Entries.Contains(entry);
        }

        public void CopyTo(
            KeyValuePair<PdfName, PdfDirectObject>[] entries,
            int index
        )
        {
            throw new NotImplementedException();
        }

        public int Count => Entries.Count;

        public bool IsReadOnly => false;

        public bool Remove(
            KeyValuePair<PdfName, PdfDirectObject> entry
        )
        {
            if (entry.Value.Equals(this[entry.Key]))
                return Remove(entry.Key);
            return false;
        }

        IEnumerator<KeyValuePair<PdfName, PdfDirectObject>> IEnumerable<KeyValuePair<PdfName, PdfDirectObject>>.
            GetEnumerator(
            )
        {
            return Entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator(
        )
        {
            return ((IEnumerable<KeyValuePair<PdfName, PdfDirectObject>>) this).GetEnumerator();
        }

        public override int CompareTo(
            PdfDirectObject obj
        )
        {
            throw new NotImplementedException();
        }

        /**
        <summary>Gets the value corresponding to the given key, forcing its instantiation as a direct
        object in case of missing entry.</summary>
        <param name="key">Key whose associated value is to be returned.</param>
      */
        public PdfDirectObject Get<T>(PdfName key) where T : PdfDataObject, new()
        {
            return Get<T>(key, true);
        }

        /**
        <summary>Gets the value corresponding to the given key, forcing its instantiation in case of
        missing entry.</summary>
        <param name="key">Key whose associated value is to be returned.</param>
        <param name="direct">Whether the item has to be instantiated directly within its container
        instead of being referenced through an indirect object.</param>
      */
        public PdfDirectObject Get<T>(PdfName key, bool direct) where T : PdfDataObject, new()
        {
            var value = this[key];
            if (value == null)
                try
                {
                    value = (PdfDirectObject) Include(direct
                        ? (PdfDataObject) new T()
                        : new PdfIndirectObject(File, new T(), new XRefEntry(0, 0)).Reference);
                    Entries[key] = value;
                    value.Virtual = true;
                }
                catch (Exception e)
                {
                    throw new Exception(typeof(T).Name + " failed to instantiate.", e);
                }

            return value;
        }

        /**
        Gets the key associated to the specified value.
      */
        public PdfName GetKey(
            PdfDirectObject value
        )
        {
            /*
              NOTE: Current PdfDictionary implementation doesn't support bidirectional maps, to say that
              the only currently-available way to retrieve a key from a value is to iterate the whole map
              (really poor performance!).
            */
            foreach (var entry in Entries)
                if (entry.Value.Equals(value))
                    return entry.Key;
            return null;
        }

        /**
        <summary>Gets the dereferenced value corresponding to the given key.</summary>
        <remarks>This method takes care to resolve the value returned by <see cref="this[PdfName]">
        this[PdfName]</see>.</remarks>
        <param name="key">Key whose associated value is to be returned.</param>
        <returns>null, if the map contains no mapping for this key.</returns>
      */
        public PdfDataObject Resolve(
            PdfName key
        )
        {
            return Resolve(this[key]);
        }

        /**
        <summary>Gets the dereferenced value corresponding to the given key, forcing its instantiation
        in case of missing entry.</summary>
        <remarks>This method takes care to resolve the value returned by <see cref="Get(PdfName)"/>.
        </remarks>
        <param name="key">Key whose associated value is to be returned.</param>
        <returns>null, if the map contains no mapping for this key.</returns>
      */
        public T Resolve<T>(
            PdfName key
        ) where T : PdfDataObject, new()
        {
            return (T) Resolve(Get<T>(key));
        }

        public override PdfObject Swap(
            PdfObject other
        )
        {
            var otherDictionary = (PdfDictionary) other;
            var otherEntries = otherDictionary.Entries;
            // Update the other!
            otherDictionary.Entries = Entries;
            otherDictionary.Update();
            // Update this one!
            Entries = otherEntries;
            Update();
            return this;
        }

        public new string ToString(
        )
        {
            var buffer = new text.StringBuilder();
            {
                // Begin.
                buffer.Append("<< ");
                // Entries.
                foreach (var entry in Entries)
                {
                    // Entry...
                    // ...key.
                    buffer.Append(entry.Key).Append(" ");
                    // ...value.
                    buffer.Append(ToString(entry.Value)).Append(" ");
                }

                // End.
                buffer.Append(">>");
            }
            return buffer.ToString();
        }
    }
}