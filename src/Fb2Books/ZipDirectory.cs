namespace Vurdalakov
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Minimal zip central-directory reader. ZipArchiveEntry does not expose the compression
    /// method, so we read it from the raw directory ourselves.
    /// </summary>
    public static class ZipDirectory
    {
        static ZipDirectory() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        public static List<ZipEntryInfo> Read(String path, Encoding legacyEncoding)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var reader = new BinaryReader(fileStream);

                // Locate End Of Central Directory (scan last 64K + 22 bytes)
                var tail = Math.Min(fileStream.Length, 65535 + 22);
                fileStream.Seek(-tail, SeekOrigin.End);
                var buffer = reader.ReadBytes((Int32)tail);
                var eocd = -1;
                for (var i = buffer.Length - 22; i >= 0; i--)
                {
                    if ((buffer[i] == 0x50) && (buffer[i + 1] == 0x4b) && (buffer[i + 2] == 5) && (buffer[i + 3] == 6))
                    {
                        eocd = i;
                        break;
                    }
                }

                if (eocd < 0)
                {
                    throw new InvalidDataException("End of central directory not found (not a zip?).");
                }

                var eocdPosition = fileStream.Length - tail + eocd;
                UInt64 count = BitConverter.ToUInt16(buffer, eocd + 10);
                UInt64 directoryOffset = BitConverter.ToUInt32(buffer, eocd + 16);

                // Zip64
                if (((count == 0xFFFF) || (directoryOffset == 0xFFFFFFFF)) && (eocdPosition >= 20))
                {
                    fileStream.Seek(eocdPosition - 20, SeekOrigin.Begin);
                    if (reader.ReadUInt32() == 0x07064b50)
                    {
                        reader.ReadUInt32();
                        var zip64Position = reader.ReadUInt64();
                        fileStream.Seek((Int64)zip64Position, SeekOrigin.Begin);
                        if (reader.ReadUInt32() == 0x06064b50)
                        {
                            reader.ReadUInt64();
                            reader.ReadUInt16();
                            reader.ReadUInt16();
                            reader.ReadUInt32();
                            reader.ReadUInt32();
                            count = reader.ReadUInt64();
                            reader.ReadUInt64();
                            reader.ReadUInt64();
                            directoryOffset = reader.ReadUInt64();
                        }
                    }
                }

                // Central directory records
                var list = new List<ZipEntryInfo>();
                fileStream.Seek((Int64)directoryOffset, SeekOrigin.Begin);
                for (UInt64 i = 0; i < count; i++)
                {
                    if (reader.ReadUInt32() != 0x02014b50)
                    {
                        throw new InvalidDataException("Corrupt central directory.");
                    }

                    reader.ReadUInt16(); // version made by
                    reader.ReadUInt16(); // version needed
                    var flags = reader.ReadUInt16();
                    var method = (Int32)reader.ReadUInt16();
                    reader.ReadUInt32(); // dos time/date
                    var crc = reader.ReadUInt32();
                    UInt64 compressedSize = reader.ReadUInt32();
                    UInt64 size = reader.ReadUInt32();
                    var nameLength = reader.ReadUInt16();
                    var extraLength = reader.ReadUInt16();
                    var commentLength = reader.ReadUInt16();
                    reader.ReadUInt16(); // disk start
                    reader.ReadUInt16(); // internal attributes
                    reader.ReadUInt32(); // external attributes
                    reader.ReadUInt32(); // local header offset

                    var nameBytes = reader.ReadBytes(nameLength);
                    var extra = reader.ReadBytes(extraLength);
                    reader.ReadBytes(commentLength);

                    var name = ZipDirectory.DecodeName(nameBytes, flags, extra, legacyEncoding);

                    for (var p = 0; (p + 4) <= extra.Length;)
                    {
                        var id = BitConverter.ToUInt16(extra, p);
                        var length = BitConverter.ToUInt16(extra, p + 2);
                        var data = p + 4;

                        // Zip64 extended info
                        if (id == 0x0001)
                        {
                            if ((size == 0xFFFFFFFF) && ((data + 8) <= extra.Length))
                            {
                                size = BitConverter.ToUInt64(extra, data);
                                data += 8;
                            }

                            if ((compressedSize == 0xFFFFFFFF) && ((data + 8) <= extra.Length))
                            {
                                compressedSize = BitConverter.ToUInt64(extra, data);
                            }
                        }
                        else if ((id == 0x9901) && (method == 99) && (length >= 7) && ((data + 7) <= extra.Length))
                        {
                            // WinZip AES: real compression method is the last field
                            method = BitConverter.ToUInt16(extra, data + 5);
                        }

                        p += 4 + length;
                    }

                    var encrypted = (flags & 1) != 0;
                    var utf8Flag = (flags & 0x800) != 0;
                    list.Add(new ZipEntryInfo((Int32)i, name, method, encrypted, utf8Flag, crc, size, compressedSize));
                }

                return list;
            }
        }

        /// <summary>
        /// Name decoding order: UTF-8 flag, Info-ZIP Unicode Path extra field (0x7075),
        /// bytes that happen to be valid UTF-8, and finally the legacy (OEM) code page.
        /// </summary>
        private static String DecodeName(Byte[] nameBytes, UInt16 flags, Byte[] extra, Encoding legacyEncoding)
        {
            if ((flags & 0x800) != 0)
            {
                return Encoding.UTF8.GetString(nameBytes);
            }

            for (var p = 0; (p + 4) <= extra.Length;)
            {
                var id = BitConverter.ToUInt16(extra, p);
                var length = BitConverter.ToUInt16(extra, p + 2);
                if ((id == 0x7075) && (length > 5) && (extra[p + 4] == 1) && ((p + 4 + length) <= extra.Length))
                {
                    return Encoding.UTF8.GetString(extra, p + 9, length - 5);
                }

                p += 4 + length;
            }

            try
            {
                return new UTF8Encoding(false, true).GetString(nameBytes);
            }
            catch (DecoderFallbackException)
            {
                return legacyEncoding.GetString(nameBytes);
            }
        }
    }
}
