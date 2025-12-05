using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Whisper.Utils;
using System.IO;
using System;

namespace Whisper.Samples
{
    public class WhisperAudio : MonoBehaviour
    {
        [Header("Whisper Manager")]
        public WhisperManager manager;
        public bool streamSegments = true;
        public bool echoSound = false;
        public bool printLanguage = false;

        [Header("Microphone Recorder")]
        public MicrophoneRecorderWithVAD recorder;

        [Header("UI - Status")]
        public TextMeshProUGUI recordingStatusText;
        public TextMeshProUGUI vadStatusText;
        public Image recordingIndicator;
        public Image vadIndicator;

        [Header("UI - Transcription")]
        public TextMeshProUGUI outputText;
        public TextMeshProUGUI timeText;

        public event Action<string> OnTranscriptionComplete;

        private string _buffer;
        private bool _isTranscribing = false;
        private AudioClip _lastRecordedClip;
        private bool _isRecording = false;

        private void Awake()
        {
            // Пытаемся найти WhisperManager через WhisperScenePersist, если не назначен в инспекторе
            if (manager == null)
            {
                manager = WhisperManagerPersist.GetWhisperManager();
                if (manager == null)
                {
                    // Если не нашли через persist, ищем в сцене
                    manager = FindFirstObjectByType<WhisperManager>();
                }
            }

            if (manager != null)
            {
                manager.OnNewSegment += OnNewSegment;
                manager.OnProgress += OnProgressHandler;
                UnityEngine.Debug.Log("WhisperManager found and subscribed to events");
            }
            else
            {
                UnityEngine.Debug.LogError("WhisperManager not found!");
            }
        }

        private void Start()
        {
            if (recorder == null)
            {
                recorder = FindFirstObjectByType<MicrophoneRecorderWithVAD>();
                UnityEngine.Debug.Log(recorder != null ? "Found recorder in Start" : "Recorder not found");
            }

            if (recorder != null)
            {
                recorder.OnRecordingStarted += OnRecordingStarted;
                recorder.OnRecordingComplete += OnRecordingComplete;
            }
        }

        private void Update()
        {
            UpdateUIStatus();
        }

        private void UpdateUIStatus()
        {
            if (recordingStatusText != null)
            {
                recordingStatusText.text = _isRecording ?
                    "<color=red>● Recording</color>" : "○ Stopped";
            }

            if (recordingIndicator != null)
            {
                recordingIndicator.color = _isRecording ? Color.red : Color.gray;
            }

            if (vadStatusText != null)
            {
                vadStatusText.text = _isTranscribing ?
                    "<color=green>▲ Transcribing</color>" :
                    (_isRecording ? "<color=yellow>● Listening</color>" : "▼ Idle");
            }

            if (vadIndicator != null)
            {
                vadIndicator.color = _isTranscribing ? Color.green :
                                   (_isRecording ? Color.yellow : Color.gray);
            }
        }

        public void OnRecordingStarted()
        {
            UnityEngine.Debug.Log("Recording started - updating UI");
            _isRecording = true;

            if (outputText != null)
                outputText.text = "";

            _buffer = "";
        }

        public void OnRecordingComplete(AudioClip recordedClip, string filePath)
        {
            UnityEngine.Debug.Log($"Recording complete, starting transcription. Clip: {recordedClip != null}, Path: {filePath}");
            _isRecording = false;
            _lastRecordedClip = recordedClip;
            TranscribeLastRecording();
        }

        public async void TranscribeLastRecording()
        {
            if (_isTranscribing)
            {
                UnityEngine.Debug.LogWarning("Already transcribing, please wait...");
                return;
            }

            AudioClip recordedClip = _lastRecordedClip;

            if (recordedClip == null)
            {
                UnityEngine.Debug.LogWarning("No recorded audio clip available!");
                return;
            }

            if (manager == null)
            {
                UnityEngine.Debug.LogError("Whisper Manager is not assigned!");
                return;
            }

            if (!manager.IsLoaded)
            {
                UnityEngine.Debug.LogWarning("Whisper model is not loaded yet. Waiting...");

                float timeout = 10f;
                float startTime = Time.time;
                while (!manager.IsLoaded && (Time.time - startTime) < timeout)
                {
                    await System.Threading.Tasks.Task.Delay(100);
                }

                if (!manager.IsLoaded)
                {
                    UnityEngine.Debug.LogError("Whisper model failed to load within timeout");
                    return;
                }
            }

            _buffer = "";
            _isTranscribing = true;

            try
            {
                var sw = new Stopwatch();
                sw.Start();

                var res = await manager.GetTextAsync(recordedClip);
                if (res == null)
                {
                    UnityEngine.Debug.LogWarning("Transcription returned null result");
                    return;
                }

                var time = sw.ElapsedMilliseconds;
                var rate = recordedClip.length / (time * 0.001f);

                if (timeText != null)
                {
                    timeText.text = $"Time: {time} ms\nRate: {rate:F1}x\nLength: {recordedClip.length:F2}s";
                }

                var text = res.Result;
                if (printLanguage)
                    text += $"\n\nLanguage: {res.Language}";

                UnityEngine.Debug.Log($"Transcription completed: {text}");

                OnTranscriptionComplete?.Invoke(text);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Transcription failed: {e.Message}");
            }
            finally
            {
                _isTranscribing = false;
            }
        }

        public void ManualTranscribe()
        {
            if (_lastRecordedClip != null)
            {
                TranscribeLastRecording();
            }
            else
            {
                UnityEngine.Debug.LogWarning("No recording available to transcribe");
            }
        }

        private void OnProgressHandler(int progress)
        {
            if (timeText != null && _isTranscribing)
            {
                timeText.text = $"Transcribing: {progress}%";
            }
        }

        private void OnNewSegment(WhisperSegment segment)
        {
            if (!streamSegments || !outputText || !_isTranscribing)
                return;

            _buffer += segment.Text;
            outputText.text = _buffer + "...";
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.OnNewSegment -= OnNewSegment;
                manager.OnProgress -= OnProgressHandler;
            }

            if (recorder != null)
            {
                recorder.OnRecordingStarted -= OnRecordingStarted;
                recorder.OnRecordingComplete -= OnRecordingComplete;
            }
        }
    }
}