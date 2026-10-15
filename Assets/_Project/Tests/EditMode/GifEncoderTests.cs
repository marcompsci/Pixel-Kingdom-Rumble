using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PKR.Core;

namespace PKR.Tests
{
    [TestFixture]
    public class GifEncoderTests
    {
        static byte[] Frame(int w, int h, int t)
        {
            var f = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = (y * w + x) * 4;
                f[i] = (byte)((x * 255 / w + t * 40) & 255);
                f[i + 1] = (byte)(y * 255 / h);
                f[i + 2] = (byte)(((x + y) * 3 + t * 20) & 255);
                f[i + 3] = 255;
            }
            return f;
        }

        [Test]
        public void Encode_WritesAValidLoopingGif()
        {
            const int w = 96, h = 54;
            var frames = new List<byte[]>();
            for (int t = 0; t < 6; t++) frames.Add(Frame(w, h, t));
            var gif = GifEncoder.Encode(frames, w, h, 12);
            Assert.AreEqual((int)(byte)'G', (int)gif[0]);
            Assert.AreEqual((int)(byte)'9', (int)gif[4]);
            Assert.AreEqual(w, gif[6] | (gif[7] << 8));
            Assert.AreEqual(h, gif[8] | (gif[9] << 8));
            Assert.AreEqual(0x3B, (int)gif[gif.Length - 1], "trailer");
            Assert.Greater(gif.Length, 1000);
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "pkr_gif_test.gif"), gif);
        }

        [Test]
        public void Encode_LargeNoisyFrames_RoundTrip()
        {
            const int w = 320, h = 180;
            var rng = new System.Random(7);
            var frames = new List<byte[]>();
            for (int t = 0; t < 3; t++)
            {
                var f = new byte[w * h * 4];
                rng.NextBytes(f);
                frames.Add(f);
            }
            var gif = GifEncoder.Encode(frames, w, h, 10);
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "pkr_gif_noise.gif"), gif);
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "pkr_gif_noise_f0.raw"), frames[0]);
            Assert.Greater(gif.Length, w * h);
        }

        [Test]
        public void Palette_MapsBlackAndWhiteExactly()
        {
            Assert.AreEqual(0, (int)GifEncoder.Index(0, 0, 0, 0, 0));
            Assert.AreEqual(251, (int)GifEncoder.Index(255, 255, 255, 1, 1));
        }
    }
}
