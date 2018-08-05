using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nett;
using OpenTK;
using OpenTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace SS14.Noise
{
    // A quick shout out to Space Scape: http://alexcpeterson.com/spacescape/
    // I spent a lot of time looking through it's tutorials and code to create this.
    // Because yes this is pretty much a 2D clone of space scape.
    class Generator
    {
        FastNoise Noise;

        List<Layer> Layers;

        public Generator()
        {
            Noise = new FastNoise();
        }

        public Image<Rgba32> FullReload(Size size)
        {
            var bitmap = new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, size.Width, size.Height, Rgba32.Black);

            ReloadConfig();
            foreach (var layer in Layers)
            {
                layer.Apply(bitmap);
            }

            return bitmap;
        }

        void ReloadConfig()
        {
            Layers = new List<Layer>();
            using (var f = File.OpenRead("../src/config.toml"))
            {
                var table = Nett.Toml.ReadFile(f);
                
                foreach (var layer in table.Get<TomlTableArray>("layers").Items.Select(t => t.Get<TomlTable>()))
                {
                    switch (layer.Get<string>("type"))
                    {
                        case "noise":
                            var layernoise = new LayerNoise(layer);
                            Layers.Add(layernoise);
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                }
            }
        }

        abstract class Layer
        {
            public abstract void Apply(Image<Rgba32> bitmap);
        }

        class LayerNoise : Layer
        {
            public Color4 InnerColor = Color4.White;
            public Color4 OuterColor = Color4.Black;
            public RustNoise.NoiseType NoiseType = RustNoise.NoiseType.Fbm;
            public uint Seed = 1234;
            public double Persistence = 0.5;
            public double Lacunarity = Math.PI * 2 / 3;
            public double Frequency = 1;
            public uint Octaves = 3;
            public double Threshold = 0;
            public BlendFactor SrcFactor = BlendFactor.One;
            public BlendFactor DstFactor = BlendFactor.One;

            public LayerNoise(Nett.TomlTable table)
            {
                if (table.TryGetValue("innercolor", out var tomlObject))
                {
                    InnerColor = Util.ColorFromHex(tomlObject.Get<string>());
                }
                if (table.TryGetValue("outercolor", out tomlObject))
                {
                    OuterColor = Util.ColorFromHex(tomlObject.Get<string>());
                }
                if (table.TryGetValue("seed", out tomlObject))
                {
                    Seed = (uint)tomlObject.Get<int>();
                }
                if (table.TryGetValue("persistence", out tomlObject))
                {
                    Persistence = double.Parse(tomlObject.Get<string>(), System.Globalization.CultureInfo.InvariantCulture);
                }
                if (table.TryGetValue("lacunarity", out tomlObject))
                {
                    Lacunarity = double.Parse(tomlObject.Get<string>(), System.Globalization.CultureInfo.InvariantCulture);
                }
                if (table.TryGetValue("frequency", out tomlObject))
                {
                    Frequency = double.Parse(tomlObject.Get<string>(), System.Globalization.CultureInfo.InvariantCulture);
                }
                if (table.TryGetValue("octaves", out tomlObject))
                {
                    Octaves = (uint)tomlObject.Get<int>();
                }
                if (table.TryGetValue("threshold", out tomlObject))
                {
                    Threshold = double.Parse(tomlObject.Get<string>(), System.Globalization.CultureInfo.InvariantCulture);
                }
                if (table.TryGetValue("sourcefactor", out tomlObject))
                {
                    SrcFactor = (BlendFactor)Enum.Parse(typeof(BlendFactor), tomlObject.Get<string>());
                }
                if (table.TryGetValue("destfactor", out tomlObject))
                {
                    DstFactor = (BlendFactor)Enum.Parse(typeof(BlendFactor), tomlObject.Get<string>());
                }
                if (table.TryGetValue("noise_type", out tomlObject))
                {
                    switch (tomlObject.Get<string>())
                    {
                        case "fbm":
                            NoiseType = RustNoise.NoiseType.Fbm;
                            break;
                        case "ridged":
                            NoiseType = RustNoise.NoiseType.Ridged;
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }

            public override void Apply(Image<Rgba32> bitmap)
            {
                var noise = new RustNoise(NoiseType);
                noise.SetSeed(Seed);
                noise.SetFrequency(Frequency);
                noise.SetPersistence(Persistence);
                noise.SetLacunarity(Lacunarity);
                noise.SetOctaves(Octaves);
                noise.SetPeriodX(bitmap.Width);
                noise.SetPeriodY(bitmap.Height);
                for (var x = 0; x < bitmap.Width; x++)
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        var bitmapCol = bitmap[x, y];
                        var dstColor = new Color4(bitmapCol.R, bitmapCol.G, bitmapCol.B, bitmapCol.A);

                        // Do noise clauclations.
                        var noiseVal = Math.Max(0, Math.Min(1, Math.Max(0, (noise.GetNoise(x, y)+1)/2)));

                        // Threshold
                        noiseVal = Math.Max(0, noiseVal - Threshold);
                        noiseVal *= 1 / (1 - Threshold);

                        // Get colors based on noise values.
                        var srcColor = Util.ColorMix(OuterColor, InnerColor, (float)noiseVal);

                        // Apply blending factors.
                        var final = Util.Blend(dstColor, srcColor, DstFactor, SrcFactor);

                        // Write back.
                        bitmap[x, y] = new Rgba32(final.R, final.G, final.B, final.A);
                    }
                }
            }
        }
    }
}
