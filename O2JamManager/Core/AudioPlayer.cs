using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NVorbis;

namespace O2JamManager.Core;

public class VorbisSampleProvider : ISampleProvider, IDisposable
{
    private readonly MemoryStream _stream;
    private readonly VorbisReader _reader;
    private readonly WaveFormat _waveFormat;

    public VorbisSampleProvider(byte[] oggBytes)
    {
        _stream = new MemoryStream(oggBytes);
        _reader = new VorbisReader(_stream);
        _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_reader.SampleRate, _reader.Channels);
    }

    public WaveFormat WaveFormat => _waveFormat;

    public TimeSpan TotalTime => _reader.TotalTime;

    public TimeSpan CurrentTime
    {
        get => _reader.TimePosition;
        set => _reader.TimePosition = value;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        return _reader.ReadSamples(buffer, offset, count);
    }

    public void Dispose()
    {
        _reader.Dispose();
        _stream.Dispose();
    }
}

public class AudioPlayer : IDisposable
{
    private WaveOutEvent? _outputDevice;
    private VolumeSampleProvider? _volumeProvider;
    private MixedSongSampleProvider? _mixedProvider;
    private VorbisSampleProvider? _vorbisProvider;
    private WaveFileReader? _waveFileReader;
    private MemoryStream? _waveStream;

    private float _volume = 0.50f; // 50% par defaut au demarrage
    private bool _isDisposed;

    public event Action? PlaybackStarted;
    public event Action? PlaybackStopped;
    public event Action? PlaybackPaused;

    public bool IsPlaying => _outputDevice?.PlaybackState == PlaybackState.Playing;
    public bool IsPaused => _outputDevice?.PlaybackState == PlaybackState.Paused;

    /// <summary>
    /// Volume direct : gain = slider_value (slider entre 0.0 et 1.0).
    /// 100% sur le slider = 100% du signal normalisé à 0.98 (puissant, clair, sans distorsion).
    /// </summary>
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0.0f, 1.0f);
            if (_volumeProvider != null)
                _volumeProvider.Volume = _volume;
            if (_outputDevice != null)
                _outputDevice.Volume = _volume;
        }
    }

    public double Duration
    {
        get
        {
            if (_mixedProvider != null)
                return _mixedProvider.TotalSeconds;
            if (_vorbisProvider != null)
                return _vorbisProvider.TotalTime.TotalSeconds;
            if (_waveFileReader != null)
                return _waveFileReader.TotalTime.TotalSeconds;
            return 0.0;
        }
    }

    public double Position
    {
        get
        {
            if (_mixedProvider != null)
                return _mixedProvider.CurrentSeconds;
            if (_vorbisProvider != null)
                return _vorbisProvider.CurrentTime.TotalSeconds;
            if (_waveFileReader != null)
                return _waveFileReader.CurrentTime.TotalSeconds;
            return 0.0;
        }
        set => Seek(value);
    }

    public void PlayMixedSong(MixedSongSampleProvider mixedProvider)
    {
        // Stop previous without triggering a false "Lecture terminée" UI event
        Stop(notify: false);

        try
        {
            _mixedProvider = mixedProvider;
            _volumeProvider = new VolumeSampleProvider(_mixedProvider) { Volume = _volume };

            // Convert to standard 16-bit PCM for 100% universal Windows compatibility
            var wave16 = _volumeProvider.ToWaveProvider16();

            _outputDevice = new WaveOutEvent
            {
                DesiredLatency = 150,
                NumberOfBuffers = 3
            };
            _outputDevice.PlaybackStopped += OnOutputPlaybackStopped;
            _outputDevice.Init(wave16);
            _outputDevice.Play();

            PlaybackStarted?.Invoke();
        }
        catch (Exception)
        {
            Stop(notify: false);
            throw;
        }
    }

    public void PlayRawBytes(byte[] audioBytes)
    {
        Stop(notify: false);

        if (audioBytes == null || audioBytes.Length < 4)
            return;

        try
        {
            _outputDevice = new WaveOutEvent
            {
                DesiredLatency = 150,
                NumberOfBuffers = 3
            };
            _outputDevice.PlaybackStopped += OnOutputPlaybackStopped;

            if (audioBytes.Length >= 4 && audioBytes[0] == 0x4F && audioBytes[1] == 0x67 && audioBytes[2] == 0x67 && audioBytes[3] == 0x53)
            {
                _vorbisProvider = new VorbisSampleProvider(audioBytes);
                _volumeProvider = new VolumeSampleProvider(_vorbisProvider) { Volume = _volume };
                _outputDevice.Init(_volumeProvider.ToWaveProvider16());
            }
            else
            {
                _waveStream = new MemoryStream(audioBytes);
                _waveFileReader = new WaveFileReader(_waveStream);
                var sampleProvider = _waveFileReader.ToSampleProvider();
                _volumeProvider = new VolumeSampleProvider(sampleProvider) { Volume = _volume };
                _outputDevice.Init(_volumeProvider.ToWaveProvider16());
            }

            _outputDevice.Play();
            PlaybackStarted?.Invoke();
        }
        catch (Exception)
        {
            Stop(notify: false);
            throw;
        }
    }

    public void Pause()
    {
        if (_outputDevice != null && _outputDevice.PlaybackState == PlaybackState.Playing)
        {
            _outputDevice.Pause();
            PlaybackPaused?.Invoke();
        }
    }

    public void Resume()
    {
        if (_outputDevice != null && _outputDevice.PlaybackState == PlaybackState.Paused)
        {
            _outputDevice.Play();
            PlaybackStarted?.Invoke();
        }
    }

    public void Seek(double seconds)
    {
        try
        {
            if (_mixedProvider != null)
            {
                _mixedProvider.CurrentSeconds = Math.Max(0, seconds);
            }
            else
            {
                var target = TimeSpan.FromSeconds(Math.Max(0, seconds));
                if (_vorbisProvider != null)
                {
                    _vorbisProvider.CurrentTime = target;
                }
                else if (_waveFileReader != null)
                {
                    _waveFileReader.CurrentTime = target;
                }
            }
        }
        catch { }
    }

    public void Stop(bool notify = true)
    {
        try
        {
            if (_outputDevice != null)
            {
                _outputDevice.PlaybackStopped -= OnOutputPlaybackStopped;
                _outputDevice.Stop();
                _outputDevice.Dispose();
                _outputDevice = null;
            }

            _volumeProvider = null;
            _mixedProvider = null;

            if (_vorbisProvider != null)
            {
                _vorbisProvider.Dispose();
                _vorbisProvider = null;
            }

            if (_waveFileReader != null)
            {
                _waveFileReader.Dispose();
                _waveFileReader = null;
            }

            if (_waveStream != null)
            {
                _waveStream.Dispose();
                _waveStream = null;
            }
        }
        catch { }

        if (notify)
        {
            PlaybackStopped?.Invoke();
        }
    }

    private void OnOutputPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        PlaybackStopped?.Invoke();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Stop(notify: false);
    }
}
