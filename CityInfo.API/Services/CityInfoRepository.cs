using CityInfo.API.DbContexts;
using CityInfo.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CityInfo.API.Services
{
    public class CityInfoRepository(CityInfoContext context) : ICityInfoRepository
    {
        public async Task<IEnumerable<City>> GetCitiesAsync()
        {
            return await context.Cities.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<IEnumerable<City>> GetCitiesReadOnlyAsync(CancellationToken cancellationToken)
        {
            return await context.Cities.AsNoTracking()
                .OrderBy(c => c.Name).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<City>> GetCitiesReadOnlyAsync(string? name, 
            string? searchQuery,
            CancellationToken cancellationToken)
        {
            if(string.IsNullOrEmpty(name)
                && string.IsNullOrWhiteSpace(searchQuery))
            {
                return await GetCitiesReadOnlyAsync(cancellationToken);
            }

            var collection = context.Cities as IQueryable<City>;

            if(!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
                collection = collection.Where(c => c.Name == name);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.Trim();
                collection = collection.Where(c => c.Name.Contains(searchQuery)
                    || (c.Description != null && c.Description.Contains(searchQuery)));
            }

            return await collection
                .AsNoTracking()
                .OrderBy(c => c.Name).ToListAsync(cancellationToken);
        }

        public async Task<City?> GetCityAsync(int cityId, bool includePointsOfInterest)
        {
            if (includePointsOfInterest)
            {
                return await context.Cities.Include(c => c.PointsOfInterest)
                    .Where(c => c.Id == cityId).FirstOrDefaultAsync();
            }

            return await context.Cities
                .Where(c => c.Id == cityId).FirstOrDefaultAsync();
        }

        public async Task<bool> CityExistsAsync(int cityID)
        {
            return await context.Cities.AnyAsync(c => c.Id == cityID);
        }

        public async Task AddPointOfInterestForCityAsync(int cityID,
            PointOfInterest pointOfInterest)
        {
            var city = await GetCityAsync(cityID, false);
            city?.PointsOfInterest.Add(pointOfInterest);
        }

        public async Task<PointOfInterest?> GetPointsOfInterestForCityAsync(int cityID,
            int pointOfInterestID)
        {

            return await context.PointsOfInterest
                .Where(p => p.CityId == cityID && p.Id == pointOfInterestID)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<PointOfInterest>> GetPointsOfInterestForCityAsync(int cityID)
        {
            return await context.PointsOfInterest
                .Where(p => p.CityId == cityID).ToListAsync();
        }

        public void DeletePointOfInterest(PointOfInterest pointOfInterest)
        {
            context.PointsOfInterest.Remove(pointOfInterest);
        }

        public async Task<int> UpdatePointsOfInterestDescriptionForCityAsync(int cityId,
            string newDescription)
        {
            return await context.PointsOfInterest
                .Where(p => p.CityId == cityId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Description, newDescription));

            //Update PointsOfInteres
            //Set Description = 'new Description'
            //Where CityId = cityId
        }

        public async Task<int> DeleteAllPointsOfInterestForCityAsync(int cityId)
        {
            return await context.PointsOfInterest
                .Where(p => p.CityId == cityId)
                .ExecuteDeleteAsync();

            //Delete From PointsOfInterest
            //Where CityId = cityId

        }

        public async Task<bool> SaveChangesAsync()
        {
            return (await context.SaveChangesAsync() >= 0);
        }
    }
}
