using GameWeb.Application.Common.Interfaces;
using GameWeb.Application.Maps.Models;

namespace GameWeb.Application.Maps.Queries;

public record GetMapQuery(int Id) : IQuery<MapDto>;

public class GetMapQueryHandler(IApplicationDbContext context, IMapper mapper) 
    : IRequestHandler<GetMapQuery, MapDto>
{
    public async Task<MapDto> Handle(GetMapQuery request, CancellationToken ct)
    {
        var map =  await context.Maps.FindAsync([request.Id], ct)
                  ?? throw new KeyNotFoundException($"Map with ID {request.Id} not found.");
        return mapper.Map<MapDto>(map);
    }
}
