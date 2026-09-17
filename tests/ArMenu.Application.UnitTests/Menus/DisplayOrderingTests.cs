using ArMenu.Application.Menus;
using ArMenu.Domain.Common;

namespace ArMenu.Application.UnitTests.Menus;

public sealed class DisplayOrderingTests
{
    [Fact]
    public void Applies_positions_in_the_requested_order()
    {
        var entries = new[] { new Entry("a"), new Entry("b"), new Entry("c") };

        var result = DisplayOrdering.Apply(entries, ["c", "a", "b"], entry => entry.Id, SetOrder);

        result.IsSuccess.ShouldBeTrue();
        entries.Select(entry => (entry.Id, entry.DisplayOrder)).ShouldBe([("a", 1), ("b", 2), ("c", 0)]);
    }

    public static TheoryData<string[]> InvalidOrders { get; } = new()
    {
        new[] { "a", "b" },
        new[] { "a", "b", "b" },
        new[] { "a", "b", "x" },
        new[] { "a", "b", "c", "d" },
    };

    [Theory]
    [MemberData(nameof(InvalidOrders))]
    public void Rejects_orders_that_do_not_list_every_entry_exactly_once(string[] requestedOrder)
    {
        var entries = new[] { new Entry("a"), new Entry("b"), new Entry("c") };

        var result = DisplayOrdering.Apply(entries, requestedOrder, entry => entry.Id, SetOrder);

        result.Error.ShouldBe(MenuErrors.ReorderMismatch);
        entries.ShouldAllBe(entry => entry.DisplayOrder == -1);
    }

    private static Result SetOrder(Entry entry, int position)
    {
        entry.DisplayOrder = position;
        return Result.Success();
    }

    private sealed class Entry(string id)
    {
        public string Id { get; } = id;

        public int DisplayOrder { get; set; } = -1;
    }
}
