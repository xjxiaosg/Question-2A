using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace rabbitproducer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class taskController : ControllerBase
    {
        [HttpPost]
        public void Post([FromBody] Models.TaskObj taskobj)
        {
            var configuration = GetConfiguration();
            var factory = new ConnectionFactory()
            {
                HostName = configuration.GetSection("RABBITMQ_HOST").Value,
                Port = Convert.ToInt32(configuration.GetSection("RABBITMQ_PORT").Value)
                //HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                //Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
            };

            string ret = "";
            string responseString = string.Empty;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://reqres.in");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string strPayload = JsonConvert.SerializeObject(taskobj);
                HttpContent c = new StringContent(strPayload, Encoding.UTF8, "application/json");
                var response = client.PostAsync("/api/login", c).Result;
                if (response.IsSuccessStatusCode)
                {
                    responseString = response.Content.ReadAsStringAsync().Result;
                    string message = responseString;
                    Console.WriteLine(factory.HostName + ":" + factory.Port);
                    using (var connection = factory.CreateConnection())
                    using (var channel = connection.CreateModel())
                    {
                        channel.QueueDeclare(queue: "TaskQueue",
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);

                        //string message = taskobj.task;
                        var body = Encoding.UTF8.GetBytes(message);

                        channel.BasicPublish(exchange: "",
                                             routingKey: "TaskQueue",
                                             basicProperties: null,
                                             body: body);
                    }
                }
            }



        }

        public IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            return builder.Build();
        }
    }
}
