using Microsoft.AspNetCore.Mvc;

namespace erp_api.Controllers.Admin;

public class UsersController : ApiControllerBase
{
    public UsersController()
    {
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IActionResult Get()
    {
        return null;
    }
}