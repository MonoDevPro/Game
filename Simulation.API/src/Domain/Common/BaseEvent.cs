using MediatR;

namespace GameWeb.Domain.Common;

public abstract record BaseEvent(int Id) : INotification;
