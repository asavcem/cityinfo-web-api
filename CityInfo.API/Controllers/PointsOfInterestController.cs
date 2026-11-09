using AutoMapper;
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.Mvc;

namespace CityInfo.API.Controllers
{
    [Route("api/cities/{cityId}/pointsofinterest")]
    [ApiController]
    public class PointsOfInterestController(ICityInfoRepository cityInfoRepository,
        IMailService mailService,
        IMapper mapper) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointOfInterestDto>>> GetPointsOfInterest(int cityID)
        {
            if (!await cityInfoRepository.CityExistsAsync(cityID))
            {
                return NotFound();
            }

            var pointsOfInterestForCity = await cityInfoRepository
                .GetPointsOfInterestForCityAsync(cityID);

            return Ok(mapper.Map<IEnumerable<PointOfInterestDto>>(pointsOfInterestForCity));

        }

        [HttpGet("{pointOfInterestId}", Name = "GetPointOfInterest")]
        public async Task<ActionResult<PointOfInterestDto>> GetPointOfInterest(int cityId,
            int pointOfInterestId)
        {
            if (!await cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterest = await cityInfoRepository
                .GetPointsOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointOfInterest is null)
            {
                return NotFound();
            }

            return Ok(mapper.Map<PointOfInterestDto>(pointOfInterest));

        }
      

        [HttpPost]
        public async Task<ActionResult<PointOfInterestDto>> CreatePointOfInterest(int cityId,
            PointOfInterestForCreationDto pointOfInteret)
        {
            if (!await cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }
            var finalPointOfInteres = mapper.Map<Entities.PointOfInterest>(pointOfInteret);

            await cityInfoRepository.AddPointOfInterestForCityAsync(cityId,
                finalPointOfInteres);
            await cityInfoRepository.SaveChangesAsync();

            var createdPointOfInterestToReturn = mapper.Map<PointOfInterestDto>(finalPointOfInteres);

            return CreatedAtRoute("GetPointOfInterest",
                new
                {
                    cityId,
                    pointOfInterestId = createdPointOfInterestToReturn.Id
                }, createdPointOfInterestToReturn);
        }

        [HttpDelete("{pointOfInterestId}")]
        public async Task<ActionResult> DeletePointOfInterest(int cityId, int pointOfInterestId)
        {
            if (!await cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterstEntity = await cityInfoRepository
                .GetPointsOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointOfInterstEntity is null)
            {
                return NotFound();
            }

            cityInfoRepository.DeletePointOfInterest(pointOfInterstEntity);
            await cityInfoRepository.SaveChangesAsync();

            mailService.Send("Point of interest deleted.",
                $"Point of interest {pointOfInterstEntity.Name} with id {pointOfInterstEntity.Id}");

            return NoContent();
        }

        [HttpPut("{pointOfInterestId}")]
        public async Task<ActionResult> UpdatePointOfInterest(int cityId,
            int pointOfInterestId,
            PointOfInterestForUpdateDto pointOfInterest)
        {
            if(!await cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

           
            var pointOfInterestEntity = await cityInfoRepository
                .GetPointsOfInterestForCityAsync(cityId, pointOfInterestId);


            if (pointOfInterestEntity is null)
            {
                return NotFound();
            }

            mapper.Map(pointOfInterest, pointOfInterestEntity);
            await cityInfoRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{pointOfInterestId}")]
        public async Task<ActionResult> PartiallUpdatePointOfInterest(int cityId, int pointOfInterestId,
            JsonPatchDocument<PointOfInterestForUpdateDto> patchDocument)
        {
            
            if (!await cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = await cityInfoRepository
                .GetPointsOfInterestForCityAsync (cityId, pointOfInterestId);

            if (pointOfInterestEntity is null)
            {
                return NotFound();
            }

            var pointOfInterestToPatch = mapper
                 .Map<PointOfInterestForUpdateDto>(pointOfInterestEntity);

            patchDocument.ApplyTo(pointOfInterestToPatch, JsonPatchError =>
            {
                var key = JsonPatchError.AffectedObject.GetType().Name;
                ModelState.AddModelError(key, JsonPatchError.ErrorMessage);
            });

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if(!TryValidateModel(pointOfInterestToPatch))
            {
                return BadRequest(ModelState);
            }

            mapper.Map(pointOfInterestToPatch, pointOfInterestEntity);
            await cityInfoRepository.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("bulk")]
        public async Task<ActionResult> BulkUpdatePointsOfInteresDescription(int cityId,
            PointsOfInterestBulkUpdateDto pointsOfInterestBulkUpdateDto)
        {
            if (!await cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var affectedRows = await cityInfoRepository
                .UpdatePointsOfInterestDescriptionForCityAsync(cityId,
                pointsOfInterestBulkUpdateDto.NewDescription);

            return Ok(new { UpdatedCount = affectedRows });
        }
        
    }
}
