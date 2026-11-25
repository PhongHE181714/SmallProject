namespace BE.BuildingBlocks.SharedKernel;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
