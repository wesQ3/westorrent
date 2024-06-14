using System.Web;

class Protocol
{
    const string ClientId = "WB";
    const string Version = "0001";
    public static async Task<HttpResponseMessage> Announce(Torrent t)
    {
        var ua = new HttpClient();
        var queryParams = HttpUtility.ParseQueryString("");
        queryParams["info_hash"] = HttpUtility.UrlEncode(t.InfoHash);
        queryParams["peer_id"] = $"-{ClientId}{Version}-f23408e26371"; // should be random
        queryParams["port"] = "6881";
        queryParams["uploaded"] = "0";
        queryParams["downloaded"] = "0";
        queryParams["left"] = $"{t.Size}";
        queryParams["compact"] = "1";
        var uri = new UriBuilder(t.Tracker)
        // var uri = new UriBuilder("http://localhost/app/announce")
        {
            Query = queryParams.ToString()
        };
        var res = await ua.GetAsync(uri.ToString());
        Console.WriteLine($"{res.StatusCode}");
        var x = await res.Content.ReadAsStringAsync();
        Console.WriteLine($"{x}");

        return res;
    }

}
