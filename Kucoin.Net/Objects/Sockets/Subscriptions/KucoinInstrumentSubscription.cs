﻿using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using CryptoExchange.Net.Sockets.MessageParsing;
using CryptoExchange.Net.Sockets.MessageParsing.Interfaces;
using Kucoin.Net.Objects.Models.Futures.Socket;
using Kucoin.Net.Objects.Sockets.Queries;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kucoin.Net.Objects.Sockets.Subscriptions
{
    internal class KucoinInstrumentSubscription : Subscription<KucoinSocketResponse, KucoinSocketResponse>
    {
        private string _topic;
        private Action<DataEvent<KucoinStreamFuturesMarkIndexPrice>> _markIndexPriceHandler;
        private Action<DataEvent<KucoinStreamFuturesFundingRate>> _fundingRateHandler;
        private readonly MessagePath _subjectPath = MessagePath.Get().Property("subject");

        public override HashSet<string> ListenerIdentifiers { get; set;  }

        public KucoinInstrumentSubscription(ILogger logger,List<string>? symbols, Action<DataEvent<KucoinStreamFuturesMarkIndexPrice>> markIndexPriceHandler, Action<DataEvent<KucoinStreamFuturesFundingRate>> fundingRateHandler) : base(logger, false)
        {
            var topic = "/contract/instrument";
            _topic = symbols?.Any() == true ? topic + ":" + string.Join(",", symbols) : topic;
            _markIndexPriceHandler = markIndexPriceHandler;
            _fundingRateHandler = fundingRateHandler;

            ListenerIdentifiers = symbols?.Any() == true ? new HashSet<string>(symbols.Select(s => topic + ":" + s)) : new HashSet<string> { topic };
        }

        public override Query? GetSubQuery(SocketConnection connection)
        {
            return new KucoinQuery("subscribe", _topic, Authenticated);
        }

        public override Query? GetUnsubQuery()
        {
            return new KucoinQuery("unsubscribe", _topic, Authenticated);
        }

        public override Task<CallResult> DoHandleMessageAsync(SocketConnection connection, DataEvent<object> message)
        {
            if (message.Data is KucoinSocketUpdate<KucoinStreamFuturesMarkIndexPrice> markUpdate)
                _markIndexPriceHandler?.Invoke(message.As(markUpdate.Data, markUpdate.Topic, SocketUpdateType.Update));

            if (message.Data is KucoinSocketUpdate<KucoinStreamFuturesFundingRate> fundingUpdate)
                _fundingRateHandler?.Invoke(message.As(fundingUpdate.Data, fundingUpdate.Topic, SocketUpdateType.Update));
            return Task.FromResult(new CallResult(null));
        }

        public override Type? GetMessageType(IMessageAccessor message)
        {
            var subject = message.GetValue<string>(_subjectPath);
            if (subject == "mark.index.price")
                return typeof(KucoinSocketUpdate<KucoinStreamFuturesMarkIndexPrice>);
            return typeof(KucoinSocketUpdate<KucoinStreamFuturesFundingRate>);
        }
    }
}
