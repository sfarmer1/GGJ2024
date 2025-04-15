
using System;
using MoonTools.ECS;
using MoonWorks.Graphics;
using Tactician.Content;
using MoonWorks.Math;
using Tactician.Components;
using Tactician.Data;
using Tactician.Messages;
using Tactician.Relations;
using Tactician.Utility;

namespace Tactician.Systems;

public class HoldSystem : MoonTools.ECS.System
{
	MoonTools.ECS.Filter CanHoldFilter;
	ProductSpawner ProductSpawner;
	DroneSpawner DroneSpawner;

	public HoldSystem(World world) : base(world)
	{

		CanHoldFilter =
			FilterBuilder
			.Include<Rectangle>()
			.Include<Position>()
			.Include<CanHold>()
			.Build();

		ProductSpawner = new ProductSpawner(world);
		DroneSpawner = new DroneSpawner(world);
	}

	void HoldOrDrop(Entity e)
	{
		if (!HasOutRelation<Holding>(e))
		{
			foreach (var o in OutRelations<Colliding>(e))
			{
				if (Has<CanBeHeld>(o))
				{
					UnrelateAll<Holding>(o); // steal
					Relate(e, o, new Holding());
					Send(new PlayStaticSoundMessage(StaticAudio.PickUp));

					var spriteInfo = Get<SpriteAnimation>(o).SpriteAnimationInfo;
					Send(new SetAnimationMessage(
						o,
						new SpriteAnimation(spriteInfo, 90, true)
					));

					if (Has<CanBeStolenFrom>(e))
					{
						// chance to spawn evil drone
						if (Rando.Int(0, 10) == 0)
						{
							DroneSpawner.SpawnEvilDrone(o);
						}
					}

					break;
				}
			}
		}
		else
		{
			// Dropping
			var holding = OutRelationSingleton<Holding>(e);
			Remove<Velocity>(holding);
			UnrelateAll<Holding>(e);
			Send(new PlayStaticSoundMessage(StaticAudio.PutDown));

			var spriteInfo = Get<SpriteAnimation>(holding).SpriteAnimationInfo;
			Send(new SetAnimationMessage(
				holding,
				new SpriteAnimation(spriteInfo, 90, true)
			));
			Set(holding, Get<Position>(holding) + new Position(0, 10));
		}
	}

	void SetHoldParameters(Entity e, float dt)
	{
		var holding = OutRelationSingleton<Holding>(e);
		var holderPos = Get<Position>(e);
		var holderDirection = Get<LastDirection>(e).Direction;

		Set(holding, holderPos + holderDirection * 16 + new Position(0, -10));
		var depth = float.Lerp(100, 10, Get<Position>(holding).Y / (float)Dimensions.GAME_H);
		Set(holding, new Depth(depth));

		// this is drone jank -evan
		if (Has<CanTargetProductSpawner>(e) || Has<CanStealProducts>(e))
		{
			Set(holding, holderPos + new Position(0, 15));
		}

		if (Has<Player>(e))
		{
			if (!HasOutRelation<HoldingText>(e))
			{
				var textEntity = CreateEntity();
				Set(textEntity, new Depth(6));
				Set(textEntity, new TextDropShadow(1, 1));
				Relate(e, textEntity, new HoldingText());
			}

			var txt = OutRelationSingleton<HoldingText>(e);
			Set(txt, holderPos);
			Set(txt, new Text(
				Fonts.KosugiID,
				FontSizes.HOLDING,
				$"${ProductSpawner.GetPrice(holding).ToString("F2")}",
				MoonWorks.Graphics.Font.HorizontalAlignment.Center,
				MoonWorks.Graphics.Font.VerticalAlignment.Middle
			));
		}
	}

	public void Inspect(Entity potentialHolder, Entity product)
	{
		var playerIndex = Get<Player>(potentialHolder).Index;
		Send(new PlayStaticSoundMessage(StaticAudio.BubbleOpen, Data.SoundCategory.Generic, 0.5f));

		var index = 0;
		if (Some<IsPopupBox>())
		{
			// jank to push old boxes farther back
			foreach (var (_, uiElement) in Relations<ShowingPopup>())
			{
				if (Has<IsPopupBox>(uiElement))
				{
					Set(uiElement, new Depth(8));
				}
				else
				{
					Set(uiElement, new Depth(6));
				}
			}

			// newly created popups will draw on top of older ones
			index = 1;
		}

		var font = Fonts.FromID(Fonts.KosugiID);

		var holderPosition = Get<Position>(potentialHolder);

		Relate(potentialHolder, product, new Inspecting());

		var xOffset = holderPosition.X < Dimensions.GAME_W * 3 / 4 ? 10 : -100;
		var yOffset = holderPosition.Y > Dimensions.GAME_H * 3 / 4 ? -100 : -30;

		var backgroundRect = CreateEntity();
		Set(backgroundRect, holderPosition + new Position(xOffset - 5, yOffset - 5));
		Set(backgroundRect, new DrawAsRectangle());
		Set(backgroundRect, new Depth(8 - index * 4));
		Set(backgroundRect, new IsPopupBox());

		if (playerIndex == 0)
		{
			Set(backgroundRect, new ColorBlend(Color.DarkGreen));
		}
		else
		{
			Set(backgroundRect, new ColorBlend(new Color(0, 52, 139)));
		}

		Relate(potentialHolder, backgroundRect, new ShowingPopup());

		var name = CreateEntity();
		Set(name, holderPosition + new Position(xOffset, yOffset));
		Set(name, new Text(Fonts.KosugiID, FontSizes.INSPECT, Get<Name>(product).TextID, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		Set(name, new TextDropShadow(1, 1));
		Set(name, new Depth(6 - index * 4));

		Relate(potentialHolder, name, new ShowingPopup());

		font.TextBounds(
			TextStorage.GetString(Get<Name>(product).TextID),
			10,
			MoonWorks.Graphics.Font.HorizontalAlignment.Left,
			MoonWorks.Graphics.Font.VerticalAlignment.Top,
			out var textBounds
		);

		var textBoundsRectangle = TextRectangle(textBounds, new Position(xOffset - 5, yOffset - 5));

		yOffset += 15;

		var price = CreateEntity();
		Set(price, holderPosition + new Position(xOffset, yOffset));
		Set(price, new Text(Fonts.KosugiID, FontSizes.INSPECT, "$" + ProductSpawner.GetPrice(product).ToString("F2"), MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		Set(price, new TextDropShadow(1, 1));
		Set(price, new Depth(6 - index * 4));

		Relate(potentialHolder, price, new ShowingPopup());
		Relate(price, product, new DisplayingProductPrice());

		yOffset += 15;

		foreach (var ingredient in OutRelations<HasIngredient>(product))
		{
			var ingredientString = CategoriesAndIngredients.GetDisplayName(Get<Ingredient>(ingredient));
			var ingredientPriceString = "$" + Get<Price>(ingredient).Value.ToString("F2");

			var ingredientName = CreateEntity();
			Set(ingredientName, holderPosition + new Position(xOffset, yOffset));
			Set(ingredientName, new Text(Fonts.KosugiID, FontSizes.INGREDIENT, ingredientString, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
			Set(ingredientName, new TextDropShadow(1, 1));
			Set(ingredientName, new Depth(6 - index * 4));

			Relate(potentialHolder, ingredientName, new ShowingPopup());

			font.TextBounds(
				ingredientString,
				8,
				MoonWorks.Graphics.Font.HorizontalAlignment.Left,
				MoonWorks.Graphics.Font.VerticalAlignment.Top,
				out textBounds
			);

			textBoundsRectangle = Rectangle.Union(
				textBoundsRectangle,
				TextRectangle(textBounds, new Position(xOffset, yOffset))
			);

			var priceAdditionalOffset = textBounds.W + 3;

			var ingredientPrice = CreateEntity();
			Set(ingredientPrice, holderPosition + new Position(xOffset + priceAdditionalOffset, yOffset));
			Set(ingredientPrice, new Text(Fonts.KosugiID, FontSizes.INGREDIENT, ingredientPriceString, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
			Set(ingredientPrice, new TextDropShadow(1, 1));
			Set(ingredientPrice, new Depth(6 - index * 4));

			Relate(potentialHolder, ingredientPrice, new ShowingPopup());
			Relate(ingredientPrice, ingredient, new DisplayingIngredientPrice());

			font.TextBounds(
				ingredientPriceString,
				8,
				MoonWorks.Graphics.Font.HorizontalAlignment.Left,
				MoonWorks.Graphics.Font.VerticalAlignment.Top,
				out textBounds
			);

			textBoundsRectangle = Rectangle.Union(
				textBoundsRectangle,
				TextRectangle(textBounds, new Position(xOffset + priceAdditionalOffset, yOffset))
			);

			yOffset += 15;
		}

		textBoundsRectangle = textBoundsRectangle.Inflate(5, 5);

		Set(backgroundRect, new Rectangle(0, 0, textBoundsRectangle.Width, textBoundsRectangle.Height));
	}

	public void StopInspect(Entity potentialHolder)
	{
		foreach (var other in OutRelations<Inspecting>(potentialHolder))
		{
			Unrelate<Inspecting>(potentialHolder, other);
		}

		foreach (var other in OutRelations<ShowingPopup>(potentialHolder))
		{
			Destroy(other);
		}
	}

	public override void Update(TimeSpan delta)
	{
		foreach (var holder in CanHoldFilter.Entities)
		{
			if (HasOutRelation<Inspecting>(holder))
			{
				var inspectedProduct = OutRelationSingleton<Inspecting>(holder);
				if (!Related<Colliding>(holder, inspectedProduct))
				{
					StopInspect(holder);
				}
			}

			if (Has<TryHold>(holder))
			{
				HoldOrDrop(holder);
				Remove<TryHold>(holder);
			}
			else if (!HasOutRelation<Inspecting>(holder) && !HasOutRelation<Holding>(holder))
			{
				foreach (var other in OutRelations<Colliding>(holder))
				{
					if (Has<CanInspect>(holder) && Has<CanBeHeld>(other))
					{
						Inspect(holder, other);
						break;
					}
				}
			}

			if (HasOutRelation<Holding>(holder))
			{
				SetHoldParameters(holder, (float)delta.TotalSeconds);
			}
			else
			{
				if (HasOutRelation<HoldingText>(holder))
					Destroy(OutRelationSingleton<HoldingText>(holder));
			}
		}

		// real-time price updates
		foreach (var (uiText, product) in Relations<DisplayingProductPrice>())
		{
			Set(uiText, new Text(Fonts.KosugiID, 10, "$" + ProductSpawner.GetPrice(product).ToString("F2"), MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		}

		foreach (var (uiText, ingredient) in Relations<DisplayingIngredientPrice>())
		{
			var ingredientPriceString = "$" + Get<Price>(ingredient).Value.ToString("F2");
			Set(uiText, new Text(Fonts.KosugiID, 8, ingredientPriceString, MoonWorks.Graphics.Font.HorizontalAlignment.Left, MoonWorks.Graphics.Font.VerticalAlignment.Top));
		}
	}

	private static Rectangle TextRectangle(WellspringCS.Wellspring.Rectangle textBounds, Position position)
	{
		return new Rectangle((int)textBounds.X + position.X, (int)textBounds.Y + position.Y, (int)textBounds.W, (int)textBounds.H);
	}
}
