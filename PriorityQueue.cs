using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriorityQueue<T>where T : new()
{
    private List<T>queueImpl;

    public Comparison<T>comparator;

    public PriorityQueue()
    {
        queueImpl = new List<T>();
        // blank initial because 0 index shouldn't be used.
        queueImpl.Add(new T());
    }

    public int Count
    {
        get
        {
            return queueImpl.Count - 1;
        }
    }

    public void Clear()
    {
        queueImpl.Clear();
        // blank initial because 0 index shouldn't be used.
        queueImpl.Add(new T());
    }

    public bool Empty()
    {
        return queueImpl.Count <= 1;
    }

    public T Top
    {
        get
        {
            return queueImpl[1];
        }
    }

    public void Add(T item)
    {
        queueImpl.Add(item);
        BubbleUp(queueImpl.Count - 1);
    }

    public bool Remove(T item)
    {
        int ind = queueImpl.IndexOf(item);
        if (ind > 0) {
            int last = queueImpl.Count - 1;
            queueImpl[ind] = queueImpl[last];
            queueImpl.RemoveAt(last);
            BubbleDown(ind);
            if (queueImpl.Count == 0) { Debug.LogError("Woops, we're down to zero"); }
            return true;
        } else {
            return false;
        }
    }

    public bool Remove(Predicate<T>matchPred)
    {
        T item = queueImpl.Find(matchPred);
        return Remove(item);
    }

    public T Find(Predicate<T>matchPred)
    {
        return queueImpl.Find(matchPred);
    }

    public bool Contains(T item)
    {
        return queueImpl.Contains(item);
    }

    public void Update(T item)
    {
        int ind = queueImpl.IndexOf(item);
        if (ind > 0) {
            // One of these two will be a noop
            BubbleUp(ind);
            BubbleDown(ind);
        }
    }

    private void BubbleUp(int ind)
    {
        while (ind > 1) {
            int parent = ind / 2;
            if (comparator(queueImpl[ind], queueImpl[parent]) < 0) {
                T tmp = queueImpl[parent];
                queueImpl[parent] = queueImpl[ind];
                queueImpl[ind] = tmp;
                ind = parent;
            } else {
                break;
            }
        }
    }

    private void BubbleDown(int ind)
    {
        if (ind == 0) {
            Debug.LogError("Bubbling an invalid index");
            return;
        }
        while (ind < queueImpl.Count) {
            int left = ind * 2;
            int right = ind * 2 + 1;

            if (left >= queueImpl.Count) { break; }

            int minInd;
            if (right >= queueImpl.Count || comparator(queueImpl[left], queueImpl[right]) < 0) {
                minInd = left;
            } else {
                minInd = right;
            }

            if (comparator(queueImpl[minInd], queueImpl[ind]) < 0) {
                T tmp = queueImpl[minInd];
                queueImpl[minInd] = queueImpl[ind];
                queueImpl[ind] = tmp;
                ind = minInd;
            } else {
                break;
            }
        }
    }
}
