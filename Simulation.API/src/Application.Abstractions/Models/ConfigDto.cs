using Application.Models.Options;

namespace Application.Models.Models;

public record  ConfigDto(AuthorityOptions Authority, NetworkOptions Network, WorldOptions World);
