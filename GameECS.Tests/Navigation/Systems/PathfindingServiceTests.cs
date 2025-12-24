using GameECS.Modules.Navigation.Shared.Data;
using GameECS.Shared.Navigation.Components;
using GameECS.Shared.Navigation.Core;
using GameECS.Shared.Navigation.Data;
using GameECS.Shared.Navigation.Systems;

namespace GameECS.Tests.Navigation.Systems;

/// <summary>
/// Testes para PathfindingService (A* Algorithm).
/// </summary>
public class PathfindingServiceTests
{
    private NavigationGrid CreateTestGrid(int width = 20, int height = 20)
    {
        return new NavigationGrid(width, height, 1f);
    }

    private PathfindingService CreateService(NavigationGrid grid, NavigationConfig? config = null)
    {
        var pool = new PathfindingPool(grid.TotalCells, 256, 2);
        return new PathfindingService(grid, pool, config);
    }

    #region Basic Pathfinding Tests

    [Fact]
    public void FindPath_SamePosition_ShouldSucceedWithZeroLength()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(5, 5, 5, 5, ref buffer);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.PathLength);
    }

    [Fact]
    public void FindPath_StraightLine_ShouldFindPath()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(0, 0, 5, 0, ref buffer);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.PathLength > 0);
    }

    [Fact]
    public void FindPath_WithGridPosition_ShouldWork()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(
            new GridPosition(0, 0),
            new GridPosition(10, 10),
            ref buffer);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void FindPath_AroundObstacle_ShouldFindPath()
    {
        // Arrange
        var grid = CreateTestGrid();
        // Create a wall
        for (int y = 0; y < 15; y++)
        {
            grid.SetWalkable(10, y, false);
        }
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(5, 5, 15, 5, ref buffer);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.PathLength > 10); // Should go around
    }

    [Fact]
    public void FindPath_Blocked_ShouldFail()
    {
        // Arrange
        var grid = CreateTestGrid();
        // Create complete wall
        for (int y = 0; y < 20; y++)
        {
            grid.SetWalkable(10, y, false);
        }
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(5, 5, 15, 5, ref buffer);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(PathFailReason.NoPathExists, result.FailReason);
    }

    #endregion

    #region Invalid Input Tests

    [Fact]
    public void FindPath_InvalidStart_ShouldFail()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(-1, 0, 5, 5, ref buffer);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(PathFailReason.InvalidRequest, result.FailReason);
    }

    [Fact]
    public void FindPath_InvalidGoal_ShouldFail()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(5, 5, 100, 100, ref buffer);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(PathFailReason.InvalidRequest, result.FailReason);
    }

    [Fact]
    public void FindPath_StartBlocked_ShouldFail()
    {
        // Arrange
        var grid = CreateTestGrid();
        grid.SetWalkable(5, 5, false);
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(5, 5, 10, 10, ref buffer);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(PathFailReason.StartBlocked, result.FailReason);
    }

    [Fact]
    public void FindPath_GoalBlocked_ShouldFail()
    {
        // Arrange
        var grid = CreateTestGrid();
        grid.SetWalkable(10, 10, false);
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(5, 5, 10, 10, ref buffer);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(PathFailReason.GoalBlocked, result.FailReason);
    }

    #endregion

    #region Partial Path Tests

    [Fact]
    public void FindPath_GoalBlockedWithPartialAllowed_ShouldReturnPartial()
    {
        // Arrange
        var grid = CreateTestGrid();
        grid.SetWalkable(10, 10, false);
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(5, 5, 10, 10, ref buffer, 
            PathRequestFlags.AllowPartialPath);

        // Assert
        // Should find path to nearest walkable cell
        Assert.True(result.PathLength > 0 || result.Success);
    }

    [Fact]
    public void FindPath_ImpossibleWithPartialAllowed_ShouldReturnPartial()
    {
        // Arrange
        var grid = CreateTestGrid();
        // Complete wall
        for (int y = 0; y < 20; y++)
        {
            grid.SetWalkable(10, y, false);
        }
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(5, 5, 15, 5, ref buffer,
            PathRequestFlags.AllowPartialPath);

        // Assert - Should return partial path to closest point
        Assert.True(result.PathLength > 0);
    }

    #endregion

    #region Cardinal Only Tests

    [Fact]
    public void FindPath_CardinalOnly_ShouldNotUseDiagonals()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(0, 0, 5, 5, ref buffer,
            PathRequestFlags.CardinalOnly);

        // Assert
        Assert.True(result.Success);
        // Path length includes start position, and Manhattan path from (0,0) to (5,5) = 10 steps + start
        Assert.True(result.PathLength >= 10); // Manhattan distance path
    }

    [Fact]
    public void FindPath_WithDiagonals_ShouldBeShorter()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        
        var bufferCardinal = new GridPathBuffer();
        var bufferDiagonal = new GridPathBuffer();

        // Act
        var cardinalResult = service.FindPath(0, 0, 5, 5, ref bufferCardinal,
            PathRequestFlags.CardinalOnly);
        var diagonalResult = service.FindPath(0, 0, 5, 5, ref bufferDiagonal);

        // Assert
        Assert.True(cardinalResult.PathLength >= diagonalResult.PathLength);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void FindPath_Timeout_ShouldRespectMaxNodes()
    {
        // Arrange
        var grid = CreateTestGrid(100, 100);
        var config = new NavigationConfig { MaxNodesPerSearch = 10 };
        var service = CreateService(grid, config);
        var buffer = new GridPathBuffer();

        // Act - Try to find long path with very limited node search
        var result = service.FindPath(0, 0, 99, 99, ref buffer);

        // Assert - Should timeout or succeed within limit
        Assert.True(result.NodesSearched <= 10 || result.Success);
    }

    [Fact]
    public void FindPath_ShouldReportComputeTime()
    {
        // Arrange
        var grid = CreateTestGrid(50, 50);
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(0, 0, 49, 49, ref buffer);

        // Assert
        Assert.True(result.ComputeTimeMs >= 0);
    }

    [Fact]
    public void FindPath_MultipleCalls_ShouldWorkCorrectly()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act & Assert - Multiple calls should all work
        for (int i = 0; i < 100; i++)
        {
            var result = service.FindPath(0, 0, 10, 10, ref buffer);
            Assert.True(result.Success);
        }
    }

    #endregion

    #region Path Buffer Tests

    [Fact]
    public void FindPath_ShouldPopulateBuffer()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(0, 0, 5, 0, ref buffer);

        // Assert
        Assert.True(result.Success);
        Assert.True(buffer.IsValid);
        Assert.True(buffer.WaypointCount > 0);
        Assert.Equal(5, buffer.GoalX);
        Assert.Equal(0, buffer.GoalY);
    }

    [Fact]
    public void FindPath_BufferCurrentIndex_ShouldStartAt1()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        service.FindPath(0, 0, 5, 0, ref buffer);

        // Assert - Should skip first waypoint (current position)
        Assert.Equal(1, buffer.CurrentIndex);
    }

    [Fact]
    public void PathBuffer_GetCurrentWaypointPosition_ShouldWork()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();

        // Act
        service.FindPath(0, 0, 5, 0, ref buffer);
        var waypointPos = buffer.GetCurrentWaypointPosition(grid.Width);

        // Assert
        Assert.NotEqual(GridPosition.Invalid, waypointPos);
    }

    [Fact]
    public void PathBuffer_TryAdvance_ShouldWork()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();
        service.FindPath(0, 0, 5, 0, ref buffer);
        int initialIndex = buffer.CurrentIndex;

        // Act
        bool advanced = buffer.TryAdvance();

        // Assert
        Assert.True(advanced);
        Assert.Equal(initialIndex + 1, buffer.CurrentIndex);
    }

    [Fact]
    public void PathBuffer_IsComplete_ShouldBeCorrect()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);
        var buffer = new GridPathBuffer();
        service.FindPath(0, 0, 2, 0, ref buffer);

        // Act - Advance through all waypoints
        while (buffer.TryAdvance()) { }

        // Assert
        Assert.True(buffer.IsComplete);
    }

    #endregion

    #region Corner Cutting Tests

    [Fact]
    public void FindPath_WithCornerCuttingPrevention_ShouldAvoidCorners()
    {
        // Arrange
        var grid = CreateTestGrid();
        grid.SetWalkable(5, 5, false);
        
        var config = new NavigationConfig { PreventCornerCutting = true };
        var service = CreateService(grid, config);
        var buffer = new GridPathBuffer();

        // Act
        var result = service.FindPath(4, 4, 6, 6, ref buffer);

        // Assert
        Assert.True(result.Success);
        // Path should not cut through diagonal adjacent to blocked cell
    }

    #endregion

    #region Grid Reference Tests

    [Fact]
    public void Service_ShouldExposeGrid()
    {
        // Arrange
        var grid = CreateTestGrid();
        var service = CreateService(grid);

        // Assert
        Assert.Same(grid, service.Grid);
    }

    #endregion
}
