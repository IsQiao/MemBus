﻿using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using JetBrains.Annotations;
using MemBus.Configurators;
using MemBus.Subscribing;
using MemBus.Tests.Help;
using Xunit;

namespace MemBus.Tests.Rx
{
    public class ObservableAsTimer
    {
        readonly IBus _bus = BusSetup
            .StartWith<Conservative>()
            .Apply<FlexibleSubscribeAdapter>(cfg => cfg.RegisterMethods(info => true))
            .Construct();

        [Fact]
        public void Timer_from_observable_functionality()
        {
            var cd = new CountdownEvent(5);
            int messages = 0;
            _bus.Subscribe(new Timers());
            
            using (_bus.Subscribe((MessageA _) => {
                cd.Signal();
                Interlocked.Increment(ref messages);
            }))
            {
                var result = cd.Wait(TimeSpan.FromMilliseconds(1700));
                if (!result)
                    throw new ArgumentException("Timer did not complete");
                Assert.Equal(5, messages);
            }
        }

        private class Timers
        {
            [UsedImplicitly]
            public IObservable<MessageA> ASignal()
            {
                return Observable
                .Timer(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(100), Scheduler.Default)
                .Take(5)
                .Select(_ => new MessageA());
            } 
        }
    }
}

