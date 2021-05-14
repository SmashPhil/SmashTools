using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Collections.ObjectModel;
using System.Collections;

namespace SmashTools
{
	/// <summary>
	/// Self ordering list that pushes most frequently accessed items to the top of the list. 
	/// Increases performance by decreasing full list searches
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[DebuggerDisplay("Count = {Count}")]
	[Serializable]
	public class SelfOrderingList<T> : IList<T>, IReadOnlyList<T>
	{
		private const int DefaultSize = 3;
			 
		private T[] contents;
		private uint[] counters;

		[ContractPublicPropertyName("Count")]
		private int size;

		private int version;

		private static readonly T[] emptyContents = new T[0];
		private static readonly uint[] emptyCounters = new uint[0];

		public SelfOrderingList()
		{
			contents = emptyContents;
			counters = emptyCounters;
			size = 0;
		}

		public SelfOrderingList(int capacity)
		{
			if (capacity < 0) throw new ArgumentOutOfRangeException("capacity", $"{capacity} is not within bounds of list. Capacity of list must be >= 0");
			Contract.EndContractBlock();

			if (capacity == 0)
			{
				contents = emptyContents;
				counters = emptyCounters;
				size = 0;
			}
			else
			{
				contents = new T[capacity];
				counters = new uint[capacity];
				size = capacity;
			}
		}

		public SelfOrderingList(IEnumerable<T> enumerable)
		{
			if (enumerable is null) throw new NullReferenceException("collection cannot be null when initializing a new SelfOrderingList");
			Contract.EndContractBlock();

			if (enumerable is ICollection<T> collection)
			{
				int count = collection.Count;
				if (count == 0)
				{
					contents = emptyContents;
					counters = emptyCounters;
				}
				else
				{
					contents = new T[count];
					counters = new uint[count];
					collection.CopyTo(contents, 0);
					size = count;
				}
			}
			else
			{
				size = 0;
				contents = emptyContents;
				counters = emptyCounters;
				foreach (T item in enumerable)
				{
					Add(item);
				}
			}
		}

		//Capacity

		bool ICollection<T>.IsReadOnly => false;

		public int Count
		{
			get
			{
				Contract.Ensures(Contract.Result<int>() >= 0);
				return size;
			}
		}

		public int Capacity 
		{
			get 
			{
				Contract.Ensures(Contract.Result<int>() >= 0);
				return contents.Length;
			}
			set 
			{
				if (value < size) 
				{
					throw new ArgumentOutOfRangeException("value is out of range of array size");
				}
				Contract.EndContractBlock();
				if (value != contents.Length) 
				{
					if (value > 0) 
					{
						T[] newItems = new T[value];
						uint[] newCounters = new uint[value];
						if (size > 0) 
						{
							Array.Copy(contents, 0, newItems, 0, size);
							Array.Copy(counters, 0, newCounters, 0, size);
						}
						contents = newItems;
						counters = newCounters;
					}
					else 
					{
						contents = emptyContents;
						counters = emptyCounters;
					}
				}
			}
		}

		public T this[int index]
		{
			get
			{
				if ((uint)index >= (uint)size)
				{
					throw new ArgumentOutOfRangeException();
				}
				Contract.EndContractBlock();
				return contents[index];
			}
			set
			{
				if ((uint)index >= (uint)size)
				{
					throw new ArgumentOutOfRangeException();
				}
				Contract.EndContractBlock();
				contents[index] = value;
				version++;
			}
		}

		private static bool IsCompatibleObject(object value) => value is T || (value is null && default(T) is null);

		public void Add(T item)
		{
			if (size == contents.Length)
			{
				EnsureCapacity(size + 1);
			}
			contents[size] = item;
			counters[size] = 0;
			size++;
			version++;
		}

		public void AddRange(IEnumerable<T> enumerable)
		{
			Contract.Ensures(Count >= Contract.OldValue(Count));

			InsertRange(size, enumerable);
		}

		public void InsertRange(int index, IEnumerable<T> enumerable) 
		{
			if (enumerable is null) 
			{
				throw new ArgumentNullException();
			}
			
			if ((uint)index > (uint)size) 
			{
				throw new ArgumentOutOfRangeException();
			}
			Contract.EndContractBlock();
			if (enumerable is ICollection<T> collection) 
			{
				int count = collection.Count;
				if (count > 0) 
				{
					EnsureCapacity(size + count);
					if (index < size) 
					{
						Array.Copy(contents, index, contents, index + count, size - index);
					}
					if (this == collection) 
					{
						Array.Copy(contents, 0, contents, index, index);
						Array.Copy(contents, index + count, contents, index * 2, size - index);
					}
					else 
					{
						T[] itemsToInsert = new T[count];
						collection.CopyTo(itemsToInsert, 0);
						itemsToInsert.CopyTo(contents, index);
						counters = new uint[count];
					}
					size += count;
				}				
			}
			else 
			{
				foreach (T item in enumerable)
				{
					Insert(index++, item);
				}
			}
			version++;			
		}

		public IEnumerator GetEnumerator() 
		{
			return new Enumerator(this);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator() 
		{
			return new Enumerator(this);
		}

		public ReadOnlyCollection<T> AsReadOnly() 
		{
			Contract.Ensures(Contract.Result<ReadOnlyCollection<T>>() != null);
			return new ReadOnlyCollection<T>(this);
		}

		public void Clear()
		{
			if (size > 0)
			{
				Array.Clear(contents, 0, size);
				Array.Clear(counters, 0, size);
			}
			version++;
		}

		public bool Contains(T item)
		{
			if (item is null)
			{
				for (int i = 0; i < size; i++)
				{
					if (contents[i] is null)
					{
						return true;
					}
				}
			}
			else
			{
				EqualityComparer<T> comparer = EqualityComparer<T>.Default;
				for (int i = 0; i < size; i++)
				{
					if (comparer.Equals(contents[i], item))
					{
						return true;
					}
				}
			}
			return false;
		}

		private void EnsureCapacity(int min)
		{
			if (contents.Length < min)
			{
				int newMin = contents.Length == 0 ? DefaultSize : contents.Length * 2;

				if (newMin < min)
				{
					newMin = min;
				}
				Capacity = newMin;
			}
		}

		public T Retrieve(T item)
		{
			for (int i = 0; i < size; i++)
			{
				if (EqualityComparer<T>.Default.Equals(contents[i], item))
				{
					counters[i]++;

					if (i > 0 && counters[i] > counters[i - 1])
					{
						Bump(i);
					}
					if (counters[i] >= uint.MaxValue)
					{
						ResetCounters();
					}
					return contents[i];
				}
			}
			return default;
		}

		public void CountIndex(int index)
		{
			if ((uint)index >= (uint)size)
			{
				throw new ArgumentOutOfRangeException();
			}
			Contract.EndContractBlock();

			counters[index]++;
			if (index > 0 && counters[index] > counters[index - 1])
			{
				Bump(index);
			}
			if (counters[index] >= uint.MaxValue)
			{
				ResetCounters();
			}
		}

		public void ResetCounters()
		{
			for (int i = 0; i < size; i++)
			{
				counters[i] = 0;
			}
		}

		public int IndexOf(T item) 
		{
			Contract.Ensures(Contract.Result<int>() >= -1);
			Contract.Ensures(Contract.Result<int>() < Count);
			return Array.IndexOf(contents, item, 0, size);
		}

		public void Bump(int index)
		{
			if ((uint)index <= 0 || (uint)index > (uint)size) 
			{
				throw new ArgumentOutOfRangeException("index");
			}
			Contract.EndContractBlock();

			T item = contents[index];
			contents[index] = contents[index - 1];
			contents[index - 1] = item;
			version++;
		}

		public void Insert(int index, T item) 
		{
			if ((uint) index > (uint)size) 
			{
				throw new ArgumentOutOfRangeException("index");
			}
			Contract.EndContractBlock();
			if (size == contents.Length)
			{
				EnsureCapacity(size + 1);
			}
			if (index < size) 
			{
				Array.Copy(contents, index, contents, index + 1, size - index);
			}
			contents[index] = item;
			size++;			
			version++;
		}

		public bool Remove(T item) 
		{
			int index = IndexOf(item);
			if (index >= 0) 
			{
				RemoveAt(index);
				return true;
			}
			return false;
		}

		public void RemoveAt(int index) 
		{
			if ((uint)index >= (uint)size) 
			{
				throw new ArgumentOutOfRangeException();
			}
			Contract.EndContractBlock();
			size--;
			if (index < size) 
			{
				Array.Copy(contents, index + 1, contents, index, size - index);
			}
			contents[size] = default;
			version++;
		}

		public void CopyTo(T[] array) 
		{
			CopyTo(array, 0);
		}

		public void CopyTo(T[] array, int arrayIndex) 
		{
			Array.Copy(contents, 0, array, arrayIndex, size);
		}

		[Serializable]
		public struct Enumerator : IEnumerator<T>, IEnumerator
		{
			private readonly SelfOrderingList<T> list;
			private int index;
			private readonly int version;
			private T current;
 
			internal Enumerator(SelfOrderingList<T> list) 
			{
				this.list = list;
				index = 0;
				version = list.version;
				current = default;
			}
 
			public void Dispose() 
			{
			}
 
			public bool MoveNext() {
 
				SelfOrderingList<T> localList = list;
 
				if (version == localList.version && ((uint)index < (uint)localList.size)) 
				{													 
					current = localList.contents[index];					
					index++;
					return true;
				}
				return MoveNextRare();
			}
 
			private bool MoveNextRare()
			{				
				if (version != list.version) 
				{
					throw new InvalidOperationException();
				}
 
				index = list.size + 1;
				current = default;
				return false;				
			}
 
			public T Current 
			{
				get 
				{
					return current;
				}
			}
 
			object IEnumerator.Current 
			{
				get 
				{
					if( index == 0 || index == list.size + 1) 
					{
						throw new InvalidOperationException();
					}
					return Current;
				}
			}
	
			void IEnumerator.Reset() 
			{
				if (version != list.version) 
				{
					throw new InvalidOperationException();
				}
				
				index = 0;
				current = default;
			}
 
		}
	}
}
