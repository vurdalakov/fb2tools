namespace Vurdalakov
{
    using System;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Builds the file name an FB2 book is expected to have:
    /// <c>Author - Title</c> or <c>Author - [Series, volume] - Title</c>.
    /// If the book has no author (or is an anthology), the <c>Author - </c> part is omitted.
    /// If the book has a translator, <c> (пер. Translator)</c> is added at the end.
    /// </summary>
    public static class Fb2FileName
    {
        private const String InvalidCharacters = "\\/:*?\"<>|";

        // Returns the expected name without extension, or null (and the reason) if the book has no title; a missing author is not a problem
        public static String Build(Fb2Info info, out String problem)
        {
            problem = null;

            if (String.IsNullOrEmpty(info.Title))
            {
                problem = "no title in XML";
                return null;
            }

            var name = new StringBuilder();

            if (!info.Anthology && !String.IsNullOrEmpty(info.Author))
            {
                name.Append(info.Author).Append(" - ");
            }

            if (!String.IsNullOrEmpty(info.Series))
            {
                name.Append('[').Append(info.Series);

                if (!String.IsNullOrEmpty(info.SeriesNumber))
                {
                    name.Append(", ").Append(info.SeriesNumber);
                }

                name.Append("] - ");
            }

            name.Append(info.Title);

            if (!String.IsNullOrEmpty(info.Translator))
            {
                name.Append(" (пер. ").Append(info.Translator).Append(')');
            }

            return name.ToString();
        }

        // The name as it can appear on disk: ':' is changed to '.', other characters that are invalid in file names are removed
        public static String MakeFileSystemSafe(String name)
        {
            var safe = new String(name.Replace(':', '.').Where(c => (InvalidCharacters.IndexOf(c) < 0) && !Char.IsControl(c)).ToArray());

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
