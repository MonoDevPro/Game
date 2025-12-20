using Game.ECS.Navigation.Shared.Core;

namespace Game.ECS.Navigation.Tests.Core;

/// <summary>
/// Testes para PathfindingContext e BinaryHeap.
/// </summary>
public class PathfindingContextTests
{
    #region PathfindingContext Tests

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var ctx = new PathfindingContext(100, 256);

        // Assert
        Assert.Equal(100, ctx.NodeCapacity);
        Assert.Equal(256, ctx.TempPathCapacity);
        Assert.Equal(0, ctx.OpenCount);
        Assert.Equal(0, ctx.Generation);
    }

    [Fact]
    public void Reset_ShouldIncrementGeneration()
    {
        // Arrange
        var ctx = new PathfindingContext(100);
        int initialGen = ctx.Generation;

        // Act
        ctx.Reset();

        // Assert
        Assert.Equal(initialGen + 1, ctx.Generation);
        Assert.Equal(0, ctx.OpenCount);
    }

    [Fact]
    public void Reset_ShouldClearClosedBits()
    {
        // Arrange
        var ctx = new PathfindingContext(100);
        ctx.MarkClosed(0);
        ctx.MarkClosed(50);

        // Act
        ctx.Reset();

        // Assert
        Assert.False(ctx.IsClosed(0));
        Assert.False(ctx.IsClosed(50));
    }

    [Fact]
    public void MarkClosed_ShouldSetBit()
    {
        // Arrange
        var ctx = new PathfindingContext(100);

        // Act
        ctx.MarkClosed(42);

        // Assert
        Assert.True(ctx.IsClosed(42));
        Assert.False(ctx.IsClosed(41));
        Assert.False(ctx.IsClosed(43));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(127)]
    public void MarkClosed_ShouldWorkForDifferentBitPositions(int index)
    {
        // Arrange
        var ctx = new PathfindingContext(200);

        // Act
        ctx.MarkClosed(index);

        // Assert
        Assert.True(ctx.IsClosed(index));
    }

    #endregion

    #region BinaryHeap Tests

    [Fact]
    public void BinaryHeap_IsEmpty_ShouldReturnTrueForEmptyHeap()
    {
        // Arrange
        var ctx = new PathfindingContext(100);

        // Assert
        Assert.True(BinaryHeap.IsEmpty(ctx));
    }

    [Fact]
    public void BinaryHeap_Push_ShouldIncreaseCount()
    {
        // Arrange
        var ctx = new PathfindingContext(100);
        SetupNode(ctx, 0, 1f);

        // Act
        BinaryHeap.Push(ctx, 0);

        // Assert
        Assert.Equal(1, ctx.OpenCount);
        Assert.False(BinaryHeap.IsEmpty(ctx));
    }

    [Fact]
    public void BinaryHeap_Pop_ShouldReturnMinFCost()
    {
        // Arrange
        var ctx = new PathfindingContext(100);
        SetupNode(ctx, 0, 3f);
        SetupNode(ctx, 1, 1f); // Minimum
        SetupNode(ctx, 2, 2f);

        BinaryHeap.Push(ctx, 0);
        BinaryHeap.Push(ctx, 1);
        BinaryHeap.Push(ctx, 2);

        // Act
        int first = BinaryHeap.Pop(ctx);

        // Assert
        Assert.Equal(1, first); // Node with FCost = 1
    }

    [Fact]
    public void BinaryHeap_Pop_ShouldMaintainHeapProperty()
    {
        // Arrange
        var ctx = new PathfindingContext(100);
        float[] costs = { 5f, 1f, 3f, 2f, 4f };
        
        for (int i = 0; i < costs.Length; i++)
        {
            SetupNode(ctx, i, costs[i]);
            BinaryHeap.Push(ctx, i);
        }

        // Act & Assert - Should pop in ascending order
        float previousCost = 0f;
        while (!BinaryHeap.IsEmpty(ctx))
        {
            int nodeIdx = BinaryHeap.Pop(ctx);
            float currentCost = ctx.Nodes[nodeIdx].FCost;
            Assert.True(currentCost >= previousCost, $"Heap property violated: {currentCost} < {previousCost}");
            previousCost = currentCost;
        }
    }

    [Fact]
    public void BinaryHeap_LargeNumberOfElements_ShouldMaintainOrder()
    {
        // Arrange
        var ctx = new PathfindingContext(1000);
        var random = new Random(42);
        
        for (int i = 0; i < 500; i++)
        {
            SetupNode(ctx, i, (float)random.NextDouble() * 100);
            BinaryHeap.Push(ctx, i);
        }

        // Act & Assert
        float previousCost = float.MinValue;
        while (!BinaryHeap.IsEmpty(ctx))
        {
            int nodeIdx = BinaryHeap.Pop(ctx);
            float currentCost = ctx.Nodes[nodeIdx].FCost;
            Assert.True(currentCost >= previousCost);
            previousCost = currentCost;
        }
    }

    private static void SetupNode(PathfindingContext ctx, int index, float gCost, float hCost = 0f)
    {
        ctx.Nodes[index] = new PathNode
        {
            X = index % 10,
            Y = index / 10,
            GCost = gCost,
            HCost = hCost,
            ParentIndex = -1,
            Generation = ctx.Generation
        };
    }

    #endregion
}

/// <summary>
/// Testes para PathfindingPool.
/// </summary>
public class PathfindingPoolTests
{
    [Fact]
    public void Constructor_ShouldPrewarmPool()
    {
        // Arrange & Act
        var pool = new PathfindingPool(100, 256, preWarmCount: 4);

        // Assert - Should be able to rent 4 without creating new
        var contexts = new List<PathfindingContext>();
        for (int i = 0; i < 4; i++)
        {
            contexts.Add(pool.Rent());
        }

        Assert.Equal(4, contexts.Count);
    }

    [Fact]
    public void Rent_ShouldReturnContext()
    {
        // Arrange
        var pool = new PathfindingPool(100, 256);

        // Act
        var ctx = pool.Rent();

        // Assert
        Assert.NotNull(ctx);
        Assert.Equal(100, ctx.NodeCapacity);
        Assert.Equal(256, ctx.TempPathCapacity);
    }

    [Fact]
    public void Rent_ShouldResetContext()
    {
        // Arrange
        var pool = new PathfindingPool(100, 256, preWarmCount: 1);
        var ctx = pool.Rent();
        ctx.MarkClosed(10);
        BinaryHeap.Push(ctx, 0);
        pool.Return(ctx);

        // Act
        var rented = pool.Rent();

        // Assert
        Assert.Equal(0, rented.OpenCount);
        Assert.False(rented.IsClosed(10));
    }

    [Fact]
    public void Return_ShouldMakeContextAvailable()
    {
        // Arrange
        var pool = new PathfindingPool(100, 256, preWarmCount: 1);
        var ctx = pool.Rent();

        // Act
        pool.Return(ctx);
        var rented = pool.Rent();

        // Assert
        Assert.NotNull(rented);
    }

    [Fact]
    public async Task Pool_ShouldBeThreadSafe()
    {
        // Arrange
        var pool = new PathfindingPool(100, 256, preWarmCount: 8);
        var contexts = new System.Collections.Concurrent.ConcurrentBag<PathfindingContext>();
        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act - Multiple threads renting and returning
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var ctx = pool.Rent();
                    contexts.Add(ctx);
                    Thread.Sleep(1); // Simulate work
                    pool.Return(ctx);
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(errors);
    }
}

/// <summary>
/// Testes para PathNode.
/// </summary>
public class PathNodeTests
{
    [Fact]
    public void FCost_ShouldBeSumOfGAndH()
    {
        // Arrange
        var node = new PathNode
        {
            GCost = 5f,
            HCost = 3f
        };

        // Assert
        Assert.Equal(8f, node.FCost);
    }

    [Fact]
    public void IsValidForGeneration_ShouldCompareGenerations()
    {
        // Arrange
        var node = new PathNode { Generation = 5 };

        // Assert
        Assert.True(node.IsValidForGeneration(5));
        Assert.False(node.IsValidForGeneration(4));
        Assert.False(node.IsValidForGeneration(6));
    }

    [Fact]
    public void GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var node1 = new PathNode { X = 5, Y = 10 };
        var node2 = new PathNode { X = 5, Y = 10 };

        // Assert
        Assert.Equal(node1.GetHashCode(), node2.GetHashCode());
    }
}
