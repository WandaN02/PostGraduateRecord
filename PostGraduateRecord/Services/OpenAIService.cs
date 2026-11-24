using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PostGraduateRecord.Services
{
    public class OpenAIService
    {
        private readonly string apiKey;
        private readonly string model;

        public OpenAIService()
        {
            apiKey = ConfigurationManager.AppSettings["OpenAIApiKey"];
            model = ConfigurationManager.AppSettings["OpenAIModel"];

            if (string.IsNullOrWhiteSpace(model))
            {
                model = "gpt-5.6-luna";
            }
        }

        public async Task<string> AskAsync(string userMessage, string systemInstructions)
        {
            if (string.IsNullOrWhiteSpace(apiKey) ||
                apiKey == "YOUR_OPENAI_API_KEY_HERE")
            {
                throw new Exception("OpenAI API key has not been configured.");
            }

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.openai.com/");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                var requestBody = new
                {
                    model = model,

                    instructions = systemInstructions,

                    input = userMessage,

                    store = false
                };

                string json = JsonConvert.SerializeObject(requestBody);

                using (var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"))
                {
                    HttpResponseMessage response =
                        await client.PostAsync("v1/responses", content);

                    string responseJson =
                        await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception(
                            "OpenAI API error: " + responseJson);
                    }

                    dynamic result =
                        JsonConvert.DeserializeObject(responseJson);

                    string output = "";

                    if (result.output != null)
                    {
                        foreach (var item in result.output)
                        {
                            if (item.content != null)
                            {
                                foreach (var contentItem in item.content)
                                {
                                    if (contentItem.type != null &&
                                        contentItem.type.ToString() == "output_text")
                                    {
                                        output +=
                                            contentItem.text.ToString();
                                    }
                                }
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(output))
                    {
                        return "I could not generate a response.";
                    }

                    return output.Trim();
                }
            }
        }
    }
}