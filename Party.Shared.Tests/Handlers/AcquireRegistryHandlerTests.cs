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
        public async Task CanDownloadOneAsync()
        {
            var serializer = new Mock<IRegistrySerializer>(MockBehavior.Strict);
            using var httpStream = new MemoryStream(new byte[] { 123 });
            serializer
                .Setup(x => x.Deserialize(It.Is<Stream>(s => s.ReadByte() == 123)))
                .Returns(ResultFactory.Reg(ResultFactory.RegScript("my-script", ResultFactory.RegVer("1.0.0"))));
            var handler = new AcquireRegistryHandler(
                new HttpClient(MockHandler("https://example.org/registry/v1/index.json", httpStream)),
                new[] { "https://example.org/registry/v1/index.json" },
                serializer.Object);

            var registry = await handler.AcquireRegistryAsync(null);

            PartyAssertions.AreDeepEqual(
                ResultFactory.Reg(ResultFactory.RegScript("my-script", ResultFactory.RegVer("1.0.0"))),
                registry);
        }

        private HttpMessageHandler MockHandler(string url, Stream stream)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
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
            return handlerMock.Object;
        }
    }
}
