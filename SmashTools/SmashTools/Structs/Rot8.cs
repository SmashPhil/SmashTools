﻿using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace SmashTools
{
	/// <summary>
	/// 8 direction Rot struct for proper diagonal capturing
	/// </summary>
	public struct Rot8 : IEquatable<Rot8>
	{
		private byte rotInt;

		public const byte NorthInt = 0;
		public const byte EastInt = 1;
		public const byte SouthInt = 2;
		public const byte WestInt = 3;
		public const byte NorthEastInt = 4;
		public const byte SouthEastInt = 5;
		public const byte SouthWestInt = 6;
		public const byte NorthWestInt = 7;

		public Rot8(byte newRot)
		{
			rotInt = newRot;
		}

		public Rot8(int newRot)
		{
			rotInt = (byte)(newRot % 8);
		}

		public Rot8 (Rot4 rot)
		{
			rotInt = rot.AsByte;
		}

		public Rot8(Rot4 rot, float angle)
		{
			int rotAsInt = rot.AsInt;
			if (rot.IsHorizontal)
			{
				switch (rotAsInt)
				{
					case 1:
						if (angle == -45)
						{
							rotAsInt = 4;
						}
						else if (angle == 45)
						{
							rotAsInt = 5;
						}
						break;
					case 3:
						if (angle == -45)
						{
							rotAsInt = 6;
						}
						else if (angle == 45)
						{
							rotAsInt = 7;
						}
						break;
				}
			}
			rotInt = (byte)(rotAsInt % 8);
		}

		public bool IsVertical => rotInt == 2 || rotInt == 4;
		public bool IsHorizontal => rotInt == 1 || rotInt == 3;
		public bool IsDiagonal => rotInt == 4 || rotInt == 5 || rotInt == 6 || rotInt == 7;

		public static Rot8 North => new Rot8(0);
		public static Rot8 East => new Rot8(1);
		public static Rot8 South => new Rot8(2);
		public static Rot8 West => new Rot8(3);
		public static Rot8 NorthEast => new Rot8(4);
		public static Rot8 SouthEast => new Rot8(5);
		public static Rot8 SouthWest => new Rot8(6);
		public static Rot8 NorthWest => new Rot8(7);

		public static Rot8 Random
		{
			get
			{
				return new Rot8(Rand.RangeInclusive(0, 7));
			}
		}

		public bool IsValid
		{
			get
			{
				return AsInt >= 0 && AsInt <= 7;
			}
		}

		public static Rot8 Invalid
		{
			get
			{
				return new Rot8
				{
					rotInt = 200
				};
			}
		}

		public IntVec3 FacingCell
		{
			get
			{
				return AsInt switch
				{
					0 => new IntVec3(0, 0, 1),
					1 => new IntVec3(1, 0, 0),
					2 => new IntVec3(0, 0, -1),
					3 => new IntVec3(-1, 0, 0),
					4 => new IntVec3(1, 0, 1),
					5 => new IntVec3(1, 0, -1),
					6 => new IntVec3(-1, 0, -1),
					7 => new IntVec3(-1, 0, 1),
					_ => default
				};
			}
		}

		public Rot8 Opposite
		{
			get
			{
				return AsInt switch
				{
					0 => new Rot8(2),
					1 => new Rot8(3),
					2 => new Rot8(0),
					3 => new Rot8(1),
					4 => new Rot8(6),
					5 => new Rot8(7),
					6 => new Rot8(4),
					7 => new Rot8(5),
					_ => default
				};
			}
		}

		public byte AsByte
		{
			get
			{
				return rotInt;
			}
			set
			{
				rotInt = (byte)(value % 8);
			}
		}

		public int AsInt
		{
			get
			{
				return rotInt;
			}
			set
			{
				if (value < 0)
				{
					value += 4000;
				}
				rotInt = (byte)(value % 8);
			}
		}

		public float AsAngle
		{
			get
			{
				return AsInt switch
				{
					0 => 0,
					1 => 90,
					2 => 180,
					3 => 270,
					4 => 45,
					5 => 135,
					6 => 225,
					7 => 315,
					_ => throw new Exception($"value cannot be > 7 but it is = {rotInt}")
				};
			}
		}

		public float AsRotationAngle
		{
			get
			{
				return AsInt switch
				{
					4 => -45,
					5 => 45,
					6 => -45,
					7 => 45,
					_ => 0
				};
			}
		}

		public int AsIntClockwise
		{
			get
			{
				return AsInt switch
				{
					0 => 0,
					1 => 2,
					2 => 4,
					3 => 6,
					4 => 1,
					5 => 3,
					6 => 5,
					7 => 7,
					_ => throw new Exception($"value cannot be > 7 but it is = {rotInt}")
				};
			}
		}

		public SpectateRectSide AsSpectateSide
		{
			get
			{
				return AsInt switch
				{
					0 => SpectateRectSide.Up,
					1 => SpectateRectSide.Right,
					2 => SpectateRectSide.Down,
					3 => SpectateRectSide.Left,
					_ => SpectateRectSide.None,
				};
			}
		}

		public Quaternion AsQuat
		{
			get
			{
				switch (rotInt)
				{
					case 0:
						return Quaternion.identity;
					case 1:
						return Quaternion.LookRotation(Vector3.right);
					case 2:
						return Quaternion.LookRotation(Vector3.back);
					case 3:
						return Quaternion.LookRotation(Vector3.left);
					default:
						Log.Error("ToQuat with Rot = " + AsInt);
						return Quaternion.identity;
				}
			}
		}

		public Vector2 AsVector2
		{
			get
			{
				return rotInt switch
				{
					0 => Vector2.up,
					1 => Vector2.right,
					2 => Vector2.down,
					3 => Vector2.left,
					4 => new Vector2(1, 1),
					5 => new Vector2(1, -1),
					6 => new Vector2(-1, -1),
					7 => new Vector2(-1, 1),
					_ => throw new Exception("rotInt's value cannot be > 7 but it is:" + rotInt)
				};
			}
		}

		public static Rot8 At(int i)
		{
			return new Rot8(i);
		}

		public static Rot8 FromDirection(IntVec2 dir)
		{
			if (dir == new IntVec2(0, 1))
			{
				return North;
			}
			else if (dir == new IntVec2(1, 1))
			{
				return NorthEast;
			}
			else if (dir == new IntVec2(1, 0))
			{
				return East;
			}
			else if (dir == new IntVec2(1, -1))
			{
				return SouthEast;
			}
			else if (dir == new IntVec2(0, -1))
			{
				return South;
			}
			else if (dir == new IntVec2(-1, -1))
			{
				return SouthWest;
			}
			else if (dir == new IntVec2(-1, 0))
			{
				return West;
			}
			else if (dir == new IntVec2(-1, 1))
			{
				return NorthWest;
			}
			return Invalid;
		}

		public static Rot8 FromAngle(float angle)
		{
			if (angle > 22.5f && angle <= 67.5f)
			{
				return NorthEast;
			}
			else if (angle > 67.5f && angle <= 112.5f)
			{
				return East;
			}
			else if (angle > 112.5f && angle <= 157.5f)
			{
				return SouthEast;
			}
			else if (angle > 157.5f && angle <= 202.5f)
			{
				return South;
			}
			else if (angle > 202.5f && angle <= 247.5f)
			{
				return SouthWest;
			}
			else if (angle > 247.5f && angle <= 292.5f)
			{
				return West;
			}
			else if (angle > 292.5f && angle <= 337.5f)
			{
				return NorthWest;
			}
			return North;
		}

		public int Difference(Rot8 rot)
		{
			if (!rot.IsValid || !IsValid)
			{
				return 0;
			}
			return Mathf.Abs(rot.AsIntClockwise - AsIntClockwise).ClampAndWrap(North.AsIntClockwise, South.AsIntClockwise);
		}

		public void Rotate(RotationDirection rotDir, bool diagonals = true)
		{
			if (rotDir == RotationDirection.Clockwise)
			{
				int asInt = AsInt;
				AsInt = asInt + 1;
			}
			if (rotDir == RotationDirection.Counterclockwise)
			{
				int asInt = AsInt;
				AsInt = asInt - 1;
			}
			if (!diagonals)
			{
				AsInt %= 4;
			}
		}

		public Rot8 Rotated(RotationDirection rotDir, bool diagonals = true)
		{
			Rot8 result = this;
			result.Rotate(rotDir, diagonals);
			return result;
		}

		public Rot8 Rotated(float angle, RotationDirection rotDir)
		{
			if (angle % 45 != 0)
			{
				SmashLog.Error($"Cannot rotate <type>Rot8</type> with angle non-multiple of 45.");
				return this;
			}
			int rotate = Mathf.RoundToInt(angle / 45);
			Rot8 newRot = this;
			for (int i = 0; i < rotate; i++)
			{
				newRot.Rotate(rotDir);
			}
			return newRot;
		}

		public static Rot8 DirectionFromCells(IntVec3 from, IntVec3 to)
		{
			IntVec3 result = to - from;
			return FromDirection(new IntVec2(result.x, result.z));
		}

		public static bool operator ==(Rot8 a, Rot8 b)
		{
			return a.AsInt == b.AsInt;
		}

		public static bool operator !=(Rot8 a, Rot8 b)
		{
			return a.AsInt != b.AsInt;
		}

		public static implicit operator Rot4(Rot8 rot)
		{
			return rot.AsInt switch
			{
				0 => Rot4.North,
				1 => Rot4.East,
				2 => Rot4.South,
				3 => Rot4.West,
				4 => Rot4.East,
				5 => Rot4.East,
				6 => Rot4.West,
				7 => Rot4.West,
				_ => throw new Exception("Rot8 must be between (0, 7) int value")
			};
		}

		public static implicit operator Rot8(Rot4 rot)
		{
			return new Rot8(rot.AsInt);
		}

		public override int GetHashCode()
		{
			return rotInt;
		}

		public override string ToString()
		{
			return rotInt.ToString();
		}

		public static Rot8 FromString(string innerText)
		{
			if (int.TryParse(innerText, out int num))
			{
				return new Rot8(num);
			}
			switch (innerText.ToUpperInvariant())
			{
				case "NORTH":
					return North;
				case "EAST":
					return East;
				case "SOUTH":
					return South;
				case "WEST":
					return West;
				case "NORTHEAST":
					return NorthEast;
				case "SOUTHEAST":
					return SouthEast;
				case "SOUTHWEST":
					return SouthWest;
				case "NORTHWEST":
					return NorthWest;
			}
			Log.Error($"Unable to parse Rot8: {innerText}");
			return Invalid;
		}

		public override bool Equals(object obj)
		{
			return obj is Rot8 rot && Equals(rot);
		}

		public bool Equals(Rot8 other)
		{
			return rotInt == other.rotInt;
		}
	}
}
