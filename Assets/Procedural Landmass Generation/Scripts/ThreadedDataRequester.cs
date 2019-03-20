using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour
{
    static ThreadedDataRequester instance;
    private Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

    #region THREADING IMPLEMENTATION
    private void Awake()
    {
        instance = FindObjectOfType<ThreadedDataRequester>();
    }

    private void Update()
    {
        if (dataQueue.Count > 0)
        {
            for (int i = 0; i < dataQueue.Count; i++)
            {
                ThreadInfo threadInfo = dataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        ThreadStart threadStart = delegate {
            instance.DataThread(generateData, callback);
        };

        new Thread(threadStart).Start();
    }

    private void DataThread(Func<object> generateData, Action<object> callback)
    {
        object data = generateData();
        lock (dataQueue)
        {
            dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }

    struct ThreadInfo
    {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> _callback, object _parameter)
        {
            this.callback = _callback;
            this.parameter = _parameter;
        }
    }
    #endregion
}
