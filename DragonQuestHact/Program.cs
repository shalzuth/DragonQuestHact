var content = new FormUrlEncodedContent(values);

using (var httpClient = new HttpClient(new Http2CustomHandler()))
{
    var q = httpClient.PostAsync("https://prd-entrypoint-gbl.gdt-game.net/Entrypoints/Current", content).Result;
    Console.WriteLine(q);
}