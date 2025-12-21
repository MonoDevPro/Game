using Game.ECS.Shared.Components.Navigation;
using Game.ECS.Shared.Core.Navigation;

namespace Game.ECS.Navigation.Tests.Core;

/// <summary>
/// Testes para NavigationGrid.
/// </summary>
public class NavigationGridTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateGridWithCorrectDimensions()
    {
        // Arrange & Act
        var grid = new NavigationGrid(100, 50, 1f);

        // Assert
        Assert.Equal(100, grid.Width);
        Assert.Equal(50, grid.Height);
        Assert.Equal(1f, grid.CellSize);
        Assert.Equal(5000, grid.TotalCells);
    }

    [Fact]
    public void Constructor_AllCells_ShouldBeWalkableByDefault()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);

        // Assert
        for (var y = 0; y < 10; y++)
        for (var x = 0; x < 10; x++)
            Assert.True(grid.IsWalkable(x, y));
    }

    #endregion

    #region Coordinate Conversion Tests

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(5, 0, 5)]
    [InlineData(0, 1, 10)]
    [InlineData(5, 5, 55)]
    [InlineData(9, 9, 99)]
    public void CoordToIndex_ShouldConvertCorrectly(int x, int y, int expectedIndex)
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);

        // Act
        int index = grid.CoordToIndex(x, y);

        // Assert
        Assert.Equal(expectedIndex, index);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(5, 5, 0)]
    [InlineData(10, 0, 1)]
    [InlineData(55, 5, 5)]
    public void IndexToCoord_ShouldConvertCorrectly(int index, int expectedX, int expectedY)
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);

        // Act
        var (x, y) = grid.IndexToCoord(index);

        // Assert
        Assert.Equal(expectedX, x);
        Assert.Equal(expectedY, y);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(9, 9, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(10, 0, false)]
    [InlineData(0, 10, false)]
    public void IsValidCoord_ShouldValidateCorrectly(int x, int y, bool expected)
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);

        // Act
        bool result = grid.IsValidCoord(x, y);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValid_WithGridPosition_ShouldValidateCorrectly()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);

        // Assert
        Assert.True(grid.IsValid(new GridPosition(5, 5)));
        Assert.False(grid.IsValid(new GridPosition(-1, 0)));
        Assert.False(grid.IsValid(GridPosition.Invalid));
    }

    #endregion

    #region Walkability Tests

    [Fact]
    public void SetWalkable_ShouldBlockCell()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);

        // Act
        grid.SetWalkable(5, 5, false);

        // Assert
        Assert.False(grid.IsWalkable(5, 5));
        Assert.True(grid.IsWalkable(4, 4)); // Vizinho ainda é walkable
    }

    [Fact]
    public void SetWalkable_OutOfBounds_ShouldNotThrow()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);

        // Act & Assert - Should not throw
        grid.SetWalkable(-1, 0, false);
        grid.SetWalkable(100, 100, false);
    }

    [Fact]
    public void IsWalkable_OutOfBounds_ShouldReturnFalse()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);

        // Assert
        Assert.False(grid.IsWalkable(-1, 0));
        Assert.False(grid.IsWalkable(0, -1));
        Assert.False(grid.IsWalkable(10, 0));
        Assert.False(grid.IsWalkable(0, 10));
    }

    [Fact]
    public void GetMovementCost_ShouldReturnCorrectValues()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);

        // Act
        grid.SetCost(5, 5, 128); // Half cost
        grid.SetWalkable(6, 6, false);

        // Assert
        Assert.True(grid.GetMovementCost(5, 5) > 1f); // Custom cost
        Assert.Equal(float.MaxValue, grid.GetMovementCost(6, 6)); // Blocked
        Assert.Equal(float.MaxValue, grid.GetMovementCost(-1, 0)); // Out of bounds
    }

    #endregion

    #region Occupancy Tests

    [Fact]
    public void TryOccupy_EmptyCell_ShouldSucceed()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);
        var pos = new GridPosition(5, 5);

        // Act
        bool result = grid.TryOccupy(pos, entityId: 1);

        // Assert
        Assert.True(result);
        Assert.True(grid.IsOccupied(5, 5));
        Assert.Equal(1, grid.GetOccupant(5, 5));
    }

    [Fact]
    public void TryOccupy_OccupiedCell_ShouldFail()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);
        var pos = new GridPosition(5, 5);
        grid.TryOccupy(pos, entityId: 1);

        // Act
        bool result = grid.TryOccupy(pos, entityId: 2);

        // Assert
        Assert.False(result);
        Assert.Equal(1, grid.GetOccupant(5, 5)); // Original occupant
    }

    [Fact]
    public void TryOccupy_BlockedCell_ShouldFail()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);
        grid.SetWalkable(5, 5, false);
        var pos = new GridPosition(5, 5);

        // Act
        bool result = grid.TryOccupy(pos, entityId: 1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Release_ShouldFreeCell()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);
        var pos = new GridPosition(5, 5);
        grid.TryOccupy(pos, entityId: 1);

        // Act
        bool result = grid.Release(pos, entityId: 1);

        // Assert
        Assert.True(result);
        Assert.False(grid.IsOccupied(5, 5));
        Assert.Equal(-1, grid.GetOccupant(5, 5));
    }

    [Fact]
    public void Release_WrongEntity_ShouldFail()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);
        var pos = new GridPosition(5, 5);
        grid.TryOccupy(pos, entityId: 1);

        // Act
        bool result = grid.Release(pos, entityId: 2);

        // Assert
        Assert.False(result);
        Assert.True(grid.IsOccupied(5, 5));
    }

    [Fact]
    public void TryMoveOccupancy_ShouldMoveAtomically()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);
        var from = new GridPosition(5, 5);
        var to = new GridPosition(6, 5);
        grid.TryOccupy(from, entityId: 1);

        // Act
        bool result = grid.TryMoveOccupancy(from, to, entityId: 1);

        // Assert
        Assert.True(result);
        Assert.False(grid.IsOccupied(5, 5));
        Assert.True(grid.IsOccupied(6, 5));
        Assert.Equal(1, grid.GetOccupant(6, 5));
    }

    [Fact]
    public void TryMoveOccupancy_DestinationOccupied_ShouldFail()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);
        var from = new GridPosition(5, 5);
        var to = new GridPosition(6, 5);
        grid.TryOccupy(from, entityId: 1);
        grid.TryOccupy(to, entityId: 2);

        // Act
        bool result = grid.TryMoveOccupancy(from, to, entityId: 1);

        // Assert
        Assert.False(result);
        Assert.True(grid.IsOccupied(5, 5)); // Should still be occupied
        Assert.Equal(2, grid.GetOccupant(6, 5)); // Original occupant
    }

    [Fact]
    public void IsWalkableAndFree_ShouldCheckBothConditions()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);
        grid.TryOccupy(new GridPosition(5, 5), entityId: 1);
        grid.SetWalkable(6, 6, false);

        // Assert
        Assert.True(grid.IsWalkableAndFree(4, 4)); // Free and walkable
        Assert.False(grid.IsWalkableAndFree(5, 5)); // Occupied
        Assert.False(grid.IsWalkableAndFree(6, 6)); // Blocked
    }

    #endregion

    #region Dynamic Obstacles Tests

    [Fact]
    public void AddDynamicObstacle_ShouldBlockCell()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);

        // Act
        grid.AddDynamicObstacle(5, 5);

        // Assert
        Assert.False(grid.IsWalkable(5, 5));
    }

    [Fact]
    public void AddDynamicObstacle_WithRadius_ShouldBlockArea()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);

        // Act
        grid.AddDynamicObstacle(5, 5, radius: 1);

        // Assert
        Assert.False(grid.IsWalkable(5, 5));
        Assert.False(grid.IsWalkable(4, 5));
        Assert.False(grid.IsWalkable(6, 5));
        Assert.False(grid.IsWalkable(5, 4));
        Assert.False(grid.IsWalkable(5, 6));
    }

    [Fact]
    public void RemoveDynamicObstacle_ShouldRestoreCell()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);
        grid.AddDynamicObstacle(5, 5);

        // Act
        grid.RemoveDynamicObstacle(5, 5);

        // Assert
        Assert.True(grid.IsWalkable(5, 5));
    }

    [Fact]
    public void ClearDynamicLayer_ShouldClearAllDynamicObstacles()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);
        grid.AddDynamicObstacle(3, 3);
        grid.AddDynamicObstacle(5, 5);
        grid.AddDynamicObstacle(7, 7);

        // Act
        grid.ClearDynamicLayer();

        // Assert
        Assert.True(grid.IsWalkable(3, 3));
        Assert.True(grid.IsWalkable(5, 5));
        Assert.True(grid.IsWalkable(7, 7));
    }

    #endregion

    #region Bulk Operations Tests

    [Fact]
    public void SetRectangle_ShouldBlockArea()
    {
        // Arrange
        var grid = new NavigationGrid(20, 20);

        // Act
        grid.SetRectangle(5, 5, 3, 3, walkable: false);

        // Assert
        for (var y = 5; y < 8; y++)
        for (var x = 5; x < 8; x++)
            Assert.False(grid.IsWalkable(x, y), $"Cell ({x},{y}) should be blocked");

        Assert.True(grid.IsWalkable(4, 5));
        Assert.True(grid.IsWalkable(8, 5));
    }

    [Fact]
    public void SetCircle_ShouldBlockCircularArea()
    {
        // Arrange
        var grid = new NavigationGrid(20, 20);

        // Act
        grid.SetCircle(10, 10, radius: 2, walkable: false);

        // Assert
        Assert.False(grid.IsWalkable(10, 10)); // Center
        Assert.False(grid.IsWalkable(10, 8)); // Top
        Assert.False(grid.IsWalkable(10, 12)); // Bottom
        Assert.False(grid.IsWalkable(8, 10)); // Left
        Assert.False(grid.IsWalkable(12, 10)); // Right
    }

    [Fact]
    public void LoadFromBytes_ToBytes_ShouldRoundTrip()
    {
        // Arrange
        var original = new NavigationGrid(10, 10);
        original.SetWalkable(5, 5, false);
        original.SetWalkable(3, 7, false);

        // Act
        byte[] data = original.ToBytes();
        var restored = new NavigationGrid(10, 10);
        restored.LoadFromBytes(data);

        // Assert
        Assert.False(restored.IsWalkable(5, 5));
        Assert.False(restored.IsWalkable(3, 7));
        Assert.True(restored.IsWalkable(0, 0));
    }

    [Fact]
    public void LoadFromBytes_WrongSize_ShouldThrow()
    {
        // Arrange
        var grid = new NavigationGrid(10, 10);
        var wrongSizeData = new byte[50]; // Wrong size

        // Act & Assert
        Assert.Throws<ArgumentException>(() => grid.LoadFromBytes(wrongSizeData));
    }

    #endregion

    #region Direction Constants Tests

    [Fact]
    public void DirectionArrays_ShouldHaveCorrectLength()
    {
        Assert.Equal(8, NavigationGrid.DirX.Length);
        Assert.Equal(8, NavigationGrid.DirY.Length);
        Assert.Equal(8, NavigationGrid.DirCost.Length);
        Assert.Equal(8, NavigationGrid.IsDiagonal.Length);
    }

    [Fact]
    public void DirectionCosts_DiagonalsShouldBeSqrt2()
    {
        // Assert
        for (var i = 0; i < 8; i++)
            if (NavigationGrid.IsDiagonal[i])
                Assert.InRange(NavigationGrid.DirCost[i], 1.41f, 1.42f);
            else
                Assert.Equal(1f, NavigationGrid.DirCost[i]);
    }

    #endregion
}