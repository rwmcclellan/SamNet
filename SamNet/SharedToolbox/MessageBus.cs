using SharedToolbox;
using System;

//
//  Thanks to Brent Edwards for the Message Bus concept below - 4/13/2010
//  http://brentedwards.net/2010/04/13/roll-your-own-simple-message-bus-event-aggregator/
//

namespace SharedToolbox
{
    public sealed class EdwardsMessageBus : IMessageBus
    {
        private Dictionary<Type, List<ActionReference>> _subscribers =
            new Dictionary<Type, List<ActionReference>>();

        private object _lock = new object();

        public void Subscribe<TMessage>(Action<TMessage> handler)
        {
            lock (_lock)
            {
                if (_subscribers.ContainsKey(typeof(TMessage)))
                {
                    var handlers = _subscribers[typeof(TMessage)];
                    handlers.Add(new ActionReference(handler));
                }
                else
                {
                    var handlers = new List<ActionReference>();
                    handlers.Add(new ActionReference(handler));
                    _subscribers[typeof(TMessage)] = handlers;
                }
            }
        }

        public void Unsubscribe<TMessage>(Action<TMessage> handler)
        {
            lock (_lock)
            {
                if (_subscribers.ContainsKey(typeof(TMessage)))
                {
                    var handlers = _subscribers[typeof(TMessage)];

                    ActionReference targetReference = null!;
                    foreach (var reference in handlers)
                    {
                        var action = (Action<TMessage>)reference.Target;
                        if ((action.Target == handler.Target) && action.Method.Equals(handler.Method))
                        {
                            targetReference = reference;
                            break;
                        }
                    }
                    handlers.Remove(targetReference);

                    if (handlers.Count == 0)
                    {
                        _subscribers.Remove(typeof(TMessage));
                    }
                }
            }
        }

        public void Publish<TMessage>(TMessage message)
        {
            var subscribers = GetSubscribers<TMessage>();
            foreach (var subscriber in subscribers)
            {
                subscriber.Invoke(message);
            }
        }

        private List<Action<TMessage>> GetSubscribers<TMessage>()
        {
            var toCall = new List<Action<TMessage>>();
            var toRemove = new List<ActionReference>();

            lock (_lock)
            {
                if (_subscribers.ContainsKey(typeof(TMessage)))
                {
                    var handlers = _subscribers[typeof(TMessage)];
                    foreach (var handler in handlers)
                    {
                        if (handler.IsAlive)
                        {
                            toCall.Add((Action<TMessage>)handler.Target);
                        }
                        else
                        {
                            toRemove.Add(handler);
                        }
                    }

                    foreach (var remove in toRemove)
                    {
                        handlers.Remove(remove);
                    }

                    if (handlers.Count == 0)
                    {
                        _subscribers.Remove(typeof(TMessage));
                    }
                }
            }

            return toCall;
        }
    }

    public sealed class ActionReference
    {
        private WeakReference WeakReference { get; set; }

        public Delegate Target { get; private set; }

        public bool IsAlive
        {
            get { return WeakReference.IsAlive; }
        }

        public ActionReference(Delegate action)
        {
            Target = action;
            WeakReference = new WeakReference(action.Target);
        }
    }

    public sealed class LoggerMessage
    {
        public string message = "";
    }

    public sealed class NotifyFocus
    {
        public bool state = true;
        public int Sender;
    }

    public sealed class NotifyField
    {
        public bool Moving = false;
        public bool SelectRaft = false;
        public int RaftId;
    }

    public sealed class ReInitPage
    {
        public bool state = true;
    }

    public sealed class EventsBusCalibrateDone
    {
        public bool success = false;
        public bool isNeedleReplacement = false;
    }
}