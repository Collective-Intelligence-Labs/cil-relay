using Confluent.Kafka;
using Google.Protobuf;
using System;
using System.Threading.Tasks;

namespace Cila
{
    public class KafkaProducer
    {
        private readonly ProducerConfig config;
        private readonly IProducer<string, byte[]> producer;

        public KafkaProducer(ProducerConfig config)
        {
            this.config = config;
            producer = new ProducerBuilder<string, byte[]>(config).SetValueSerializer(Serializers.ByteArray).Build();
        }

        public async Task ProduceAsync(string topic, InfrastructureEvent message)
        {
            try
            {
                var result = await producer.ProduceAsync(topic, new Message<string, byte[]> { Key = null, Value = message.ToByteArray() });
                Console.WriteLine($"Produced message '{message}' to topic {result.Topic}, partition {result.Partition}, offset {result.Offset}");
            }
            catch (ProduceException<string, byte[]> ex)
            {
                Console.WriteLine($"Delivery failed: {ex.Error.Reason}");
            }
        }

        public void Dispose()
        {
            producer?.Dispose();
        }
    }
}