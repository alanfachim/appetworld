using Carimbador;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using StackExchange.Redis;

namespace Testes
{
    [TestClass]
    public class UnitTest1
    {
        public Mock<AmazonDynamoDBClient> MockDynamoDb { get; private set; }
        public Mock<IDatabase> MockRedisR { get; private set; }
        public Mock<IDatabase> MockRedisW { get; private set; }
        public Mock<AmazonSQSClient> MockSQS { get; private set; }
        public UnitTest1()
        {
            MockDynamoDb = new Mock<AmazonDynamoDBClient>();
            object p = MockDynamoDb.Setup(p => p.GetItemAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, AttributeValue>>(), default)).Returns(() =>
            {
                var result = new GetItemResponse();
                result.Item = new Dictionary<string, AttributeValue>() { { "", null } };
                return Task.FromResult(result);
            });
            MockRedisR = new Mock<IDatabase>();
            object q = MockRedisR.Setup(q => q.StringGetAsync(It.IsAny<RedisKey>(), default)).Returns(() =>
            {
                return Task.FromResult((RedisValue)"");
            });
            MockRedisW = new Mock<IDatabase>();
            object s = MockRedisW.Setup(q => q.StringGetAsync(It.IsAny<RedisKey>(), default)).Returns(() =>
            {
                return Task.FromResult((RedisValue)"");
            });
            MockSQS = new Mock<AmazonSQSClient>();
            object r = MockSQS.Setup(q => q.ReceiveMessageAsync(It.IsAny<string>(), default)).Returns(() =>
            {
                var result = new ReceiveMessageResponse();
                return Task.FromResult(result);
            });

        }

        [TestMethod]
        public void binarySerach()
        {

            DAO data = new DAO(MockDynamoDb.Object, MockRedisR.Object, MockRedisW.Object, MockSQS.Object);
            List<Dictionary<string, object>> obj = new List<Dictionary<string, object>>(){
                new Dictionary<string, object>() { { "chave", 1 } },
                new Dictionary<string, object>() { { "chave", 2 } },
                new Dictionary<string, object>() { { "chave", 3 } }
                };

            var result = data.binarySerach(obj, 1, "chave");
            Assert.AreEqual(result["chave"], 1);
            var result2 = data.binarySerach(obj, 2, "chave");
            Assert.AreEqual(result["chave"], 1);
            var result3 = data.binarySerach(obj, 3, "chave");
            Assert.AreEqual(result["chave"], 1);
        }

        [TestMethod]
        public void binarySerachRange()
        {
            DAO data = new DAO(MockDynamoDb.Object, MockRedisR.Object, MockRedisW.Object, MockSQS.Object);
            List<Dictionary<string, object>> obj = new List<Dictionary<string, object>>(){
                new Dictionary<string, object>() { { "chave", 1 } },
                new Dictionary<string, object>() { { "chave", 1 }, { "atributo","teste" } },
                new Dictionary<string, object>() { { "chave", 2 } },
                new Dictionary<string, object>() { { "chave", 3 } },
                new Dictionary<string, object>() { { "chave", 3 } }
                };

            var result = data.binarySerachRange(obj, 1, "chave");
            Assert.AreEqual(result[0]["chave"], 1);
            Assert.AreEqual(result[1]["atributo"], "teste");

            List<Dictionary<string, object>> obj2 = new List<Dictionary<string, object>>(){
                new Dictionary<string, object>() { { "chave", 1 }, { "atributo","teste0" } },
                new Dictionary<string, object>() { { "chave", 1 }, { "atributo","teste1" } },
                new Dictionary<string, object>() { { "chave", 2 }, { "atributo","teste2" } },
                new Dictionary<string, object>() { { "chave", 2 }, { "atributo","teste3" } },
                new Dictionary<string, object>() { { "chave", 2 }, { "atributo","teste4" } },
                new Dictionary<string, object>() { { "chave", 3 }, { "atributo","teste5" } }
                };

            var result2 = data.binarySerachRange(obj2, 3, "chave");
            Assert.AreEqual(result2[0]["chave"], 3);
            Assert.AreEqual(result2[0]["atributo"], "teste5");
            var result3 = data.binarySerachRange(obj2, 2, "chave");
            Assert.AreEqual(result3[0]["chave"], 2);
            Assert.AreEqual(result3[2]["atributo"], "teste4");
            Assert.AreEqual(result3.Count, 3);
        }

        [TestMethod]
        public void getGrupParameters()
        {
            DAO data = new DAO(MockDynamoDb.Object, MockRedisR.Object, MockRedisW.Object, MockSQS.Object);
            data.getGrupParameters("0001",null);
        }

        [TestMethod]
        public void buscaRegras()
        {
            DAO data = new DAO(MockDynamoDb.Object, MockRedisR.Object, MockRedisW.Object, MockSQS.Object);
            data.buscaRegras();
        }

        [TestMethod]
        public void Processar()
        {
            DAO data = new DAO(MockDynamoDb.Object, MockRedisR.Object, MockRedisW.Object, MockSQS.Object);
            data.Processar();
        }

    }
}
