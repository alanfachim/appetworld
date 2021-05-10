using Amazon.DynamoDBv2.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Carimbador
{
    class Negocio
    {
        private AmazonSQSClient Sqs;
        private string queueURL = "https://sqs.sa-east-1.amazonaws.com/469122751664/fila";
        private DAO db;
        public Negocio()
        {
            db = new DAO();
            Sqs = new AmazonSQSClient();
        }
        public class CompararRegra : IComparer<Dictionary<string, AttributeValue>>
        {
            public int Compare(Dictionary<string, AttributeValue> first, Dictionary<string, AttributeValue> second)
            {
                return string.Compare(first["inicio"].ToString(), second["inicio"].ToString());
            }
        }
        private Task<DeleteMessageResponse> DeleteMessage(string value)
        {
            DeleteMessageRequest receiveMessageParams = new DeleteMessageRequest
            {
                QueueUrl = queueURL,
                ReceiptHandle = value
            };
            return Sqs.DeleteMessageAsync(receiveMessageParams);
        }
        public void Processar()
        {
            ReceiveMessageRequest receiveMessageParams = new ReceiveMessageRequest
            {
                QueueUrl = queueURL,
                MaxNumberOfMessages = 10,
                VisibilityTimeout = 30,
                WaitTimeSeconds = 10
            };

            while (true)
            {
                var i0 = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                var response = Sqs.ReceiveMessageAsync(receiveMessageParams).Result;
                var message_threads = new List<Task<string>>();
                var time1 = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - i0;
                var inicio = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

                if (response.Messages.Count > 0)
                {
                    List<Task> taskMessages = new List<Task>();
                    for (int i = 0; i < response.Messages.Count; i++)
                    {
                        var message = response.Messages[i];
                        Dictionary<string, object> input;
                        try
                        {
                            input = JsonConvert.DeserializeObject<Dictionary<string, object>>(message.Body);
                            input.Add("ReceiptHandle", message.ReceiptHandle);
                            input.Add("startup", inicio);

                            Task task = db.getGrupParameters(input["cod_produto"].ToString(), input).ContinueWith((rt1) =>
                           {
                               var value = (Dictionary<string, object>)rt1.Result;
                               var grupo = (Dictionary<string, AttributeValue>)value["grupo"];
                               var input = (Dictionary<string, object>)value["input"];
                               var chave_regra = grupo["grupo"].S.ToString() + ';';
                               foreach (var parametro in grupo["parametros"].L)
                               {
                                   var p = ((JObject)input["parametros"])[parametro.S];
                                   if (p != null)
                                   {
                                       chave_regra += parametro.S + '=' + p["value"] + ';';
                                   }
                               }
                               return db.buscaRegras(chave_regra, input).Result;
                           }).ContinueWith((rt2) =>
                           {
                               var value = rt2.Result;
                               var regras = (List<Dictionary<string, AttributeValue>>)value["regra"];
                               var input = (Dictionary<string, object>)value["input"];
                               HPCsharp.Algorithm.Quicksort<Dictionary<string, AttributeValue>>(regras.ToArray(), 0, regras.Count, new CompararRegra());
                               for (var r = 0; r < regras.Count; r++)
                               {
                                   if (int.Parse(regras[r]["inicio"].S.Split(';')[0]) <= int.Parse(input["database"].ToString()) &&
                                   int.Parse(input["carencia"].ToString()) > int.Parse(regras[r]["inicio"].S.Split(';')[1]) && int.Parse(input["carencia"].ToString()) < int.Parse(regras[r]["fim"].N))
                                   {
                                       List<Task<Dictionary<string, object>>> taskList = new List<Task<Dictionary<string, object>>>();
                                       taskList.Add(db.buscaPontoCurva(regras[r]["curva_transfer"].S, input["database"].ToString(), input["vencimento"].ToString(), input));
                                       taskList.Add(db.buscaPontoCurva(regras[r]["curva_custo"].S, input["database"].ToString(), input["vencimento"].ToString(), input));
                                       //buscar decomposicao  
                                       return Task.WhenAll(taskList.ToArray()).Result;
                                   }
                               }
                               var delete = DeleteMessage(input["ReceiptHandle"].ToString()).Result;
                               return null;
                           }).ContinueWith((rt2) =>
                           {

                               var value = rt2.Result;
                               var input = ((Dictionary<string, object>)value[0]["input"])["startup"].ToString();
                               Console.WriteLine(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - long.Parse(input));
                               //publicar no kafka
                               return ((Dictionary<string, object>)value[0]["input"])["ReceiptHandle"].ToString();
                           }).ContinueWith((rt2) =>
                           {
                               var value = rt2.Result;
                               return DeleteMessage(value);
                           });
                            taskMessages.Add(task);

                        }
                        catch (Exception error)
                        {
                            // task = removeFromQueue(message);
                        }
                    }
                    var retorno = Task.WhenAll(taskMessages.ToArray());
                }
            }
        }


    }
}
