namespace Tactician.Components;

public readonly record struct Colliding;

public readonly record struct Holding;

public readonly record struct HasScore;

public readonly record struct UpdateDisplayScoreOnDestroy(bool Negative);

public readonly record struct DontMove;

public readonly record struct DontDraw;

public readonly record struct CountUpScore(int Start, int End);

public readonly record struct DontTime;