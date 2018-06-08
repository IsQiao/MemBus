using System;
using System.Linq;
using System.Reflection;
using MemBus.Setup;
using MemBus.Subscribing;
using MemBus.Tests.Help;
using MemBus.Support;
using Xunit;
using NSubstitute;


namespace MemBus.Tests.Subscribing
{
    public class UsingSubscriptionAdapterService
    {
        [Fact]
        public void Unconfigured_setup_will_throw_invalid_op()
        {
            var setup = new FlexibleSubscribeAdapter();
            (new Action(() => ((ISetup<IConfigurableBus>)setup).Accept(null))).Throws<InvalidOperationException>();
        }

        
        [Fact]
        public void When_having_some_configuration_adapter_adds_itself_as_service()
        {
            var setup = new FlexibleSubscribeAdapter();
            setup.RegisterMethods("Handle");

            var bus = (IConfigurableBus)Substitute.For(new[] { typeof(IConfigurableBus), typeof(IBus) }, new object[0]);
            ((ISetup<IConfigurableBus>)setup).Accept(bus);

            bus.Received().AddService<IAdapterServices>(setup);
        }

        [Fact]
        public void Integrative_test_of_finding_all_handlers_in_complex_scenario()
        {
            var setup = new FlexibleSubscribeAdapter();
            setup
                .ByInterface(typeof(IClassicIHandleStuffI<>))
                .RegisterMethods("Handle")
                .RegisterMethods("Schmandle");
            
            var handler = new SomeCrazyHandler();
            var simpleResolver = new SimpleResolver();
            ((IAdapterServices) setup).WireUpSubscriber(simpleResolver, handler);

            var subs = simpleResolver.ToList();

            subs.ShouldHaveCount(5);

            subs.Where(s=>s.Handles(typeof(MessageASpecialization))).Each(s=>s.Push(new MessageASpecialization()));
            handler.MessageACount.ShouldBeEqualTo(1);
            handler.MsgSpecialACount.ShouldBeEqualTo(1);

            subs.Where(s => s.Handles(typeof(MessageB))).Each(s => s.Push(new MessageB()));
            handler.MessageBCount.ShouldBeEqualTo(2); //There are 2 handle methods for MsgB :)

            subs.Where(s => s.Handles(typeof(MessageC))).Each(s => s.Push(new MessageC()));
            handler.MessageCCount.ShouldBeEqualTo(1);
        }

        [Fact]
        public void Subscriptions_are_built_for_object_method_based()
        {
            var builder = new MethodScanner("Handle").MakeBuilder();
            var subs = builder.BuildSubscriptions(new SomeHandler());
            subs.ShouldNotBeNull();
            subs.ShouldHaveCount(1);
        }

        [Fact]
        public void Subscriptions_for_object_method_based_work_correctly()
        {
            var builder = new MethodScanner("Handle").MakeBuilder();
            var handler = new SomeHandler();
            var subs = builder.BuildSubscriptions(handler);
            var subscription = subs.First();
            subscription.Handles(typeof(MessageA)).ShouldBeTrue();
            subscription.Push(new MessageA());
            handler.MsgACalls.ShouldBeEqualTo(1);
        }

        
        [Theory]
        [InlineData(typeof(InvalidHandlerInterfaceBecauseNoParameter), 0)]
        [InlineData(typeof(InvalidHandlerInterfaceBecauseTwoMethodsOfrequestedPattern), 2)]
        [InlineData(typeof(InvalidHandlerInterfaceBecauseReturnType), 1)]
        [InlineData(typeof(InvalidHandlerInterfaceBecauseTwoParams), 0)]
        public void These_handler_interfaces_will_return_zero_endpoints(Type implementation, int expectedCandidateCount)
        {
            var interfaceType = implementation.GetInterfaces().First();
            var obj = Activator.CreateInstance(implementation);
            var bld = new InterfaceBasedBuilder(interfaceType);
            var meths = bld.GetMethodInfos(obj).ToList();
            meths.ShouldHaveCount(expectedCandidateCount);
        }

        [Theory]
        [InlineData(typeof(IInvalidHandlerInterfaceBecauseNoParameter))]
        [InlineData(typeof(IInvalidHandlerInterfaceBecauseTwoMethodsOfrequestedPattern))]
        [InlineData(typeof(IInvalidHandlerInterfaceBecauseReturnType))]
        [InlineData(typeof(IInvalidHandlerInterfaceBecauseTwoParams))]
        public void interfaces_are_unsuable_in_ioc_scenario(Type @interface)
        {
            @interface.InterfaceIsSuitableAsIoCHandler().ShouldBeFalse();
        }

        [Fact]
        public void Non_generic_interface_is_properly_handled()
        {
            var builder = new InterfaceBasedBuilder(typeof(ItfNonGenericForHandles)).MakeBuilder();
            var targetToAdapt = new AHandlerThroughSimpleInterface();
            var subs = builder.BuildSubscriptions(targetToAdapt);
            subs.ShouldHaveCount(1);
            var s = subs.First();
            s.Handles(typeof(MessageA)).ShouldBeTrue();
            s.Push(new MessageA());
            targetToAdapt.MsgCount.ShouldBeEqualTo(1);
        }

        [Fact]
        public void Two_subscriptions_expected_from_aquainting_crazy_handler()
        {
            var builder = new InterfaceBasedBuilder(typeof (IClassicIHandleStuffI<>)).MakeBuilder();
            var subs = builder.BuildSubscriptions(new SomeCrazyHandler());
            subs.ShouldHaveCount(2);
        }

        [Fact]
        public void explicit_implementation_of_interfaces_is_supported()
        {
            var builder = new InterfaceBasedBuilder(typeof(IClassicIHandleStuffI<>)).MakeBuilder();
            var subs = builder.BuildSubscriptions(new HandlerWithExplicitImpl());
            subs.ShouldHaveCount(1);
        }
    }
}