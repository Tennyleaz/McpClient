using PortAudioSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace McpClient.Services;

internal class AudioService : IDisposable
{
    private PortAudioSharp.Stream stream;
    private const int CHANNEL_COUNT = 1, CHUNK_SECONDS = 5;
    private BlockingCollection<short[]> sampleQueue; // default is a ConcurrentQueue
    private int sampleRate = 44100;

    public AudioService()
    {
        PortAudio.Initialize();
    }

    public (string name, int id) GetFirstDevice()
    {
        for (int i = 0; i < PortAudio.DeviceCount; ++i)
        {
            DeviceInfo deviceInfo = PortAudio.GetDeviceInfo(i);
            if (deviceInfo.maxInputChannels >= CHANNEL_COUNT)
            {
                return (deviceInfo.name, i);
            }
        }

        return (null, -1);
    }

    public bool StartRecord(int index)
    {
        // We are already streaming
        if (stream != null)
            return false;

        // Check if device exists
        if (index >= PortAudio.DeviceCount)
            return false;
        DeviceInfo deviceInfo = PortAudio.GetDeviceInfo(index);

        // Prepare parameters
        StreamParameters param = new StreamParameters
        {
            channelCount = CHANNEL_COUNT,
            device = index,
            hostApiSpecificStreamInfo = IntPtr.Zero,
            sampleFormat = SampleFormat.Int16,
            suggestedLatency = deviceInfo.defaultLowInputLatency
        };
        sampleRate = (int)deviceInfo.defaultSampleRate;

        stream = new PortAudioSharp.Stream(inParams: param, outParams: null, sampleRate: sampleRate,
            framesPerBuffer: 0,
            streamFlags: StreamFlags.ClipOff,
            callback: StreamCallback,
            userData: IntPtr.Zero
        );

        // Start save task
        sampleQueue = new BlockingCollection<short[]>();
        Task.Run(BackgroundChunkWriter);

        // Start record
        stream.Start();
        return true;
    }

    private StreamCallbackResult StreamCallback(IntPtr input, IntPtr output, uint frameCount, ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userData)
    {
        int length = (int)frameCount * CHANNEL_COUNT;
        Int16[] samples = new Int16[length];
        Marshal.Copy(input, samples, 0, length);

        //allSamples.AddRange(samples);
        if (!sampleQueue.TryAdd(samples))
        {
            Debug.WriteLine("Queue is full!");
        }

        return StreamCallbackResult.Continue;
    }

    private void BackgroundChunkWriter()
    {
        List<short> chunkBuffer = new List<short>();
        int chunkIndex = 0;
        int chunkSamples = sampleRate * CHUNK_SECONDS;
        foreach (short[] samples in sampleQueue.GetConsumingEnumerable())
        {
            chunkBuffer.AddRange(samples);
            while (chunkBuffer.Count >= chunkSamples)
            {
                List<short> oneChunk = chunkBuffer.GetRange(0, chunkSamples);
                chunkBuffer.RemoveRange(0, chunkSamples);
                WriteWavFile($"chunk_{chunkIndex++}.wav", sampleRate, oneChunk);
            }
        }

        // On exit, flush remaining
        if (chunkBuffer.Count > 0)
        {
            WriteWavFile($"chunk_{chunkIndex++}.wav", sampleRate, chunkBuffer);
        }

        Debug.WriteLine("BackgroundChunkWriter done.");
    }

    private static void WriteWavFile(string filename, int sampleRate, List<short> samples)
    {
        int blockAlign = CHANNEL_COUNT * 2; // 2 bytes per sample (16 bit)
        int byteRate = sampleRate * blockAlign;
        int subchunk2Size = samples.Count * 2; // 2 bytes per sample
        int chunkSize = 36 + subchunk2Size;    // header size + data

        using (FileStream fs = new FileStream(filename, FileMode.Create))
        using (BinaryWriter wr = new BinaryWriter(fs))
        {
            // RIFF header
            wr.Write(Encoding.ASCII.GetBytes("RIFF"));
            wr.Write(chunkSize);
            wr.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt  subchunk
            wr.Write(Encoding.ASCII.GetBytes("fmt "));
            wr.Write(16);                   // Subchunk1Size for PCM
            wr.Write((short)1);             // AudioFormat (1 = PCM)
            wr.Write((short)CHANNEL_COUNT); // NumChannels
            wr.Write(sampleRate);           // SampleRate
            wr.Write(byteRate);             // ByteRate
            wr.Write((short)blockAlign);    // BlockAlign
            wr.Write((short)16);            // BitsPerSample

            // data subchunk
            wr.Write(Encoding.ASCII.GetBytes("data"));
            wr.Write(subchunk2Size);

            // Write samples
            foreach (short sample in samples)
            {
                wr.Write(sample);
            }
        }

        Debug.WriteLine("WriteWavFile: " + filename);
    }

    public bool StopRecord()
    {
        if (stream != null)
        {
            stream.Stop();
            stream.Dispose();
            stream = null;
        }

        if (sampleQueue != null)
        {
            sampleQueue.CompleteAdding();
            sampleQueue.Dispose();
            sampleQueue = null;
        }

        return true;
    }

    public void Dispose()
    {
        StopRecord();
    }
}
