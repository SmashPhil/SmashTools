using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using Verse;

namespace SmashTools
{
	public struct SavedField<T1> : IEquatable<SavedField<T1>>, INestedType
	{
		public SavedField(T1 duplicate)
		{
			first = duplicate;
			second = duplicate;
		}

		public SavedField(T1 first, T1 second)
		{
			this.first = first;
			this.second = second;
		}

		public static T1 DefaultValue => (T1)Activator.CreateInstance(typeof(T1));

		public T1 First
		{
			get
			{
				return first;
			}
			set
			{
				if(value is T1)
				{
					first = value;
					return;
				}
				Log.Error("Tried to assign value of different type to SavedField. T1: " + typeof(T1) + " value: " + value.GetType());
			}
		}

		public T1 Second
		{
			get
			{
				return second;
			}
			set
			{
				if (value is T1)
				{
					second = value;
					return;
				}
				Log.Error("Tried to assign value of different type to SavedField. T2: " + typeof(T1) + " value: " + value.GetType());
			}
		}

		public Type InnerType => first.GetType();

		public override string ToString()
		{
			return $"({first},{second})";
		}

		public static object FromTypedString(string entry, Type objType)
		{
			entry = entry.TrimStart(new char[] { '(' }).TrimEnd(new char[] { ')' });
			string[] data = entry.Split(new char[] { ',' });

			try
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object value1 = AccessTools.Method(typeof(ParseHelper), nameof(ParseHelper.FromString), new Type[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { data[0], objType });
				object value2 = AccessTools.Method(typeof(ParseHelper), nameof(ParseHelper.FromString), new Type[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { data[1], objType });
				return new SavedField<object>(value1, value2);
			}
			catch(Exception ex)
			{
				Log.Error($"{entry} is not a valid SavedField format. Exception: {ex}");
				return new SavedField<object>(default);
			}
		}

		public override int GetHashCode()
		{
			int hash = 0;
			if (First is object)
				hash = EqualityComparer<T1>.Default.GetHashCode(First);
			if (Second is object)
				hash = (hash << 3) + hash ^ EqualityComparer<T1>.Default.GetHashCode(Second);
			return hash;
		}

		public override bool Equals(object obj)
		{
			return obj is SavedField<T1> field && Equals(field);
		}

		public bool Equals(SavedField<T1> other)
		{
			return EqualityComparer<T1>.Default.Equals(first, other.first) && EqualityComparer<T1>.Default.Equals(second, other.second);
		}

		public static bool operator ==(SavedField<T1> lhs, SavedField<T1> rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(SavedField<T1> lhs, SavedField<T1> rhs)
		{
			return !(lhs == rhs);
		}

		private T1 first;

		private T1 second;
	}
}
