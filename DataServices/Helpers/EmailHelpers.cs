using Newtonsoft.Json.Linq;

namespace DataServices.Helpers
{
    // example of searching elastic response for tokens
    public class EmailHelpers
    {
        public static void DynamicToResponse(ref Email email, string inResponse)
        {
            if (inResponse != null)
            {
                //add elastic info - Route, ReceivedDetail
                var jsonObject = JObject.Parse(inResponse);

                var jsonResponse = jsonObject["hits"]["hits"].FirstOrDefault();

                if (jsonResponse == null)
                {
                    return;
                }

                var source = jsonResponse["_source"];

                var hopTo = ("51.892128,-2.128382"); //Corvid
                email.Route = source
                    .SelectTokens("$..location")
                    .Where(t => t.Path.Contains("ReceivedHop"))
                    .Select(t =>
                    {
                        var hop = new Hop
                        {
                            HopFrom = t.SelectToken("lat") + "," + t.SelectToken("lon"),
                            HopTo = hopTo
                        };

                        hopTo = hop.HopFrom;
                        return hop;
                    })
                    .ToList();

                foreach (var jsonToken in source.SelectTokens("$..*").Where(j => j.Path.EndsWith("Received']") && j.Path.Contains("ReceivedHop")))
                {
                    email.ReceivedDetail.Add(jsonToken.First().ToString());
                }
            }
        }

        public static string DecodeEmail(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var streamReader = new StreamReader(stream);

            return streamReader.ReadToEnd();
        }
    }

    public class Email
    {
        public string _id { get; set; }

        public IList<string> ReceivedDetail { get; set; }

        public IEnumerable<Hop> Route { get; set; }

        public DateTime LocalTime { get; set; }

        public string EnvelopeFrom { get; set; }

        public string HeaderFrom { get; set; }

        public string Recipients { get; set; }

        public byte[] Raw { get; set; }
    }

    public class Hop
    {
        public string HopTo { get; set; }

        public string HopFrom { get; set; }
    }
}

