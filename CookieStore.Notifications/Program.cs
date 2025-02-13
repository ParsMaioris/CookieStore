﻿using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

string rabbitMqConnectionString = "amqps://pdfnvtxf:bnpGPG4SYTEYSLDmF7XTcBrS7rhK28TD@gull.rmq.cloudamqp.com/pdfnvtxf";
var factory = new ConnectionFactory() { Uri = new Uri(rabbitMqConnectionString) };

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.ExchangeDeclare(
    exchange: "payment_processed_exchange",
    type: ExchangeType.Fanout,
    durable: true,
    autoDelete: false,
    arguments: null);

channel.QueueDeclare(
    queue: "notification_queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);
channel.QueueBind(
    queue: "notification_queue",
    exchange: "payment_processed_exchange",
    routingKey: "");

Console.WriteLine(" [*] Waiting for processed payment events (Notifications)...");

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] Notification service received: {message}");

    var processed = JsonSerializer.Deserialize<PaymentProcessed>(message);
    Console.WriteLine($" [x] Sending notification for OrderId: {processed?.OrderId}");
    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
};

channel.BasicConsume(
    queue: "notification_queue",
    autoAck: false,
    consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

public record PaymentProcessed(string OrderId, string Status);