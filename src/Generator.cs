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

                        case "points":
                            var layerpoint = new LayerPoints(layer);
                            Layers.Add(layerpoint);
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
                        // Do noise calculations.
                        var noiseVal = Math.Min(1, Math.Max(0, (noise.GetNoise(x, y) + 1) / 2));

                        // Threshold
                        noiseVal = Math.Max(0, noiseVal - Threshold);
                        noiseVal *= 1 / (1 - Threshold);

                        // Get colors based on noise values.
                        var srcColor = Util.ColorMix(OuterColor, InnerColor, (float)noiseVal);

                        // Apply blending factors & write back.
                        var dstColor = bitmap[x, y].Convert();
                        bitmap[x, y] = Util.Blend(dstColor, srcColor, DstFactor, SrcFactor).Convert();
                    }
                }
            }
        }

        class LayerPoints : Layer
        {
            public int Seed = 1234;
            public int PointCount = 100;

            public Color4 CloseColor = Color4.White;
            public Color4 FarColor = Color4.Black;

            public BlendFactor SrcFactor = BlendFactor.One;
            public BlendFactor DstFactor = BlendFactor.One;

            // Noise mask stuff.
            public bool Masked = false;
            public RustNoise.NoiseType MaskNoiseType = RustNoise.NoiseType.Fbm;
            public uint MaskSeed = 1234;
            public double MaskPersistence = 0.5;
            public double MaskLacunarity = Math.PI * 2 / 3;
            public double MaskFrequency = 1;
            public uint MaskOctaves = 3;
            public double MaskThreshold = 0;
            public int PointSize = 1;


            public LayerPoints(Nett.TomlTable table)
            {
                if (table.TryGetValue("seed", out var tomlObject))
                {
                    Seed = tomlObject.Get<int>();
                }

                if (table.TryGetValue("count", out tomlObject))
                {
                    PointCount = tomlObject.Get<int>();
                }

                if (table.TryGetValue("sourcefactor", out tomlObject))
                {
                    SrcFactor = (BlendFactor)Enum.Parse(typeof(BlendFactor), tomlObject.Get<string>());
                }
                if (table.TryGetValue("destfactor", out tomlObject))
                {
                    DstFactor = (BlendFactor)Enum.Parse(typeof(BlendFactor), tomlObject.Get<string>());
                }

                if (table.TryGetValue("farcolor", out tomlObject))
                {
                    FarColor = Util.ColorFromHex(tomlObject.Get<string>());
                }
                if (table.TryGetValue("closecolor", out tomlObject))
                {
                    CloseColor = Util.ColorFromHex(tomlObject.Get<string>());
                }
                if (table.TryGetValue("pointsize", out tomlObject))
                {
                    PointSize = tomlObject.Get<int>();
                }

                // Noise mask stuff.
                if (table.TryGetValue("mask", out tomlObject))
                {
                    Masked = tomlObject.Get<bool>();
                }

                if (table.TryGetValue("maskseed", out tomlObject))
                {
                    MaskSeed = (uint)tomlObject.Get<int>();
                }
                if (table.TryGetValue("maskpersistence", out tomlObject))
                {
                    MaskPersistence = double.Parse(tomlObject.Get<string>(), System.Globalization.CultureInfo.InvariantCulture);
                }
                if (table.TryGetValue("masklacunarity", out tomlObject))
                {
                    MaskLacunarity = double.Parse(tomlObject.Get<string>(), System.Globalization.CultureInfo.InvariantCulture);
                }
                if (table.TryGetValue("maskfrequency", out tomlObject))
                {
                    MaskFrequency = double.Parse(tomlObject.Get<string>(), System.Globalization.CultureInfo.InvariantCulture);
                }
                if (table.TryGetValue("maskoctaves", out tomlObject))
                {
                    MaskOctaves = (uint)tomlObject.Get<int>();
                }
                if (table.TryGetValue("maskthreshold", out tomlObject))
                {
                    MaskThreshold = double.Parse(tomlObject.Get<string>(), System.Globalization.CultureInfo.InvariantCulture);
                }
                if (table.TryGetValue("masknoise_type", out tomlObject))
                {
                    switch (tomlObject.Get<string>())
                    {
                        case "fbm":
                            MaskNoiseType = RustNoise.NoiseType.Fbm;
                            break;
                        case "ridged":
                            MaskNoiseType = RustNoise.NoiseType.Ridged;
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }

            public override void Apply(Image<Rgba32> bitmap)
            {
                var random = new Random(Seed);

                // Temporary buffer so we don't mess up blending.
                var buffer = new Image<Rgba32>(SixLabors.ImageSharp.Configuration.Default, bitmap.Width, bitmap.Height, Rgba32.Black);

                if (Masked)
                {
                    GenPointsMasked(buffer);
                }
                else
                {
                    GenPoints(buffer);
                }

                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        var dstColor = bitmap[x, y].Convert();
                        var srcColor = buffer[x, y].Convert();

                        bitmap[x, y] = Util.Blend(dstColor, srcColor, DstFactor, SrcFactor).Convert();
                    }
                }
            }

            void GenPoints(Image<Rgba32> buffer)
            {
                var o = PointSize - 1;
                var random = new Random(Seed);
                for (int i = 0; i < PointCount; i++)
                {
                    var relX = random.NextDouble();
                    var relY = random.NextDouble();

                    var x = (int)(relX * buffer.Width);
                    var y = (int)(relY * buffer.Height);

                    var dist = random.NextDouble();

                    for (int ox = x - o; ox < x + o; ox++)
                    {
                        for (int oy = y - o; oy < y + o; oy++)
                        {
                            var color = Util.ColorMix(CloseColor, FarColor, (float)dist).Convert();
                            buffer[Util.SaneMod(ox, buffer.Width), Util.SaneMod(oy, buffer.Width)] = color;
                        }
                    }
                }
            }

            void GenPointsMasked(Image<Rgba32> buffer)
            {
                var o = PointSize - 1;
                var random = new Random(Seed);
                var noise = new RustNoise(MaskNoiseType);
                noise.SetSeed(MaskSeed);
                noise.SetFrequency(MaskFrequency);
                noise.SetPersistence(MaskPersistence);
                noise.SetLacunarity(MaskLacunarity);
                noise.SetOctaves(MaskOctaves);
                noise.SetPeriodX(buffer.Width);
                noise.SetPeriodY(buffer.Height);

                const int MaxPointAttemptCount = 999;
                int PointAttemptCount = 0;

                for (int i = 0; i < PointCount; i++)
                {
                    var relX = random.NextDouble();
                    var relY = random.NextDouble();

                    var x = (int)(relX * buffer.Width);
                    var y = (int)(relY * buffer.Height);

                    // Grab noise at this point.
                    var noiseVal = Math.Min(1, Math.Max(0, (noise.GetNoise(x, y) + 1) / 2));
                    // Threshold
                    noiseVal = Math.Max(0, noiseVal - MaskThreshold);
                    noiseVal *= 1 / (1 - MaskThreshold);

                    var randomThresh = random.NextDouble();
                    if (randomThresh > noiseVal)
                    {
                        if (++PointAttemptCount <= MaxPointAttemptCount)
                        {
                            i--;
                        }
                        continue;
                    }

                    var dist = random.NextDouble();

                    for (int ox = x - o; ox <= x + o; ox++)
                    {
                        for (int oy = y - o; oy <= y + o; oy++)
                        {
                            var color = Util.ColorMix(CloseColor, FarColor, (float)dist).Convert();
                            buffer[Util.SaneMod(ox, buffer.Width), Util.SaneMod(oy, buffer.Height)] = color;
                        }
                    }
                }
            }
        }
    }
}
