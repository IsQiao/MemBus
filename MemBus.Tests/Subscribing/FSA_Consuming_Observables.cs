﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using JetBrains.Annotations;
using MemBus.Subscribing;
using MemBus.Tests.Help;
using Xunit;

namespace MemBus.Tests.Subscribing
{
    public class FsaConsumingObservables : FlexibleSubscribingIntegrationContext
    {
        private readonly Tester _tester = new Tester();

        private class Tester : MessageReceiver
        {
            [UsedImplicitly]
            public void EOne(IObservable<string> messages)
            {
                messages.Subscribe(Add);
            }
        }

        protected override IEnumerable<object> GetEndpoints()
        {
            yield return _tester;
        }

        protected override void ConfigureAdapter(FlexibleSubscribeAdapter adp)
        {
            adp.RegisterMethods(mi => mi.Name.StartsWith("E"));
        }

        [Fact]
        public void string_msg_received()
        {
            Bus.Publish("Hello");

            _tester.AssertContainsMessageOfType<string>();
        }
    }

    
    public class FsaProducingObservables : FlexibleSubscribingIntegrationContext
    {
        private readonly Tester _tester = new Tester();

        private class Tester
        {
            [UsedImplicitly]
            public IObservable<string> EOne()
            {
                return Observable.Return("Hello from EOne");
            }
        }

        protected override IEnumerable<object> GetEndpoints()
        {
            yield return _tester;
        }

        protected override void ConfigureAdapter(FlexibleSubscribeAdapter adp)
        {
            adp.RegisterMethods(mi => mi.Name.StartsWith("E"));
        }

        [Fact]
        public void string_msg_received()
        {
            Messages.OfType<string>().ShouldContain(o => o == "Hello from EOne");
        }
    }

    
    public class FsaMappingObservables : FlexibleSubscribingIntegrationContext
    {
        private readonly Tester _tester = new Tester();

        private class Tester
        {
            [UsedImplicitly]
            public IObservable<MessageB> EOne(IObservable<MessageA> aMessages)
            {
                return aMessages.Select(a=> new MessageB {Id = a.Name});
            }
        }

        protected override IEnumerable<object> GetEndpoints()
        {
            yield return _tester;
        }

        protected override void ConfigureAdapter(FlexibleSubscribeAdapter adp)
        {
            adp.RegisterMethods(mi => mi.Name.StartsWith("E"));
        }

        [Fact]
        public void observable_mapping_working()
        {
            Bus.Publish(new MessageA {Name = "My sweet message"});
            var msgB = Messages.OfType<MessageB>().FirstOrDefault();
            msgB.ShouldNotBeNull();
            msgB.Id.ShouldBeEqualTo("My sweet message");
        }
    }
}