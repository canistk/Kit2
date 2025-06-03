using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2
{
    public interface IQueue<T>
    {
        bool AllowsDuplicates { get; }

        T this[int index] { get; }

        void Enqueue(T item);

        T Dequeue();
    }
}