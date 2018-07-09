using System;
using System.Runtime.InteropServices;
using OpenTK;

// Tiling 2D noise implementation taken from https://www.gamedev.net/blogs/entry/2138456-seamless-noise/

namespace SS14.Noise
{
    public sealed class RustNoise : IDisposable
    {
        public const double TAU = Math.PI * 2;

        private IntPtr NoiseGenerator;

        public RustNoise(NoiseType type)
        {
            NoiseGenerator = _GeneratorMake((byte)type);
        }

        public void SetFrequency(double frequency)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(RustNoise));
            }
            _GeneratorSetFrequency(NoiseGenerator, frequency);
        }

        public void SetLacunarity(double lacunarity)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(RustNoise));
            }
            _GeneratorSetLacunarity(NoiseGenerator, lacunarity);
        }

        public void SetPersistence(double persistence)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(RustNoise));
            }
            _GeneratorSetPersistence(NoiseGenerator, persistence);
        }

        public void SetPeriodX(double periodX)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(RustNoise));
            }
            _GeneratorSetPeriodX(NoiseGenerator, periodX);
        }

        public void SetPeriodY(double periodY)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(RustNoise));
            }
            _GeneratorSetPeriodY(NoiseGenerator, periodY);
        }

        public void SetOctaves(uint octaves)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(RustNoise));
            }
            if (octaves > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(octaves), octaves, "Octave count cannot be greater than 32.");
            }
            _GeneratorSetOctaves(NoiseGenerator, octaves);
        }

        public void SetSeed(uint seed)
        {
            _GeneratorSetSeed(NoiseGenerator, seed);
        }

        public double GetNoise(float x, float y)
        {
            return GetNoise(new Vector2(x, y));
        }

        public double GetNoise(Vector2 vec)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(RustNoise));
            }

            return _GetNoiseTiled2D(NoiseGenerator, new Vec2(vec.X, vec.Y));
        }

        ~RustNoise()
        {
            Dispose();
        }

        public bool Disposed => NoiseGenerator == IntPtr.Zero;

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            _GeneratorDispose(NoiseGenerator);
            NoiseGenerator = IntPtr.Zero;
        }

        public enum NoiseType
        {
            Fbm = 0,
            Ridged = 1,
        }

        #region FFI
        [StructLayout(LayoutKind.Sequential)]
        struct Vec4
        {
            public double X;
            public double Y;
            public double Z;
            public double W;

            public Vec4(double x, double y, double z, double w)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Vec2
        {
            public double X;
            public double Y;

            public Vec2(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        [DllImport("ss14_noise.dll", EntryPoint = "generator_new", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern IntPtr _GeneratorMake(byte noiseType);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_octaves", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern void _GeneratorSetOctaves(IntPtr gen, uint octaves);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_persistence", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern void _GeneratorSetPersistence(IntPtr gen, double persistence);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_lacunarity", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern void _GeneratorSetLacunarity(IntPtr gen, double lacunarity);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_period_x", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern void _GeneratorSetPeriodX(IntPtr gen, double periodX);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_period_y", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern void _GeneratorSetPeriodY(IntPtr gen, double periodY);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_frequency", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern void _GeneratorSetFrequency(IntPtr gen, double frequency);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_set_seed", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern void _GeneratorSetSeed(IntPtr gen, uint seed);

        [DllImport("ss14_noise.dll", EntryPoint = "generator_dispose", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern void _GeneratorDispose(IntPtr gen);

        [DllImport("ss14_noise.dll", EntryPoint = "get_noise", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern double _GetNoise(IntPtr gen, Vec4 pos);

        [DllImport("ss14_noise.dll", EntryPoint = "get_noise_tiled_2d", CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern double _GetNoiseTiled2D(IntPtr gen, Vec2 pos);
        #endregion
    }
}