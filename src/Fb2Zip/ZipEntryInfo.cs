namespace Vurdalakov
{
    using System;
    using System.Linq;

    /// <summary>One central-directory record.</summary>
    internal sealed class ZipEntryInfo
    {
        public ZipEntryInfo(Int32 index, String name, Int32 method, Boolean encrypted, Boolean utf8Flag, UInt32 crc32, UInt64 size, UInt64 compressedSize)
        {
            this.Index = index;
            this.Name = name;
            this.Method = method;
            this.Encrypted = encrypted;
            this.Utf8Flag = utf8Flag;
            this.Crc32 = crc32;
            this.Size = size;
            this.CompressedSize = compressedSize;
        }

        public Int32 Index { get; }

        public String Name { get; }

        public Int32 Method { get; }

        public Boolean Encrypted { get; }

        public Boolean Utf8Flag { get; }

        public UInt32 Crc32 { get; }

        public UInt64 Size { get; }

        public UInt64 CompressedSize { get; }

        public Boolean IsDirectory => this.Name.EndsWith('/') || this.Name.EndsWith('\\');

        // Deflate (8) is the only acceptable method; everything else (including Stored and
        // Deflate64) is reported and repacked. Directory entries carry no data and are ignored.
        public Boolean BadMethod => !this.IsDirectory && ((this.Method != 8) || this.Encrypted);

        // A non-ASCII name without the UTF-8 flag is ambiguous (depends on the reader's code page).
        // ASCII-only names are identical in every encoding, so they do not need the flag.
        public Boolean BadName => !this.Utf8Flag && this.Name.Any(c => c > 127);

        public Boolean Supported => !this.BadMethod && !this.BadName;

        public String MethodName => this.Method switch
        {
            0 => "Stored",
            1 => "Shrink",
            2 => "Reduce1",
            3 => "Reduce2",
            4 => "Reduce3",
            5 => "Reduce4",
            6 => "Implode",
            8 => "Deflate",
            9 => "Deflate64",
            10 => "PKWARE DCL Implode",
            12 => "BZip2",
            14 => "LZMA",
            18 => "IBM TERSE",
            19 => "IBM LZ77",
            20 => "Zstd (deprecated)",
            93 => "Zstandard",
            94 => "MP3",
            95 => "XZ",
            96 => "JPEG",
            97 => "WavPack",
            98 => "PPMd",
            99 => "AES-encrypted",
            _ => $"Unknown({this.Method})",
        };
    }
}
