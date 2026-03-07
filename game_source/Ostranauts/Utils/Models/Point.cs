using System;
using UnityEngine;

namespace Ostranauts.Utils.Models
{
	public struct Point
	{
		public Point(double x, double y)
		{
			this.X = x;
			this.Y = y;
		}

		public Point(Vector3 v2)
		{
			this.X = (double)v2.x;
			this.Y = (double)v2.y;
		}

		public Point(ShipSitu situ)
		{
			this.X = situ.vPosx;
			this.Y = situ.vPosy;
		}

		public void Set(double x, double y)
		{
			this.X = x;
			this.Y = y;
		}

		public Vector2 GetVector2(Point target)
		{
			return new Vector2((float)(target.X - this.X), (float)(target.Y - this.Y));
		}

		public Vector2 ToVector2()
		{
			return new Vector2((float)this.X, (float)this.Y);
		}

		public static Point operator *(Point v1, decimal scalar)
		{
			return new Point(v1.X * (double)scalar, v1.Y * (double)scalar);
		}

		public static Point operator *(decimal scalar, Point v1)
		{
			return new Point(v1.X * (double)scalar, v1.Y * (double)scalar);
		}

		public static Point operator *(Point v1, double scalar)
		{
			return new Point(v1.X * scalar, v1.Y * scalar);
		}

		public static Point operator *(double scalar, Point v1)
		{
			return new Point(v1.X * scalar, v1.Y * scalar);
		}

		public static Point operator +(Point v1, Point v2)
		{
			return new Point(v1.X + v2.X, v1.Y + v2.Y);
		}

		public static Point operator -(Point v1, Point v2)
		{
			return new Point(v1.X - v2.X, v1.Y - v2.Y);
		}

		public static Point operator /(Point v1, double scalar)
		{
			return new Point(v1.X / scalar, v1.Y / scalar);
		}

		public Point normalized
		{
			get
			{
				double magnitude = this.magnitude;
				if (magnitude == 0.0)
				{
					return new Point(0.0, 0.0);
				}
				return new Point(this.X / magnitude, this.Y / magnitude);
			}
		}

		public Point perpendicular
		{
			get
			{
				return (this.X == 0.0) ? new Point(this.Y, -this.X) : new Point(-this.Y, this.X);
			}
		}

		public double magnitude
		{
			get
			{
				return Math.Sqrt(this.X * this.X + this.Y * this.Y);
			}
		}

		public double Dot(Point b)
		{
			return this.X * b.X + this.Y * b.Y;
		}

		public override string ToString()
		{
			return string.Concat(new object[]
			{
				"(",
				this.X,
				", ",
				this.Y,
				")"
			});
		}

		public double X;

		public double Y;
	}
}
