using System;
using MoonWorks;
using MoonWorks.Graphics;
using MoonWorks.Input;
using Tactician.Content;
using Tactician.GameStates;

namespace Tactician;

public class TacticianGame : Game {
    private readonly GameplayState GameplayState;
    private readonly HowToPlayState HowToPlayState;
    private readonly LoadState LoadState;

    private GameState CurrentState;

    public TacticianGame(
        WindowCreateInfo windowCreateInfo,
        FramePacingSettings framePacingSettings,
        ShaderFormat shaderFormats,
        bool debugMode
    ) : base(windowCreateInfo, framePacingSettings, shaderFormats, debugMode) {
        Inputs.Mouse.Hide();

        TextureAtlases.Init(GraphicsDevice);
        StaticAudioPacks.Init(AudioDevice);
        StreamingAudio.Init(AudioDevice);
        Fonts.LoadAll(GraphicsDevice);

        GameplayState = new GameplayState(this, null);
        LoadState = new LoadState(this, GameplayState);

        HowToPlayState = new HowToPlayState(this, GameplayState);
        GameplayState.SetTransitionState(HowToPlayState); // i hate this

        SetState(LoadState);
    }

    protected override void Update(TimeSpan dt) {
        if (Inputs.Keyboard.IsPressed(KeyCode.F11)) {
            if (MainWindow.ScreenMode == ScreenMode.Fullscreen)
                MainWindow.SetScreenMode(ScreenMode.Windowed);
            else
                MainWindow.SetScreenMode(ScreenMode.Fullscreen);
        }

        CurrentState.Update(dt);
    }

    protected override void Draw(double alpha) {
        CurrentState.Draw(MainWindow, alpha);
    }

    protected override void Destroy() {
    }

    public void SetState(GameState gameState) {
        if (CurrentState != null) CurrentState.End();

        gameState.Start();
        CurrentState = gameState;
    }
}