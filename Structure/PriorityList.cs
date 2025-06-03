using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Kit2
{

	public class PriorityList<TPriority, TObject>
		where TPriority : System.IComparable<TPriority>
		where TObject : System.IEquatable<TObject>
	{
		private List<(TPriority Priority, TObject Item)> lists;
		private bool descending;

		public PriorityList(bool descending = false)
		{
			this.lists = new List<(TPriority, TObject)>();
			this.descending = descending;
		}

		public void Add(TPriority priority, TObject item)
		{
			lists.Add((priority, item));
			SortItems();
		}

		public int Remove(TObject item)
		{
			//return items.RemoveAll(i => EqualityComparer<TObject>.Default.Equals(i.Item, item));
			return lists.RemoveAll(o => o.Equals(item));
		}


		public TObject Peek()
		{
			if (!TryPeek(out var item))
			{
				throw new System.InvalidOperationException("PriorityList is empty.");
			}
			return item;
		}

		public bool TryPeek(out TObject item)
		{
			item = default;
			if (lists.Count == 0)
				return false;
			if (lists.Count == 1)
			{
				item = lists[0].Item;
				return true;
			}
			item = lists.First().Item;
			return item != null;
		}

		public int Count => lists.Count;
		public TObject this[int idx]
		{
			get
			{
				if (idx < 0 || idx > lists.Count)
					throw new System.IndexOutOfRangeException();
				return lists[idx].Item;
			}
		}
		public void Clear()
		{
			lists.Clear();
		}

		public int IndexOf(TObject item)
		{
			for (int i = 0; i < lists.Count; ++i)
			{
				if (!lists[i].Item.Equals(item))
					continue;
				return i;
			}
			return -1;
		}

		private void SortItems()
		{
			if (descending)
			{
				lists = lists.OrderByDescending(i => i.Priority).ToList();
			}
			else
			{
				lists = lists.OrderBy(i => i.Priority).ToList();
			}
		}
	}

	public interface IPriorityObj : IComparable<IPriorityObj>, IEquatable<IPriorityObj>
	{
		public float Priority { get; }
		public object Value { get; }
	}
	public class PriorityList<PriorityObject>
		where PriorityObject : System.IComparable<PriorityObject>, System.IEquatable<PriorityObject>
	{
		private List<PriorityObject> lists;
		private bool descending;

		public PriorityList(bool descending = false)
		{
			this.lists = new List<PriorityObject>();
			this.descending = descending;
		}

		public void Add(PriorityObject item)
		{
			lists.Add(item);
			SortItems();
		}

		public int Remove(PriorityObject item)
		{
			return lists.RemoveAll(o => o.Equals(item));
		}


		public PriorityObject Peek()
		{
			if (!TryPeek(out var item))
			{
				throw new System.InvalidOperationException("PriorityList is empty.");
			}
			return item;
		}

		public bool TryPeek(out PriorityObject item)
		{
			item = default;
			if (lists.Count == 0)
				return false;
			if (lists.Count == 1)
			{
				item = lists[0];
				return true;
			}
			item = lists.First();
			return item != null;
		}

		public int Count => lists.Count;
		public PriorityObject this[int idx]
		{
			get
			{
				if (idx < 0 || idx > lists.Count)
					throw new System.IndexOutOfRangeException();
				return lists[idx];
			}
		}
		public void Clear()
		{
			lists.Clear();
		}

		public int IndexOf(PriorityObject item)
		{
			for (int i = 0; i < lists.Count; ++i)
			{
				if (!lists[i].Equals(item))
					continue;
				return i;
			}
			return -1;
		}

		public override string ToString()
		{
			return base.ToString();                   
		}

		private void SortItems()
		{
			if (descending)
			{
				lists = lists.OrderByDescending(o => o).ToList();
			}
			else
			{
				lists = lists.OrderBy(o => o).ToList();
			}
		}
	}

}
