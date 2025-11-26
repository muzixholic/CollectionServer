using System.Net;
using System.Text;
using System.Text.Json;

namespace CollectionServer.ContractTests.Fakes;

public class MockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri?.ToString() ?? string.Empty;

        if (uri.Contains("googleapis.com"))
        {
            if (uri.Contains("9780135957059"))
            {
                return Task.FromResult(CreateResponse(new
                {
                    totalItems = 1,
                    items = new[]
                    {
                        new
                        {
                            volumeInfo = new
                            {
                                title = "The Pragmatic Programmer",
                                authors = new[] { "Andrew Hunt", "David Thomas" },
                                industryIdentifiers = new[]
                                {
                                    new { type = "ISBN_13", identifier = "9780135957059" }
                                }
                            }
                        }
                    }
                }));
            }
            else if (uri.Contains("9788936434267"))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
            else if (uri.Contains("080442957X"))
            {
                return Task.FromResult(CreateResponse(new
                {
                    totalItems = 1,
                    items = new[]
                    {
                        new
                        {
                            volumeInfo = new
                            {
                                title = "Test Book",
                                authors = new[] { "Test Author" },
                                industryIdentifiers = new[]
                                {
                                    new { type = "ISBN_10", identifier = "080442957X" }
                                }
                            }
                        }
                    }
                }));
            }
            else if (uri.Contains("9788966262281"))
            {
                return Task.FromResult(CreateResponse(new
                {
                    totalItems = 1,
                    items = new[]
                    {
                        new
                        {
                            volumeInfo = new
                            {
                                title = "Test Book 2",
                                authors = new[] { "Test Author 2" },
                                industryIdentifiers = new[]
                                {
                                    new { type = "ISBN_13", identifier = "9788966262281" }
                                }
                            }
                        }
                    }
                }));
            }
        }

        if (uri.Contains("dapi.kakao.com") && uri.Contains("9788936434267"))
        {
            return Task.FromResult(CreateResponse(new
            {
                documents = new[]
                {
                    new
                    {
                        title = "데미안",
                        authors = new[] { "헤르만 헤세" },
                        isbn = "9788936434267",
                        thumbnail = "http://example.com/image.jpg"
                    }
                }
            }));
        }

        if (uri.Contains("aladin.co.kr"))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static HttpResponseMessage CreateResponse(object content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json")
        };
    }
}
