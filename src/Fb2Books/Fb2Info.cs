namespace Vurdalakov
{
    using System;

    /// <summary>Metadata of an FB2 book (title-info, document id and the cover image).</summary>
    public sealed class Fb2Info
    {
        // First author: first name + last name (nickname if there is no real name); the middle name is not included
        public String Author { get; internal set; }

        public String Translator { get; internal set; }

        public String Title { get; internal set; }

        public String Language { get; internal set; }

        // Inner XML of the annotation element
        public String Annotation { get; internal set; }

        // First sequence (series) and its number
        public String Series { get; internal set; }

        public String SeriesNumber { get; internal set; }

        // Value of document-info/id; only read when the cover is requested
        public String DocumentId { get; internal set; }

        // Cover image; only read when the cover is requested. CoverError is set if the image data is damaged.
        public Byte[] CoverImage { get; internal set; }

        public String CoverContentType { get; internal set; }

        public String CoverError { get; internal set; }
    }
}
