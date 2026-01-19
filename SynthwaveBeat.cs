using System;
using System.IO;
using System.Linq;

public static class SynthwaveBeat
{
    const int SampleRate = 44100;
    const short BitsPerSample = 16;
    const short Channels = 1;

    public static void Generate(string path, int bpm = 128, int bars = 4, int? seed = null)
    {
        Random rng = seed.HasValue ? new Random(seed.Value) : new Random();

        int samplesPerBeat = SampleRate * 60 / bpm;
        int stepsPerBeat = 4;                 // 16th notes
        int samplesPerStep = samplesPerBeat / stepsPerBeat;
        int totalSteps = bars * 16;
        int totalSamples = samplesPerStep * totalSteps;

        short[] buffer = new short[totalSamples];

        double[] scale = { 55, 65.4, 73.4, 82.4, 98 };

        bool[] kickPattern =
        {
            true, false, false, false,
            true, false, false, false,
            true, false, false, false,
            true, false, false, false
        };

        bool[] snarePattern =
        {
            false, false, false, false,
            true, false, false, false,
            false, false, false, false,
            true, false, false, false
        };

        bool[] hatPattern = Enumerable.Range(0, 16)
                                      .Select(_ => rng.NextDouble() > 0.35)
                                      .ToArray();

        double[] bassLine = new double[16];
        RegenerateBass(bassLine, scale, rng);

        for (int i = 0; i < totalSamples; i++)
        {
            int step = (i / samplesPerStep) % 16;
            int stepSample = i % samplesPerStep;
            double t = (double)stepSample / SampleRate;

            // regenerate bass every bar
            if (step == 0 && stepSample == 0)
                RegenerateBass(bassLine, scale, rng);

            double sample = 0;

            // 🔊 KICK
            if (kickPattern[step])
            {
                double env = Math.Exp(-t * 30);
                sample += Math.Sin(2 * Math.PI * 50 * t) * env * 0.9;
            }

            // 🧨 SNARE
            if (snarePattern[step])
            {
                double env = Math.Exp(-t * 45);
                sample += (rng.NextDouble() * 2 - 1) * env * 0.4;
            }

            // ✨ HI-HAT
            if (hatPattern[step])
            {
                double env = Math.Exp(-t * 90);
                sample += (rng.NextDouble() * 2 - 1) * env * 0.15;
            }

            // 🖤 BASS (gated per step)
            double bassFreq = bassLine[step];
            double bassEnv = Math.Exp(-t * 8);
            double bass = Math.Sign(Math.Sin(2 * Math.PI * bassFreq * t)) * bassEnv * 0.3;

            if (kickPattern[step])
                bass *= 0.35; // sidechain feel

            sample += bass;

            sample = Math.Clamp(sample, -1, 1);
            buffer[i] = (short)(sample * short.MaxValue);
        }

        WriteWav(path, buffer);
    }

    static void RegenerateBass(double[] bassLine, double[] scale, Random rng)
    {
        for (int i = 0; i < bassLine.Length; i++)
        {
            bassLine[i] = scale[rng.Next(scale.Length)];
            if (rng.NextDouble() < 0.25)
                bassLine[i] *= 0.5; // octave down
        }
    }

    static void WriteWav(string path, short[] data)
    {
        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        int byteRate = SampleRate * Channels * BitsPerSample / 8;
        int dataSize = data.Length * sizeof(short);

        bw.Write("RIFF".ToCharArray());
        bw.Write(36 + dataSize);
        bw.Write("WAVEfmt ".ToCharArray());
        bw.Write(16);
        bw.Write((short)1);
        bw.Write(Channels);
        bw.Write(SampleRate);
        bw.Write(byteRate);
        bw.Write((short)(Channels * BitsPerSample / 8));
        bw.Write(BitsPerSample);
        bw.Write("data".ToCharArray());
        bw.Write(dataSize);

        foreach (var s in data)
            bw.Write(s);
    }
}
