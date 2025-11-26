using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Whisper.Samples;

public class MicrophoneRecorderWithVAD : MonoBehaviour
{
    [Header("Transcription")]
    public WhisperAudio transcriptionManager;

    public System.Action<AudioClip, string> OnRecordingComplete;
    public event System.Action OnRecordingStarted;
    public AudioClip RecordedClip => recordingClip;

    [Header("Input (New Input System)")]
    public InputActionProperty recordAction;

    [Header("Microphone settings")]
    public int maxRecordSeconds = 300;
    public int requestedSampleRate = 0;
    public string fileName = "Presentation.wav";
    public string micDevice = "";

    [Header("VAD (local, used only if PresentationSettings absent or useVAD true)")]
    public bool alwaysSave = false; // если true Ч сохран€ем всЄ, без обрезки
    public float vadThreshold = 0.02f;
    public float vadFrameSeconds = 0.05f;
    public float minSpeechSeconds = 0.15f;
    public float preRollSeconds = 0.15f;
    public float postRollSeconds = 0.3f;
    public float minSaveLengthSeconds = 0.2f;

    [Header("Presentation Settings (ScriptableObject)")]
    public PresentationSettings presentationSettings;

    private AudioClip recordingClip;
    public bool isRecording = false;
    private int channels = 1;
    private int usedSampleRate = 0;
    private string path;

    void Start()
    {
        if (string.IsNullOrEmpty(micDevice))
        {
            if (Microphone.devices.Length > 0)
                micDevice = Microphone.devices[0];
            else
                micDevice = null;
        }

        if (recordAction != null && recordAction.action != null)
            recordAction.action.performed += OnRecordAction;

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string folder = Path.Combine(projectRoot, "Presentations");
        string path = Path.Combine(folder, fileName);
    }

    public void TriggerTranscription()
    {
        if (transcriptionManager != null)
        {
            transcriptionManager.TranscribeLastRecording();
        }
    }

    void OnEnable()
    {
        if (recordAction != null && recordAction.action != null)
            recordAction.action.Enable();
    }

    void OnDisable()
    {
        if (recordAction != null && recordAction.action != null)
        {
            recordAction.action.performed -= OnRecordAction;
            recordAction.action.Disable();
        }
    }

    private void OnRecordAction(InputAction.CallbackContext ctx)
    {
        if (!isRecording) StartRecording();
        else StopRecordingAndSave();
    }

    public void StartRecording()
    {
        if (micDevice == null)
        {
            Debug.LogError("No microphone device available.");
            return;
        }
        if (isRecording) return;

        int minFreq = 0, maxFreq = 0;
        Microphone.GetDeviceCaps(micDevice, out minFreq, out maxFreq);
        int fps = requestedSampleRate > 0 ? requestedSampleRate : AudioSettings.outputSampleRate;

        if (minFreq == 0 && maxFreq == 0)
        {
            usedSampleRate = fps;
        }
        else
        {
            if (maxFreq == 0)
                usedSampleRate = fps;
            else
            {
                if (fps >= minFreq && fps <= maxFreq)
                    usedSampleRate = fps;
                else
                    usedSampleRate = Mathf.Clamp(fps, minFreq, maxFreq);
            }
        }

        Debug.Log($"Starting mic '{micDevice}' with sampleRate={usedSampleRate}, caps min={minFreq}, max={maxFreq}");

        recordingClip = Microphone.Start(micDevice, false, maxRecordSeconds, usedSampleRate);
        channels = recordingClip.channels;

        StartCoroutine(WaitForMicStart());
    }

    System.Collections.IEnumerator WaitForMicStart()
    {
        while (Microphone.GetPosition(micDevice) <= 0)
            yield return null;

        isRecording = true;

        OnRecordingStarted?.Invoke();
    }

    public void StopRecordingAndSave()
    {
        if (!isRecording || recordingClip == null)
        {
            Debug.LogWarning("Not recording.");
            return;
        }

        int pos = Microphone.GetPosition(micDevice);
        Debug.Log($"Microphone.GetPosition = {pos} samples (per channel)");

        Microphone.End(micDevice);
        isRecording = false;

        if (pos <= 0)
        {
            Debug.LogWarning("Recorded sample count is zero.");
            return;
        }

        int samplesPerChannel = pos;
        int totalSamples = samplesPerChannel * channels;
        float[] allData = new float[totalSamples];

        bool got = recordingClip.GetData(allData, 0);
        if (!got)
        {
            Debug.LogWarning("AudioClip.GetData returned false Ч cannot read data.");
            return;
        }

        bool useVAD = true;
        if (presentationSettings != null)
            useVAD = presentationSettings.useVAD;
        else
            useVAD = !alwaysSave;

        if (!useVAD)
        {
            SaveClipToWav(allData, channels, usedSampleRate, samplesPerChannel);
            return;
        }

        int frameSamples = Mathf.Max(1, Mathf.RoundToInt(vadFrameSeconds * usedSampleRate));
        int framesCount = samplesPerChannel / frameSamples;
        if (framesCount <= 0)
        {
            Debug.LogWarning("Recorded too short for VAD frames.");
            return;
        }

        bool[] speechFrame = new bool[framesCount];
        for (int f = 0; f < framesCount; f++)
        {
            long idxStart = (long)f * frameSamples * channels;
            long idxEnd = idxStart + (long)frameSamples * channels;
            double sumSq = 0;
            long cnt = 0;
            for (long i = idxStart; i < idxEnd; i++)
            {
                float s = allData[i];
                sumSq += s * s;
                cnt++;
            }
            double rms = 0;
            if (cnt > 0) rms = Math.Sqrt(sumSq / cnt);
            speechFrame[f] = rms >= vadThreshold;
        }

        int minSpeechFrames = Mathf.Max(1, Mathf.RoundToInt(minSpeechSeconds / vadFrameSeconds));
        int preRollFrames = Mathf.RoundToInt(preRollSeconds / vadFrameSeconds);
        int postRollFrames = Mathf.RoundToInt(postRollSeconds / vadFrameSeconds);

        bool[] keepFrame = new bool[framesCount];
        int fIndex = 0;
        while (fIndex < framesCount)
        {
            if (!speechFrame[fIndex]) { fIndex++; continue; }

            int start = fIndex;
            int end = fIndex;
            while (end + 1 < framesCount && speechFrame[end + 1]) end++;

            int lengthFrames = end - start + 1;
            if (lengthFrames >= minSpeechFrames)
            {
                int s = Mathf.Max(0, start - preRollFrames);
                int e = Mathf.Min(framesCount - 1, end + postRollFrames);
                for (int k = s; k <= e; k++) keepFrame[k] = true;
            }
            fIndex = end + 1;
        }

        long keptFrames = 0;
        for (int f = 0; f < framesCount; f++) if (keepFrame[f]) keptFrames++;
        long keptSamplesPerChannel = keptFrames * frameSamples;
        long totalKeptSamples = keptSamplesPerChannel * channels;
        float keptSeconds = (float)keptSamplesPerChannel / usedSampleRate;

        if (keptSeconds < minSaveLengthSeconds)
        {
            Debug.Log($"Nothing significant detected (kept {keptSeconds:F3}s). File will not be saved.");
            return;
        }

        float[] keptData = new float[totalKeptSamples];
        long writePos = 0;
        for (int f = 0; f < framesCount; f++)
        {
            if (!keepFrame[f]) continue;
            long srcStart = (long)f * frameSamples * channels;
            long srcEnd = srcStart + (long)frameSamples * channels;
            for (long i = srcStart; i < srcEnd; i++)
            {
                keptData[writePos++] = allData[i];
            }
        }

        SaveClipToWav(keptData, channels, usedSampleRate, (int)keptSamplesPerChannel);

        AudioClip processedClip = AudioClip.Create("ProcessedRecording",
            (int)keptSamplesPerChannel, channels, usedSampleRate, false);
        processedClip.SetData(keptData, 0);

        OnRecordingComplete?.Invoke(processedClip, path);
    }

    private void SaveClipToWav(float[] samples, int channels, int sampleRate, int samplesPerChannel)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string folder = Path.Combine(projectRoot, "Presentations");
        try
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to create Presentations folder: " + e);
            folder = Application.persistentDataPath;
        }

        path = Path.Combine(folder, fileName);

        try
        {
            SaveWav(path, samples, channels, sampleRate, samplesPerChannel);
            float seconds = (float)samplesPerChannel / sampleRate;
            Debug.Log($"Saved WAV: {path} ({seconds:F2}s) sampleRate={sampleRate} channels={channels}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save WAV: " + ex);
        }
    }

    private void SaveWav(string filepath, float[] samples, int channels, int sampleRate, int samplesPerChannel)
    {
        int totalSamples = samplesPerChannel * channels;
        if (samples.Length < totalSamples)
            totalSamples = samples.Length;

        short[] intData = new short[totalSamples];
        byte[] bytesData = new byte[totalSamples * 2];
        const float maxShort = 32767f;
        for (int i = 0; i < totalSamples; i++)
        {
            float f = Mathf.Clamp(samples[i], -1f, 1f);
            short s = (short)(f * maxShort);
            intData[i] = s;
        }
        Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);

        using (FileStream fs = new FileStream(filepath, FileMode.Create))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            int byteRate = sampleRate * channels * 2;
            int dataSize = bytesData.Length;
            int fileSize = 36 + dataSize;

            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(fileSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)(channels * 2));
            bw.Write((short)16);

            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);

            bw.Write(bytesData, 0, bytesData.Length);
        }
    }

    void OnApplicationQuit()
    {
        if (isRecording && micDevice != null)
        {
            Microphone.End(micDevice);
        }
    }
}
