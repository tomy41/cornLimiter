using CornLimiter.Application.Commands;
using CornLimiter.Application.Validators;
using FluentValidation.TestHelper;

namespace CornLmiterTests.Application.Validators;

/// <summary>
/// Unit tests for the <see cref="SellOneCommandValidator"/> class.
/// </summary>
public class SellOneCommandValidatorTests
{
    private readonly SellOneCommandValidator _validator;

    public SellOneCommandValidatorTests()
    {
        _validator = new SellOneCommandValidator();
    }

    /// <summary>
    /// Tests that validation succeeds when FarmerCode is a valid non-empty Guid.
    /// </summary>
    [Fact]
    public void Validate_ValidFarmerCode_PassesValidation()
    {
        // Arrange
        var command = new SellOneCommand
        {
            FarmerCode = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(command);

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
        var command = new SellOneCommand
        {
            FarmerCode = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(command);

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
        var command = new SellOneCommand
        {
            FarmerCode = default
        };

        // Act
        var result = _validator.TestValidate(command);

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
        var command = new SellOneCommand
        {
            FarmerCode = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(command);

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
        var command = new SellOneCommand
        {
            FarmerCode = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FarmerCode);
        Assert.False(result.IsValid);
    }
}