using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DiscountApplierTests
{
    private List<dynamic> _testCart;
    private dynamic _testUserContext;

    [TestInitialize]
    public void Setup()
    {
        _testCart = new List<dynamic>
        {
            new { n = "Item1", p = 100.0, q = 2, cat = "A" }, // $200 total, category A
            new { n = "Item2", p = 50.0, q = 1, cat = "B" }   // $50 total, category B
        };

        _testUserContext = new { u = "user123", lvl = 2, hist = new[] { "WELCOME" } };
    }

    [TestMethod]
    public void ProcessDiscountValidation_InvalidCode_ReturnsFailure()
    {
        // Arrange
        var invalidCode = "INVALID_CODE";

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(_testCart, invalidCode, _testUserContext);

        // Assert
        Assert.IsFalse(result.success);
        Assert.AreEqual("Invalid code", result.msg);
        Assert.AreEqual(0.0, result.amt);
        Assert.AreEqual(250.0, result.final); // Original cart total
    }

    [TestMethod]
    public void ProcessDiscountValidation_InsufficientUserLevel_ReturnsFailure()
    {
        // Arrange
        var lowLevelUser = new { u = "user123", lvl = 0, hist = new string[0] };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(_testCart, "SAVE20", lowLevelUser);

        // Assert
        Assert.IsFalse(result.success);
        Assert.AreEqual("Insufficient level", result.msg);
        Assert.AreEqual(0.0, result.amt);
        Assert.AreEqual(250.0, result.final);
    }

    [TestMethod]
    public void ProcessDiscountValidation_CodeAlreadyUsed_ReturnsFailure()
    {
        // Arrange
        var userWithHistory = new { u = "user123", lvl = 2, hist = new[] { "SAVE20" } };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(_testCart, "SAVE20", userWithHistory);

        // Assert
        Assert.IsFalse(result.success);
        Assert.AreEqual("Already used", result.msg);
        Assert.AreEqual(0.0, result.amt);
        Assert.AreEqual(250.0, result.final);
    }

    [TestMethod]
    public void ProcessDiscountValidation_MinimumNotMet_ReturnsFailure()
    {
        // Arrange
        var smallCart = new List<dynamic>
        {
            new { n = "SmallItem", p = 10.0, q = 1, cat = "A" } // Only $10 total
        };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(smallCart, "SAVE20", _testUserContext);

        // Assert
        Assert.IsFalse(result.success);
        Assert.AreEqual("Minimum not met", result.msg);
        Assert.AreEqual(0.0, result.amt);
        Assert.AreEqual(10.0, result.final);
    }

    [TestMethod]
    public void ProcessDiscountValidation_MaximumExceeded_ReturnsFailure()
    {
        // Arrange
        var largeCart = new List<dynamic>
        {
            new { n = "ExpensiveItem", p = 600.0, q = 1, cat = "A" } // $600 total, exceeds SAVE20 max of 500
        };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(largeCart, "SAVE20", _testUserContext);

        // Assert
        Assert.IsFalse(result.success);
        Assert.AreEqual("Maximum exceeded", result.msg);
        Assert.AreEqual(0.0, result.amt);
        Assert.AreEqual(600.0, result.final);
    }

    [TestMethod]
    public void ProcessDiscountValidation_ExclusiveDiscount_SAVE20_Success()
    {
        // Arrange - SAVE20 is exclusive, 20% off category A items, min $100, max $500

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(_testCart, "SAVE20", _testUserContext);

        // Assert
        Assert.IsTrue(result.success);
        Assert.AreEqual("Applied", result.msg);
        Assert.AreEqual(40.0, result.amt); // 20% of $200 (category A items only)
        Assert.AreEqual(210.0, result.final); // $250 - $40
    }

    [TestMethod]
    public void ProcessDiscountValidation_ExclusiveDiscount_VIP50_Success()
    {
        // Arrange
        var vipUser = new { u = "vipuser", lvl = 3, hist = new string[0] };
        var largeCart = new List<dynamic>
        {
            new { n = "Item1", p = 100.0, q = 3, cat = "A" }, // $300
            new { n = "Item2", p = 50.0, q = 2, cat = "B" }   // $100
        };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(largeCart, "VIP50", vipUser);

        // Assert
        Assert.IsTrue(result.success);
        Assert.AreEqual("Applied", result.msg);
        Assert.AreEqual(200.0, result.amt); // 50% of $400, but capped at max discount
        Assert.AreEqual(200.0, result.final); // $400 - $200
    }

    [TestMethod]
    public void ProcessDiscountValidation_StackableDiscount_SAVE10_Success()
    {
        // Arrange - SAVE10 is stackable, 10% off all items, min $50
        var userWithoutHistory = new { u = "user123", lvl = 1, hist = new string[0] };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(_testCart, "SAVE10", userWithoutHistory);

        // Assert
        Assert.IsTrue(result.success);
        Assert.AreEqual("Stacked applied", result.msg);
        Assert.AreEqual(25.0, result.amt); // 10% of $250
        Assert.AreEqual(225.0, result.final); // $250 - $25
    }

    [TestMethod]
    public void ProcessDiscountValidation_StackableDiscount_WELCOME_Success()
    {
        // Arrange - WELCOME is stackable, 15% off all items
        var newUser = new { u = "newuser", lvl = 0, hist = new string[0] };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(_testCart, "WELCOME", newUser);

        // Assert
        Assert.IsTrue(result.success);
        Assert.AreEqual("Stacked applied", result.msg);
        Assert.AreEqual(37.5, result.amt); // 15% of $250
        Assert.AreEqual(212.5, result.final); // $250 - $37.5
    }

    [TestMethod]
    public void ProcessDiscountValidation_StackableWithHistory_Success()
    {
        // Arrange - User has WELCOME in history, applying SAVE10
        var userWithWelcome = new { u = "user123", lvl = 1, hist = new[] { "WELCOME" } };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(_testCart, "SAVE10", userWithWelcome);

        // Assert
        Assert.IsTrue(result.success);
        Assert.AreEqual("Stacked applied", result.msg);
        // Should calculate additional discount on top of existing WELCOME discount
        Assert.IsTrue(result.amt > 0);
        Assert.IsTrue(result.final < 250.0);
    }

    [TestMethod]
    public void ProcessDiscountValidation_CategorySpecificDiscount_Success()
    {
        // Arrange - Test SAVE20 which only applies to category A items
        var categoryAOnlyCart = new List<dynamic>
        {
            new { n = "Item1", p = 150.0, q = 1, cat = "A" } // $150, category A
        };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(categoryAOnlyCart, "SAVE20", _testUserContext);

        // Assert
        Assert.IsTrue(result.success);
        Assert.AreEqual("Applied", result.msg);
        Assert.AreEqual(30.0, result.amt); // 20% of $150
        Assert.AreEqual(120.0, result.final); // $150 - $30
    }

    [TestMethod]
    public void ProcessDiscountValidation_CategorySpecificDiscount_NoEligibleItems()
    {
        // Arrange - Cart with only category B items, applying SAVE20 (category A only)
        var categoryBOnlyCart = new List<dynamic>
        {
            new { n = "Item1", p = 150.0, q = 1, cat = "B" } // $150, category B
        };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(categoryBOnlyCart, "SAVE20", _testUserContext);

        // Assert
        Assert.IsFalse(result.success);
        Assert.AreEqual("Minimum not met", result.msg); // No category A items, so $0 eligible amount
    }

    [TestMethod]
    public void ProcessDiscountValidation_EmptyCart_ReturnsFailure()
    {
        // Arrange
        var emptyCart = new List<dynamic>();

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(emptyCart, "SAVE10", _testUserContext);

        // Assert
        Assert.IsFalse(result.success);
        Assert.AreEqual("Minimum not met", result.msg);
        Assert.AreEqual(0.0, result.amt);
        Assert.AreEqual(0.0, result.final);
    }

    [TestMethod]
    public void ProcessDiscountValidation_EdgeCase_ExactMinimum()
    {
        // Arrange - Cart that exactly meets SAVE20 minimum of $100 for category A
        var exactMinCart = new List<dynamic>
        {
            new { n = "Item1", p = 100.0, q = 1, cat = "A" } // Exactly $100
        };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(exactMinCart, "SAVE20", _testUserContext);

        // Assert
        Assert.IsTrue(result.success);
        Assert.AreEqual("Applied", result.msg);
        Assert.AreEqual(20.0, result.amt); // 20% of $100
        Assert.AreEqual(80.0, result.final); // $100 - $20
    }

    [TestMethod]
    public void ProcessDiscountValidation_EdgeCase_ExactMaximum()
    {
        // Arrange - Cart that exactly meets SAVE20 maximum of $500 for category A
        var exactMaxCart = new List<dynamic>
        {
            new { n = "Item1", p = 500.0, q = 1, cat = "A" } // Exactly $500
        };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(exactMaxCart, "SAVE20", _testUserContext);

        // Assert
        Assert.IsTrue(result.success);
        Assert.AreEqual("Applied", result.msg);
        Assert.AreEqual(100.0, result.amt); // 20% of $500
        Assert.AreEqual(400.0, result.final); // $500 - $100
    }

    [TestMethod]
    public void ProcessDiscountValidation_MixedCategories_CategorySpecificDiscount()
    {
        // Arrange - Mixed cart with both categories, applying category A discount
        var mixedCart = new List<dynamic>
        {
            new { n = "ItemA", p = 100.0, q = 1, cat = "A" }, // $100, eligible
            new { n = "ItemB", p = 200.0, q = 1, cat = "B" }  // $200, not eligible
        };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(mixedCart, "SAVE20", _testUserContext);

        // Assert
        Assert.IsTrue(result.success);
        Assert.AreEqual("Applied", result.msg);
        Assert.AreEqual(20.0, result.amt); // 20% of $100 (category A only)
        Assert.AreEqual(280.0, result.final); // $300 - $20
    }

    [TestMethod]
    public void ProcessDiscountValidation_StackableDiscount_NoAdditionalDiscount()
    {
        // This test would require a scenario where stacking results in no additional discount
        // This might happen if the user has already used all possible stackable discounts
        // and the new discount doesn't add any value
        
        // Arrange - Create a scenario where stackable discount results in 0 additional discount
        var userWithMaxStackable = new { u = "user123", lvl = 1, hist = new[] { "SAVE20" } };
        var smallCart = new List<dynamic>
        {
            new { n = "SmallItem", p = 60.0, q = 1, cat = "A" } // $60 total
        };

        // Act
        var result = DiscountApplier.ProcessDiscountValidation(smallCart, "SAVE20", userWithMaxStackable);

        // Assert - This should fail because SAVE10 is not stackable with itself
        Assert.IsFalse(result.success);
        Assert.AreEqual("Already used", result.msg);
    }
}
