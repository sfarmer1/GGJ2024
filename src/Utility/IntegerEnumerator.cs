namespace Tactician.Utility;

public ref struct IntegerEnumerator {
    private readonly int End;
    private readonly int Increment;

    public IntegerEnumerator GetEnumerator() {
        return this;
    }

    public IntegerEnumerator(int start, int end) {
        Current = start;
        End = end;
        if (end >= start)
            Increment = 1;
        else if (end < start)
            Increment = -1;
        else
            Increment = 0;
    }

    // does not include a, but does include b.
    public static IntegerEnumerator IntegersBetween(int a, int b) {
        return new IntegerEnumerator(a, b);
    }

    public bool MoveNext() {
        Current += Increment;
        return (Increment > 0 && Current <= End) || (Increment < 0 && Current >= End);
    }

    public int Current { get; private set; }
}