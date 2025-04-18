using System;
using System.Numerics;
using MoonWorks;
using MoonWorks.Audio;
using MoonWorks.Graphics;
using Tactician.Content;
using Tactician.Graphics;

namespace Tactician.GameStates;

public class HowToPlayState : GameState {
    private readonly AudioDevice _audioDevice;
    private readonly TacticianGame _game;
    private readonly GraphicsDevice _graphicsDevice;

    private readonly SpriteBatch _hiResSpriteBatch;
    private readonly Sampler _linearSampler;
    private readonly Texture _renderTexture;
    private readonly GameState _transitionState;

    private StreamingVoice _voice;

    public HowToPlayState(TacticianGame game, GameState transitionState) {
        _audioDevice = game.AudioDevice;
        _game = game;
        _graphicsDevice = game.GraphicsDevice;
        _transitionState = transitionState;

        _linearSampler = Sampler.Create(_graphicsDevice, SamplerCreateInfo.LinearClamp);
        _hiResSpriteBatch = new SpriteBatch(_graphicsDevice, game.MainWindow.SwapchainFormat);

        _renderTexture = Texture.Create2D(_graphicsDevice, Dimensions.GAME_W, Dimensions.GAME_H,
            game.MainWindow.SwapchainFormat, TextureUsageFlags.ColorTarget | TextureUsageFlags.Sampler);
    }

    public override void Start() {
        var sound = StreamingAudio.Lookup(StreamingAudio.tutorial_type_beat);
        if (_voice == null) {
            _voice = _audioDevice.Obtain<StreamingVoice>(sound.Format);
            _voice.Loop = true;
        }

        sound.Seek(0);
        _voice.Load(sound);
        _voice.SetVolume(0.0f); // TODO: Re-enable audio
        _voice.Play();
    }

    public override void Update(TimeSpan delta) {
        if (_game.Inputs.AnyPressed) _game.SetState(_transitionState);
    }

    public override void Draw(Window window, double alpha) {
        var commandBuffer = _graphicsDevice.AcquireCommandBuffer();

        var swapchainTexture = commandBuffer.AcquireSwapchainTexture(window);

        if (swapchainTexture != null) {
            _hiResSpriteBatch.Start();

            var logoAnimation = SpriteAnimations.Screen_HowToPlay;
            var sprite = logoAnimation.Frames[0];
            _hiResSpriteBatch.Add(
                new Vector3(0, 0, -1f),
                0,
                new Vector2(sprite.SliceRect.W, sprite.SliceRect.H),
                Color.White,
                sprite.UV.LeftTop, sprite.UV.Dimensions
            );

            _hiResSpriteBatch.Upload(commandBuffer);

            var renderPass = commandBuffer.BeginRenderPass(new ColorTargetInfo(_renderTexture, Color.Black));

            var hiResViewProjectionMatrices =
                new ViewProjectionMatrices(Matrix4x4.Identity, GetHiResProjectionMatrix());

            _hiResSpriteBatch.Render(
                renderPass,
                TextureAtlases.TP_HiRes.Texture,
                _linearSampler,
                hiResViewProjectionMatrices
            );

            commandBuffer.EndRenderPass(renderPass);

            commandBuffer.Blit(_renderTexture, swapchainTexture, Filter.Nearest);
        }

        _graphicsDevice.Submit(commandBuffer);
    }

    public override void End() {
        _voice.Stop();
    }

    private Matrix4x4 GetHiResProjectionMatrix() {
        return Matrix4x4.CreateOrthographicOffCenter(
            0,
            Dimensions.GAME_W,
            Dimensions.GAME_H,
            0,
            0.01f,
            1000
        );
    }
}