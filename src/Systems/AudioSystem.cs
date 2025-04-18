using System;
using MoonTools.ECS;
using MoonWorks.Audio;
using Tactician.Content;
using Tactician.Data;
using Tactician.Messages;
using Tactician.Utility;

namespace Tactician.Systems;

public class AudioSystem : MoonTools.ECS.System {
    private readonly AudioDevice _audioDevice;
    private readonly PersistentVoice _droneVoice;
    private readonly StreamingSoundID[] _gameplaySongs;
    private readonly StreamingVoice _musicVoice;

    public AudioSystem(World world, AudioDevice audioDevice) : base(world) {
        _audioDevice = audioDevice;

        _gameplaySongs = [
            StreamingAudio.attentiontwerkers,
            StreamingAudio.attention_shoppers_v1,
            StreamingAudio.attention_shoppers_v2
        ];

        var streamingAudioData = StreamingAudio.Lookup(StreamingAudio.attention_shoppers_v1);
        _musicVoice = _audioDevice.Obtain<StreamingVoice>(streamingAudioData.Format);
        _musicVoice.SetVolume(0.0f); // TODO: re-enable audio

        _droneVoice = _audioDevice.Obtain<PersistentVoice>(StaticAudio.Lookup(StaticAudio.Drone1).Format);
        _droneVoice.SetVolume(0.5f); // TODO: re-enable audio
    }

    public override void Update(TimeSpan delta) {
        foreach (var staticSoundMessage in ReadMessages<PlayStaticSoundMessage>())
            PlayStaticSound(
                staticSoundMessage.Sound,
                staticSoundMessage.Volume,
                staticSoundMessage.Pitch,
                staticSoundMessage.Pan,
                staticSoundMessage.Category
            );

        if (SomeMessage<PlaySongMessage>()) {
            var streamingAudioData = StreamingAudio.Lookup(_gameplaySongs.GetRandomItem());
            streamingAudioData.Seek(0);
            _musicVoice.Load(streamingAudioData);
            _musicVoice.Play();
        }

        if (SomeMessage<StopDroneSounds>()) _droneVoice.Stop();
    }

    public void Cleanup() {
        _musicVoice.Unload();
        _musicVoice.Stop();
        _musicVoice.Dispose();

        _droneVoice.Stop();
        _droneVoice.Dispose();
    }

    private void PlayStaticSound(
        AudioBuffer sound,
        float volume,
        float pitch,
        float pan,
        SoundCategory soundCategory
    ) {
        SourceVoice voice;
        if (soundCategory == SoundCategory.Drone) {
            voice = _droneVoice;
            voice.Stop(); // drones should interrupt their own lines
        }
        else {
            voice = _audioDevice.Obtain<TransientVoice>(sound.Format);
        }

        voice.SetVolume(0.0f); // TODO: re-enable audio
        voice.SetPitch(pitch);
        voice.SetPan(pan);
        voice.Submit(sound);
        voice.Play();
    }
}