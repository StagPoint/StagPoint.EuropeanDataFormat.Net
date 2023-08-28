// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System.Runtime.CompilerServices;

namespace StagPoint.EDF.Net
{
	/// <summary>
	/// Provides a few commonly-used mathematical operations
	/// </summary>
	internal static class MathUtil
	{
		/// <summary>
		/// Linearly interpolates between a and b by t.
		///
		/// The parameter t is clamped to the range [0, 1].
		///		When t = 0 returns a.
		/// 	When t = 1 return b.
		/// 	When t = 0.5 returns the midpoint of a and b.
		/// </summary>
		/// <param name="a">The start value</param>
		/// <param name="b">The end value</param>
		/// <param name="t">The interpolation value (between 0.0 and 1.0) between a and b</param>
		/// <returns>The interpolated float result between the two float values.</returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static double Lerp( double a, double b, double t )
		{
			return (1.0 - t) * a + b * t;
		}

		/// <summary>
		/// Performs an "inverse interpolation" between a and b, returning the time t that
		/// the given value lies between both of those endpoints. 
		/// </summary>
		/// <param name="a">The start value</param>
		/// <param name="b">The end value</param>
		/// <param name="value">A value between start and end for which to return the interpolation value</param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static double InverseLerp( double a, double b, double value )
		{
			return (value - a) / (b - a);
		}

		/// <summary>
		/// Remaps a number from one range to another
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static double Remap( double fromA, double fromB, double toA, double toB, double value )
		{
			var t = InverseLerp( fromA, fromB, value );
			return Lerp( toA, toB, t );
		}
	}
}
