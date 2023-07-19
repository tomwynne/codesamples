public string lastResult = "";

public async Task<string> HttpClientPut(string URL, string Body) {
	HttpClient client = new HttpClient();

	try {
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

		client.DefaultRequestHeaders.ExpectContinue = false;

		var request = new HttpRequestMessage(HttpMethod.Put, URL);
		request.Headers.Add("x-auth-token", "letmein");

		var content = new StringContent(Body, Nothing, "application/json");
		request.Content = content;

		Dim response = await client.SendAsync(request);
		//response.EnsureSuccessStatusCode();    // don't do this, failure messages won't happen

		return await response.Content.ReadAsStringAsync();
	}
	catch(exception ex) {
		lastResult = ex.Message;
		if(ex.InnerException != null)
			lastResult += "\r\n" + ex.InnerException.Message;
		return "";
	}
}