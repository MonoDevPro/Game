
using Game.ECS.Navigation.Shared.Components;

namespace GameECS.Tests.Navigation.Components;

/// <summary>
/// Testes para componentes compartilhados.
/// </summary>
public class SharedComponentsTests
{
    #region GridPosition Tests

    [Fact]
    public void GridPosition_Constructor_ShouldSetValues()
    {
        // Act
        var pos = new GridPosition(5, 10);

        // Assert
        Assert.Equal(5, pos.X);
        Assert.Equal(10, pos.Y);
    }

    [Fact]
    public void GridPosition_Zero_ShouldBeOrigin()
    {
        Assert.Equal(0, GridPosition.Zero.X);
        Assert.Equal(0, GridPosition.Zero.Y);
    }

    [Fact]
    public void GridPosition_Invalid_ShouldBeNegative()
    {
        Assert.Equal(-1, GridPosition.Invalid.X);
        Assert.Equal(-1, GridPosition.Invalid.Y);
    }

    [Fact]
    public void GridPosition_Equals_ShouldWork()
    {
        var pos1 = new GridPosition(5, 10);
        var pos2 = new GridPosition(5, 10);
        var pos3 = new GridPosition(5, 11);

        Assert.True(pos1.Equals(pos2));
        Assert.False(pos1.Equals(pos3));
        Assert.True(pos1 == pos2);
        Assert.True(pos1 != pos3);
    }

    [Fact]
    public void GridPosition_ManhattanDistance_ShouldCalculateCorrectly()
    {
        var pos1 = new GridPosition(0, 0);
        var pos2 = new GridPosition(3, 4);

        Assert.Equal(7, pos1.ManhattanDistanceTo(pos2));
    }

    [Fact]
    public void GridPosition_ChebyshevDistance_ShouldCalculateCorrectly()
    {
        var pos1 = new GridPosition(0, 0);
        var pos2 = new GridPosition(3, 4);

        Assert.Equal(4, pos1.ChebyshevDistanceTo(pos2));
    }

    [Fact]
    public void GridPosition_Addition_ShouldWork()
    {
        var pos1 = new GridPosition(3, 4);
        var pos2 = new GridPosition(2, 1);

        var result = pos1 + pos2;

        Assert.Equal(5, result.X);
        Assert.Equal(5, result.Y);
    }

    [Fact]
    public void GridPosition_Subtraction_ShouldWork()
    {
        var pos1 = new GridPosition(5, 5);
        var pos2 = new GridPosition(2, 1);

        var result = pos1 - pos2;

        Assert.Equal(3, result.X);
        Assert.Equal(4, result.Y);
    }

    [Fact]
    public void GridPosition_ToString_ShouldFormat()
    {
        var pos = new GridPosition(5, 10);
        Assert.Equal("(5, 10)", pos.ToString());
    }

    [Fact]
    public void GridPosition_GetHashCode_ShouldBeConsistent()
    {
        var pos1 = new GridPosition(5, 10);
        var pos2 = new GridPosition(5, 10);

        Assert.Equal(pos1.GetHashCode(), pos2.GetHashCode());
    }

    #endregion

    #region MovementDirection Tests

    [Theory]
    [InlineData(MovementDirection.North, 0, -1)]
    [InlineData(MovementDirection.South, 0, 1)]
    [InlineData(MovementDirection.East, 1, 0)]
    [InlineData(MovementDirection.West, -1, 0)]
    [InlineData(MovementDirection.NorthEast, 1, -1)]
    [InlineData(MovementDirection.SouthEast, 1, 1)]
    [InlineData(MovementDirection.SouthWest, -1, 1)]
    [InlineData(MovementDirection.NorthWest, -1, -1)]
    [InlineData(MovementDirection.None, 0, 0)]
    public void MovementDirection_ToOffset_ShouldReturnCorrectValues(
        MovementDirection dir, int expectedDx, int expectedDy)
    {
        var (dx, dy) = dir.ToOffset();

        Assert.Equal(expectedDx, dx);
        Assert.Equal(expectedDy, dy);
    }

    [Theory]
    [InlineData(MovementDirection.North, false)]
    [InlineData(MovementDirection.NorthEast, true)]
    [InlineData(MovementDirection.East, false)]
    [InlineData(MovementDirection.SouthEast, true)]
    [InlineData(MovementDirection.South, false)]
    [InlineData(MovementDirection.SouthWest, true)]
    [InlineData(MovementDirection.West, false)]
    [InlineData(MovementDirection.NorthWest, true)]
    public void MovementDirection_IsDiagonal_ShouldBeCorrect(MovementDirection dir, bool expected)
    {
        Assert.Equal(expected, dir.IsDiagonal());
    }

    [Theory]
    [InlineData(0, -1, MovementDirection.North)]
    [InlineData(1, -1, MovementDirection.NorthEast)]
    [InlineData(1, 0, MovementDirection.East)]
    [InlineData(1, 1, MovementDirection.SouthEast)]
    [InlineData(0, 1, MovementDirection.South)]
    [InlineData(-1, 1, MovementDirection.SouthWest)]
    [InlineData(-1, 0, MovementDirection.West)]
    [InlineData(-1, -1, MovementDirection.NorthWest)]
    [InlineData(0, 0, MovementDirection.None)]
    public void MovementDirection_FromOffset_ShouldWork(int dx, int dy, MovementDirection expected)
    {
        var result = MovementDirectionExtensions.FromOffset(dx, dy);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MovementDirection_FromPositions_ShouldWork()
    {
        var from = new GridPosition(5, 5);
        var to = new GridPosition(6, 4);

        var result = MovementDirectionExtensions.FromPositions(from, to);

        Assert.Equal(MovementDirection.NorthEast, result);
    }

    [Theory]
    [InlineData(MovementDirection.North, MovementDirection.South)]
    [InlineData(MovementDirection.NorthEast, MovementDirection.SouthWest)]
    [InlineData(MovementDirection.East, MovementDirection.West)]
    [InlineData(MovementDirection.SouthEast, MovementDirection.NorthWest)]
    [InlineData(MovementDirection.South, MovementDirection.North)]
    [InlineData(MovementDirection.SouthWest, MovementDirection.NorthEast)]
    [InlineData(MovementDirection.West, MovementDirection.East)]
    [InlineData(MovementDirection.NorthWest, MovementDirection.SouthEast)]
    [InlineData(MovementDirection.None, MovementDirection.None)]
    public void MovementDirection_Opposite_ShouldReturnOpposite(
        MovementDirection dir, MovementDirection expected)
    {
        Assert.Equal(expected, dir.Opposite());
    }

    #endregion

    #region PathRequest Tests

    [Fact]
    public void PathRequest_Create_ShouldSetDefaults()
    {
        var request = PathRequest.Create(10, 20);

        Assert.Equal(10, request.TargetX);
        Assert.Equal(20, request.TargetY);
        Assert.Equal(PathPriority.Normal, request.Priority);
        Assert.Equal(PathRequestFlags.None, request.Flags);
    }

    [Fact]
    public void PathRequest_Create_WithOptions_ShouldSetValues()
    {
        var request = PathRequest.Create(10, 20, 
            PathPriority.High, 
            PathRequestFlags.AllowPartialPath | PathRequestFlags.CardinalOnly);

        Assert.Equal(PathPriority.High, request.Priority);
        Assert.True(request.Flags.HasFlag(PathRequestFlags.AllowPartialPath));
        Assert.True(request.Flags.HasFlag(PathRequestFlags.CardinalOnly));
    }

    [Fact]
    public void PathRequest_CreateFromGridPosition_ShouldWork()
    {
        var target = new GridPosition(15, 25);
        var request = PathRequest.Create(target);

        Assert.Equal(15, request.TargetX);
        Assert.Equal(25, request.TargetY);
    }

    #endregion

    #region PathState Tests

    [Fact]
    public void PathState_IsActive_ShouldBeCorrect()
    {
        Assert.True(new PathState { Status = PathStatus.Pending }.IsActive);
        Assert.True(new PathState { Status = PathStatus.Computing }.IsActive);
        Assert.True(new PathState { Status = PathStatus.Ready }.IsActive);
        Assert.True(new PathState { Status = PathStatus.Following }.IsActive);
        Assert.False(new PathState { Status = PathStatus.None }.IsActive);
        Assert.False(new PathState { Status = PathStatus.Completed }.IsActive);
        Assert.False(new PathState { Status = PathStatus.Failed }.IsActive);
    }

    [Fact]
    public void PathState_HasFailed_ShouldBeCorrect()
    {
        Assert.True(new PathState { Status = PathStatus.Failed }.HasFailed);
        Assert.False(new PathState { Status = PathStatus.Completed }.HasFailed);
    }

    [Fact]
    public void PathState_IsComplete_ShouldBeCorrect()
    {
        Assert.True(new PathState { Status = PathStatus.Completed }.IsComplete);
        Assert.False(new PathState { Status = PathStatus.Following }.IsComplete);
    }

    #endregion

    #region GridPathBuffer Tests

    [Fact]
    public void GridPathBuffer_Clear_ShouldResetValues()
    {
        var buffer = new GridPathBuffer
        {
            WaypointCount = 10,
            CurrentIndex = 5,
            GoalX = 100,
            GoalY = 200
        };

        buffer.Clear();

        Assert.Equal(0, buffer.WaypointCount);
        Assert.Equal(0, buffer.CurrentIndex);
        Assert.Equal(0, buffer.GoalX);
        Assert.Equal(0, buffer.GoalY);
    }

    [Fact]
    public void GridPathBuffer_IsValid_ShouldBeCorrect()
    {
        var buffer = new GridPathBuffer();
        Assert.False(buffer.IsValid);

        buffer.WaypointCount = 1;
        Assert.True(buffer.IsValid);
    }

    [Fact]
    public void GridPathBuffer_IsComplete_ShouldBeCorrect()
    {
        var buffer = new GridPathBuffer
        {
            WaypointCount = 5,
            CurrentIndex = 3
        };

        Assert.False(buffer.IsComplete);

        buffer.CurrentIndex = 5;
        Assert.True(buffer.IsComplete);
    }

    [Fact]
    public void GridPathBuffer_RemainingCount_ShouldBeCorrect()
    {
        var buffer = new GridPathBuffer
        {
            WaypointCount = 10,
            CurrentIndex = 3
        };

        Assert.Equal(7, buffer.RemainingCount);
    }

    [Fact]
    public void GridPathBuffer_SetAndGetWaypoint_ShouldWork()
    {
        var buffer = new GridPathBuffer { WaypointCount = 5 };
        
        buffer.SetWaypoint(0, 100);
        buffer.SetWaypoint(1, 200);

        Assert.Equal(100, buffer.GetWaypoint(0));
        Assert.Equal(200, buffer.GetWaypoint(1));
    }

    [Fact]
    public void GridPathBuffer_GetWaypoint_OutOfBounds_ShouldReturnNegative()
    {
        var buffer = new GridPathBuffer { WaypointCount = 2 };

        Assert.Equal(-1, buffer.GetWaypoint(5));
        Assert.Equal(-1, buffer.GetWaypoint(-1));
    }

    [Fact]
    public void GridPathBuffer_SetGoal_ShouldWork()
    {
        var buffer = new GridPathBuffer();
        buffer.SetGoal(new GridPosition(50, 75));

        Assert.Equal(50, buffer.GoalX);
        Assert.Equal(75, buffer.GoalY);
    }

    [Fact]
    public void GridPathBuffer_Goal_ShouldReturnGridPosition()
    {
        var buffer = new GridPathBuffer { GoalX = 10, GoalY = 20 };
        var goal = buffer.Goal;

        Assert.Equal(10, goal.X);
        Assert.Equal(20, goal.Y);
    }

    [Fact]
    public void GridPathBuffer_TryAdvance_ShouldWork()
    {
        var buffer = new GridPathBuffer { WaypointCount = 3, CurrentIndex = 0 };

        Assert.True(buffer.TryAdvance());
        Assert.Equal(1, buffer.CurrentIndex);

        Assert.True(buffer.TryAdvance());
        Assert.Equal(2, buffer.CurrentIndex);

        Assert.True(buffer.TryAdvance());
        Assert.Equal(3, buffer.CurrentIndex);

        Assert.False(buffer.TryAdvance()); // No more waypoints
    }

    #endregion
}
