namespace Vurdalakov
{
    using System;

    /// <summary>Metadata of an FB2 book (title-info, document id and the cover image).</summary>
    public sealed class Fb2Info
    {
        // First author: first name + last name (nickname if there is no real name); the middle name is not included
        public String Author { get; set; }

        public String AuthorFirstName { get; set; }

        public String AuthorLastName { get; set; }

        // Number of authors that have a name; only the first one is in Author
        public Int32 AuthorCount { get; set; }

        // The title-info has an <anthology>true</anthology> element: the book has no author of its own (a collection)
        public Boolean Anthology { get; set; }

        public String Translator { get; set; }

        public String TranslatorFirstName { get; set; }

        public String TranslatorLastName { get; set; }

        public String Title { get; set; }

        public String Language { get; set; }

        // Inner XML of the annotation element
        public String Annotation { get; set; }

        // First sequence (series) and its number
        public String Series { get; set; }

        public String SeriesNumber { get; set; }

        // Value of document-info/id; only read when the cover is requested
        public String DocumentId { get; set; }

        // Cover image; only read when the cover is requested. CoverError is set if the image data is damaged.
        public Byte[] CoverImage { get; set; }

        public String CoverContentType { get; set; }

        public String CoverError { get; set; }
    }
}
