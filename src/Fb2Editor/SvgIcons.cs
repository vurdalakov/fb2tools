namespace Fb2Editor
{
    using System;
    using System.Drawing;
    using System.Reflection;

    using Svg;

    // Icons drawn from the SVG files embedded in the program (Resources folder)
    internal static class SvgIcons
    {
        // Renders the embedded SVG (for example "zipfile.svg") as a square bitmap of the given size
        public static Bitmap Render(String resourceName, Int32 size)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Resource not found: {resourceName}"))
            {
                return SvgDocument.Open<SvgDocument>(stream).Draw(size, size);
            }
        }
    }
}
