using System;
using PasswordProtectedChecker.Pdf.Interfaces;

namespace PasswordProtectedChecker.Pdf
{
    /**
      <summary>PDF file representation.</summary>
    */
    public class File
    {
        public File(IInputStream stream)
        {
            using (var reader = new Reader(stream, this))
            {
                var info = reader.ReadInfo();
                var version = info.Version;
                var trailer = (PdfDictionary) new ImplicitContainer(this, info.Trailer).DataObject;
                if (trailer.ContainsKey(PdfName.Encrypt)) // Encrypted file.
                    throw new NotImplementedException("Encrypted files are currently not supported.");
            }
        }

        private sealed class ImplicitContainer : PdfIndirectObject
        {
            public ImplicitContainer(File file, PdfDataObject dataObject) : base(file, dataObject,
                new XRefEntry(int.MinValue, int.MinValue))
            {
            }
        }
    }
}