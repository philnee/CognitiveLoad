using System;
using System.Collections.Generic;
using System.Linq;

// See https://aka.ms/new-console-template for more information

public class DiscountApplier
{
    private sealed record DiscountRule(double d, double min, double max, string[] cat, bool excl, bool stack, int usr);

    // Single, strongly-typed rules source with consistent numeric types
    private static readonly Dictionary<string, DiscountRule> Rules = new()
    {
        ["SAVE10"] = new DiscountRule(d: 0.1,  min: 50.0,  max: 999999.0, cat: Array.Empty<string>(), excl: false, stack: true,  usr: 0),
        ["SAVE20"] = new DiscountRule(d: 0.2,  min: 100.0, max: 500.0,    cat: new[] { "A" },        excl: true,  stack: false, usr: 1),
        ["WELCOME"]= new DiscountRule(d: 0.15, min: 0.0,   max: 250.0,    cat: Array.Empty<string>(), excl: false, stack: true,  usr: 0),
        ["VIP50"]  = new DiscountRule(d: 0.5,  min: 200.0, max: 999999.0, cat: Array.Empty<string>(), excl: true,  stack: false, usr: 3)
    };

    public static dynamic ProcessDiscountValidation(List<dynamic> cart, string code, dynamic userContext)
    {
        // Helpers
        double LineAmount(dynamic x) => Convert.ToDouble(x.p) * Convert.ToDouble(x.q);
        double CartTotal(IEnumerable<dynamic> items) => items.Sum(LineAmount);
        IEnumerable<dynamic> ItemsForRule(DiscountRule r) =>
            cart.Where(x => r.cat.Length == 0 || r.cat.Contains((string)x.cat));

        if (!Rules.ContainsKey(code))
        {
            return new { success = false, msg = "Invalid code", amt = 0.0, final = CartTotal(cart) };
        }

        var rule = Rules[code];

        if ((int)userContext.lvl < rule.usr)
        {
            return new { success = false, msg = "Insufficient level", amt = 0.0, final = CartTotal(cart) };
        }

        var userHist = ((IEnumerable<string>)userContext.hist) ?? Array.Empty<string>();
        if (!rule.stack && userHist.Contains(code))
        {
            return new { success = false, msg = "Already used", amt = 0.0, final = CartTotal(cart) };
        }

        var applicableTotal = ItemsForRule(rule).Sum(LineAmount);
        var cartTotal = CartTotal(cart);

        if (applicableTotal < rule.min)
        {
            return new { success = false, msg = "Minimum not met", amt = 0.0, final = cartTotal };
        }

        if (applicableTotal > rule.max)
        {
            return new { success = false, msg = "Maximum exceeded", amt = 0.0, final = cartTotal };
        }

        if (rule.excl)
        {
            // Exclusive: simple cap using double overload of Math.Min
            var rawDiscount = applicableTotal * rule.d;
            var amt = Math.Min(rawDiscount, rule.max);
            return new
            {
                success = true,
                msg = "Applied",
                amt,
                final = cartTotal - amt
            };
        }

        // Stackable: compute against previous stackable codes used by the user
        var prevStackable = userHist.Where(h => Rules.ContainsKey(h) && Rules[h].stack).ToArray();
        return ProcessStackableDiscount(cart, code, rule, prevStackable);
    }

    private static dynamic ProcessStackableDiscount(List<dynamic> c, string cd, DiscountRule rule, string[] prevCodes)
    {
        double LineAmount(dynamic x) => Convert.ToDouble(x.p) * Convert.ToDouble(x.q);
        double CartTotal(IEnumerable<dynamic> items) => items.Sum(LineAmount);
        bool ItemMatches(DiscountRule r, dynamic x) => r.cat.Length == 0 || r.cat.Contains((string)x.cat);

        var baseAmt = c.Where(x => ItemMatches(rule, x)).Sum(LineAmount);

        // Sum discounts from previously applied stackable codes (excluding the current code)
        var stackedDisc = prevCodes
            .Where(pc => pc != cd && Rules.ContainsKey(pc))
            .Sum(pc =>
            {
                var r = Rules[pc];
                var applicable = c.Where(x => ItemMatches(r, x)).Sum(LineAmount);
                // For stacking computation, apply percentage; cap will be handled by the "new discount" logic below via max
                return applicable * r.d;
            });

        // New discount on the remaining base amount, then cap to rule.max considering what was already stacked
        var newDisc = Math.Max(0.0, (baseAmt - stackedDisc)) * rule.d;

        // Remaining headroom under this rule's max is (rule.max - stackedDisc), cannot be negative
        var remainingCap = Math.Max(0.0, rule.max - stackedDisc);
        var totalDiscToApplyNow = Math.Min(newDisc, remainingCap);

        var cartTotal = CartTotal(c);

        return totalDiscToApplyNow <= 0.0
            ? new { success = false, msg = "No additional discount", amt = 0.0, final = cartTotal }
            : new
            {
                success = true,
                msg = "Stacked applied",
                amt = totalDiscToApplyNow,
                final = cartTotal - stackedDisc - totalDiscToApplyNow
            };
    }
}
