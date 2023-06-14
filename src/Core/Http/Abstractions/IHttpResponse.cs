namespace JJMasterData.Core.Web.Http.Abstractions;

public interface IHttpResponse
{
    /// <summary>
    /// Ends the HttpResponse and sends the data to the client.
    /// </summary>
    /// <param name="data">Data to the client. Can be a HTML or JSON .</param>
    /// <param name="contentType">Optional. Usually application/json</param>
    void SendResponse(string data, string contentType = null);

    void ClearResponse();
    void AddResponseHeader(string key, string value);
    void Redirect(string url);
}