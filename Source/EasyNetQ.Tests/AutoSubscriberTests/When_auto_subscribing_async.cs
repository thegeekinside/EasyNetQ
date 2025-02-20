using EasyNetQ.AutoSubscribe;
using EasyNetQ.Tests.Mocking;

namespace EasyNetQ.Tests.AutoSubscriberTests;

public class When_auto_subscribing_async : IDisposable
{
    private readonly MockBuilder mockBuilder;

    private const string expectedQueueName1 =
        "EasyNetQ.Tests.AutoSubscriberTests.When_auto_subscribing_async+MessageA, EasyNetQ.Tests_my_app:a0ebdb4503cc0df2295d8d8d99c1faf8";

    private const string expectedQueueName2 =
        "EasyNetQ.Tests.AutoSubscriberTests.When_auto_subscribing_async+MessageB, EasyNetQ.Tests_MyExplicitId";

    private const string expectedQueueName3 =
        "EasyNetQ.Tests.AutoSubscriberTests.When_auto_subscribing_async+MessageC, EasyNetQ.Tests_my_app:930ab443eb64f3ebffd65c9e494933d9";

    public When_auto_subscribing_async()
    {
        mockBuilder = new MockBuilder();

        var autoSubscriber = new AutoSubscriber(mockBuilder.Bus, "my_app");
        autoSubscriber.Subscribe(new[] { typeof(MyAsyncConsumer) });
    }

    public void Dispose()
    {
        mockBuilder.Dispose();
    }

    [Fact]
    public void Should_have_declared_the_queues()
    {
        void VerifyQueueDeclared(string queueName) =>
            mockBuilder.Channels[1].Received().QueueDeclare(
                Arg.Is(queueName),
                Arg.Is(true),
                Arg.Is(false),
                Arg.Is(false),
                Arg.Is((IDictionary<string, object>)null)
            );

        VerifyQueueDeclared(expectedQueueName1);
        VerifyQueueDeclared(expectedQueueName2);
        VerifyQueueDeclared(expectedQueueName3);
    }

    [Fact]
    public void Should_have_bound_to_queues()
    {
        void ConsumerStarted(int channelIndex, string queueName, string topicName) =>
            mockBuilder.Channels[1].Received().QueueBind(
                Arg.Is(queueName),
                Arg.Any<string>(),
                Arg.Is(topicName),
                Arg.Is((IDictionary<string, object>)null)
            );

        ConsumerStarted(1, expectedQueueName1, "#");
        ConsumerStarted(2, expectedQueueName2, "#");
        ConsumerStarted(3, expectedQueueName3, "Important");
    }

    [Fact]
    public void Should_have_started_consuming_from_the_correct_queues()
    {
        mockBuilder.ConsumerQueueNames.Contains(expectedQueueName1).Should().BeTrue();
        mockBuilder.ConsumerQueueNames.Contains(expectedQueueName2).Should().BeTrue();
        mockBuilder.ConsumerQueueNames.Contains(expectedQueueName3).Should().BeTrue();
    }

    //Discovered by reflection over test assembly, do not remove.
    private class MyAsyncConsumer : IConsumeAsync<MessageA>, IConsumeAsync<MessageB>, IConsumeAsync<MessageC>
    {
        public Task ConsumeAsync(MessageA message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        [AutoSubscriberConsumer(SubscriptionId = "MyExplicitId")]
        public Task ConsumeAsync(MessageB message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        [ForTopic("Important")]
        public Task ConsumeAsync(MessageC message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private class MessageA
    {
    }

    private class MessageB
    {
    }

    private class MessageC
    {
    }
}
