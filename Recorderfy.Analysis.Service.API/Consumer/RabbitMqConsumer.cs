using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Recorderfy.Analisys.Service.BLL.Interfaces;
using Recorderfy.Analisys.Service.DAL.Interfaces;

namespace Recorderfy.Analysis.Service.API.Consumer
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqConsumer> _logger;
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMqConsumer(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<RabbitMqConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() => _logger.LogInformation("RabbitMQ Consumer está deteniendo..."));

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                    Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                    UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest"
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declarar exchange
                _channel.ExchangeDeclare(
                    exchange: "analysis-exchange",
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false
                );

                // Declarar cola
                var queueName = _channel.QueueDeclare(
                    queue: "analysis-queue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                ).QueueName;

                // Binding patterns para el servicio de análisis
                _channel.QueueBind(queueName, "analysis-exchange", "analysis.api.analisis.analizar");
                _channel.QueueBind(queueName, "analysis-exchange", "analysis.api.analisis.getByPaciente");
                _channel.QueueBind(queueName, "analysis-exchange", "analysis.api.analisis.getById");
                _channel.QueueBind(queueName, "analysis-exchange", "analysis.api.analisis.getDeterioro");
                _channel.QueueBind(queueName, "analysis-exchange", "analysis.api.analisis.getLineaBase");
                _channel.QueueBind(queueName, "analysis-exchange", "analysis.api.evaluacion.procesar");
                _channel.QueueBind(queueName, "analysis-exchange", "analysis.api.evaluacion.getById");
                _channel.QueueBind(queueName, "analysis-exchange", "analysis.api.evaluacion.getByPaciente");

                _logger.LogInformation("RabbitMQ Consumer conectado y escuchando mensajes...");

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var routingKey = ea.RoutingKey;

                        _logger.LogInformation($"Mensaje recibido con routing key: {routingKey}");

                        var response = await ProcessMessageAsync(routingKey, message);

                        // Enviar respuesta si hay ReplyTo
                        if (!string.IsNullOrEmpty(ea.BasicProperties.ReplyTo))
                        {
                            var replyProps = _channel.CreateBasicProperties();
                            replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

                            var responseBytes = Encoding.UTF8.GetBytes(response);

                            _channel.BasicPublish(
                                exchange: "",
                                routingKey: ea.BasicProperties.ReplyTo,
                                basicProperties: replyProps,
                                body: responseBytes
                            );

                            _logger.LogInformation($"Respuesta enviada a {ea.BasicProperties.ReplyTo}");
                        }

                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error procesando mensaje: {ex.Message}");
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                };

                _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en RabbitMQ Consumer: {ex.Message}");
                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task<string> ProcessMessageAsync(string routingKey, string message)
        {
            using var scope = _serviceProvider.CreateScope();

            var parts = routingKey.Split('.');
            if (parts.Length < 4) return CreateErrorResponse("Routing key inválido");

            var entity = parts[2]; // "analisis" o "evaluacion"
            var action = parts[3]; // "analizar", "getByPaciente", etc.

            return entity switch
            {
                "analisis" => await ProcessAnalisisAsync(action, message, scope),
                "evaluacion" => await ProcessEvaluacionAsync(action, message, scope),
                _ => CreateErrorResponse($"Entidad desconocida: {entity}")
            };
        }

        private async Task<string> ProcessAnalisisAsync(string action, string message, IServiceScope scope)
        {
            try
            {
                var analisisService = scope.ServiceProvider.GetRequiredService<IAnalisisService>();
                var analisisRepository = scope.ServiceProvider.GetRequiredService<IAnalisisRepository>();

                return action switch
                {
                    "analizar" => await HandleAnalizar(analisisService, message),
                    "getByPaciente" => await HandleGetByPaciente(analisisRepository, message),
                    "getById" => await HandleGetById(analisisRepository, message),
                    "getDeterioro" => await HandleGetDeterioro(analisisRepository),
                    "getLineaBase" => await HandleGetLineaBase(analisisRepository, message),
                    _ => CreateErrorResponse($"Acción desconocida: {action}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en ProcessAnalisisAsync: {ex.Message}");
                return CreateErrorResponse(ex.Message);
            }
        }

        private async Task<string> ProcessEvaluacionAsync(string action, string message, IServiceScope scope)
        {
            try
            {
                var analisisService = scope.ServiceProvider.GetRequiredService<IAnalisisService>();

                return action switch
                {
                    "procesar" => await HandleProcesarEvaluacion(analisisService, message),
                    "getById" => await HandleGetEvaluacionById(analisisService, message),
                    "getByPaciente" => await HandleGetEvaluacionByPaciente(analisisService, message),
                    _ => CreateErrorResponse($"Acción desconocida: {action}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en ProcessEvaluacionAsync: {ex.Message}");
                return CreateErrorResponse(ex.Message);
            }
        }

        // ==================== HANDLERS PARA ANÁLISIS ====================

        private async Task<string> HandleAnalizar(IAnalisisService service, string message)
        {
            var request = JsonSerializer.Deserialize<Recorderfy.Analisys.Service.Model.DTOs.AnalisisRequest>(
                message, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (request == null)
                return CreateErrorResponse("Request inválido");

            var resultado = await service.RealizarAnalisisAsync(request);

            return JsonSerializer.Serialize(new
            {
                success = true,
                data = resultado,
                timestamp = DateTime.UtcNow
            });
        }

        private async Task<string> HandleGetByPaciente(IAnalisisRepository repository, string message)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
            
            if (data == null || !data.ContainsKey("pacienteId"))
                return CreateErrorResponse("PacienteId requerido");

            var pacienteId = Guid.Parse(data["pacienteId"]);
            var analisis = await repository.ObtenerPorPacienteAsync(pacienteId);

            return JsonSerializer.Serialize(new
            {
                success = true,
                data = analisis,
                count = analisis.Count,
                timestamp = DateTime.UtcNow
            });
        }

        private async Task<string> HandleGetById(IAnalisisRepository repository, string message)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
            
            if (data == null || !data.ContainsKey("analisisId"))
                return CreateErrorResponse("AnalisisId requerido");

            var analisisId = Guid.Parse(data["analisisId"]);
            var analisis = await repository.ObtenerPorIdAsync(analisisId);

            if (analisis == null)
                return CreateErrorResponse("Análisis no encontrado", 404);

            return JsonSerializer.Serialize(new
            {
                success = true,
                data = analisis,
                timestamp = DateTime.UtcNow
            });
        }

        private async Task<string> HandleGetDeterioro(IAnalisisRepository repository)
        {
            var analisis = await repository.ObtenerAnalisisConDeterioroAsync();

            return JsonSerializer.Serialize(new
            {
                success = true,
                data = analisis,
                count = analisis.Count,
                timestamp = DateTime.UtcNow
            });
        }

        private async Task<string> HandleGetLineaBase(IAnalisisRepository repository, string message)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
            
            if (data == null || !data.ContainsKey("pacienteId"))
                return CreateErrorResponse("PacienteId requerido");

            var pacienteId = Guid.Parse(data["pacienteId"]);
            var lineaBase = await repository.ObtenerLineaBaseActivaAsync(pacienteId);

            if (lineaBase == null)
                return CreateErrorResponse("Línea base no encontrada", 404);

            return JsonSerializer.Serialize(new
            {
                success = true,
                data = lineaBase,
                timestamp = DateTime.UtcNow
            });
        }

        // ==================== HANDLERS PARA EVALUACIONES ====================

        private async Task<string> HandleProcesarEvaluacion(IAnalisisService service, string message)
        {
            var request = JsonSerializer.Deserialize<Recorderfy.Analisys.Service.Model.DTOs.EvaluacionCompletaRequest>(
                message,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (request == null)
                return CreateErrorResponse("Request inválido");

            var resultado = await service.ProcesarEvaluacionCompletaAsync(request);

            return JsonSerializer.Serialize(new
            {
                success = true,
                data = resultado,
                message = $"Evaluación completada exitosamente. Score promedio: {resultado.ScoreGlobalPromedio:F2}",
                timestamp = DateTime.UtcNow
            });
        }

        private async Task<string> HandleGetEvaluacionById(IAnalisisService service, string message)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
            
            if (data == null || !data.ContainsKey("evaluacionId"))
                return CreateErrorResponse("EvaluacionId requerido");

            var evaluacionId = Guid.Parse(data["evaluacionId"]);
            var evaluacion = await service.ObtenerEvaluacionCompletaPorIdAsync(evaluacionId);

            return JsonSerializer.Serialize(new
            {
                success = true,
                data = evaluacion,
                timestamp = DateTime.UtcNow
            });
        }

        private async Task<string> HandleGetEvaluacionByPaciente(IAnalisisService service, string message)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
            
            if (data == null || !data.ContainsKey("pacienteId"))
                return CreateErrorResponse("PacienteId requerido");

            var pacienteId = Guid.Parse(data["pacienteId"]);
            var evaluaciones = await service.ObtenerEvaluacionesPorPacienteAsync(pacienteId);

            return JsonSerializer.Serialize(new
            {
                success = true,
                data = evaluaciones,
                count = evaluaciones.Count,
                timestamp = DateTime.UtcNow
            });
        }

        // ==================== HELPERS ====================

        private string CreateErrorResponse(string message, int statusCode = 400)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = message,
                statusCode = statusCode,
                timestamp = DateTime.UtcNow
            });
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
