using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nett;
using OpenTK;
using OpenTK.Graphics;

namespace SS14.Noise
{
    class Generator
    {
        FastNoise Noise;

        List<Layer> Layers;

        public Generator()
        {
            Noise = new FastNoise();
        }

        public void Reload()
        {

        }

        public Bitmap FullReload(Size size)
        {
            var bitmap = new Bitmap(size.Width, size.Height);

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
            using (var f = File.OpenRead("src/config.toml"))
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
            public abstract void Apply(Bitmap bitmap);
        }

        class LayerNoise : Layer
        {
            public Color4 Color = Color4.White;
            public RustNoise.NoiseType NoiseType = RustNoise.NoiseType.Fbm;
            public uint Seed = 1234;
            public double Persistence = 0.5;
            public double Lacunarity = Math.PI * 2 / 3;
            public double Frequency = 1;
            public uint Octaves = 3;

            public LayerNoise(Nett.TomlTable table)
            {
                Color = Util.ColorFromHex(table.Get<string>("color"));
                if (table.TryGetValue("seed", out var tomlObject))
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

            public override void Apply(Bitmap bitmap)
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
                        var oldColor = (Color4)bitmap.GetPixel(x, y);
                        if (x == 200 && (y == 580 || y == 20))
                        {
                            Console.WriteLine("yes");
                        }
                        var val = Math.Max(0, Math.Min(1, (float)Math.Max(0, (noise.GetNoise(x, y)+1)/2)));
                        var col = new Color4(Color.R * val, Color.G * val, Color.B * val, 0);
                        var o = new Color4(oldColor.R + col.R, oldColor.G + col.G, oldColor.B + col.B, 1);
                        bitmap.SetPixel(x, y, (Color)o);
                    }
                }
            }
        }
    }
}
