using CityInfo.API.Entities;
using System.Collections;

namespace CityInfo.API.Services
{
    public interface ICityInfoRepository
    {
        Task<IEnumerable<City>> GetCitiesAsync();

        Task<IEnumerable<City>> GetCitiesReadOnlyAsync(CancellationToken cancellationToken);

        Task<IEnumerable<City>> GetCitiesReadOnlyAsync(string? name, 
            string? searchQuery, 
            CancellationToken cancellationToken);


        Task<bool> CityExistsAsync(int cityID);

        Task<City?> GetCityAsync(int cityId,
            bool includePointsOfInterest);

        Task AddPointOfInterestForCityAsync(int cityID, 
            PointOfInterest pointOfInterest);

        Task<IEnumerable<PointOfInterest>> GetPointsOfInterestForCityAsync(int cityID);

        Task<PointOfInterest?> GetPointsOfInterestForCityAsync(int cityID,
            int pointOfInterestID);

        void DeletePointOfInterest(PointOfInterest pointOfInterest);

        Task<int> UpdatePointsOfInterestDescriptionForCityAsync(int cityId,
            string newDescription);

        Task<int> DeleteAllPointsOfInterestForCityAsync(int cityId);

        Task<bool> SaveChangesAsync();
    }
}
