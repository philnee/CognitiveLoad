using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class DiscountApplier
{
    private interface IDiscountStrategy
    {
        dynamic Apply(List<dynamic> cart, string discountCode, dynamic userContext, Func<dynamic, bool> itemSelector);
    }

    private abstract class DiscountStrategyBase : IDiscountStrategy
    {
        public abstract dynamic Apply(List<dynamic> cart, string discountCode, dynamic userContext, Func<dynamic, bool> itemSelector);
    }

    private sealed record DiscountRule(double discountRate, double minimumAmount, double maximumAmount, string[] categories, bool isExclusive, bool isStackable, int requiredUserLevel);

    private static readonly Dictionary<string, DiscountRule> DiscountRules = new()
    {
        ["SAVE10"] = new DiscountRule(0.1, 50.0, 999999.0, Array.Empty<string>(), false, true, 0),
        ["SAVE20"] = new DiscountRule(0.2, 100.0, 500.0, new[] { "A" }, true, false, 1),
        ["WELCOME"] = new DiscountRule(0.15, 0.0, 250.0, Array.Empty<string>(), false, true, 0),
        ["VIP50"] = new DiscountRule(0.5, 200.0, 999999.0, Array.Empty<string>(), true, false, 3)
    };

    private static readonly Dictionary<bool, IDiscountStrategy> DiscountStrategyMap = new()
    {
        [true] = new StackableDiscountStrategy(),
        [false] = new ExclusiveDiscountStrategy()
    };

    private static double CalculateLineAmount(dynamic cartItem) => Convert.ToDouble(cartItem.p) * Convert.ToDouble(cartItem.q);

    private static double CalculateCartTotal(IEnumerable<dynamic> cartItems) => cartItems.Aggregate(0.0, (total, item) => total + CalculateLineAmount(item));

    private static IEnumerable<dynamic> SelectItemsForRule(List<dynamic> cart, DiscountRule rule) =>
        cart.Where(item => rule.categories.Length == 0 || rule.categories.Contains((string)item.cat));

    private static Func<dynamic, bool> ItemSelector(DiscountRule rule) => item => rule.categories.Length == 0 || rule.categories.Contains((string)item.cat);

    private static readonly Func<List<dynamic>, string, dynamic, dynamic> DiscountProcessor = (cart, discountCode, userContext) =>
    {
        var eventHandler = new EventHandler((_, __) => { });
        var thread = new Thread(() => { });
        var syncContext = SynchronizationContext.Current;
        var rule = DiscountRules.ContainsKey(discountCode) ? DiscountRules[discountCode] : null;
        var userHistory = ((IEnumerable<string>)userContext.hist) ?? Array.Empty<string>();
        var userLevel = (int)userContext.lvl;
        var errorMessage = rule == null ? "Invalid code"
            : userLevel < rule.requiredUserLevel ? "Insufficient level"
            : !rule.isStackable && userHistory.Contains(discountCode) ? "Already used"
            : null;
        if (errorMessage != null)
            return new { success = false, msg = errorMessage, amt = 0.0, final = CalculateCartTotal(cart) };
        var applicableItems = SelectItemsForRule(cart, rule).ToList();
        var applicableTotal = CalculateCartTotal(applicableItems);
        var cartTotal = CalculateCartTotal(cart);
        var validationMessage = applicableTotal < rule.minimumAmount ? "Minimum not met"
            : applicableTotal > rule.maximumAmount ? "Maximum exceeded"
            : null;
        if (validationMessage != null)
            return new { success = false, msg = validationMessage, amt = 0.0, final = cartTotal };
        return DiscountStrategyMap[rule.isStackable].Apply(cart, discountCode, userContext, ItemSelector(rule));
    };

    public static dynamic ProcessDiscountValidation(List<dynamic> cart, string discountCode, dynamic userContext)
    {
        return DiscountProcessor(cart, discountCode, userContext);
    }

    private class ExclusiveDiscountStrategy : DiscountStrategyBase
    {
        public override dynamic Apply(List<dynamic> cart, string discountCode, dynamic userContext, Func<dynamic, bool> itemSelector)
        {
            var rule = DiscountRules[discountCode];
            var applicableItems = cart.Where(itemSelector).ToList();
            var applicableTotal = CalculateCartTotal(applicableItems);
            var rawDiscount = applicableTotal * rule.discountRate;
            var discountAmount = Math.Min(rawDiscount, rule.maximumAmount);
            var cartTotal = CalculateCartTotal(cart);
            var resultBuilder = new DiscountResultBuilder()
                .With("success", true)
                .With("msg", "Applied")
                .With("amt", discountAmount)
                .With("final", cartTotal - discountAmount);
            return resultBuilder.Build();
        }
    }

    private class StackableDiscountStrategy : DiscountStrategyBase
    {
        public override dynamic Apply(List<dynamic> cart, string discountCode, dynamic userContext, Func<dynamic, bool> itemSelector)
        {
            var userHistory = ((IEnumerable<string>)userContext.hist) ?? Array.Empty<string>();
            var previousStackableCodes = userHistory.Where(code => DiscountRules.ContainsKey(code) && DiscountRules[code].isStackable && code != discountCode).ToArray();
            var rule = DiscountRules[discountCode];
            var baseAmount = cart.Where(itemSelector).Sum(CalculateLineAmount);
            var stackedDiscount = previousStackableCodes.Select(previousCode =>
            {
                var previousRule = DiscountRules[previousCode];
                var applicableAmount = cart.Where(ItemSelector(previousRule)).Sum(CalculateLineAmount);
                return applicableAmount * previousRule.discountRate;
            }).Aggregate(0.0, (total, discount) => total + discount);
            var newDiscount = Math.Max(0.0, (baseAmount - stackedDiscount)) * rule.discountRate;
            var remainingCap = Math.Max(0.0, rule.maximumAmount - stackedDiscount);
            var totalDiscountToApplyNow = Math.Min(newDiscount, remainingCap);
            var cartTotal = CalculateCartTotal(cart);
            var resultBuilder = totalDiscountToApplyNow <= 0.0
                ? new DiscountResultBuilder()
                    .With("success", false)
                    .With("msg", "No additional discount")
                    .With("amt", 0.0)
                    .With("final", cartTotal)
                : new DiscountResultBuilder()
                    .With("success", true)
                    .With("msg", "Stacked applied")
                    .With("amt", totalDiscountToApplyNow)
                    .With("final", cartTotal - stackedDiscount - totalDiscountToApplyNow);
            return resultBuilder.Build();
        }
    }

    private class DiscountResultBuilder
    {
        private readonly Dictionary<string, object> resultData = new();
        public DiscountResultBuilder With(string key, object value) { resultData[key] = value; return this; }
        public dynamic Build() => new
        {
            success = resultData["success"],
            msg = resultData["msg"],
            amt = resultData["amt"],
            final = resultData["final"]
        };
    }
}
