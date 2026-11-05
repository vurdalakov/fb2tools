namespace Fb2Editor
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;

    /// <summary>Standard Windows shell icons.</summary>
    internal static class ShellIcons
    {
        private const Int32 StockIconFolder = 3; // SIID_FOLDER
        private const UInt32 GetIcon = 0x00000100; // SHGSI_ICON
        private const UInt32 SmallIcon = 0x00000001; // SHGSI_SMALLICON

        // The standard folder icon of the current Windows theme, small size; null if it is not available
        public static Bitmap GetFolderImage()
        {
            var info = new StockIconInfo { Size = (UInt32)Marshal.SizeOf<StockIconInfo>() };

            if ((SHGetStockIconInfo(ShellIcons.StockIconFolder, ShellIcons.GetIcon | ShellIcons.SmallIcon, ref info) != 0) || (info.IconHandle == IntPtr.Zero))
            {
                return null;
            }

            try
            {
                using (var icon = Icon.FromHandle(info.IconHandle))
                {
                    return icon.ToBitmap();
                }
            }
            finally
            {
                DestroyIcon(info.IconHandle);
            }
        }

        [DllImport("shell32.dll", SetLastError = false)]
        private static extern Int32 SHGetStockIconInfo(Int32 identifier, UInt32 flags, ref StockIconInfo info);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern Boolean DestroyIcon(IntPtr iconHandle);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StockIconInfo
        {
            public UInt32 Size;
            public IntPtr IconHandle;
            public Int32 SystemImageIndex;
            public Int32 IconIndex;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public String Path;
        }
    }
}
