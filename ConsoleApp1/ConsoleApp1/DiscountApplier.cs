using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class DiscountApplier
{
    private sealed record DiscountRule(double discountRate, double minimumAmount, double maximumAmount, string[] categories, bool isExclusive, bool isStackable, int requiredUserLevel);

    private static readonly Dictionary<string, DiscountRule> DiscountRules = new()
    {
        ["SAVE10"] = new DiscountRule(0.1, 50.0, 999999.0, Array.Empty<string>(), false, true, 0),
        ["SAVE20"] = new DiscountRule(0.2, 100.0, 500.0, new[] { "A" }, true, false, 1),
        ["WELCOME"] = new DiscountRule(0.15, 0.0, 250.0, Array.Empty<string>(), false, true, 0),
        ["VIP50"] = new DiscountRule(0.5, 200.0, 999999.0, Array.Empty<string>(), true, false, 3)
    };

    public class CartItem
    {
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class UserContext
    {
        public int Level { get; set; }
        public IEnumerable<string> History { get; set; } = Array.Empty<string>();
    }

    private static double LineAmount(CartItem item) => item.Price * item.Quantity;
    private static double CartTotal(IEnumerable<CartItem> items) => items.Sum(LineAmount);

    private static IEnumerable<CartItem> GetApplicableItems(List<CartItem> cart, DiscountRule rule) =>
        cart.Where(item => rule.categories.Length == 0 || rule.categories.Contains(item.Category));

    private static double CalculateStackedDiscount(List<CartItem> cart, string[] previousCodes)
    {
        double total = 0.0;
        foreach (var code in previousCodes)
        {
            var rule = DiscountRules[code];
            var applicableAmount = GetApplicableItems(cart, rule).Sum(LineAmount);
            total += applicableAmount * rule.discountRate;
        }
        return total;
    }

    public static dynamic ProcessDiscountValidation(List<CartItem> cart, string discountCode, UserContext userContext)
    {
        if (!DiscountRules.ContainsKey(discountCode))
            return new { success = false, msg = "Invalid code", amt = 0.0, final = CartTotal(cart) };

        var rule = DiscountRules[discountCode];
        var userLevel = userContext.Level;
        var userHistory = userContext.History ?? Array.Empty<string>();

        if (userLevel < rule.requiredUserLevel)
            return new { success = false, msg = "Insufficient level", amt = 0.0, final = CartTotal(cart) };

        if (!rule.isStackable && userHistory.Contains(discountCode))
            return new { success = false, msg = "Already used", amt = 0.0, final = CartTotal(cart) };

        var applicableTotal = GetApplicableItems(cart, rule).Sum(LineAmount);
        var cartTotal = CartTotal(cart);

        if (applicableTotal < rule.minimumAmount)
            return new { success = false, msg = "Minimum not met", amt = 0.0, final = cartTotal };

        if (applicableTotal > rule.maximumAmount)
            return new { success = false, msg = "Maximum exceeded", amt = 0.0, final = cartTotal };

        if (rule.isExclusive)
        {
            var rawDiscount = applicableTotal * rule.discountRate;
            var discountAmount = Math.Min(rawDiscount, rule.maximumAmount);
            return new { success = true, msg = "Applied", amt = discountAmount, final = cartTotal - discountAmount };
        }

        // Stackable logic
        var previousStackableCodes = userHistory.Where(code => DiscountRules.ContainsKey(code) && DiscountRules[code].isStackable && code != discountCode).ToArray();
        var baseAmount = GetApplicableItems(cart, rule).Sum(LineAmount);
        var stackedDiscount = CalculateStackedDiscount(cart, previousStackableCodes);
        var newDiscount = Math.Max(0.0, (baseAmount - stackedDiscount)) * rule.discountRate;
        var remainingCap = Math.Max(0.0, rule.maximumAmount - stackedDiscount);
        var totalDiscountToApplyNow = Math.Min(newDiscount, remainingCap);

        if (totalDiscountToApplyNow <= 0.0)
            return new { success = false, msg = "No additional discount", amt = 0.0, final = cartTotal };

        return new { success = true, msg = "Stacked applied", amt = totalDiscountToApplyNow, final = cartTotal - stackedDiscount - totalDiscountToApplyNow };
    }
}
