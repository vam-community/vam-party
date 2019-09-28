using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using Party.Shared.Serializers;

namespace Party.Shared.Handlers
{
    public class AcquireRegistryHandlerTests
    {
        [Test]
        public async Task CanDownloadOne()
        {
            var serializer = new Mock<IRegistrySerializer>(MockBehavior.Strict);
            using var httpStream = new MemoryStream(new byte[] { 123 });
            serializer
                .Setup(x => x.Deserialize(It.Is<Stream>(s => s.ReadByte() == 123)))
                .Returns(TestFactory.Reg(TestFactory.RegScript("my-script", TestFactory.RegVer("1.0.0"))));
            var handler = new AcquireRegistryHandler(
                new HttpClient(MockHandler(("https://example.org/registry/v1/index.json", httpStream))),
                new[] { "https://example.org/registry/v1/index.json" },
                serializer.Object);

            var registry = await handler.AcquireRegistryAsync(null);

            PartyAssertions.AreDeepEqual(
                TestFactory.Reg(TestFactory.RegScript("my-script", TestFactory.RegVer("1.0.0"))),
                registry);
        }

        [Test]
        public async Task CanOverride()
        {
            var serializer = new Mock<IRegistrySerializer>(MockBehavior.Strict);
            using var httpStream = new MemoryStream(new byte[] { 123 });
            serializer
                .Setup(x => x.Deserialize(It.Is<Stream>(s => s.ReadByte() == 123)))
                .Returns(TestFactory.Reg(TestFactory.RegScript("my-script", TestFactory.RegVer("1.0.0"))));
            var handler = new AcquireRegistryHandler(
                new HttpClient(MockHandler(("https://overridden.example.org/registry/v1/index.json", httpStream))),
                new[] { "https://example.org/registry/v1/index.json" },
                serializer.Object);

            var registry = await handler.AcquireRegistryAsync(new[] { "https://overridden.example.org/registry/v1/index.json" });

            PartyAssertions.AreDeepEqual(
                TestFactory.Reg(TestFactory.RegScript("my-script", TestFactory.RegVer("1.0.0"))),
                registry);
        }

        [Test]
        public async Task CanMergeAsync()
        {
            var serializer = new Mock<IRegistrySerializer>(MockBehavior.Strict);
            using var httpStream1 = new MemoryStream(new byte[] { 1 });
            using var httpStream2 = new MemoryStream(new byte[] { 2 });
            serializer
                .Setup(x => x.Deserialize(It.Is<Stream>(s => s.ReadByte() == 1 || s.Seek(0, SeekOrigin.Begin) == -1)))
                .Returns(TestFactory.Reg(
                    TestFactory.RegScript("my-script", TestFactory.RegVer("1.0.0"))
                ));
            serializer
                .Setup(x => x.Deserialize(It.Is<Stream>(s => s.ReadByte() == 2 || s.Seek(0, SeekOrigin.Begin) == -1)))
                .Returns(TestFactory.Reg(
                    TestFactory.RegScript("my-script", TestFactory.RegVer("2.0.0")),
                    TestFactory.RegScript("other-script", TestFactory.RegVer("1.0.0"))
                ));
            var client = new HttpClient(MockHandler(
                ("https://source1.example.org/registry/v1/index.json", httpStream1),
                ("https://source2.example.org/registry/v1/index.json", httpStream2)
            ));
            var handler = new AcquireRegistryHandler(
                client,
                new[] { "https://source1.example.org/registry/v1/index.json", "https://source2.example.org/registry/v1/index.json" },
                serializer.Object);

            var registry = await handler.AcquireRegistryAsync(null);

            PartyAssertions.AreDeepEqual(
                TestFactory.Reg(
                    TestFactory.RegScript("my-script",
                        TestFactory.RegVer("1.0.0"),
                        TestFactory.RegVer("2.0.0")),
                    TestFactory.RegScript("other-script",
                        TestFactory.RegVer("1.0.0"))),
                registry);
        }

        private HttpMessageHandler MockHandler(params (string url, Stream stream)[] list)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            foreach (var item in list)
            {
                var (url, stream) = item;
                MockHandlerCall(handlerMock, url, stream);
            }
            return handlerMock.Object;
        }

        private static void MockHandlerCall(Mock<HttpMessageHandler> handlerMock, string url, Stream stream)
        {
            handlerMock
               .Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString() == url),
                  ItExpr.IsAny<CancellationToken>()
               )
               // prepare the expected response of the mocked http call
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StreamContent(stream)
               });
        }
    }
}
