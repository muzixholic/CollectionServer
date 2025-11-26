using System.Net;
using System.Text;
using System.Text.Json;

namespace CollectionServer.IntegrationTests.Fakes;

public class MockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri?.ToString() ?? "";
        
        // Google Books API Mock
        if (uri.Contains("googleapis.com"))
        {
            if (uri.Contains("9780135957059")) // The Pragmatic Programmer
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
            else if (uri.Contains("9788936434267")) // 데미안 (Google Books might not have it, but let's simulate not found or found)
            {
                // Simulate Not Found in Google Books, so it falls back to others
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
            else if (uri.Contains("080442957X")) // Test ISBN for JSON format check
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
            else if (uri.Contains("9788966262281")) // Test ISBN for JSON format check
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

        // Kakao Book API Mock
        if (uri.Contains("dapi.kakao.com"))
        {
            if (uri.Contains("9788936434267")) // 데미안
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
        }

        // Aladin API Mock
        if (uri.Contains("aladin.co.kr"))
        {
             // Simulate failure or success depending on test needs
             return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        // Default Not Found
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private HttpResponseMessage CreateResponse(object content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json")
        };
    }
}