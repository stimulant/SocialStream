using System;
using Microsoft.Xna.Framework.Audio;

namespace SocialStream.Helpers
{
    /// <summary>
    /// A class to manage and play audio samples.
    /// </summary>
    internal class Audio : IDisposable
    {
        /// <summary>
        /// The private instance of this Singleton.
        /// </summary>
        private static Audio _instance;

        /// <summary>
        /// The XNA AudioEngine.
        /// </summary>
        private AudioEngine _audioEngine;

        /// <summary>
        /// The XNA WaveBank.
        /// </summary>
        private WaveBank _waveBank;

        /// <summary>
        /// The XNA SoundBank.
        /// </summary>
        private SoundBank _soundBank;

        /// <summary>
        /// Prevents a default instance of the <see cref="Audio"/> class from being created.
        /// </summary>
        private Audio()
        {
            LoadAudioContent();
        }

        /// <summary>
        /// Gets the singleton instance of this class.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static Audio Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Audio();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Loads the audio data manually.
        /// </summary>
        public static void Initialize()
        {
            _instance = new Audio();
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose IDisposable members.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _audioEngine.Dispose();
            _waveBank.Dispose();
            _soundBank.Dispose();
        }

        #endregion

        /// <summary>
        /// Plays an audio sample.
        /// </summary>
        /// <param name="soundCue">The sound cue.</param>
        public void PlayCue(string soundCue)
        {
            if (_audioEngine == null)
            {
                return;
            }

            Cue cue = null;
            try
            {
                cue = _soundBank.GetCue(soundCue);
            }
            catch
            {
                throw;
            }

            if (cue != null)
            {
                cue.Play();
            }
        }

        /// <summary>
        /// Load up the audio files.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "I don't know what this means, or how I'd fix it.")]
        private void LoadAudioContent()
        {
            string filename = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string path = System.IO.Path.GetDirectoryName(filename) + "\\Resources\\Audio\\";

            try
            {
                _audioEngine = new AudioEngine(path + "Audio.xgs");
                _waveBank = new WaveBank(_audioEngine, path + "Audio.xwb");
                _soundBank = new SoundBank(_audioEngine, path + "Audio.xsb");
            }
            catch
            {
                _audioEngine = null;
                _waveBank = null;
                _soundBank = null;
                throw;
            }
        }
    }
}
