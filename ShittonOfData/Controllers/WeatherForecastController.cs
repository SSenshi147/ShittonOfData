using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Text;

namespace ShittonOfData.Controllers;
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IService _service;
    private readonly IContainer _container;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IService service, IContainer container)
    {
        _logger = logger;
        this._service = service;
        this._container = container;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task Get()
    {
        var sw = new StreamWriter("out.csv");
        await sw.WriteLineAsync("id,code");
        foreach (var item in Enumerable.Range(0, 5000))
        {
            await sw.WriteLineAsync($"{Guid.NewGuid()},HU");
        }
        sw.Close();
    }

    [HttpPost]
    [Route("post")]
    public async Task<string> Test([FromBody][Required] Data data)
    {
        var sw = Stopwatch.StartNew();
        await _service.Do(data);
        sw.Stop();
        return sw.ElapsedMilliseconds.ToString();
    }

    [HttpGet]
    [Route("stats")]
    public async Task<string> GetStats()
    {
        var sb = new StringBuilder();

        foreach (var task in _container.Tasks)
        {
            sb.AppendLine($"{task.Key}: completed={task.Value.Status} succ={task.Value.IsCompletedSuccessfully} faulted={task.Value.IsFaulted}");
        }

        return sb.ToString();
    }


    [HttpPost]
    [Route("file")]
    public async Task<string> TestFile()
    {
        var sw = Stopwatch.StartNew();
        var csvPath = "https://raw.githubusercontent.com/SSenshi147/ShittonOfData/master/ShittonOfData/out.csv";
        var threads = 8;

        using var httpclient = new HttpClient();
        var response = await httpclient.GetAsync(csvPath);
        var text = await response.Content.ReadAsStringAsync();
        var lines = text.Split('\n');

        //var lines = await System.IO.File.ReadAllLinesAsync(csvPath);
        var chunkSize = lines.Length / threads;
        var chunks = lines.Chunk(chunkSize);

        int i = 0;
        foreach (var chunk in chunks)
        {
            var task = Task.Run(async () =>
            {
                foreach (var line in chunk)
                {
                    try
                    {
                        var split = line.Split(',');
                        await _service.Do(new Data()
                        {
                            Code = split[1],
                            Id = Guid.TryParse(split[0], out var result) ? result : Guid.NewGuid(),
                        });
                    }
                    catch (Exception)
                    {
                    }
                }
            });
            var res = _container.Tasks.TryAdd(i++, task);
            ;
        }

        //foreach (var task in _container.Tasks)
        //{
        //    var asd = Task.Run(() => task.Value.Start());
        //    _container.Tasks.TryAdd(i++, asd);
        //}

        //await Parallel.ForEachAsync(chunks, async (chunk, ct) =>
        //{
        //    foreach (var line in chunk)
        //    {
        //        try
        //        {
        //            var split = line.Split(',');
        //            await _service.Do(new Data()
        //            {
        //                Code = split[1],
        //                Id = Guid.TryParse(split[0], out var result) ? result : Guid.NewGuid(),
        //            });
        //        }
        //        catch (Exception)
        //        {
        //        }
        //    }
        //});

        sw.Stop();
        return sw.ElapsedMilliseconds.ToString();
    }
}

public interface IService
{
    Task<string> Do(Data data);
}

public class Service : IService
{
    private const int DELAY = 150;

    private readonly ILogger<Service> _logger;

    public Service(ILogger<Service> logger)
    {
        this._logger = logger;
    }

    public async Task<string> Do(Data data)
    {
        await Task.Delay(DELAY);
        _logger.LogInformation($"{data.Code}_{data.Id}");
        return $"{data.Code}_{data.Id}";
    }
}

public interface IContainer
{
    ConcurrentDictionary<int, Task> Tasks { get; }
}

public class Container : IContainer
{
    private ConcurrentDictionary<int, Task> _inner = new();

    public ConcurrentDictionary<int, Task> Tasks => _inner;
}

public class Data
{
    public required string Code { get; set; }
    public required Guid Id { get; set; }
}