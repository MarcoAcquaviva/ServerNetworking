using System;
using NUnit.Framework;

namespace GameServerExample2B.Test
{
    public class TestGameServer
    {
        private FakeTransport transport;
        private FakeClock clock;
        private GameServer server;

        [SetUp]
        public void SetupTests()
        {
            transport = new FakeTransport();
            clock = new FakeClock();
            server = new GameServer(transport, clock);
        }

        [Test]
        public void TestZeroNow()
        {
            Assert.That(server.Now, Is.EqualTo(0));
        }

        [Test]
        public void TestClientsOnStart()
        {
            Assert.That(server.NumClients, Is.EqualTo(0));
        }

        [Test]
        public void TestGameObjectsOnStart()
        {
            Assert.That(server.NumGameObjects, Is.EqualTo(0));
        }

        [Test]
        public void TestJoinNumOfClients()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinNumOfGameObjects()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumGameObjects, Is.EqualTo(1));
        }

        [Test]
        public void TestWelcomeAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            FakeData welcome = transport.ClientDequeue();
            Assert.That(welcome.data[0], Is.EqualTo(1));
        }

        [Test]
        public void TestSpawnAvatarAfterJoin()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientDequeue();
            Assert.That(() => transport.ClientDequeue(), Throws.InstanceOf<FakeQueueEmpty>());
        }

        [Test]
        public void TestJoinSameClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinSameAddressClient()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 1);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinSameAddressAvatars()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "tester", 1);
            server.SingleStep();
            Assert.That(server.NumGameObjects, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsSamePort()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 0);
            server.SingleStep();
            Assert.That(server.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsWelcome()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();

            Assert.That(transport.ClientQueueCount, Is.EqualTo(5));

            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("foobar"));
            Assert.That(transport.ClientDequeue().endPoint.Address, Is.EqualTo("foobar"));
        }

        [Test]
        public void TestWelcomeSize()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();

            FakeData welcome = new FakeData();
            int n = (int)transport.ClientQueueCount;
            for (int i = 0; i < n; i++)
            {
                FakeData temp = transport.ClientDequeue();
                if (temp.data[0] == 1)
                {
                    welcome = temp;
                    Assert.That(welcome.data.Length, Is.EqualTo(25));
                    break;
                }
            }

            Assert.That(welcome.data[0], Is.EqualTo(1));
        }

        [Test]
        public void TestUpdateSize()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();

            FakeData update = new FakeData();
            int n = (int)transport.ClientQueueCount;
            for (int i = 0; i < n; i++)
            {
                FakeData temp = transport.ClientDequeue();
                if (temp.data[0] == 3)
                {
                    update = temp;
                    Assert.That(update.data.Length, Is.EqualTo(21));
                    break;
                }
            }

            Assert.That(update.data[0], Is.EqualTo(3));
        }

        [Test]
        public void TestServerDequeue()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();

            int n = (int)transport.ClientQueueCount;
            for (int i = 0; i < n; i++)
            {
                transport.ClientDequeue();
            }
            Assert.That(transport.ClientQueueCount, Is.EqualTo(0));
        }

        [Test]
        public void TestEvilUpdate()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();


            // TODO get the id from the welcome packets
            FakeData welcome = new FakeData();
            int n = (int)transport.ClientQueueCount;
            for (int i = 0; i < n; i++)
            {
                FakeData temp = transport.ClientDequeue();
                if (temp.data[0] == 1)
                {
                    welcome = temp;
                    Assert.That(welcome.data.Length, Is.EqualTo(25));
                    break;
                }
            }

            //try to move the id from the other player 

            // Get Avatar ID
            uint idAvatar = welcome.data[5];

            //Move Other Avatar
            Packet move = new Packet(3, idAvatar, 1, 1, 2);
            //Float number take the index 9 = 64
            transport.ClientEnqueue(move, "foobar", 1);
            server.SingleStep();



            Console.WriteLine(transport.ClientQueueCount);
            n = (int)transport.ClientQueueCount;
            for (int i = 0; i < n; i++)
            {
                FakeData temp = transport.ClientDequeue();
                if (temp.data[0] == 3)
                {
                    Assert.That(temp.data.Length, Is.EqualTo(21));
                    Assert.That(temp.data[17], Is.Not.EqualTo(10));
                    Assert.That(temp.data[5], Is.Not.EqualTo(1));
                }
            }
        }

        [Test]
        public void TestCorrectUpdate()
        {
            Packet packet = new Packet(0);
            transport.ClientEnqueue(packet, "tester", 0);
            server.SingleStep();
            transport.ClientEnqueue(packet, "foobar", 1);
            server.SingleStep();


            // TODO get the id from the welcome packets
            FakeData welcome = new FakeData();
            int n = (int)transport.ClientQueueCount;
            for (int i = 0; i < n; i++)
            {
                FakeData temp = transport.ClientDequeue();
                if (temp.data[0] == 1)
                {
                    welcome = temp;
                    Assert.That(welcome.data.Length, Is.EqualTo(25));
                    break;
                }
            }

            // try to move the id from the other player 

            // Get Avatar ID
            uint idAvatar = welcome.data[5];

            //Move Avatar
            Packet move = new Packet(3, idAvatar, 1, 1, 2);
            //Float number take the index 9 = 64
            transport.ClientEnqueue(move, welcome.endPoint.Address, welcome.endPoint.Port);
            server.SingleStep();


            n = (int)transport.ClientQueueCount;
            for (int i = 0; i < n; i++)
            {
                FakeData temp;
                do
                {
                    temp = transport.ClientDequeue();
                    i++;
                } while (temp.data[17] >= 10 || i < n);

                Assert.That(temp.data.Length, Is.EqualTo(21));
                for (int j = 0; j < temp.data.Length; j++)
                {
                    if (j == 5 || j == 17)
                        Console.WriteLine(temp.endPoint.Address + " update byte " + j + " is: " + temp.data[j]);
                }

                Assert.That(temp.data[17], Is.Not.GreaterThan(10));

            }
        }
    }
}
