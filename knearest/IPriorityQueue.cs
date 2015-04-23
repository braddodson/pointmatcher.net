using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace knearest
{
    interface IPriorityQueue<T>
    {
        T MaxItem { get; }

        float MaxPriority { get; }

        void Enqueue(T node, float priority);

        IEnumerable<T> Items { get; }

        IEnumerable<float> Priorities { get; }
    }

    class ListPriorityQueue<T> : IPriorityQueue<T>
    {
        private struct Entry
        {
            public T data;
            public float priority;
        }

        private Entry[] items;

        public ListPriorityQueue(int maxSize)
        {
            this.items = new Entry[maxSize];
            for (int i = 0; i < this.items.Length; i++)
            {
                this.items[i] = new Entry { priority = float.PositiveInfinity };
            }
        }

        public T MaxItem
        {
            get { return items[items.Length - 1].data; }
        }

        public float MaxPriority
        {
            get { return items[items.Length - 1].priority; }
        }

        public void Enqueue(T node, float priority)
        {
            // don't add if it's larger than the entire list
            if (priority > MaxPriority)
            {
                return;
            }

            // otherwise, shift things up to make a spot for it
            int i;
            for (i = this.items.Length - 1; i > 0; i--)
            {
                if (this.items[i - 1].priority > priority)
                {
                    this.items[i] = this.items[i - 1];
                }
                else
                {
                    break;
                }
            }

            // place it in it's sorted spot
            this.items[i] = new Entry { data = node, priority = priority };
        }

        public IEnumerable<T> Items
        {
            get { return this.items.Select(e => e.data); }
        }

        public IEnumerable<float> Priorities
        {
            get { return this.items.Select(e => e.priority); }
        }
    }
}
