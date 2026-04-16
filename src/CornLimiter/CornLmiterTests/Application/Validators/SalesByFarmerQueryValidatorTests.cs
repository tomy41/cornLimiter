using CornLimiter.Application.Queries;
using CornLimiter.Application.Validators;
using FluentValidation.TestHelper;

namespace CornLmiterTests.Application.Validators;

/// <summary>
/// Unit tests for the <see cref="SalesByFarmerQueryValidator"/> class.
/// </summary>
public class SalesByFarmerQueryValidatorTests
{
    private readonly SalesByFarmerQueryValidator _validator;

    public SalesByFarmerQueryValidatorTests()
    {
        _validator = new SalesByFarmerQueryValidator();
    }

    /// <summary>
    /// Tests that validation succeeds when FarmerCode is a valid non-empty Guid.
    /// </summary>
    [Fact]
    public void Validate_ValidFarmerCode_PassesValidation()
    {
        // Arrange
        var query = new SalesByFarmerQuery
        {
            FarmerCode = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Tests that validation fails when FarmerCode is Guid.Empty.
    /// </summary>
    [Fact]
    public void Validate_EmptyGuidFarmerCode_FailsValidation()
    {
        // Arrange
        var query = new SalesByFarmerQuery
        {
            FarmerCode = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FarmerCode)
            .WithErrorMessage("Farmer code cannot be Guid.Empty.");
    }

    /// <summary>
    /// Tests that validation fails when FarmerCode is default (empty).
    /// </summary>
    [Fact]
    public void Validate_DefaultFarmerCode_FailsValidation()
    {
        // Arrange
        var query = new SalesByFarmerQuery
        {
            FarmerCode = default
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FarmerCode);
    }

    /// <summary>
    /// Tests that validation includes the correct error message for empty farmer code.
    /// </summary>
    [Fact]
    public void Validate_EmptyFarmerCode_HasCorrectErrorMessage()
    {
        // Arrange
        var query = new SalesByFarmerQuery
        {
            FarmerCode = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Farmer code cannot be Guid.Empty.");
    }

    /// <summary>
    /// Tests that validation enforces NotEmpty rule.
    /// </summary>
    [Fact]
    public void Validate_EmptyFarmerCode_FailsNotEmptyRule()
    {
        // Arrange
        var query = new SalesByFarmerQuery
        {
            FarmerCode = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FarmerCode);
        Assert.False(result.IsValid);
    }
}