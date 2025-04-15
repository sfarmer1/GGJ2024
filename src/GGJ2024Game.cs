using MoonWorks.Graphics;
using MoonWorks;
using Tactician.Content;
using Tactician.GameStates;

namespace Tactician
{
	public class TacticianGame : Game
	{
		LoadState LoadState;
		LogoState LogoState;
		CreditsState CreditsState;
		GameplayState GameplayState;
		TitleState TitleState;
		HowToPlayState HowToPlayState;

		GameState CurrentState;

		public TacticianGame(
			WindowCreateInfo windowCreateInfo,
			FramePacingSettings framePacingSettings,
			ShaderFormat shaderFormats,
			bool debugMode
		) : base(windowCreateInfo, framePacingSettings, shaderFormats, debugMode)
		{
			Inputs.Mouse.Hide();

			TextureAtlases.Init(GraphicsDevice);
			StaticAudioPacks.Init(AudioDevice);
			StreamingAudio.Init(AudioDevice);
			Fonts.LoadAll(GraphicsDevice);

			CreditsState = new CreditsState(this, TitleState);
			LogoState = new LogoState(this, CreditsState, TitleState);
			TitleState = new TitleState(this, LogoState, HowToPlayState);
			LoadState = new LoadState(this, LogoState);
			CreditsState.SetTransitionState(TitleState); // i hate this

			GameplayState = new GameplayState(this, TitleState);
			HowToPlayState = new HowToPlayState(this, GameplayState);
			TitleState.SetTransitionStateB(HowToPlayState);

			SetState(LoadState);
		}

		protected override void Update(System.TimeSpan dt)
		{
			if (Inputs.Keyboard.IsPressed(MoonWorks.Input.KeyCode.F11))
			{
				if (MainWindow.ScreenMode == ScreenMode.Fullscreen)
					MainWindow.SetScreenMode(ScreenMode.Windowed);
				else
					MainWindow.SetScreenMode(ScreenMode.Fullscreen);

			}

			CurrentState.Update(dt);
		}

		protected override void Draw(double alpha)
		{
			CurrentState.Draw(MainWindow, alpha);
		}

		protected override void Destroy()
		{

		}

		public void SetState(GameState gameState)
		{
			if (CurrentState != null)
			{
				CurrentState.End();
			}

			gameState.Start();
			CurrentState = gameState;
		}
	}
}
