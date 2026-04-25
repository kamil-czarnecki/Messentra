using Messentra.Features.Explorer.MessageGrid;
using Shouldly;
using Xunit;

namespace Messentra.UnitTests.Features.Explorer.MessageGrid;

public sealed class DefaultColumnsShould
{
    [Fact]
    public void HaveFiveDefaultColumns()
    {
        DefaultColumns.DefaultView.Columns.Count.ShouldBe(5);
    }

    [Fact]
    public void HaveSeqAsFirstAndNonRemovable()
    {
        var seq = DefaultColumns.DefaultView.Columns[0];
        seq.Id.ShouldBe("seq");
        seq.IsRemovable.ShouldBeFalse();
        seq.Source.ShouldBe(ColumnSource.BrokerProperty);
        seq.PropertyKey.ShouldBe("SequenceNumber");
    }

    [Fact]
    public void HaveAllOtherDefaultColumnsRemovable()
    {
        DefaultColumns.DefaultView.Columns.Skip(1).ShouldAllBe(c => c.IsRemovable);
    }

    [Fact]
    public void HaveTwoDlqColumns()
    {
        DefaultColumns.DlqColumns.Count.ShouldBe(2);
    }

    [Fact]
    public void HaveDlqColumnsNonRemovable()
    {
        DefaultColumns.DlqColumns.ShouldAllBe(c => !c.IsRemovable);
    }

    [Fact]
    public void HaveDefaultViewAsBuiltIn()
    {
        DefaultColumns.DefaultView.IsBuiltIn.ShouldBeTrue();
        DefaultColumns.DefaultView.Id.ShouldBe("default");
    }

    [Fact]
    public void HaveColumnsInAscendingOrder()
    {
        var orders = DefaultColumns.DefaultView.Columns.Select(c => c.Order).ToList();
        orders.ShouldBe(orders.OrderBy(x => x).ToList());
    }
}