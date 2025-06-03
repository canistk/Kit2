using System;
using System.Collections;
using System.Collections.Generic;
namespace Kit2
{
    public class CircularBuffer<T> : IQueue<T>, IEnumerable<T>
    {
        private     int head;
        private     int tail;
        private     T[] array;
        public      int Count { get; private set; }

        public readonly bool IsAllowExpandSize  ;
        public bool     AllowsDuplicates        => throw new NotImplementedException();
        public T        this[int index]         => array[index];
        public int      capacity                { get; private set; }

        public CircularBuffer() : this(2) { }
        public CircularBuffer(int capacity, bool allowExpandSize = false)
        {
            if (capacity < 1)
                throw new System.InvalidOperationException($"Invalid Capacity :{capacity}");
            int num;
            for (num = 1; num < capacity; num *= 2) { }
            array = new T[num];
            this.capacity           = capacity;
            this.IsAllowExpandSize  = allowExpandSize;
            UpdateVersion();
        }

        #region Utils
        private void TryExpandSize()
        {
            if (!IsAllowExpandSize)
                throw new System.OutOfMemoryException($"{nameof(IsAllowExpandSize)} = {IsAllowExpandSize}");

            int num = 2 * array.Length;
            T[] destinationArray = new T[num];
            if (head <= tail)
            {
                Array.Copy(array, head, destinationArray, 0, Count);
            }
            else
            {
                int num2 = array.Length - head;
                Array.Copy(array, head, destinationArray, 0, num2);
                Array.Copy(array, 0, destinationArray, num2, Count - num2);
            }

            head = 0;
            tail = Count;
            array = destinationArray;
        }

        private static void PrevPointer(ref int pt, in int len)
        {
            pt = (len + pt - 1) % len;
            // let array length = 4, tail = 0, pop 1
            // tail = (4 + 0 - 1) % 4 = 3
            // tail = (4 + 1 - 1) % 4 = 0
            // tail = (4 + 2 - 1) % 4 = 1
            // tail = (4 + 3 - 1) % 4 = 2
        }

        private static void NextPointer(ref int pt, in int len)
        {
            pt = (pt + 1) % len;
        }
        #endregion Utils

        #region Version
        private long version;
        private void UpdateVersion()            => version = System.DateTime.UtcNow.Ticks;
        private bool IsSameVersion(long tick)   => version == tick;
        #endregion Version

        #region Public API
        public void Clear()
        {
            Count = 0;
            head = tail = 0;
        }
        /// <summary>
        /// Add item from tail
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            if (Count == array.Length)
            {
                if (IsAllowExpandSize)
                {
                    TryExpandSize();
                }
                else
                {
                    --Count;
                    NextPointer(ref head, array.Length);
                }
            }

            array[tail]     = item; // assign
            ++Count;
            NextPointer(ref tail, array.Length);
            UpdateVersion();
        }

        /// <summary>
        /// Take item from head
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public T Dequeue()
        {
            if (Count == 0)
                throw new NullReferenceException("Empty queue");

            T rst           = array[head];  // read
            array[head]     = default(T);   // clean
            --Count;
            NextPointer(ref head, array.Length);
            if (Count == 0)
            {
                head = tail = 0;
            }
            UpdateVersion();
            return rst;
        }

        public T Peek()
        {
            if (Count == 0)
                throw new NullReferenceException("Empty queue");

            return array[head]; // read
        }

        /// <summary>
        /// Add item from head
        /// </summary>
        /// <param name="item"></param>
        public void InvEnqueue(T item)
        {
            if (Count == array.Length)
            {
                if (IsAllowExpandSize)
                {
                    TryExpandSize();
                }
                else
                {
                    --Count;
                    PrevPointer(ref tail, array.Length);
                }
            }

            PrevPointer(ref head, array.Length);
            ++Count;
            array[head]     = item;
            UpdateVersion();
        }

        /// <summary>
        /// Take item from tail
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public T InvDequeue()
        {
            if (Count == 0)
                throw new NullReferenceException("Empty queue");

            PrevPointer(ref tail, array.Length);
            T rst           = array[tail];
            array[tail]     = default(T);
            --Count;
            if (Count == 0)
            {
                head = tail = 0;
            }
            UpdateVersion();
            return rst;
        }

        public T InvPeek()
        {
            if (Count == 0)
                throw new NullReferenceException("Empty queue");

            var idx = tail;
            PrevPointer(ref idx, array.Length);
            return array[idx];
        }

		public T InvPeek(int extra = 0)
		{
			if (Count == 0)
				throw new NullReferenceException("Empty queue");
            if (extra < 0)
                throw new InvalidOperationException("Cannot use negative numbers");
            if (extra >= Count)
                throw new IndexOutOfRangeException($"Current buffer size : {Count}, cannot access any further record.");

			var idx = tail;
            do
            {
                PrevPointer(ref idx, array.Length);
            }
            while (--extra >= 0);
			return array[idx];
		}

		public bool TryPeek(int relative, out T rst)
        {
            rst = default(T);
            if (relative >= 0)
            {
                if (relative >= Count)
                    return false;
                // ASC
                var idx = head;
                while (relative-- > 0)
                    NextPointer(ref idx, array.Length);
                rst = array[idx];
                return true;
            }
            else
            {
                // DSC
                // note: -1 = the first tail item.
                if (Math.Abs(relative) > Count)
                    return false;

                var idx = tail; // Note : tail is pointing to next slot,
                while (relative++ < 0)
                    PrevPointer(ref idx, array.Length);
                rst = array[idx];
                return true;
            }
        }
        #endregion Public API

        #region IEnumerable
        public IEnumerator<T> GetEnumerator()
        {
            var ver = version;
            var h   = head;
            var t   = tail;
            var len = array.Length;
            if (Count > 0 && Count == array.Length)
            {
                yield return array[h];
                NextPointer(ref h, len);
            }
            while (h != t)
            {
                if (!IsSameVersion(ver))
                    throw new System.InvalidOperationException("Collection changed");
                yield return array[h];
                NextPointer(ref h, len);
            }
        }

        public IEnumerator<T> GetInvEnumerator()
        {
            var ver = version;
            var h = head;
            var t = tail;
            var len = array.Length;
            if (Count > 0 && Count == array.Length)
            {
                PrevPointer(ref t, len);
                yield return array[t];
            }
            while (h != t)
            {
                if (!IsSameVersion(ver))
                    throw new System.InvalidOperationException("Collection changed");
                PrevPointer(ref t, len);
                yield return array[t];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion IEnumerable

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[");
            for (int i = 0; i < array.Length; ++i)
            {
                if (array[i] != null)
                    sb.Append(array[i].ToString());
                else
                    sb.Append("Null");

                if (i < array.Length - 1)
                    sb.Append(", ");
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}