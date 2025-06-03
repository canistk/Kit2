using System.Collections.Generic;

namespace Kit2
{
	/// <summary>A priority queue is a data structure that holds information that has some sort of priority value.
	/// When an item is removed from a priority queue, it's always the item with the highest priority.
	/// Priority queues are used in many important computer algorithms,
	/// in particular graph-based shortest-path algorithms.</summary>
	/// <typeparam name="ITEM"></typeparam>
	/// <see cref="https://visualstudiomagazine.com/articles/2012/11/01/priority-queues-with-c.aspx"/>
	/// <remarks>alway dequeue min value.</remarks>
	public class PriorityQueue<ITEM> where ITEM : System.IComparable<ITEM>
	{
		private List<ITEM> data;

		public PriorityQueue()
		{
			this.data = new List<ITEM>();
		}

		public void Enqueue(ITEM item)
		{
			data.Add(item);
			int ci = data.Count - 1; // child index; start at end
			while (ci > 0)
			{
				int pi = (ci - 1) / 2; // parent index
				if (data[ci].CompareTo(data[pi]) >= 0)
					break; // child item is larger than (or equal) parent so we're done
				ITEM tmp = data[ci];
				data[ci] = data[pi];
				data[pi] = tmp;
				ci = pi;
			}
		}

		public ITEM Dequeue()
		{
			// assumes pq is not empty; up to calling code
			int li = data.Count - 1; // last index (before removal)
			ITEM frontItem = data[0];   // fetch the front
			data[0] = data[li];
			data.RemoveAt(li);

			--li; // last index (after removal)
			int pi = 0; // parent index. start at front of pq
			while (true)
			{
				int ci = pi * 2 + 1; // left child index of parent
				if (ci > li)
					break;  // no children so done
				int rc = ci + 1;     // right child
				if (rc <= li && data[rc].CompareTo(data[ci]) < 0) // if there is a rc (ci + 1), and it is smaller than left child, use the rc instead
					ci = rc;
				if (data[pi].CompareTo(data[ci]) <= 0)
					break; // parent is smaller than (or equal to) smallest child so done
				ITEM tmp = data[pi];
				data[pi] = data[ci];
				data[ci] = tmp; // swap parent and child
				pi = ci;
			}
			return frontItem;
		}

		public ITEM Peek()
		{
			ITEM frontItem = data[0];
			return frontItem;
		}

		public int Count => data.Count;

		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.AppendFormat("{0}<{1}> count = {2}", GetType().Name, typeof(ITEM).Name, data.Count);
			for (int i = 0; i < data.Count; ++i)
			{
				sb.AppendFormat("\n\r{0}", data[i].ToString());
			}
			return sb.ToString();
		}

		public bool IsConsistent()
		{
			// is the heap property true for all data?
			if (data.Count == 0)
				return true;
			int li = data.Count - 1; // last index
			for (int pi = 0; pi < data.Count; ++pi) // each parent index
			{
				int lci = 2 * pi + 1; // left child index
				int rci = 2 * pi + 2; // right child index

				if (lci <= li && data[pi].CompareTo(data[lci]) > 0)
					return false; // if lc exists and it's greater than parent then bad.
				if (rci <= li && data[pi].CompareTo(data[rci]) > 0)
					return false; // check the right child too.
			}
			return true; // passed all checks
		} // IsConsistent
	} // PriorityQueue

	/// <summary>
	/// Based on http://blogs.msdn.com/b/ericlippert/archive/2007/10/08/path-finding-using-a-in-c-3-0-part-three.aspx
	/// Backported to C# 2.0
	/// </summary>
	public class PriorityQueue<PRIORITY, VALUE>
	{
		private SortedDictionary<PRIORITY, LinkedList<VALUE>> list = new SortedDictionary<PRIORITY, LinkedList<VALUE>>();

		public void Enqueue(VALUE value, PRIORITY priority)
		{
			if (!list.TryGetValue(priority, out LinkedList<VALUE> q))
			{
				q = new LinkedList<VALUE>();
				list.Add(priority, q);
			}
			q.AddLast(value);
		}

		public VALUE Dequeue()
		{
			// will throw exception if there isn¡¦t any first element!
			SortedDictionary<PRIORITY, LinkedList<VALUE>>.KeyCollection.Enumerator enume = list.Keys.GetEnumerator();
			enume.MoveNext();
			PRIORITY key = enume.Current;
			LinkedList<VALUE> v = list[key];
			VALUE res = v.First.Value;
			v.RemoveFirst();
			if (v.Count == 0)
			{ // nothing left of the top priority.
				list.Remove(key);
			}
			return res;
		}

		public VALUE Peek()
		{
			if (list.Count == 0)
				return default(VALUE);
			var enume = list.Keys.GetEnumerator();
			enume.MoveNext();
			return list[enume.Current].First.Value;
		}

		public void Replace(VALUE value, PRIORITY oldPriority, PRIORITY newPriority)
		{
			LinkedList<VALUE> v = list[oldPriority];
			v.Remove(value);

			if (v.Count == 0)
			{ // nothing left of the top priority.
				list.Remove(oldPriority);
			}

			Enqueue(value, newPriority);
		}

		public bool IsEmpty => list.Count == 0;

		public int Count => list.Count;

		public void Clear()
		{
			list.Clear();
		}

		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach (PRIORITY key in list.Keys)
			{
				foreach (VALUE val in list[key])
				{
					sb.AppendFormat("{0}, ", val);
				}
			}
			sb.Insert(0, $"{GetType().Name}<{typeof(PRIORITY).Name}> count = {sb.Length}");
			return sb.ToString();
		}
	}
}