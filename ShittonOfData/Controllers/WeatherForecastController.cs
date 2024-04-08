using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ShittonOfData.Controllers;
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IService _service;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IService service)
    {
        _logger = logger;
        this._service = service;
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
        return await _service.Do(data);
    }

    [HttpPost]
    [Route("file")]
    public async Task<string> TestFile([FromBody][Required] Data data)
    {
        return await _service.Do(data);
    }
}

public interface IService
{
    Task<string> Do(Data data);
}

public class Service : IService
{
    private const int DELAY = 150;

    public async Task<string> Do(Data data)
    {
        await Task.Delay(DELAY);
        return $"{data.Code}_{data.Id}";
    }
}

public class Data
{
    public required string Code { get; set; }
    public required Guid Id { get; set; }
}