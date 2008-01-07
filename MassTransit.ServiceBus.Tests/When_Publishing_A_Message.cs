using System;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace MassTransit.ServiceBus.Tests
{
    using Messages;

    [TestFixture]
    public class When_Publishing_A_Message :
        ServiceBusSetupFixture
    {
        [Test]
        public void The_Message_Should_Be_Delivered_To_All_Subscribers()
        {
            bool _updated = false;
            ManualResetEvent _updateEvent = new ManualResetEvent(false);

            _serviceBus.Subscribe<UpdateMessage>(
                delegate
                    {
                        _updated = true;
                        _updateEvent.Set();
                    });

            UpdateMessage um = new UpdateMessage();

            _serviceBus.Publish(um);

            Assert.That(_updateEvent.WaitOne(TimeSpan.FromSeconds(3), true), Is.True,
                        "Timeout expired waiting for message");

            Assert.That(_updated, Is.True);
        }

        [Test]
        public void The_Message_Should_Be_Handled_By_A_Remote_Service()
        {
            bool _updated = false;
            ManualResetEvent _updateEvent = new ManualResetEvent(false);

            _remoteServiceBus.Subscribe<UpdateMessage>(
                delegate
                    {
                        _updated = true;
                        _updateEvent.Set();
                    });

            Thread.Sleep(TimeSpan.FromSeconds(5));

            UpdateMessage um = new UpdateMessage();

            _serviceBus.Publish(um);

            Assert.That(_updateEvent.WaitOne(TimeSpan.FromSeconds(3), true), Is.True,
                        "Timeout expired waiting for message");

            Assert.That(_updated, Is.True);
        }

        [Test]
        public void The_Message_Should_Be_Handled_By_A_Remote_Service_And_Replyable()
        {
            bool _updated = false;
            ManualResetEvent _updateEvent = new ManualResetEvent(false);

            MessageReceivedCallback<UpdateMessage> handler =
                delegate(MessageContext<UpdateMessage> ctx)
                    {
                        _updated = true;
                        _updateEvent.Set();

                        ctx.Bus.Send(ctx.Envelope.ReturnTo, new UpdateAcceptedMessage());
                    };

            bool _replied = false;
            ManualResetEvent _repliedEvent = new ManualResetEvent(false);

            _remoteServiceBus.Subscribe(handler);

            _serviceBus.Subscribe<UpdateAcceptedMessage>(
                delegate
                    {
                        _replied = true;
                        _repliedEvent.Set();
                    });

            Thread.Sleep(TimeSpan.FromSeconds(5));

            UpdateMessage um = new UpdateMessage();

            _serviceBus.Publish(um);

            Assert.That(_updateEvent.WaitOne(TimeSpan.FromSeconds(3), true), Is.True,
                        "Timeout expired waiting for message");

            Assert.That(_updated, Is.True);

            Assert.That(_repliedEvent.WaitOne(TimeSpan.FromSeconds(3), true), Is.True, "NO response message received");

            Assert.That(_replied, Is.True, "Reply Failed");
        }

        [Test]
        public void Multiple_Local_Services_Should_Be_Available()
        {
            bool _updated = false;
            ManualResetEvent _updateEvent = new ManualResetEvent(false);

            _serviceBus.Subscribe<UpdateMessage>(
                delegate
                    {
                        _updated = true;
                        _updateEvent.Set();
                    });

            bool _deleted = false;
            ManualResetEvent _deleteEvent = new ManualResetEvent(false);

            _serviceBus.Subscribe<DeleteMessage>(
                delegate
                    {
                        _deleted = true;
                        _deleteEvent.Set();
                    });

            Thread.Sleep(TimeSpan.FromSeconds(5));

            DeleteMessage dm = new DeleteMessage();

            _serviceBus.Publish(dm);

            Assert.That(_deleteEvent.WaitOne(TimeSpan.FromSeconds(12), true), Is.True,
                        "Timeout expired waiting for message");

            Assert.That(_deleted, Is.True);

            UpdateMessage um = new UpdateMessage();

            _serviceBus.Publish(um);

            Assert.That(_updateEvent.WaitOne(TimeSpan.FromSeconds(12), true), Is.True,
                        "Timeout expired waiting for message");

            Assert.That(_updated, Is.True);
        }

        [Test]
        public void Multiple_Remote_Services_Should_Be_Available()
        {
            bool _updated = false;
            ManualResetEvent _updateEvent = new ManualResetEvent(false);

            _remoteServiceBus.Subscribe<UpdateMessage>(
                delegate
                {
                    _updated = true;
                    _updateEvent.Set();
                });

            bool _deleted = false;
            ManualResetEvent _deleteEvent = new ManualResetEvent(false);

            _remoteServiceBus.Subscribe<DeleteMessage>(
                delegate
                {
                    _deleted = true;
                    _deleteEvent.Set();
                });

            Thread.Sleep(TimeSpan.FromSeconds(3));

            DeleteMessage dm = new DeleteMessage();

            _serviceBus.Publish(dm);

            Assert.That(_deleteEvent.WaitOne(TimeSpan.FromSeconds(12), true), Is.True,
                        "Timeout expired waiting for message");

            Assert.That(_deleted, Is.True);

            UpdateMessage um = new UpdateMessage();

            _serviceBus.Publish(um);

            Assert.That(_updateEvent.WaitOne(TimeSpan.FromSeconds(12), true), Is.True,
                        "Timeout expired waiting for message");

            Assert.That(_updated, Is.True);
        }

        //[Test]
        //public void Poison_Letters_Should_Be_Moved_To_A_Poison_Queue()
        //{
        //    ManualResetEvent updateEvent = new ManualResetEvent(false);


        //    //this ends up in a seperate thread and I am therefore unable to figure out how to test
        //    _serviceBus.Subscribe<PoisonMessage>(delegate(MessageContext<PoisonMessage> cxt)
        //                                                               {
        //                                                                   try
        //                                                                   {
        //                                                                       cxt.Message.ThrowException();
        //                                                                   }
        //                                                                   catch(Exception)
        //                                                                   {
        //                                                                       cxt.MarkPoison();
        //                                                                   }
        //                                                               }
                
        //        );

        //   _serviceBus.Publish(new PoisonMessage());

        //    updateEvent.WaitOne(TimeSpan.FromSeconds(3), true);
        //    VerifyMessageInQueue(_serviceBus.Endpoint.PoisonEndpoint.Address, new PoisonMessage());
        //}
    }

    [Serializable]
    public class UpdateMessage : IMessage
    {
    }

    [Serializable]
    public class UpdateAcceptedMessage : IMessage
    {
    }

    [Serializable]
    public class DeleteMessage : IMessage
    {
    }
}