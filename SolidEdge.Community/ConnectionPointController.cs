﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace SolidEdgeCommunity;

/// <summary>
/// Default controller that handles connecting\disconnecting to COM events via IConnectionPointContainer and IConnectionPoint interfaces.
/// </summary>
public class ConnectionPointController
{
    private readonly object _sink;
    private readonly Dictionary<IConnectionPoint, int> _connectionPointDictionary = [];

    public ConnectionPointController(object sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        _sink = sink;
    }

    /// <summary>
    /// Establishes a connection between a connection point object and the client's sink.
    /// </summary>
    /// <typeparam name="TInterface">Interface type of the outgoing interface whose connection point object is being requested.</typeparam>
    /// <param name="container">An object that implements the IConnectionPointContainer inferface.</param>
    public void AdviseSink<TInterface>(object container) where TInterface : class
    {
        bool lockTaken = false;

        try
        {
            Monitor.Enter(this, ref lockTaken);

            // Prevent multiple event Advise() calls on same sink.
            if (IsSinkAdvised<TInterface>(container))
            {
                return;
            }

            IConnectionPointContainer cpc = null;
            int cookie = 0;

            cpc = (IConnectionPointContainer)container;
            var item = typeof(TInterface).GUID;
            cpc.FindConnectionPoint(ref item, out IConnectionPoint cp);

            if (cp != null)
            {
                cp.Advise(_sink, out cookie);
                _connectionPointDictionary.Add(cp, cookie);
            }
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(this);
            }
        }
    }

    /// <summary>
    /// Determines if a connection between a connection point object and the client's sink is established.
    /// </summary>
    /// <param name="container">An object that implements the IConnectionPointContainer inferface.</param>
    public bool IsSinkAdvised<TInterface>(object container) where TInterface : class
    {
        bool lockTaken = false;

        try
        {
            Monitor.Enter(this, ref lockTaken);

            IConnectionPointContainer cpc = null;
            int cookie = 0;

            cpc = (IConnectionPointContainer)container;
            var item = typeof(TInterface).GUID;
            cpc.FindConnectionPoint(ref item, out IConnectionPoint cp);

            cookie = _connectionPointDictionary[cp];
            return true;
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(this);
            }
        }
    }

    /// <summary>
    /// Terminates an advisory connection previously established between a connection point object and a client's sink.
    /// </summary>
    /// <typeparam name="TInterface">Interface type of the interface whose connection point object is being requested to be removed.</typeparam>
    /// <param name="container">An object that implements the IConnectionPointContainer inferface.</param>
    public void UnadviseSink<TInterface>(object container) where TInterface : class
    {
        bool lockTaken = false;

        try
        {
            Monitor.Enter(this, ref lockTaken);

            IConnectionPointContainer cpc = null;
            int cookie = 0;

            cpc = (IConnectionPointContainer)container;
            var item = typeof(TInterface).GUID;
            cpc.FindConnectionPoint(ref item, out IConnectionPoint cp);

            cookie = _connectionPointDictionary[cp];
            cp.Unadvise(cookie);
            _connectionPointDictionary.Remove(cp);
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(this);
            }
        }
    }

    /// <summary>
    /// Terminates all advisory connections previously established.
    /// </summary>
    public void UnadviseAllSinks()
    {
        bool lockTaken = false;

        try
        {
            Monitor.Enter(this, ref lockTaken);
            Dictionary<IConnectionPoint, int>.Enumerator enumerator = _connectionPointDictionary.GetEnumerator();
            while (enumerator.MoveNext())
            {
                enumerator.Current.Key.Unadvise(enumerator.Current.Value);
            }
        }
        finally
        {
            _connectionPointDictionary.Clear();

            if (lockTaken)
            {
                Monitor.Exit(this);
            }
        }
    }
}