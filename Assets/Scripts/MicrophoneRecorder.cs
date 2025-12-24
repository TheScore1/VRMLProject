using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Whisper.Samples;

public class MicrophoneRecorder : MonoBehaviour
{
    [Header("Transcription")]
    public WhisperAudio transcriptionManager;

    public Action<AudioClip, string> OnRecordingComplete;
    public event Action OnRecordingStarted;
    public AudioClip RecordedClip => recordingClip;
    public bool SpeechEnded;

    [Header("Input (New Input System)")]
    public InputActionProperty recordAction;

    [Header("Microphone settings")]
    public int maxRecordSeconds = 300;
    public int requestedSampleRate = 0;
    public string fileName = "Presentation.wav";
    public string micDevice = "";

    [Header("Presentation Settings (ScriptableObject)")]
    public PresentationSettings presentationSettings;

    private AudioClip recordingClip;
    public bool isRecording = false;
    private int channels = 1;
    private int usedSampleRate = 0;
    private string path;

    private bool presentationSaved = false;
    private int answerIndex = 1;

    void Start()
    {
        MicrophoneStateManager.IsActuallySpeaking = false;

        if (string.IsNullOrEmpty(micDevice))
        {
            if (Microphone.devices.Length > 0)
                micDevice = Microphone.devices[0];
            else
                micDevice = null;
        }

        if (recordAction != null && recordAction.action != null)
            recordAction.action.performed += OnRecordAction;
    }

    public void ResetSpeechState()
    {
        presentationSaved = false;
        SpeechEnded = false;
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
        if (!MicrophoneStateManager.IsRemoteHeld)
        {
            Debug.LogWarning("Trying to speak without remote in hands.");
            return;
        }
        if (isRecording) return;
        if (MicrophoneStateManager.IsProcessing)
        {
            Debug.LogWarning("Transcribation isn't finished. Waiting for it.");
            return;
        }

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
        MicrophoneStateManager.IsActuallySpeaking = true;

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
        MicrophoneStateManager.IsActuallySpeaking = false;
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
            Debug.LogWarning("AudioClip.GetData returned false — cannot read data.");
            return;
        }

        SaveClipToWav(allData, channels, usedSampleRate, samplesPerChannel, null);

        AudioClip processedClip = AudioClip.Create("ProcessedRecording",
            samplesPerChannel, channels, usedSampleRate, false);
        processedClip.SetData(allData, 0);

        OnRecordingComplete?.Invoke(processedClip, path);
    }

    private void SaveClipToWav(float[] samples, int channels, int sampleRate, int samplesPerChannel, string overrideFileName)
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

        string chosenFileName;
        if (!string.IsNullOrEmpty(overrideFileName))
        {
            chosenFileName = overrideFileName;
        }
        else
        {
            if (!presentationSaved)
            {
                chosenFileName = fileName;
                presentationSaved = true;
                SpeechEnded = true;
                Debug.Log("First presentation saved — SpeechEnded set to true.");
            }
            else
            {
                chosenFileName = $"Presentation_Answer_{answerIndex}.wav";
                answerIndex++;
            }
        }

        path = Path.Combine(folder, chosenFileName);

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

    public void ResetSession()
    {
        presentationSaved = false;
        answerIndex = 1;
        SpeechEnded = false;
        Debug.Log("Presentation session reset: presentationSaved=false, answerIndex=1, SpeechEnded=false");
    }

    void OnApplicationQuit()
    {
        if (isRecording && micDevice != null)
        {
            Microphone.End(micDevice);
        }
    }
}
