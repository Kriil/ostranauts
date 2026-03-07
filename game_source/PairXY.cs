using System;

public struct PairXY
{
	public PairXY(int x, int y)
	{
		this.x = x;
		this.y = y;
	}

	public bool IsInvalid()
	{
		return this.x < 0 || this.y < 0;
	}

	public bool IsValid()
	{
		return !this.IsInvalid();
	}

	public static PairXY GetInvalid()
	{
		return new PairXY(-1, -1);
	}

	public static bool operator ==(PairXY a, PairXY b)
	{
		return a.x == b.x && a.y == b.y;
	}

	public static bool operator !=(PairXY a, PairXY b)
	{
		return a.x != b.x || a.y != b.y;
	}

	public bool Equals(PairXY p)
	{
		return this == p;
	}

	public override string ToString()
	{
		return string.Concat(new object[]
		{
			"{",
			this.x,
			",",
			this.y,
			"}"
		});
	}

	public int x;

	public int y;
}
