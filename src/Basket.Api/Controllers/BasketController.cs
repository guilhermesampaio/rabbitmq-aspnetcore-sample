using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RabbitMQ.Client;

namespace Basket.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BasketController : ControllerBase
    {
        private readonly IMemoryCache cache;
        private readonly string basketCache = "basketCache";
        public BasketController(IMemoryCache cache)
        {
            this.cache = cache;
        }
        [HttpGet("{id}")]
        public IActionResult Get(Guid id)
        {
            var baskets = cache.Get<IList<Basket>>(basketCache);
            if (baskets is null)
                return NotFound();

            var basket = baskets.ToList().Where(it => it.Id == id).FirstOrDefault();

            if (basket is null)
                return NotFound();

            return Ok(basket);
        }

        [HttpPost]
        public IActionResult Post([FromBody] Basket basket)
        {
            var baskets = cache.Get<IList<Basket>>("basketCache");
            if (baskets is null)
            {
                baskets = new List<Basket>();
            }

            baskets.Add(basket);

            var cacheOptions = new MemoryCacheEntryOptions();
            cacheOptions.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

            cache.Set("basketCache", baskets, cacheOptions);

            return Ok(basket);
        }

        [HttpPost("{id}/checkout")]
        public IActionResult Checkout(Guid id)
        {
            var baskets = cache.Get<IList<Basket>>(basketCache);
            if (baskets is null)
                return NotFound();

            var basket = baskets.ToList().Where(it => it.Id == id).FirstOrDefault();

            if (basket is null)
                return NotFound();

            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                UserName = "guest",
                Password = "guest"
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(
                    queue: "hello",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var message = "hello, world!";
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "", routingKey: "hello", basicProperties: null, body: body);

                Debug.WriteLine("Mensagem enviada");
            }

            return Ok();
        }


    }

    public class Basket
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Created { get; set; } = DateTime.Now;
        public IEnumerable<Item> Items { get; set; }

    }

    public class Item
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
