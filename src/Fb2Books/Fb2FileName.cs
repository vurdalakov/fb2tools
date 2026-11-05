namespace Vurdalakov
{
    using System;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Builds the file name an FB2 book is expected to have:
    /// <c>Author - Title</c> or <c>Author - [Series, volume] - Title</c>.
    /// </summary>
    public static class Fb2FileName
    {
        private const String InvalidCharacters = "\\/:*?\"<>|";

        // Returns the expected name without extension, or null (and the reason) if the book has no author or title
        public static String Build(Fb2Info info, out String problem)
        {
            problem = null;

            if (String.IsNullOrEmpty(info.Author))
            {
                problem = "no author in XML";
                return null;
            }

            if (String.IsNullOrEmpty(info.Title))
            {
                problem = "no title in XML";
                return null;
            }

            var name = new StringBuilder(info.Author).Append(" - ");

            if (!String.IsNullOrEmpty(info.Series))
            {
                name.Append('[').Append(info.Series);

                if (!String.IsNullOrEmpty(info.SeriesNumber))
                {
                    name.Append(", ").Append(info.SeriesNumber);
                }

                name.Append("] - ");
            }

            return name.Append(info.Title).ToString();
        }

        // The name as it can appear on disk: characters that are invalid in file names are removed
        public static String MakeFileSystemSafe(String name)
        {
            var safe = new String(name.Where(c => (InvalidCharacters.IndexOf(c) < 0) && !Char.IsControl(c)).ToArray());

            return safe.TrimEnd('.', ' ');
        }

        // Compares names the way a person would: Unicode-normalized, white space collapsed, and ignoring
        // characters that cannot be in a file name (which tools remove or replace with '_' in different ways).
        // Letter case matters.
        public static Boolean AreSame(String actual, String expected) => String.Equals(Fb2FileName.Normalize(actual), Fb2FileName.Normalize(expected), StringComparison.Ordinal);

        private static String Normalize(String name)
        {
            var letters = name.Normalize(NormalizationForm.FormC).Where(c => (InvalidCharacters.IndexOf(c) < 0) && (c != '_') && !Char.IsControl(c));

            return String.Join(' ', new String(letters.ToArray()).Split((Char[])null, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }
}
