// Copyright (C) 2023 Jonah Stagner (StagPoint). All rights reserved.

using System;
using System.Globalization;
using System.IO;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace StagPoint.EDF.Net
{
	/// <summary>
	/// Stores a fixed-length ASCII string representing a whole number. For consistency
	/// </summary>
	public class EdfAsciiDateTime : EdfAsciiField
	{
		#region Public properties

		public DateTime Value { get; set; }

		/// <summary>
		/// You may encounter legacy EDF files which contain invalid Start Date values ("mm.dd.yy" instead of "dd.mm.yy"),
		/// such as those in the "sleep-heart-health-study-psg-database-1.0.0" dataset. Since it may still be necessary
		/// to read those files, you can set <see cref="UseAlternateDateFormat"/> to TRUE when necessary.
		/// You should otherwise have no other need to change this value. 
		/// </summary>
		public bool UseAlternateDateFormat { get; set; } = false;

		#endregion

		#region Constructors

		public EdfAsciiDateTime() : base( 16 )
		{
			this.Value = DateTime.Today;
		}

		public EdfAsciiDateTime( DateTime value ) : base( 16 )
		{
			this.Value = value;
		}

		#endregion

		#region EdfAsciiField overrides

		internal override void ReadFrom( BinaryReader buffer )
		{
			// Dates are stored as dd.MM.yy and times as HH.mm.ss
			var dateString = BufferHelper.ReadFromBuffer( buffer, 8 );
			var timeString = BufferHelper.ReadFromBuffer( buffer, 8 );

			this.Value = DateTime.ParseExact( 
				$"{dateString} {timeString}", 
				UseAlternateDateFormat ? "MM.dd.yy HH.mm.ss" : "dd.MM.yy HH.mm.ss", 
				CultureInfo.InvariantCulture );
		}

		internal override void WriteTo( BinaryWriter buffer )
		{
			BufferHelper.WriteToBuffer( buffer, Value.ToString( "dd.MM.yy" ), 8 );
			BufferHelper.WriteToBuffer( buffer, Value.ToString( "HH.mm.ss" ), 8 );
		}

		#endregion

		#region Base class overrides and implicit type conversion

		public override string ToString()
		{
			return Value.ToString( CultureInfo.InvariantCulture );
		}

		public override int GetHashCode()
		{
			// ReSharper disable once NonReadonlyMemberInGetHashCode
			return Value.GetHashCode();
		}

		public static implicit operator DateTime( EdfAsciiDateTime field )
		{
			return field.Value;
		}

		#endregion
	}
}
