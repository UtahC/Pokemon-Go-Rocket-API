#region
using Google.Protobuf;
using PokemonGo.RocketAPI.GeneratedCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace PokemonGo.RocketAPI
{
    class HttpClientRequester
    {
        public async Task<Response> PostProto<TRequest>(HttpClient client, string url, TRequest request)
                  where TRequest : IMessage<TRequest>
        {
            //Encode payload and put in envelop, then send
            var data = request.ToByteString();
            var result = await client.PostAsync(url, new ByteArrayContent(data.ToByteArray()));

            //Decode message
            var responseData = await result.Content.ReadAsByteArrayAsync();
            var codedStream = new CodedInputStream(responseData);
            var decodedResponse = new Response();
            decodedResponse.MergeFrom(codedStream);

            return decodedResponse;
        }

        private bool waitingForResponse = false;
        public async Task<TResponsePayload> PostProtoPayload<TRequest, TResponsePayload>(HttpClient client,
                string url, TRequest request) where TRequest : IMessage<TRequest>
                where TResponsePayload : IMessage<TResponsePayload>, new()
        {
            ByteString payload = null;

            while (waitingForResponse)
                await Task.Delay(30);
            waitingForResponse = true;

            Response response = null;
            int count = 0;
            do
            {
                count++;
                //ColoredConsoleWrite(ConsoleColor.Red, "ArgumentOutOfRangeException - Restarting");
                //ColoredConsoleWrite(ConsoleColor.Red, ($"[DEBUG] [{DateTime.Now.ToString("HH:mm:ss")}] requesting {typeof(TResponsePayload).Name}"));
                response = await PostProto(client, url, request);
                waitingForResponse = false;



                //Decode payload
                //todo: multi-payload support

                await Task.Delay(30);// request every 30ms, up this value for not spam their server
            } while (response.Payload.Count < 1 && count < 30);
            payload = response.Payload[0];

            var parsedPayload = new TResponsePayload();
            parsedPayload.MergeFrom(payload);

            return parsedPayload;
        }
    }
}
