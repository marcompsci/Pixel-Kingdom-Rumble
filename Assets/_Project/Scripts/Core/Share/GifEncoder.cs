using System;
using System.Collections.Generic;
using System.IO;

namespace PKR.Core
{
    /// <summary>
    /// A small animated-GIF writer for highlight clips (pure C#, no Unity). Frames are RGBA32, bottom row first (Unity's
    /// texture order) or top row first. Colours go through one fixed 6x7x6 palette (252 colours + spares) with a little
    /// ordered dithering, so encoding is fast and needs no per-clip palette search. Loops forever.
    /// </summary>
    public static class GifEncoder
    {
        const int RLevels = 6, GLevels = 7, BLevels = 6;
        static readonly byte[] Palette = BuildPalette();
        static readonly int[,] Bayer = { { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 } };

        static byte[] BuildPalette()
        {
            var p = new byte[256 * 3];
            int i = 0;
            for (int r = 0; r < RLevels; r++)
            for (int g = 0; g < GLevels; g++)
            for (int b = 0; b < BLevels; b++)
            {
                p[i * 3] = (byte)(r * 255 / (RLevels - 1));
                p[i * 3 + 1] = (byte)(g * 255 / (GLevels - 1));
                p[i * 3 + 2] = (byte)(b * 255 / (BLevels - 1));
                i++;
            }
            return p;
        }

        /// <summary>Palette index for a colour at pixel (x, y) (ordered dither between palette steps).</summary>
        public static byte Index(byte r, byte g, byte b, int x, int y)
        {
            float d = (Bayer[y & 3, x & 3] + 0.5f) / 16f - 0.5f;
            int ri = Q(r, RLevels, d), gi = Q(g, GLevels, d), bi = Q(b, BLevels, d);
            return (byte)((ri * GLevels + gi) * BLevels + bi);
        }

        static int Q(byte v, int levels, float dither)
        {
            float f = v / 255f * (levels - 1) + dither;
            int q = (int)Math.Round(f);
            return q < 0 ? 0 : q >= levels ? levels - 1 : q;
        }

        /// <summary>
        /// Encodes the frames. rgba: width*height*4 bytes each. bottomUp: rows start at the bottom (Unity). delayCs:
        /// hundredths of a second per frame.
        /// </summary>
        public static byte[] Encode(IList<byte[]> rgba, int width, int height, int delayCs, bool bottomUp = true)
        {
            if (rgba == null || rgba.Count == 0) throw new ArgumentException("no frames");
            if (width <= 0 || height <= 0 || width > 4096 || height > 4096) throw new ArgumentException("bad size");
            using (var ms = new MemoryStream())
            {
                var w = new BinaryWriter(ms);
                w.Write(new[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' });
                w.Write((ushort)width);
                w.Write((ushort)height);
                w.Write((byte)0xF7);  // global colour table, 8 bits, 256 entries
                w.Write((byte)0);     // background index
                w.Write((byte)0);     // aspect
                w.Write(Palette);
                // Loop forever (NETSCAPE2.0)
                w.Write(new byte[] { 0x21, 0xFF, 0x0B });
                w.Write(System.Text.Encoding.ASCII.GetBytes("NETSCAPE2.0"));
                w.Write(new byte[] { 0x03, 0x01, 0x00, 0x00, 0x00 });
                var indices = new byte[width * height];
                foreach (var frame in rgba)
                {
                    if (frame == null || frame.Length < width * height * 4) continue;
                    for (int y = 0; y < height; y++)
                    {
                        int srcRow = bottomUp ? height - 1 - y : y;
                        for (int x = 0; x < width; x++)
                        {
                            int s = (srcRow * width + x) * 4;
                            indices[y * width + x] = Index(frame[s], frame[s + 1], frame[s + 2], x, y);
                        }
                    }
                    // Graphic control extension: delay, no transparency
                    w.Write(new byte[] { 0x21, 0xF9, 0x04, 0x04 });
                    w.Write((ushort)Math.Max(2, delayCs));
                    w.Write(new byte[] { 0x00, 0x00 });
                    // Image descriptor
                    w.Write((byte)0x2C);
                    w.Write((ushort)0); w.Write((ushort)0);
                    w.Write((ushort)width); w.Write((ushort)height);
                    w.Write((byte)0);
                    WriteLzw(w, indices, 8);
                }
                w.Write((byte)0x3B);
                w.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>GIF LZW: variable-width codes packed little-endian into 255-byte sub-blocks.</summary>
        static void WriteLzw(BinaryWriter w, byte[] pixels, int minCodeSize)
        {
            w.Write((byte)minCodeSize);
            int clear = 1 << minCodeSize, end = clear + 1;
            var dict = new Dictionary<int, int>(4096);
            int codeSize = minCodeSize + 1, next = end + 1;
            var block = new List<byte>(256);
            int bitBuf = 0, bitCount = 0;

            void Emit(int code)
            {
                bitBuf |= code << bitCount;
                bitCount += codeSize;
                while (bitCount >= 8)
                {
                    block.Add((byte)(bitBuf & 0xFF));
                    bitBuf >>= 8;
                    bitCount -= 8;
                    if (block.Count == 255) { w.Write((byte)255); w.Write(block.ToArray()); block.Clear(); }
                }
            }

            Emit(clear);
            int prefix = pixels[0];
            for (int i = 1; i < pixels.Length; i++)
            {
                int k = pixels[i];
                int key = (prefix << 8) | k;
                if (dict.TryGetValue(key, out int code)) { prefix = code; continue; }
                Emit(prefix);
                if (next < 4096)
                {
                    dict[key] = next++;
                    if (next > (1 << codeSize) && codeSize < 12) codeSize++;
                }
                else
                {
                    Emit(clear);
                    dict.Clear();
                    codeSize = minCodeSize + 1;
                    next = end + 1;
                }
                prefix = k;
            }
            Emit(prefix);
            Emit(end);
            if (bitCount > 0) block.Add((byte)(bitBuf & 0xFF));
            if (block.Count > 0) { w.Write((byte)block.Count); w.Write(block.ToArray()); }
            w.Write((byte)0);
        }
    }
}
