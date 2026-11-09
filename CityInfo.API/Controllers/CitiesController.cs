using AutoMapper;
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CityInfo.API.Controllers
{
    [ApiController]
    [Route("api/cities")]
    public class CitiesController(ICityInfoRepository cityInfoRepository,
        IMapper mapper) : ControllerBase
    {
        [HttpGet()]
        public async Task<ActionResult<IEnumerable<CityDto>>> GetCities(string? name,
            string? searchQuery,
            CancellationToken cancellationToken = default)
        {
            var cityEntities = await cityInfoRepository.GetCitiesReadOnlyAsync(name,
                searchQuery, 
                cancellationToken);

            return Ok(mapper.Map<IEnumerable<CityWithoutPointsOfInterestDto>>(cityEntities));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCity(int id, bool includePointsOfInterest = false)
        {
            var city = await cityInfoRepository.GetCityAsync(id,
                includePointsOfInterest);

            if (city == null)
            {
                return NotFound();
            }

            if(includePointsOfInterest)
            {
                return Ok(mapper.Map<CityDto>(city));
            }

            return Ok(mapper.Map<CityWithoutPointsOfInterestDto>(city));
        }
    }
}
