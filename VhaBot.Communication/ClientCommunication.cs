using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Lifetime;
using VhaBot.Configuration;

namespace VhaBot.Communication
{
    public delegate MessageResult SendMessageDelegate(ClientCommunication sender, MessageBase message);
    public delegate ConfigurationBot GetConfigurationBotDelegate();
    public delegate ConfigurationCore GetConfigurationCoreDelegate();

    public class ClientCommunication : MarshalByRefObject
    {
        private bool _disposed = false;
        private string _id;
        private Queue<MessageBase> _queue;
        private DateTime _lastSeen = DateTime.Now;
        private SendMessageDelegate _onSendMessage;
        private GetConfigurationBotDelegate _onGetConfigurationBot;
        private GetConfigurationCoreDelegate _onGetConfigurationCore;

        public bool Disposed { get { return this._disposed; } }
        public string ID { get { return this._id; } }
        public int QueueSize
        {
            get
            {
                if (this._queue == null)
                    return -1;
                lock (this._queue)
                    return this._queue.Count;
            }
        }
        public DateTime LastSeen { get { return this._lastSeen; } }
        public TimeSpan IdleTime { get { return DateTime.Now - this._lastSeen; } }
        public int CoreID { get { return System.Diagnostics.Process.GetCurrentProcess().Id; } }

        public ClientCommunication(
            string id,
            Queue<MessageBase> queue,
            SendMessageDelegate sendMessage,
            GetConfigurationBotDelegate getConfigurationBot,
            GetConfigurationCoreDelegate getConfigurationCore)
        {
            this._id = id;
            this._queue = queue;
            this._onSendMessage = sendMessage;
            this._onGetConfigurationBot = getConfigurationBot;
            this._onGetConfigurationCore = getConfigurationCore;
        }

        public void Dispose()
        {
            this._disposed = true;
            this._onSendMessage = null;
            this._queue = null;
        }

        public MessageResult SendMessage(MessageBase message)
        {
            if (this._onSendMessage == null)
                return MessageResult.NotConnected;
            try { return this._onSendMessage(this, message); }
            catch { return MessageResult.Error; }
        }

        public ConfigurationBot GetConfigurationBot()
        {
            if (this._onGetConfigurationBot == null)
                return null;
            return this._onGetConfigurationBot();
        }

        public ConfigurationCore GetConfigurationCore()
        {
            if (this._onGetConfigurationCore == null)
                return null;
            return this._onGetConfigurationCore();
        }

        public MessageBase Dequeue()
        {
            if (this._queue == null)
                return null;
            lock (this._queue)
                if (this._queue.Count > 0)
                    return this._queue.Dequeue();
            return null;
        }

        public void Ping()
        {
            this._lastSeen = DateTime.Now;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
